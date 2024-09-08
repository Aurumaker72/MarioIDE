using Gemini.Framework.Commands;

namespace MarioIDE.Modules.Inspector.Commands;

[CommandDefinition]
internal class ViewInspectorCommandDefinition : CommandDefinition
{
    public const string CommandName = "View.Inspector";
    public override string Name => CommandName;
    public override string Text => "Inspector";
    public override string ToolTip => string.Empty;
}
