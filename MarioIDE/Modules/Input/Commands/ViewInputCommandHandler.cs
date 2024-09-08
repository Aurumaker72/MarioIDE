using Gemini.Framework.Commands;
using Gemini.Framework.Services;
using Gemini.Framework.Threading;
using MarioIDE.Modules.Input.ViewModels;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace MarioIDE.Modules.Input.Commands;

[CommandHandler]
public class ViewInputCommandHandler : CommandHandlerBase<ViewInputCommandDefinition>
{
    private readonly IShell _shell;

    [ImportingConstructor]
    public ViewInputCommandHandler(IShell shell)
    {
        _shell = shell;
    }

    public override Task Run(Command command)
    {
        _shell.ShowTool<IInput>();
        return TaskUtility.Completed;
    }
}