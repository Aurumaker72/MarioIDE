using Gemini.Framework.Commands;

namespace MarioIDE.Modules.Input.Commands;

[CommandDefinition]
public class ViewInputCommandDefinition : CommandDefinition
{
    public const string CommandName = "View.Input";
    public override string Name => CommandName;
    public override string Text => "Input";
    public override string ToolTip => string.Empty;
}