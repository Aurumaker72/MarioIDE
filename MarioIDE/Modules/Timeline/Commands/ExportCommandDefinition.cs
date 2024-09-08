using Gemini.Framework.Commands;

namespace MarioIDE.Modules.Timeline.Commands;

[CommandDefinition]
internal class ExportCommandDefinition : CommandDefinition
{
    public const string CommandName = "File.Export";
    public override string Name => CommandName;
    public override string Text => "Export M64";
    public override string ToolTip => string.Empty;
}