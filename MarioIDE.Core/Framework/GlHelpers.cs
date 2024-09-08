using OpenTK.Graphics.OpenGL4;

namespace MarioIDE.Core.Framework;

public static class GlHelpers
{
    public static GlState SaveGlState()
    {
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
        return new GlState(
            prevVao,
            prevArrayBuffer,
            prevProgram,
            prevBlendEnabled,
            prevScissorTestEnabled,
            prevBlendEquationRgb,
            prevBlendEquationAlpha,
            prevBlendFuncSrcRgb,
            prevBlendFuncSrcAlpha, prevBlendFuncDstRgb, prevBlendFuncDstAlpha, prevCullFaceEnabled, prevDepthTestEnabled, prevActiveTexture, prevTexture2D);
    }

    public static void RestoreGlState(GlState state)
    {
        GL.BindTexture(TextureTarget.Texture2D, state.TextureBinding2D);
        GL.ActiveTexture((TextureUnit)state.ActiveTexture);
        GL.UseProgram(state.CurrentProgram);
        GL.BindVertexArray(state.VertexArrayBinding);
        GL.BindBuffer(BufferTarget.ArrayBuffer, state.ArrayBufferBinding);
        GL.BlendEquationSeparate((BlendEquationMode)state.BlendEquationRgb, (BlendEquationMode)state.BlendEquationAlpha);
        GL.BlendFuncSeparate(
            (BlendingFactorSrc)state.BlendSrcRgb,
            (BlendingFactorDest)state.BlendDstRgb,
            (BlendingFactorSrc)state.BlendSrcAlpha,
            (BlendingFactorDest)state.BlendDstAlpha);
        if (state.Blend) GL.Enable(EnableCap.Blend);
        else GL.Disable(EnableCap.Blend);
        if (state.DepthTest) GL.Enable(EnableCap.DepthTest);
        else GL.Disable(EnableCap.DepthTest);
        if (state.CullFace) GL.Enable(EnableCap.CullFace);
        else GL.Disable(EnableCap.CullFace);
        if (state.ScissorTest) GL.Enable(EnableCap.ScissorTest);
        else GL.Disable(EnableCap.ScissorTest);
    }
}