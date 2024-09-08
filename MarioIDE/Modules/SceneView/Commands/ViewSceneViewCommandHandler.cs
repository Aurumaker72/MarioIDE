using Gemini.Framework.Commands;
using Gemini.Framework.Services;
using Gemini.Framework.Threading;
using MarioIDE.Modules.SceneView.ViewModels;
using System.ComponentModel.Composition;
using System.Threading.Tasks;

namespace MarioIDE.Modules.SceneView.Commands;

[CommandHandler]
internal class ViewSceneViewCommandHandler : CommandHandlerBase<ViewSceneViewCommandDefinition>
{
    private readonly IShell _shell;

    [ImportingConstructor]
    public ViewSceneViewCommandHandler(IShell shell)
    {
        _shell = shell;
    }

    public override Task Run(Command command)
    {
        _shell.ShowTool<ISceneView>();
        return TaskUtility.Completed;
    }
}