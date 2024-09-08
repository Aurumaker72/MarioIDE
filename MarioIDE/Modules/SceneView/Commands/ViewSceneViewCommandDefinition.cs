using Gemini.Framework.Commands;

namespace MarioIDE.Modules.SceneView.Commands;

[CommandDefinition]
public class ViewSceneViewCommandDefinition : CommandDefinition
{
    public const string CommandName = "View.SceneView";
    public override string Name => CommandName;
    public override string Text => "Scene View";
    public override string ToolTip => string.Empty;
}