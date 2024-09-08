using Gemini.Framework.Menus;
using MarioIDE.Modules.SceneView.Commands;
using System.ComponentModel.Composition;
using MarioIDE.Modules.Inspector.Commands;

namespace MarioIDE.Modules.Inspector;

public static class MenuDefinitions
{
    [Export]
    public static MenuItemDefinition ViewInspectorMenuItem = new CommandMenuItemDefinition<ViewInspectorCommandDefinition>(Gemini.Modules.MainMenu.MenuDefinitions.ViewToolsMenuGroup, 1000);
}