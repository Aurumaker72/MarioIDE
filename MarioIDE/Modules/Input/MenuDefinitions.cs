using Gemini.Framework.Menus;
using MarioIDE.Modules.Input.Commands;
using System.ComponentModel.Composition;

namespace MarioIDE.Modules.Input;

public static class MenuDefinitions
{
    [Export]
    public static MenuItemDefinition ViewInputMenuItem = new CommandMenuItemDefinition<ViewInputCommandDefinition>(Gemini.Modules.MainMenu.MenuDefinitions.ViewToolsMenuGroup, 0);
}