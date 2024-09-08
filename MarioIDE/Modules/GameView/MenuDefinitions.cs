using Gemini.Framework.Menus;
using MarioIDE.Modules.GameView.Commands;
using System.ComponentModel.Composition;

namespace MarioIDE.Modules.GameView;

public static class MenuDefinitions
{
    [Export]
    public static MenuItemDefinition ViewGameViewMenuItem = new CommandMenuItemDefinition<ViewGameViewCommandDefinition>(MarioIDE.MenuDefinitions.ViewCustomToolsMenuGroup, 0);
}