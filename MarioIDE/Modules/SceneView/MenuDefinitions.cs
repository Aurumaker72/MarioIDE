using Gemini.Framework.Menus;
using MarioIDE.Modules.SceneView.Commands;
using System.ComponentModel.Composition;

namespace MarioIDE.Modules.SceneView;

public static class MenuDefinitions
{
    [Export]
    public static MenuItemDefinition ViewSceneViewMenuItem = new CommandMenuItemDefinition<ViewSceneViewCommandDefinition>(MarioIDE.MenuDefinitions.ViewCustomToolsMenuGroup, 1);
}