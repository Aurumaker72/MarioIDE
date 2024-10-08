﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ImGuiNET;
using OpenTK;
using OpenTK.Graphics.OpenGL4;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace MarioIDE.Framework.Models;

internal class ImGuiController : IDisposable
{
    private bool _frameBegun;

    private int _vertexArray;
    private int _vertexBuffer;
    private int _vertexBufferSize;
    private int _indexBuffer;
    private int _indexBufferSize;

    private int _fontTexture;

    private int _shader;
    private int _shaderFontTextureLocation;
    private int _shaderProjectionMatrixLocation;

    private int _windowWidth;
    private int _windowHeight;

    private readonly Vector2 _scaleFactor = Vector2.One;

    private static bool _khrDebugAvailable;

    private readonly List<int> _keys = new();
    private readonly IntPtr _context;

    private readonly Stopwatch _sw = Stopwatch.StartNew();
    private float _lastWheel;

    public ImGuiController(int width, int height)
    {
        _windowWidth = width;
        _windowHeight = height;

        int major = GL.GetInteger(GetPName.MajorVersion);
        int minor = GL.GetInteger(GetPName.MinorVersion);

        _khrDebugAvailable = major == 4 && minor >= 3 || IsExtensionSupported("KHR_debug");

        _context = ImGui.CreateContext();
        ImGui.SetCurrentContext(_context);

        ImGuiIOPtr io = ImGui.GetIO();
        unsafe
        {
            ImGui.GetIO().NativePtr->IniFilename = null;
        }

        io.Fonts.AddFontDefault();

        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;

        CreateDeviceResources();
        SetKeyMappings();

        SetPerFrameImGuiData(1f / 60f);

        ImGui.NewFrame();
        _frameBegun = true;
    }

    public void CreateDeviceResources()
    {
        _vertexBufferSize = 10000;
        _indexBufferSize = 2000;

        int prevVao = GL.GetInteger(GetPName.VertexArrayBinding);
        int prevArrayBuffer = GL.GetInteger(GetPName.ArrayBufferBinding);

        _vertexArray = GL.GenVertexArray();
        GL.BindVertexArray(_vertexArray);
        LabelObject(ObjectLabelIdentifier.VertexArray, _vertexArray, "ImGui");

        _vertexBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
        LabelObject(ObjectLabelIdentifier.Buffer, _vertexBuffer, "VBO: ImGui");
        GL.BufferData(BufferTarget.ArrayBuffer, _vertexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);

        _indexBuffer = GL.GenBuffer();
        GL.BindBuffer(BufferTarget.ElementArrayBuffer, _indexBuffer);
        LabelObject(ObjectLabelIdentifier.Buffer, _indexBuffer, "EBO: ImGui");
        GL.BufferData(BufferTarget.ElementArrayBuffer, _indexBufferSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);

        RecreateFontDeviceTexture();

        const string vertexSource = @"#version 330 core

uniform mat4 projection_matrix;

layout(location = 0) in vec2 in_position;
layout(location = 1) in vec2 in_texCoord;
layout(location = 2) in vec4 in_color;

out vec4 color;
out vec2 texCoord;

void main()
{
    gl_Position = projection_matrix * vec4(in_position, 0, 1);
    color = in_color;
    texCoord = in_texCoord;
}";
        const string fragmentSource = @"#version 330 core

uniform sampler2D in_fontTexture;

in vec4 color;
in vec2 texCoord;

out vec4 outputColor;

void main()
{
    outputColor = color * texture(in_fontTexture, texCoord);
}";

        _shader = CreateProgram("ImGui", vertexSource, fragmentSource);
        _shaderProjectionMatrixLocation = GL.GetUniformLocation(_shader, "projection_matrix");
        _shaderFontTextureLocation = GL.GetUniformLocation(_shader, "in_fontTexture");

        int stride = Marshal.SizeOf<ImDrawVert>();
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, 0);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, 8);
        GL.VertexAttribPointer(2, 4, VertexAttribPointerType.UnsignedByte, true, stride, 16);

        GL.EnableVertexAttribArray(0);
        GL.EnableVertexAttribArray(1);
        GL.EnableVertexAttribArray(2);

        GL.BindVertexArray(prevVao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, prevArrayBuffer);
    }

    /// <summary>
    /// Recreates the device texture used to render text.
    /// </summary>
    public void RecreateFontDeviceTexture()
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out _);

        int mips = (int)Math.Floor(Math.Log(Math.Max(width, height), 2));

        int prevActiveTexture = GL.GetInteger(GetPName.ActiveTexture);
        GL.ActiveTexture(TextureUnit.Texture0);
        int prevTexture2D = GL.GetInteger(GetPName.TextureBinding2D);

        _fontTexture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _fontTexture);
        GL.TexStorage2D(TextureTarget2d.Texture2D, mips, SizedInternalFormat.Rgba8, width, height);
        LabelObject(ObjectLabelIdentifier.Texture, _fontTexture, "ImGui Text Atlas");

        GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, width, height, PixelFormat.Bgra, PixelType.UnsignedByte, pixels);

        GL.GenerateMipmap(GenerateMipmapTarget.Texture2D);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.Repeat);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.Repeat);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, mips - 1);

        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);

        // Restore state
        GL.BindTexture(TextureTarget.Texture2D, prevTexture2D);
        GL.ActiveTexture((TextureUnit)prevActiveTexture);

        io.Fonts.SetTexID((IntPtr)_fontTexture);

        io.Fonts.ClearTexData();
    }

    /// <summary>
    /// Renders the ImGui draw list data.
    /// </summary>
    public void Render()
    {
        if (_frameBegun)
        {
            _frameBegun = false;
            ImGui.Render();
            RenderImDrawData(ImGui.GetDrawData());
        }
    }

    /// <summary>
    /// Updates ImGui input and IO configuration state.
    /// </summary>
    public void Update(int width, int height, MouseState mouseState, KeyboardState keyboardState)
    {
        _windowWidth = width;
        _windowHeight = height;

        ImGui.SetCurrentContext(_context);
        if (_frameBegun) ImGui.Render();
        SetPerFrameImGuiData((float)_sw.Elapsed.TotalSeconds);
        _sw.Restart();
        UpdateImGuiInput(mouseState, keyboardState);
        _frameBegun = true;
        ImGui.NewFrame();
    }

    /// <summary>
    /// Sets per-frame data based on the associated window.
    /// This is called by Update(float).
    /// </summary>
    private void SetPerFrameImGuiData(float deltaSeconds)
    {
        ImGuiIOPtr io = ImGui.GetIO();
        io.DisplaySize = new Vector2(
            _windowWidth / _scaleFactor.X,
            _windowHeight / _scaleFactor.Y);
        io.DisplayFramebufferScale = _scaleFactor;
        io.DeltaTime = deltaSeconds; // DeltaTime is in seconds.
    }

    private readonly List<char> _pressedChars = new();

    private void UpdateImGuiInput(MouseState mouseState, KeyboardState keyboardState)
    {
        ImGuiIOPtr io = ImGui.GetIO();

        io.MouseDown[0] = mouseState.LeftButton;
        io.MouseDown[1] = mouseState.RightButton;
        io.MouseDown[2] = mouseState.MiddleButton;
        io.MousePos = new Vector2(mouseState.X, mouseState.Y);
        io.MouseWheel = 0;

        if (Math.Abs(_lastWheel - mouseState.Wheel) > 0.0001f)
        {
            io.MouseWheel = (mouseState.Wheel - _lastWheel) * 1f;
            _lastWheel = mouseState.Wheel;
        }

        foreach (int t in _keys)
        {
            io.KeysDown[t] = keyboardState.IsKeyDown((Keys)t);
        }

        foreach (char c in _pressedChars)
        {
            io.AddInputCharacter(c);
        }

        _pressedChars.Clear();

        io.KeyCtrl = keyboardState.IsKeyDown(Keys.LControlKey) || keyboardState.IsKeyDown(Keys.RControlKey);
        io.KeyShift = keyboardState.IsKeyDown(Keys.LShiftKey) || keyboardState.IsKeyDown(Keys.RShiftKey);
        io.KeySuper = keyboardState.IsKeyDown(Keys.LWin) || keyboardState.IsKeyDown(Keys.RWin);
        io.KeyAlt = keyboardState.IsKeyDown(Keys.Alt);
    }

    public void PressChar(char keyChar)
    {
        _pressedChars.Add(keyChar);
    }

    private void SetKeyMappings()
    {
        ImGuiIOPtr io = ImGui.GetIO();
        _keys.Add(io.KeyMap[(int)ImGuiKey.Tab] = (int)Keys.Tab);
        _keys.Add(io.KeyMap[(int)ImGuiKey.LeftArrow] = (int)Keys.Left);
        _keys.Add(io.KeyMap[(int)ImGuiKey.RightArrow] = (int)Keys.Right);
        _keys.Add(io.KeyMap[(int)ImGuiKey.UpArrow] = (int)Keys.Up);
        _keys.Add(io.KeyMap[(int)ImGuiKey.DownArrow] = (int)Keys.Down);
        _keys.Add(io.KeyMap[(int)ImGuiKey.PageUp] = (int)Keys.PageUp);
        _keys.Add(io.KeyMap[(int)ImGuiKey.PageDown] = (int)Keys.PageDown);
        _keys.Add(io.KeyMap[(int)ImGuiKey.Home] = (int)Keys.Home);
        _keys.Add(io.KeyMap[(int)ImGuiKey.End] = (int)Keys.End);
        _keys.Add(io.KeyMap[(int)ImGuiKey.Delete] = (int)Keys.Delete);
        _keys.Add(io.KeyMap[(int)ImGuiKey.Backspace] = (int)Keys.Back);
        _keys.Add(io.KeyMap[(int)ImGuiKey.Enter] = (int)Keys.Enter);
        _keys.Add(io.KeyMap[(int)ImGuiKey.Escape] = (int)Keys.Escape);
        _keys.Add(io.KeyMap[(int)ImGuiKey.Space] = (int)Keys.Space);
        _keys.Add(io.KeyMap[(int)ImGuiKey.A] = (int)Keys.A);
        _keys.Add(io.KeyMap[(int)ImGuiKey.C] = (int)Keys.C);
        _keys.Add(io.KeyMap[(int)ImGuiKey.V] = (int)Keys.V);
        _keys.Add(io.KeyMap[(int)ImGuiKey.X] = (int)Keys.X);
        _keys.Add(io.KeyMap[(int)ImGuiKey.Y] = (int)Keys.Y);
        _keys.Add(io.KeyMap[(int)ImGuiKey.Z] = (int)Keys.Z);
    }

    private void RenderImDrawData(ImDrawDataPtr drawData)
    {
        if (drawData.CmdListsCount == 0)
        {
            return;
        }

        // Get intial state.
        int prevVao = GL.GetInteger(GetPName.VertexArrayBinding);
        int prevArrayBuffer = GL.GetInteger(GetPName.ArrayBufferBinding);
        int prevProgram = GL.GetInteger(GetPName.CurrentProgram);
        bool prevBlendEnabled = GL.GetBoolean(GetPName.Blend);
        bool prevScissorTestEnabled = GL.GetBoolean(GetPName.ScissorTest);
        int prevBlendEquationRgb = GL.GetInteger(GetPName.BlendEquationRgb);
        int prevBlendEquationAlpha = GL.GetInteger(GetPName.BlendEquationAlpha);
        int prevBlendFuncSrcRgb = GL.GetInteger(GetPName.BlendSrcRgb);
        int prevBlendFuncSrcAlpha = GL.GetInteger(GetPName.BlendSrcAlpha);
        int prevBlendFuncDstRgb = GL.GetInteger(GetPName.BlendDstRgb);
        int prevBlendFuncDstAlpha = GL.GetInteger(GetPName.BlendDstAlpha);
        bool prevCullFaceEnabled = GL.GetBoolean(GetPName.CullFace);
        bool prevDepthTestEnabled = GL.GetBoolean(GetPName.DepthTest);
        int prevActiveTexture = GL.GetInteger(GetPName.ActiveTexture);
        GL.ActiveTexture(TextureUnit.Texture0);
        int prevTexture2D = GL.GetInteger(GetPName.TextureBinding2D);
        int[] prevScissorBox = new int[4];
        unsafe
        {
            fixed (int* iptr = &prevScissorBox[0])
            {
                GL.GetInteger(GetPName.ScissorBox, iptr);
            }
        }

        // Bind the element buffer (thru the VAO) so that we can resize it.
        GL.BindVertexArray(_vertexArray);
        // Bind the vertex buffer so that we can resize it.
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vertexBuffer);
        for (int i = 0; i < drawData.CmdListsCount; i++)
        {
            ImDrawListPtr cmdList = drawData.CmdListsRange[i];

            int vertexSize = cmdList.VtxBuffer.Size * Marshal.SizeOf<ImDrawVert>();
            if (vertexSize > _vertexBufferSize)
            {
                int newSize = (int)Math.Max(_vertexBufferSize * 1.5f, vertexSize);

                GL.BufferData(BufferTarget.ArrayBuffer, newSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
                _vertexBufferSize = newSize;
            }

            int indexSize = cmdList.IdxBuffer.Size * sizeof(ushort);
            if (indexSize > _indexBufferSize)
            {
                int newSize = (int)Math.Max(_indexBufferSize * 1.5f, indexSize);
                GL.BufferData(BufferTarget.ElementArrayBuffer, newSize, IntPtr.Zero, BufferUsageHint.DynamicDraw);
                _indexBufferSize = newSize;
            }
        }

        // Setup orthographic projection matrix into our constant buffer
        ImGuiIOPtr io = ImGui.GetIO();
        Matrix4 mvp = Matrix4.CreateOrthographicOffCenter(
            0.0f,
            io.DisplaySize.X,
            io.DisplaySize.Y,
            0.0f,
            -1.0f,
            1.0f);

        GL.UseProgram(_shader);
        GL.UniformMatrix4(_shaderProjectionMatrixLocation, false, ref mvp);
        GL.Uniform1(_shaderFontTextureLocation, 0);

        GL.BindVertexArray(_vertexArray);

        drawData.ScaleClipRects(io.DisplayFramebufferScale);

        GL.Enable(EnableCap.Blend);
        GL.Enable(EnableCap.ScissorTest);
        GL.BlendEquation(BlendEquationMode.FuncAdd);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        GL.Disable(EnableCap.CullFace);
        GL.Disable(EnableCap.DepthTest);

        // Render command lists
        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            ImDrawListPtr cmdList = drawData.CmdListsRange[n];

            GL.BufferSubData(BufferTarget.ArrayBuffer, IntPtr.Zero, cmdList.VtxBuffer.Size * Marshal.SizeOf<ImDrawVert>(), cmdList.VtxBuffer.Data);
            GL.BufferSubData(BufferTarget.ElementArrayBuffer, IntPtr.Zero, cmdList.IdxBuffer.Size * sizeof(ushort), cmdList.IdxBuffer.Data);

            for (int cmdI = 0; cmdI < cmdList.CmdBuffer.Size; cmdI++)
            {
                ImDrawCmdPtr pcmd = cmdList.CmdBuffer[cmdI];
                /*if (pcmd.UserCallback != IntPtr.Zero)
                {
                    throw new NotImplementedException();
                }*/

                GL.ActiveTexture(TextureUnit.Texture0);
                GL.BindTexture(TextureTarget.Texture2D, (int)pcmd.TextureId);

                // We do _windowHeight - (int)clip.W instead of (int)clip.Y because gl has flipped Y when it comes to these coordinates
                Vector4 clip = pcmd.ClipRect;
                GL.Scissor((int)clip.X, _windowHeight - (int)clip.W, (int)(clip.Z - clip.X), (int)(clip.W - clip.Y));

                if ((io.BackendFlags & ImGuiBackendFlags.RendererHasVtxOffset) != 0)
                {
                    GL.DrawElementsBaseVertex(PrimitiveType.Triangles, (int)pcmd.ElemCount, DrawElementsType.UnsignedShort, (IntPtr)(pcmd.IdxOffset * sizeof(ushort)), unchecked((int)pcmd.VtxOffset));
                }
                else
                {
                    GL.DrawElements(BeginMode.Triangles, (int)pcmd.ElemCount, DrawElementsType.UnsignedShort, (int)pcmd.IdxOffset * sizeof(ushort));
                }
            }
        }

        GL.Disable(EnableCap.Blend);
        GL.Disable(EnableCap.ScissorTest);

        // Reset state
        GL.BindTexture(TextureTarget.Texture2D, prevTexture2D);
        GL.ActiveTexture((TextureUnit)prevActiveTexture);
        GL.UseProgram(prevProgram);
        GL.BindVertexArray(prevVao);
        GL.Scissor(prevScissorBox[0], prevScissorBox[1], prevScissorBox[2], prevScissorBox[3]);
        GL.BindBuffer(BufferTarget.ArrayBuffer, prevArrayBuffer);
        GL.BlendEquationSeparate((BlendEquationMode)prevBlendEquationRgb, (BlendEquationMode)prevBlendEquationAlpha);
        GL.BlendFuncSeparate(
            (BlendingFactorSrc)prevBlendFuncSrcRgb,
            (BlendingFactorDest)prevBlendFuncDstRgb,
            (BlendingFactorSrc)prevBlendFuncSrcAlpha,
            (BlendingFactorDest)prevBlendFuncDstAlpha);
        if (prevBlendEnabled) GL.Enable(EnableCap.Blend);
        else GL.Disable(EnableCap.Blend);
        if (prevDepthTestEnabled) GL.Enable(EnableCap.DepthTest);
        else GL.Disable(EnableCap.DepthTest);
        if (prevCullFaceEnabled) GL.Enable(EnableCap.CullFace);
        else GL.Disable(EnableCap.CullFace);
        if (prevScissorTestEnabled) GL.Enable(EnableCap.ScissorTest);
        else GL.Disable(EnableCap.ScissorTest);
    }

    /// <summary>
    /// Frees all graphics resources used by the renderer.
    /// </summary>
    public void Dispose()
    {
        GL.DeleteVertexArray(_vertexArray);
        GL.DeleteBuffer(_vertexBuffer);
        GL.DeleteBuffer(_indexBuffer);

        GL.DeleteTexture(_fontTexture);
        GL.DeleteProgram(_shader);
    }

    public static void LabelObject(ObjectLabelIdentifier objLabelIdent, int glObject, string name)
    {
        if (_khrDebugAvailable)
            GL.ObjectLabel(objLabelIdent, glObject, name.Length, name);
    }

    private static bool IsExtensionSupported(string name)
    {
        int n = GL.GetInteger(GetPName.NumExtensions);
        for (int i = 0; i < n; i++)
        {
            string extension = GL.GetString(StringNameIndexed.Extensions, i);
            if (extension == name) return true;
        }

        return false;
    }

    public static int CreateProgram(string name, string vertexSource, string fragmentSoruce)
    {
        int program = GL.CreateProgram();
        LabelObject(ObjectLabelIdentifier.Program, program, $"Program: {name}");

        int vertex = CompileShader(name, ShaderType.VertexShader, vertexSource);
        int fragment = CompileShader(name, ShaderType.FragmentShader, fragmentSoruce);

        GL.AttachShader(program, vertex);
        GL.AttachShader(program, fragment);

        GL.LinkProgram(program);

        GL.GetProgram(program, GetProgramParameterName.LinkStatus, out int success);
        if (success == 0)
        {
            string info = GL.GetProgramInfoLog(program);
            Debug.WriteLine($"GL.LinkProgram had info log [{name}]:\n{info}");
        }

        GL.DetachShader(program, vertex);
        GL.DetachShader(program, fragment);

        GL.DeleteShader(vertex);
        GL.DeleteShader(fragment);

        return program;
    }

    private static int CompileShader(string name, ShaderType type, string source)
    {
        int shader = GL.CreateShader(type);
        LabelObject(ObjectLabelIdentifier.Shader, shader, $"Shader: {name}");

        GL.ShaderSource(shader, source);
        GL.CompileShader(shader);

        GL.GetShader(shader, ShaderParameter.CompileStatus, out int success);
        if (success == 0)
        {
            string info = GL.GetShaderInfoLog(shader);
            Debug.WriteLine($"GL.CompileShader for shader '{name}' [{type}] had info log:\n{info}");
        }

        return shader;
    }
}