using Gemini.Framework.Menus;
using System.ComponentModel.Composition;

namespace MarioIDE;

public static class MenuDefinitions
{
    [Export]
    public static MenuItemGroupDefinition ViewCustomToolsMenuGroup = new(Gemini.Modules.MainMenu.MenuDefinitions.ViewMenu, 1);
}