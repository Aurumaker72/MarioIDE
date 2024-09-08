using Sodium;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace MarioIDE;

internal static class Unlocker
{
    private const string ROMS_FOLDER = "roms";
    private const string LIBS_FOLDER = "libs";

    internal static readonly Dictionary<GameVersion, byte[]> VersionBytes = new()
    {
        { GameVersion.JP, null },
        { GameVersion.US, null }
    };

    internal static readonly Dictionary<GameVersion, string> VersionDlls = new()
    {
        { GameVersion.JP, LIBS_FOLDER + @"\sm64_jp.dll" },
        { GameVersion.US, LIBS_FOLDER + @"\sm64_us.dll" }
    };

    private static readonly Dictionary<string, GameVersion> RomsSha1 = new()
    {
        { "8a20a5c83d6ceb0f0506cfc9fa20d8f438cafe51", GameVersion.JP },
        { "9bef1128717f958171a4afac3ed78ee2bb4e86ce", GameVersion.US  }
    };

    private static readonly Dictionary<GameVersion, string> EncryptedVersionDlls = new()
    {
        { GameVersion.JP, LIBS_FOLDER + @"\sm64_jp.locked.dll" },
        { GameVersion.US, LIBS_FOLDER + @"\sm64_us.locked.dll" }
    };

    public static void UnlockRoms()
    {
        foreach (string romPath in Directory.EnumerateFiles(ROMS_FOLDER, "*.*"))
        {
            string sha1 = Sha1(romPath);
            if (RomsSha1.TryGetValue(sha1, out GameVersion gameVersion))
            {
                string dllPath = EncryptedVersionDlls[gameVersion];
                byte[] romBytes = File.ReadAllBytes(romPath);
                byte[] encryptedBytes = File.ReadAllBytes(dllPath);
                KeyPair headerKey = PublicKeyBox.GenerateKeyPair(romBytes.Take(32).ToArray());
                byte[] dllBytes = SealedPublicKeyBox.Open(encryptedBytes, headerKey);
                VersionBytes[gameVersion] = dllBytes;
            }
        }
    }

    public static void LockRoms()
    {
        if (!Directory.Exists(ROMS_FOLDER))
        {
            Directory.CreateDirectory(ROMS_FOLDER);
        }

        foreach (string romPath in Directory.EnumerateFiles(ROMS_FOLDER, "*.*").ToArray())
        {
            string sha1 = Sha1(romPath);
            if (RomsSha1.TryGetValue(sha1, out GameVersion gameVersion))
            {
                LockRom(gameVersion, romPath);
            }
        }
    }

    private static void LockRom(GameVersion gameVersion, string romPath)
    {
        string dllPath = VersionDlls[gameVersion];
        if (!File.Exists(dllPath))
        {
            return;
        }

        string fullPath = Path.ChangeExtension(dllPath, ".locked.dll");
        if (File.Exists(fullPath))
        {
            File.Delete(fullPath);
        }

        byte[] romBytes = File.ReadAllBytes(romPath);
        byte[] dllBytes = File.ReadAllBytes(dllPath);
        KeyPair headerKey = PublicKeyBox.GenerateKeyPair(romBytes.Take(32).ToArray());
        byte[] encryptedBytes = SealedPublicKeyBox.Create(dllBytes, headerKey);
        File.WriteAllBytes(fullPath, encryptedBytes);
        File.Delete(dllPath);
    }

    private static string Sha1(string path)
    {
        using SHA1 cryptoProvider = SHA1.Create();
        return BitConverter.ToString(cryptoProvider.ComputeHash(File.ReadAllBytes(path)))
            .Replace("-", string.Empty)
            .ToLower();
    }
}