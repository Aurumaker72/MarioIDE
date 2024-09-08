using Gemini.Framework.Commands;

namespace MarioIDE.Modules.GameView.Commands;

[CommandDefinition]
public class ViewGameViewCommandDefinition : CommandDefinition
{
    public const string CommandName = "View.GameView";
    public override string Name => CommandName;
    public override string Text => "Game View";
    public override string ToolTip => string.Empty;
}