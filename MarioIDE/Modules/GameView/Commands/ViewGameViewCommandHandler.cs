using Gemini.Framework.Commands;
using Gemini.Framework.Services;
using Gemini.Framework.Threading;
using MarioIDE.Modules.GameView.ViewModels;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace MarioIDE.Modules.GameView.Commands;

[CommandHandler]
internal class ViewGameViewCommandHandler : CommandHandlerBase<ViewGameViewCommandDefinition>
{
    private readonly IShell _shell;

    [ImportingConstructor]
    public ViewGameViewCommandHandler(IShell shell)
    {
        _shell = shell;
    }

    public override Task Run(Command command)
    {
        _shell.ShowTool<IGameView>();
        return TaskUtility.Completed;
    }
}