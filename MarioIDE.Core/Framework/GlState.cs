namespace MarioIDE.Core.Framework;

public struct GlState
{
    public int VertexArrayBinding { get; }
    public int ArrayBufferBinding { get; }
    public int CurrentProgram { get; }
    public bool Blend { get; }
    public bool ScissorTest { get; }
    public int BlendEquationRgb { get; }
    public int BlendEquationAlpha { get; }
    public int BlendSrcRgb { get; }
    public int BlendSrcAlpha { get; }
    public int BlendDstRgb { get; }
    public int BlendDstAlpha { get; }
    public bool CullFace { get; }
    public bool DepthTest { get; }
    public int ActiveTexture { get; }
    public int TextureBinding2D { get; }

    public GlState(int vertexArrayBinding, int arrayBufferBinding, int currentProgram, bool blend, bool scissorTest, int blendEquationRgb, int blendEquationAlpha, int blendSrcRgb, int blendSrcAlpha, int blendDstRgb, int blendDstAlpha, bool cullFace, bool depthTest, int activeTexture, int textureBinding2D)
    {
        VertexArrayBinding = vertexArrayBinding;
        ArrayBufferBinding = arrayBufferBinding;
        CurrentProgram = currentProgram;
        Blend = blend;
        ScissorTest = scissorTest;
        BlendEquationRgb = blendEquationRgb;
        BlendEquationAlpha = blendEquationAlpha;
        BlendSrcRgb = blendSrcRgb;
        BlendSrcAlpha = blendSrcAlpha;
        BlendDstRgb = blendDstRgb;
        BlendDstAlpha = blendDstAlpha;
        CullFace = cullFace;
        DepthTest = depthTest;
        ActiveTexture = activeTexture;
        TextureBinding2D = textureBinding2D;
    }
}