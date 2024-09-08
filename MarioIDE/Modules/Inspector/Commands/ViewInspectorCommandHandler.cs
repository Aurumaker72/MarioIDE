using Gemini.Framework.Commands;
using Gemini.Framework.Services;
using Gemini.Framework.Threading;
using MarioIDE.Core.Modules.Inspector;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace MarioIDE.Modules.Inspector.Commands;

[CommandHandler]
internal class ViewInspectorCommandHandler : CommandHandlerBase<ViewInspectorCommandDefinition>
{
    private readonly IShell _shell;

    [ImportingConstructor]
    public ViewInspectorCommandHandler(IShell shell)
    {
        _shell = shell;
    }

    public override Task Run(Command command)
    {
        _shell.ShowTool<IInspector>();
        return TaskUtility.Completed;
    }
}
