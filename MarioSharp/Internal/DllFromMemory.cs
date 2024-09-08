﻿using System.Runtime.InteropServices;

namespace MarioSharp.Internal;

internal class DllFromMemory : IDisposable
{
    public class DllException : Exception
    {
        public DllException(string message) : base(message)
        {
        }
    }

    public List<SectionInfo> Sections = new();
    public bool Disposed { get; private set; }
    public bool IsDll { get; private set; }

    private IntPtr _pCode = IntPtr.Zero;
    private IntPtr _pNtHeaders = IntPtr.Zero;
    private IntPtr[] _importModules;
    private bool _initialized;
    private DllEntryDelegate _dllEntry;

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate bool DllEntryDelegate(IntPtr hinstDll, DllReason fdwReason, IntPtr lpReserved);

    [UnmanagedFunctionPointer(CallingConvention.Winapi)]
    private delegate void ImageTlsDelegate(IntPtr dllHandle, DllReason reason, IntPtr reserved);

    public DllFromMemory(byte[] data)
    {
        Disposed = false;
        if (data == null) throw new ArgumentNullException(nameof(data));
        MemoryLoadLibrary(data);
    }

    ~DllFromMemory()
    {
        Dispose();
    }

    public T Read<T>(IntPtr address)
    {
        return Marshal.PtrToStructure<T>(address);
    }

    public byte[] ReadRaw(IntPtr address, int count)
    {
        byte[] result = new byte[count];
        Marshal.Copy(address, result, 0, count);
        return result;
    }

    public void WriteRaw(IntPtr address, byte[] value)
    {
        Marshal.Copy(value, 0, address, value.Length);
    }

    public void Write<T>(IntPtr address, T value)
    {
        Marshal.StructureToPtr(value, address, true);
    }

    public TDelegate GetDelegateFromFuncName<TDelegate>(string funcName) where TDelegate : class
    {
        if (!typeof(Delegate).IsAssignableFrom(typeof(TDelegate))) throw new ArgumentException(typeof(TDelegate).Name + " is not a delegate");
        if (!(Marshal.GetDelegateForFunctionPointer(GetPtrFromFuncName(funcName), typeof(TDelegate)) is TDelegate res)) throw new DllException("Unable to get managed delegate");
        return res;
    }

    public Delegate GetDelegateFromFuncName(string funcName, Type delegateType)
    {
        if (delegateType == null) throw new ArgumentNullException(nameof(delegateType));
        if (!typeof(Delegate).IsAssignableFrom(delegateType)) throw new ArgumentException(delegateType.Name + " is not a delegate");
        Delegate res = Marshal.GetDelegateForFunctionPointer(GetPtrFromFuncName(funcName), delegateType);
        if (res == null) throw new DllException("Unable to get managed delegate");
        return res;
    }

    private readonly Dictionary<string, IntPtr> _cache = new();

    public IntPtr GetPtrFromFuncName(string funcName)
    {
        // TODO this is slow as fuck. Optimize this
        if (_cache.TryGetValue(funcName, out IntPtr r))
        {
            return r;
        }

        if (Disposed) throw new ObjectDisposedException("DLLFromMemory");
        if (string.IsNullOrEmpty(funcName)) throw new ArgumentException("funcName");
        if (!IsDll) throw new InvalidOperationException("Loaded Module is not a DLL");
        if (!_initialized) throw new InvalidOperationException("Dll is not initialized");

        IntPtr pDirectory = PtrAdd(_pNtHeaders, Of.ImageNtHeadersOptionalHeader + (Is64BitProcess ? Of64.ImageOptionalHeaderExportTable : Of32.ImageOptionalHeaderExportTable));
        ImageDataDirectory directory = PtrRead<ImageDataDirectory>(pDirectory);
        if (directory.Size == 0) throw new DllException("Dll has no export table");

        IntPtr pExports = PtrAdd(_pCode, directory.VirtualAddress);
        ImageExportDirectory exports = PtrRead<ImageExportDirectory>(pExports);
        if (exports.NumberOfFunctions == 0 || exports.NumberOfNames == 0) throw new DllException("Dll exports no functions");

        IntPtr pNameRef = PtrAdd(_pCode, exports.AddressOfNames);
        IntPtr pOrdinal = PtrAdd(_pCode, exports.AddressOfNameOrdinals);
        for (int i = 0; i < exports.NumberOfNames; i++, pNameRef = PtrAdd(pNameRef, sizeof(uint)), pOrdinal = PtrAdd(pOrdinal, sizeof(ushort)))
        {
            uint nameRef = PtrRead<uint>(pNameRef);
            ushort ordinal = PtrRead<ushort>(pOrdinal);
            string curFuncName = Marshal.PtrToStringAnsi(PtrAdd(_pCode, nameRef));
            if (curFuncName == funcName)
            {
                if (ordinal > exports.NumberOfFunctions) throw new DllException("Invalid function ordinal");
                IntPtr pAddressOfFunction = PtrAdd(_pCode, exports.AddressOfFunctions + (uint)(ordinal * 4));
                IntPtr result = PtrAdd(_pCode, PtrRead<uint>(pAddressOfFunction));
                _cache.Add(funcName, result);
                return result;
            }
        }

        throw new DllException("Dll exports no function named " + funcName);
    }

    private void MemoryLoadLibrary(byte[] data)
    {
        if (data.Length < Marshal.SizeOf(typeof(ImageDosHeader))) throw new DllException("Not a valid executable file");
        ImageDosHeader dosHeader = BytesReadStructAt<ImageDosHeader>(data, 0);
        if (dosHeader.e_magic != Win.ImageDosSignature) throw new BadImageFormatException("Not a valid executable file");

        if (data.Length < dosHeader.e_lfanew + Marshal.SizeOf(typeof(ImageNtHeaders))) throw new DllException("Not a valid executable file");
        ImageNtHeaders orgNtHeaders = BytesReadStructAt<ImageNtHeaders>(data, dosHeader.e_lfanew);

        if (orgNtHeaders.Signature != Win.ImageNtSignature) throw new BadImageFormatException("Not a valid PE file");
        if (orgNtHeaders.FileHeader.Machine != GetMachineType()) throw new BadImageFormatException("Machine type doesn't fit (i386 vs. AMD64)");
        if ((orgNtHeaders.OptionalHeader.SectionAlignment & 1) > 0) throw new BadImageFormatException("Wrong section alignment"); //Only support multiple of 2
        if (orgNtHeaders.OptionalHeader.AddressOfEntryPoint == 0) throw new DllException("Module has no entry point");

        Win.GetNativeSystemInfo(out SystemInfo systemInfo);
        uint lastSectionEnd = 0;
        int ofSection = Win.IMAGE_FIRST_SECTION(dosHeader.e_lfanew, orgNtHeaders.FileHeader.SizeOfOptionalHeader);
        for (int i = 0; i != orgNtHeaders.FileHeader.NumberOfSections; i++, ofSection += Sz.ImageSectionHeader)
        {
            ImageSectionHeader section = BytesReadStructAt<ImageSectionHeader>(data, ofSection);
            uint endOfSection = section.VirtualAddress + (section.SizeOfRawData > 0 ? section.SizeOfRawData : orgNtHeaders.OptionalHeader.SectionAlignment);
            if (endOfSection > lastSectionEnd) lastSectionEnd = endOfSection;
        }

        uint alignedImageSize = AlignValueUp(orgNtHeaders.OptionalHeader.SizeOfImage, systemInfo.dwPageSize);
        uint alignedLastSection = AlignValueUp(lastSectionEnd, systemInfo.dwPageSize);
        if (alignedImageSize != alignedLastSection) throw new BadImageFormatException("Wrong section alignment");

        IntPtr oldHeaderOptionalHeaderImageBase;
        if (Is64BitProcess) oldHeaderOptionalHeaderImageBase = (IntPtr)unchecked((long)orgNtHeaders.OptionalHeader.ImageBaseLong);
        else oldHeaderOptionalHeaderImageBase = (IntPtr)unchecked((int)(orgNtHeaders.OptionalHeader.ImageBaseLong >> 32));

        // reserve memory for image of library
        _pCode = Win.VirtualAlloc(oldHeaderOptionalHeaderImageBase, (UIntPtr)orgNtHeaders.OptionalHeader.SizeOfImage, AllocationType.Reserve | AllocationType.Commit, MemoryProtection.Readwrite);

        // try to allocate memory at arbitrary position
        if (_pCode == IntPtr.Zero) _pCode = Win.VirtualAlloc(IntPtr.Zero, (UIntPtr)orgNtHeaders.OptionalHeader.SizeOfImage, AllocationType.Reserve | AllocationType.Commit, MemoryProtection.Readwrite);

        if (_pCode == IntPtr.Zero) throw new DllException("Out of Memory");

        if (Is64BitProcess && PtrSpanBoundary(_pCode, alignedImageSize, 32))
        {
            // Memory block may not span 4 GB (32 bit) boundaries.
            List<IntPtr> blockedMemory = new List<IntPtr>();
            while (PtrSpanBoundary(_pCode, alignedImageSize, 32))
            {
                blockedMemory.Add(_pCode);
                _pCode = Win.VirtualAlloc(IntPtr.Zero, (UIntPtr)alignedImageSize, AllocationType.Reserve | AllocationType.Commit, MemoryProtection.Readwrite);
                if (_pCode == IntPtr.Zero) break;
            }

            foreach (IntPtr ptr in blockedMemory) Win.VirtualFree(ptr, IntPtr.Zero, AllocationType.Release);
            if (_pCode == IntPtr.Zero) throw new DllException("Out of Memory");
        }

        // commit memory for headers
        IntPtr headers = Win.VirtualAlloc(_pCode, (UIntPtr)orgNtHeaders.OptionalHeader.SizeOfHeaders, AllocationType.Commit, MemoryProtection.Readwrite);
        if (headers == IntPtr.Zero) throw new DllException("Out of Memory");

        // copy PE header to code
        Marshal.Copy(data, 0, headers, (int)orgNtHeaders.OptionalHeader.SizeOfHeaders);
        _pNtHeaders = PtrAdd(headers, dosHeader.e_lfanew);

        IntPtr locationDelta = PtrSub(_pCode, oldHeaderOptionalHeaderImageBase);
        if (locationDelta != IntPtr.Zero)
        {
            // update relocated position
            Marshal.OffsetOf(typeof(ImageNtHeaders), "OptionalHeader");
            Marshal.OffsetOf(typeof(ImageOptionalHeader), "ImageBaseLong");
            IntPtr pImageBase = PtrAdd(_pNtHeaders, Of.ImageNtHeadersOptionalHeader + (Is64BitProcess ? Of64.ImageOptionalHeaderImageBase : Of32.ImageOptionalHeaderImageBase));
            PtrWrite(pImageBase, _pCode);
        }

        // copy sections from DLL file block to new memory location
        CopySections(ref orgNtHeaders, _pCode, _pNtHeaders, data);

        // adjust base address of imported data
        if (locationDelta != IntPtr.Zero)
        {
            PerformBaseRelocation(ref orgNtHeaders, _pCode, locationDelta);
        }

        // load required dlls and adjust function table of imports
        _importModules = BuildImportTable(ref orgNtHeaders, _pCode);

        // mark memory pages depending on section headers and release
        // sections that are marked as "discardable"
        FinalizeSections(ref orgNtHeaders, _pCode, _pNtHeaders, systemInfo.dwPageSize);

        // TLS callbacks are executed BEFORE the main loading
        ExecuteTls(ref orgNtHeaders, _pCode, _pNtHeaders);

        // get entry point of loaded library
        IsDll = (orgNtHeaders.FileHeader.Characteristics & Win.ImageFileDll) != 0;
        if (orgNtHeaders.OptionalHeader.AddressOfEntryPoint != 0)
        {
            if (IsDll)
            {
                // notify library about attaching to process
                IntPtr dllEntryPtr = PtrAdd(_pCode, orgNtHeaders.OptionalHeader.AddressOfEntryPoint);
                _dllEntry = (DllEntryDelegate)Marshal.GetDelegateForFunctionPointer(dllEntryPtr, typeof(DllEntryDelegate));

                _initialized = _dllEntry != null && _dllEntry(_pCode, DllReason.DllProcessAttach, IntPtr.Zero);
                if (!_initialized) throw new DllException("Can't attach DLL to process");
            }
        }
    }

    private void CopySections(ref ImageNtHeaders orgNtHeaders, IntPtr pCode, IntPtr pNtHeaders, byte[] data)
    {
        Sections.Clear();
        IntPtr pSection = Win.IMAGE_FIRST_SECTION(pNtHeaders, orgNtHeaders.FileHeader.SizeOfOptionalHeader);
        for (int i = 0; i < orgNtHeaders.FileHeader.NumberOfSections; i++, pSection = PtrAdd(pSection, Sz.ImageSectionHeader))
        {
            ImageSectionHeader section = PtrRead<ImageSectionHeader>(pSection);

            unsafe
            {
                string sectionName = Marshal.PtrToStringAnsi((IntPtr)(&section.Name));
                Sections.Add(new SectionInfo(sectionName, pCode + (int)section.VirtualAddress, (int)section.PhysicalAddress));
            }

            if (section.SizeOfRawData == 0)
            {
                // section doesn't contain data in the dll itself, but may define uninitialized data
                uint size = orgNtHeaders.OptionalHeader.SectionAlignment;
                if (size > 0)
                {
                    IntPtr dest = Win.VirtualAlloc(PtrAdd(pCode, section.VirtualAddress), (UIntPtr)size, AllocationType.Commit, MemoryProtection.Readwrite);
                    if (dest == IntPtr.Zero) throw new DllException("Unable to allocate memory");

                    // Always use position from file to support alignments smaller than page size (allocation above will align to page size).
                    dest = PtrAdd(pCode, section.VirtualAddress);

                    // NOTE: On 64bit systems we truncate to 32bit here but expand again later when "PhysicalAddress" is used.
                    PtrWrite(PtrAdd(pSection, Of.ImageSectionHeaderPhysicalAddress), unchecked((uint)(ulong)(long)dest));

                    Win.MemSet(dest, 0, (UIntPtr)size);
                }

                // section is empty
                continue;
            }

            {
                // commit memory block and copy data from dll
                IntPtr dest = Win.VirtualAlloc(PtrAdd(pCode, section.VirtualAddress), (UIntPtr)section.SizeOfRawData, AllocationType.Commit, MemoryProtection.Readwrite);
                if (dest == IntPtr.Zero) throw new DllException("Out of memory");

                // Always use position from file to support alignments smaller than page size (allocation above will align to page size).
                dest = PtrAdd(pCode, section.VirtualAddress);
                Marshal.Copy(data, checked((int)section.PointerToRawData), dest, checked((int)section.SizeOfRawData));

                // NOTE: On 64bit systems we truncate to 32bit here but expand again later when "PhysicalAddress" is used.
                PtrWrite(PtrAdd(pSection, Of.ImageSectionHeaderPhysicalAddress), unchecked((uint)(ulong)(long)dest));
            }
        }
    }

    private static bool PerformBaseRelocation(ref ImageNtHeaders orgNtHeaders, IntPtr pCode, IntPtr delta)
    {
        if (orgNtHeaders.OptionalHeader.BaseRelocationTable.Size == 0) return delta == IntPtr.Zero;

        for (IntPtr pRelocation = PtrAdd(pCode, orgNtHeaders.OptionalHeader.BaseRelocationTable.VirtualAddress); ;)
        {
            ImageBaseRelocation relocation = PtrRead<ImageBaseRelocation>(pRelocation);
            if (relocation.VirtualAdress == 0) break;

            IntPtr pDest = PtrAdd(pCode, relocation.VirtualAdress);
            IntPtr pRelInfo = PtrAdd(pRelocation, Sz.ImageBaseRelocation);
            uint relCount = (relocation.SizeOfBlock - Sz.ImageBaseRelocation) / 2;
            for (uint i = 0; i != relCount; i++, pRelInfo = PtrAdd(pRelInfo, sizeof(ushort)))
            {
                ushort relInfo = (ushort)Marshal.PtrToStructure(pRelInfo, typeof(ushort));
                BasedRelocationType type = (BasedRelocationType)(relInfo >> 12); // the upper 4 bits define the type of relocation
                int offset = relInfo & 0xfff; // the lower 12 bits define the offset
                IntPtr pPatchAddr = PtrAdd(pDest, offset);

                switch (type)
                {
                    case BasedRelocationType.ImageRelBasedAbsolute:
                        // skip relocation
                        break;
                    case BasedRelocationType.ImageRelBasedHighlow:
                        // change complete 32 bit address
                        int patchAddrHl = (int)Marshal.PtrToStructure(pPatchAddr, typeof(int));
                        patchAddrHl += (int)delta;
                        Marshal.StructureToPtr(patchAddrHl, pPatchAddr, false);
                        break;
                    case BasedRelocationType.ImageRelBasedDir64:
                        long patchAddr64 = (long)Marshal.PtrToStructure(pPatchAddr, typeof(long));
                        patchAddr64 += (long)delta;
                        Marshal.StructureToPtr(patchAddr64, pPatchAddr, false);
                        break;
                }
            }

            // advance to next relocation block
            pRelocation = PtrAdd(pRelocation, relocation.SizeOfBlock);
        }

        return true;
    }

    private static IntPtr[] BuildImportTable(ref ImageNtHeaders orgNtHeaders, IntPtr pCode)
    {
        List<IntPtr> importModules = new List<IntPtr>();
        uint numEntries = orgNtHeaders.OptionalHeader.ImportTable.Size / Sz.ImageImportDescriptor;
        IntPtr pImportDesc = PtrAdd(pCode, orgNtHeaders.OptionalHeader.ImportTable.VirtualAddress);
        for (uint i = 0; i != numEntries; i++, pImportDesc = PtrAdd(pImportDesc, Sz.ImageImportDescriptor))
        {
            ImageImportDescriptor importDesc = PtrRead<ImageImportDescriptor>(pImportDesc);
            if (importDesc.Name == 0) break;

            IntPtr handle = Win.LoadLibrary(PtrAdd(pCode, importDesc.Name));
            if (PtrIsInvalidHandle(handle))
            {
                foreach (IntPtr m in importModules) Win.FreeLibrary(m);
                importModules.Clear();
                throw new DllException("Can't load libary " + Marshal.PtrToStringAnsi(PtrAdd(pCode, importDesc.Name)));
            }

            importModules.Add(handle);

            IntPtr pThunkRef, pFuncRef;
            if (importDesc.OriginalFirstThunk > 0)
            {
                pThunkRef = PtrAdd(pCode, importDesc.OriginalFirstThunk);
                pFuncRef = PtrAdd(pCode, importDesc.FirstThunk);
            }
            else
            {
                // no hint table
                pThunkRef = PtrAdd(pCode, importDesc.FirstThunk);
                pFuncRef = PtrAdd(pCode, importDesc.FirstThunk);
            }

            for (int szRef = IntPtr.Size; ; pThunkRef = PtrAdd(pThunkRef, szRef), pFuncRef = PtrAdd(pFuncRef, szRef))
            {
                IntPtr readThunkRef = PtrRead<IntPtr>(pThunkRef), writeFuncRef;
                if (readThunkRef == IntPtr.Zero) break;
                if (Win.IMAGE_SNAP_BY_ORDINAL(readThunkRef))
                {
                    writeFuncRef = Win.GetProcAddress(handle, Win.IMAGE_ORDINAL(readThunkRef));
                }
                else
                {
                    writeFuncRef = Win.GetProcAddress(handle, PtrAdd(PtrAdd(pCode, readThunkRef), Of.ImageImportByNameName));
                }

                if (writeFuncRef == IntPtr.Zero) throw new DllException("Can't get adress for imported function");
                PtrWrite(pFuncRef, writeFuncRef);
            }
        }

        return importModules.Count > 0 ? importModules.ToArray() : null;
    }

    private static void FinalizeSections(ref ImageNtHeaders orgNtHeaders, IntPtr pCode, IntPtr pNtHeaders, uint pageSize)
    {
        UIntPtr imageOffset = Is64BitProcess ? (UIntPtr)(unchecked((ulong)pCode.ToInt64()) & 0xffffffff00000000) : UIntPtr.Zero;
        IntPtr pSection = Win.IMAGE_FIRST_SECTION(pNtHeaders, orgNtHeaders.FileHeader.SizeOfOptionalHeader);
        ImageSectionHeader section = PtrRead<ImageSectionHeader>(pSection);
        SectionFinalizeData sectionData = new SectionFinalizeData();
        sectionData.Address = PtrBitOr(PtrAdd((IntPtr)0, section.PhysicalAddress), imageOffset);
        sectionData.AlignedAddress = PtrAlignDown(sectionData.Address, (UIntPtr)pageSize);
        sectionData.Size = GetRealSectionSize(ref section, ref orgNtHeaders);
        sectionData.Characteristics = section.Characteristics;
        sectionData.Last = false;
        pSection = PtrAdd(pSection, Sz.ImageSectionHeader);

        // loop through all sections and change access flags
        for (int i = 1; i < orgNtHeaders.FileHeader.NumberOfSections; i++, pSection = PtrAdd(pSection, Sz.ImageSectionHeader))
        {
            section = PtrRead<ImageSectionHeader>(pSection);
            IntPtr sectionAddress = PtrBitOr(PtrAdd((IntPtr)0, section.PhysicalAddress), imageOffset);
            IntPtr alignedAddress = PtrAlignDown(sectionAddress, (UIntPtr)pageSize);
            IntPtr sectionSize = GetRealSectionSize(ref section, ref orgNtHeaders);

            // Combine access flags of all sections that share a page
            // TODO(fancycode): We currently share flags of a trailing large section with the page of a first small section. This should be optimized.
            PtrAdd(sectionData.Address, sectionData.Size);

            if (sectionData.AlignedAddress == alignedAddress || unchecked((ulong)PtrAdd(sectionData.Address, sectionData.Size).ToInt64()) > (ulong)alignedAddress)
            {
                // Section shares page with previous
                if ((section.Characteristics & Win.ImageScnMemDiscardable) == 0 || (sectionData.Characteristics & Win.ImageScnMemDiscardable) == 0)
                {
                    sectionData.Characteristics = (sectionData.Characteristics | section.Characteristics) & ~Win.ImageScnMemDiscardable;
                }
                else
                {
                    sectionData.Characteristics |= section.Characteristics;
                }

                sectionData.Size = PtrSub(PtrAdd(sectionAddress, sectionSize), sectionData.Address);
                continue;
            }

            FinalizeSection(sectionData, pageSize, orgNtHeaders.OptionalHeader.SectionAlignment);

            sectionData.Address = sectionAddress;
            sectionData.AlignedAddress = alignedAddress;
            sectionData.Size = sectionSize;
            sectionData.Characteristics = section.Characteristics;
        }

        sectionData.Last = true;
        FinalizeSection(sectionData, pageSize, orgNtHeaders.OptionalHeader.SectionAlignment);
    }

    private static void FinalizeSection(SectionFinalizeData sectionData, uint pageSize, uint sectionAlignment)
    {
        if (sectionData.Size == IntPtr.Zero)
            return;

        if ((sectionData.Characteristics & Win.ImageScnMemDiscardable) > 0)
        {
            // section is not needed any more and can safely be freed
            if (sectionData.Address == sectionData.AlignedAddress &&
                (sectionData.Last ||
                 sectionAlignment == pageSize ||
                 unchecked((ulong)sectionData.Size.ToInt64()) % pageSize == 0)
               )
            {
                // Only allowed to decommit whole pages
                Win.VirtualFree(sectionData.Address, sectionData.Size, AllocationType.Decommit);
            }

            return;
        }

        // determine protection flags based on characteristics
        int readable = (sectionData.Characteristics & (uint)ImageSectionFlags.ImageScnMemRead) != 0 ? 1 : 0;
        int writeable = (sectionData.Characteristics & (uint)ImageSectionFlags.ImageScnMemWrite) != 0 ? 1 : 0;
        int executable = (sectionData.Characteristics & (uint)ImageSectionFlags.ImageScnMemExecute) != 0 ? 1 : 0;
        uint protect = (uint)ProtectionFlags[executable, readable, writeable];
        if ((sectionData.Characteristics & Win.ImageScnMemNotCached) > 0) protect |= Win.PageNocache;

        // change memory access flags
        if (!Win.VirtualProtect(sectionData.Address, sectionData.Size, protect, out _))
            throw new DllException("Error protecting memory page");
    }

    private static void ExecuteTls(ref ImageNtHeaders orgNtHeaders, IntPtr pCode, IntPtr pNtHeaders)
    {
        if (orgNtHeaders.OptionalHeader.TLSTable.VirtualAddress == 0) return;
        ImageTlsDirectory tlsDir = PtrRead<ImageTlsDirectory>(PtrAdd(pCode, orgNtHeaders.OptionalHeader.TLSTable.VirtualAddress));
        IntPtr pCallBack = tlsDir.AddressOfCallBacks;
        if (pCallBack != IntPtr.Zero)
        {
            for (IntPtr callback; (callback = PtrRead<IntPtr>(pCallBack)) != IntPtr.Zero; pCallBack = PtrAdd(pCallBack, IntPtr.Size))
            {
                ImageTlsDelegate tls = (ImageTlsDelegate)Marshal.GetDelegateForFunctionPointer(callback, typeof(ImageTlsDelegate));
                tls(pCode, DllReason.DllProcessAttach, IntPtr.Zero);
            }
        }
    }

    public static bool Is64BitProcess => IntPtr.Size == 8;

    private static uint GetMachineType()
    {
        return IntPtr.Size == 8 ? Win.ImageFileMachineAmd64 : Win.ImageFileMachineI386;
    }

    private static uint AlignValueUp(uint value, uint alignment)
    {
        return value + alignment - 1 & ~(alignment - 1);
    }

    private static IntPtr GetRealSectionSize(ref ImageSectionHeader section, ref ImageNtHeaders ntHeaders)
    {
        uint size = section.SizeOfRawData;
        if (size == 0)
        {
            if ((section.Characteristics & Win.ImageScnCntInitializedData) > 0)
            {
                size = ntHeaders.OptionalHeader.SizeOfInitializedData;
            }
            else if ((section.Characteristics & Win.ImageScnCntUninitializedData) > 0)
            {
                size = ntHeaders.OptionalHeader.SizeOfUninitializedData;
            }
        }

        return IntPtr.Size == 8 ? (IntPtr)size : (IntPtr)unchecked((int)size);
    }

    public void Close()
    {
        ((IDisposable)this).Dispose();
    }

    void IDisposable.Dispose()
    {
        Dispose();
        GC.SuppressFinalize(this);
    }

    // TODO: Fix Access Violation a few minutes after disposing (leaking memory right now)
    public void Dispose()
    {
        /*if (_initialized)
        {
            if (_dllEntry != null) _dllEntry.Invoke(_pCode, DllReason.DllProcessDetach, IntPtr.Zero);
            _initialized = false;
        }

        if (_importModules != null)
        {
            foreach (IntPtr m in _importModules)
                if (!PtrIsInvalidHandle(m))
                    Win.FreeLibrary(m);
            _importModules = null;
        }

        if (_pCode != IntPtr.Zero)
        {
            Win.VirtualFree(_pCode, IntPtr.Zero, AllocationType.Release);
            _pCode = IntPtr.Zero;
            _pNtHeaders = IntPtr.Zero;
        }

        Disposed = true;*/

        _initialized = false;
        Disposed = true;
    }

    // Protection flags for memory pages (Executable, Readable, Writeable)
    private static readonly PageProtection[,,] ProtectionFlags = new PageProtection[2, 2, 2]
    {
        {
            // not executable
            { PageProtection.Noaccess, PageProtection.Writecopy },
            { PageProtection.Readonly, PageProtection.Readwrite }
        },
        {
            // executable
            { PageProtection.Execute, PageProtection.ExecuteWritecopy },
            { PageProtection.ExecuteRead, PageProtection.ExecuteReadwrite }
        }
    };

    private struct SectionFinalizeData
    {
        internal IntPtr Address;
        internal IntPtr AlignedAddress;
        internal IntPtr Size;
        internal uint Characteristics;
        internal bool Last;
    }

    private class Of
    {
        internal const int ImageNtHeadersOptionalHeader = 24;
        internal const int ImageSectionHeaderPhysicalAddress = 8;
        internal const int ImageImportByNameName = 2;
    }

    private class Of32
    {
        internal const int ImageOptionalHeaderImageBase = 28;
        internal const int ImageOptionalHeaderExportTable = 96;
    }

    private class Of64
    {
        internal const int ImageOptionalHeaderImageBase = 24;
        internal const int ImageOptionalHeaderExportTable = 112;
    }

    private class Sz
    {
        internal const int ImageSectionHeader = 40;
        internal const int ImageBaseRelocation = 8;
        internal const int ImageImportDescriptor = 20;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ImageDosHeader
    {
        public ushort e_magic; // Magic number
        public ushort e_cblp; // Bytes on last page of file
        public ushort e_cp; // Pages in file
        public ushort e_crlc; // Relocations
        public ushort e_cparhdr; // Size of header in paragraphs
        public ushort e_minalloc; // Minimum extra paragraphs needed
        public ushort e_maxalloc; // Maximum extra paragraphs needed
        public ushort e_ss; // Initial (relative) SS value
        public ushort e_sp; // Initial SP value
        public ushort e_csum; // Checksum
        public ushort e_ip; // Initial IP value
        public ushort e_cs; // Initial (relative) CS value
        public ushort e_lfarlc; // File address of relocation table
        public ushort e_ovno; // Overlay number
        public ushort e_res1a, e_res1b, e_res1c, e_res1d; // Reserved words
        public ushort e_oemid; // OEM identifier (for e_oeminfo)
        public ushort e_oeminfo; // OEM information; e_oemid specific
        public ushort e_res2a, e_res2b, e_res2c, e_res2d, e_res2e, e_res2f, e_res2g, e_res2h, e_res2i, e_res2j; // Reserved words
        public int e_lfanew; // File address of new exe header
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ImageNtHeaders
    {
        public uint Signature;
        public ImageFileHeader FileHeader;
        public ImageOptionalHeader OptionalHeader;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ImageFileHeader
    {
        public ushort Machine;
        public ushort NumberOfSections;
        public uint TimeDateStamp;
        public uint PointerToSymbolTable;
        public uint NumberOfSymbols;
        public ushort SizeOfOptionalHeader;
        public ushort Characteristics;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ImageOptionalHeader
    {
        public MagicType Magic;
        public byte MajorLinkerVersion;
        public byte MinorLinkerVersion;
        public uint SizeOfCode;
        public uint SizeOfInitializedData;
        public uint SizeOfUninitializedData;
        public uint AddressOfEntryPoint;
        public uint BaseOfCode;
        public ulong ImageBaseLong;
        public uint SectionAlignment;
        public uint FileAlignment;
        public ushort MajorOperatingSystemVersion;
        public ushort MinorOperatingSystemVersion;
        public ushort MajorImageVersion;
        public ushort MinorImageVersion;
        public ushort MajorSubsystemVersion;
        public ushort MinorSubsystemVersion;
        public uint Win32VersionValue;
        public uint SizeOfImage;
        public uint SizeOfHeaders;
        public uint CheckSum;
        public SubSystemType Subsystem;
        public DllCharacteristicsType DllCharacteristics;
        public IntPtr SizeOfStackReserve;
        public IntPtr SizeOfStackCommit;
        public IntPtr SizeOfHeapReserve;
        public IntPtr SizeOfHeapCommit;
        public uint LoaderFlags;
        public uint NumberOfRvaAndSizes;
        public ImageDataDirectory ExportTable;
        public ImageDataDirectory ImportTable;
        public ImageDataDirectory ResourceTable;
        public ImageDataDirectory ExceptionTable;
        public ImageDataDirectory CertificateTable;
        public ImageDataDirectory BaseRelocationTable;
        public ImageDataDirectory Debug;
        public ImageDataDirectory Architecture;
        public ImageDataDirectory GlobalPtr;
        public ImageDataDirectory TLSTable;
        public ImageDataDirectory LoadConfigTable;
        public ImageDataDirectory BoundImport;
        public ImageDataDirectory IAT;
        public ImageDataDirectory DelayImportDescriptor;
        public ImageDataDirectory CLRRuntimeHeader;
        public ImageDataDirectory Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ImageDataDirectory
    {
        public uint VirtualAddress;
        public uint Size;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ImageSectionHeader
    {
        public ulong Name; //8 byte string
        public uint PhysicalAddress;
        public uint VirtualAddress;
        public uint SizeOfRawData;
        public uint PointerToRawData;
        public uint PointerToRelocations;
        public uint PointerToLinenumbers;
        public ushort NumberOfRelocations;
        public ushort NumberOfLinenumbers;
        public uint Characteristics;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ImageBaseRelocation
    {
        public uint VirtualAdress;
        public uint SizeOfBlock;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ImageImportDescriptor
    {
        public uint OriginalFirstThunk;
        public uint TimeDateStamp;
        public uint ForwarderChain;
        public uint Name;
        public uint FirstThunk;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ImageExportDirectory
    {
        public uint Characteristics;
        public uint TimeDateStamp;
        public ushort MajorVersion;
        public ushort MinorVersion;
        public uint Name;
        public uint Base;
        public uint NumberOfFunctions;
        public uint NumberOfNames;
        public uint AddressOfFunctions; // RVA from base of image
        public uint AddressOfNames; // RVA from base of image
        public uint AddressOfNameOrdinals; // RVA from base of image
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct SystemInfo
    {
        public ushort wProcessorArchitecture;
        public ushort wReserved;
        public uint dwPageSize;
        public IntPtr lpMinimumApplicationAddress;
        public IntPtr lpMaximumApplicationAddress;
        public IntPtr dwActiveProcessorMask;
        public uint dwNumberOfProcessors;
        public uint dwProcessorType;
        public uint dwAllocationGranularity;
        public ushort wProcessorLevel;
        public ushort wProcessorRevision;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ImageTlsDirectory
    {
        public IntPtr StartAddressOfRawData;
        public IntPtr EndAddressOfRawData;
        public IntPtr AddressOfIndex;
        public IntPtr AddressOfCallBacks;
        public IntPtr SizeOfZeroFill;
        public uint Characteristics;
    }

    private enum MagicType : ushort
    {
        ImageNtOptionalHdr32Magic = 0x10b,
        ImageNtOptionalHdr64Magic = 0x20b
    }

    private enum SubSystemType : ushort
    {
        ImageSubsystemUnknown = 0,
        ImageSubsystemNative = 1,
        ImageSubsystemWindowsGui = 2,
        ImageSubsystemWindowsCui = 3,
        ImageSubsystemPosixCui = 7,
        ImageSubsystemWindowsCeGui = 9,
        ImageSubsystemEfiApplication = 10,
        ImageSubsystemEfiBootServiceDriver = 11,
        ImageSubsystemEfiRuntimeDriver = 12,
        ImageSubsystemEfiRom = 13,
        ImageSubsystemXbox = 14
    }

    private enum DllCharacteristicsType : ushort
    {
        Res0 = 0x0001,
        Res1 = 0x0002,
        Res2 = 0x0004,
        Res3 = 0x0008,
        ImageDllCharacteristicsDynamicBase = 0x0040,
        ImageDllCharacteristicsForceIntegrity = 0x0080,
        ImageDllCharacteristicsNxCompat = 0x0100,
        ImageDllcharacteristicsNoIsolation = 0x0200,
        ImageDllcharacteristicsNoSeh = 0x0400,
        ImageDllcharacteristicsNoBind = 0x0800,
        Res4 = 0x1000,
        ImageDllcharacteristicsWdmDriver = 0x2000,
        ImageDllcharacteristicsTerminalServerAware = 0x8000
    }

    private enum BasedRelocationType
    {
        ImageRelBasedAbsolute = 0,
        ImageRelBasedHigh = 1,
        ImageRelBasedLow = 2,
        ImageRelBasedHighlow = 3,
        ImageRelBasedHighadj = 4,
        ImageRelBasedMipsJmpaddr = 5,
        ImageRelBasedMipsJmpaddr16 = 9,
        ImageRelBasedIa64Imm64 = 9,
        ImageRelBasedDir64 = 10
    }

    private enum AllocationType : uint
    {
        Commit = 0x1000,
        Reserve = 0x2000,
        Reset = 0x80000,
        LargePages = 0x20000000,
        Physical = 0x400000,
        TopDown = 0x100000,
        WriteWatch = 0x200000,
        Decommit = 0x4000,
        Release = 0x8000
    }

    private enum MemoryProtection : uint
    {
        Execute = 0x10,
        ExecuteRead = 0x20,
        ExecuteReadwrite = 0x40,
        ExecuteWritecopy = 0x80,
        Noaccess = 0x01,
        Readonly = 0x02,
        Readwrite = 0x04,
        Writecopy = 0x08,
        GuardModifierflag = 0x100,
        NocacheModifierflag = 0x200,
        WritecombineModifierflag = 0x400
    }

    private enum PageProtection
    {
        Noaccess = 0x01,
        Readonly = 0x02,
        Readwrite = 0x04,
        Writecopy = 0x08,
        Execute = 0x10,
        ExecuteRead = 0x20,
        ExecuteReadwrite = 0x40,
        ExecuteWritecopy = 0x80,
        Guard = 0x100,
        Nocache = 0x200,
        Writecombine = 0x400,
    }

    private enum ImageSectionFlags : uint
    {
        ImageScnLnkNrelocOvfl = 0x01000000, // Section contains extended relocations.
        ImageScnMemDiscardable = 0x02000000, // Section can be discarded.
        ImageScnMemNotCached = 0x04000000, // Section is not cachable.
        ImageScnMemNotPaged = 0x08000000, // Section is not pageable.
        ImageScnMemShared = 0x10000000, // Section is shareable.
        ImageScnMemExecute = 0x20000000, // Section is executable.
        ImageScnMemRead = 0x40000000, // Section is readable.
        ImageScnMemWrite = 0x80000000 // Section is writeable.
    }

    private enum DllReason : uint
    {
        DllProcessAttach = 1,
        DllThreadAttach = 2,
        DllThreadDetach = 3,
        DllProcessDetach = 0
    }

    private class Win
    {
        public const ushort ImageDosSignature = 0x5A4D;
        public const uint ImageNtSignature = 0x00004550;
        public const uint ImageFileMachineI386 = 0x014c;
        public const uint ImageFileMachineAmd64 = 0x8664;
        public const uint PageNocache = 0x200;
        public const uint ImageScnCntInitializedData = 0x00000040;
        public const uint ImageScnCntUninitializedData = 0x00000080;
        public const uint ImageScnMemDiscardable = 0x02000000;
        public const uint ImageScnMemNotCached = 0x04000000;
        public const uint ImageFileDll = 0x2000;

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern IntPtr VirtualAlloc(IntPtr lpAddress, UIntPtr dwSize, AllocationType flAllocationType, MemoryProtection flProtect);

        [DllImport("msvcrt.dll", EntryPoint = "memset", CallingConvention = CallingConvention.Cdecl, SetLastError = false)]
        public static extern IntPtr MemSet(IntPtr dest, int c, UIntPtr count);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, SetLastError = true)]
        public static extern IntPtr LoadLibrary(IntPtr lpFileName);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi, ExactSpelling = true, SetLastError = true)]
        public static extern IntPtr GetProcAddress(IntPtr hModule, IntPtr procName);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool VirtualFree(IntPtr lpAddress, IntPtr dwSize, AllocationType dwFreeType);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool VirtualProtect(IntPtr lpAddress, IntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern void GetNativeSystemInfo(out SystemInfo lpSystemInfo);

        // Equivalent to the IMAGE_FIRST_SECTION macro
        public static IntPtr IMAGE_FIRST_SECTION(IntPtr pNtHeader, ushort ntheaderFileHeaderSizeOfOptionalHeader)
        {
            return PtrAdd(pNtHeader, Of.ImageNtHeadersOptionalHeader + ntheaderFileHeaderSizeOfOptionalHeader);
        }

        // Equivalent to the IMAGE_FIRST_SECTION macro
        public static int IMAGE_FIRST_SECTION(int lfanew, ushort ntheaderFileHeaderSizeOfOptionalHeader)
        {
            return lfanew + Of.ImageNtHeadersOptionalHeader + ntheaderFileHeaderSizeOfOptionalHeader;
        }

        // Equivalent to the IMAGE_ORDINAL32/64 macros
        public static IntPtr IMAGE_ORDINAL(IntPtr ordinal)
        {
            return (IntPtr)(int)(unchecked((ulong)ordinal.ToInt64()) & 0xffff);
        }

        // Equivalent to the IMAGE_SNAP_BY_ORDINAL32/64 macro
        public static bool IMAGE_SNAP_BY_ORDINAL(IntPtr ordinal)
        {
            return IntPtr.Size == 8 ? ordinal.ToInt64() < 0 : ordinal.ToInt32() < 0;
        }
    }

    private static T PtrRead<T>(IntPtr ptr)
    {
        return (T)Marshal.PtrToStructure(ptr, typeof(T));
    }

    private static void PtrWrite<T>(IntPtr ptr, T val)
    {
        Marshal.StructureToPtr(val, ptr, false);
    }

    private static IntPtr PtrAdd(IntPtr p, int v)
    {
        return (IntPtr)(p.ToInt64() + v);
    }

    private static IntPtr PtrAdd(IntPtr p, uint v)
    {
        return IntPtr.Size == 8 ? (IntPtr)(p.ToInt64() + v) : (IntPtr)(p.ToInt32() + unchecked((int)v));
    }

    private static IntPtr PtrAdd(IntPtr p, IntPtr v)
    {
        return IntPtr.Size == 8 ? (IntPtr)(p.ToInt64() + v.ToInt64()) : (IntPtr)(p.ToInt32() + v.ToInt32());
    }

    private static IntPtr PtrSub(IntPtr p, IntPtr v)
    {
        return IntPtr.Size == 8 ? (IntPtr)(p.ToInt64() - v.ToInt64()) : (IntPtr)(p.ToInt32() - v.ToInt32());
    }

    private static IntPtr PtrBitOr(IntPtr p, UIntPtr v)
    {
        return IntPtr.Size == 8 ? (IntPtr)unchecked((long)(unchecked((ulong)p.ToInt64()) | v.ToUInt64())) : (IntPtr)unchecked((int)(unchecked((uint)p.ToInt32()) | v.ToUInt32()));
    }

    private static IntPtr PtrAlignDown(IntPtr p, UIntPtr align)
    {
        return (IntPtr)unchecked((long)(unchecked((ulong)p.ToInt64()) & ~(align.ToUInt64() - 1)));
    }

    private static bool PtrIsInvalidHandle(IntPtr h)
    {
        return h == IntPtr.Zero || h == (IntPtr)(-1);
    }

    private static bool PtrSpanBoundary(IntPtr p, uint size, int boundaryBits)
    {
        return unchecked((ulong)p.ToInt64()) >> boundaryBits < unchecked((ulong)p.ToInt64()) + size >> boundaryBits;
    }

    private static T BytesReadStructAt<T>(byte[] buf, int offset)
    {
        int size = Marshal.SizeOf(typeof(T));
        IntPtr ptr = Marshal.AllocHGlobal(size);
        Marshal.Copy(buf, offset, ptr, size);
        T res = (T)Marshal.PtrToStructure(ptr, typeof(T));
        Marshal.FreeHGlobal(ptr);
        return res;
    }
}