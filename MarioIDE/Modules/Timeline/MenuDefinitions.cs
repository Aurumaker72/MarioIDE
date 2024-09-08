using Gemini.Framework.Menus;
using MarioIDE.Modules.Timeline.Commands;
using System.ComponentModel.Composition;

namespace MarioIDE.Modules.Timeline;

public static class MenuDefinitions
{
    [Export] public static MenuItemGroupDefinition FileExportMenuGroup = new(Gemini.Modules.MainMenu.MenuDefinitions.FileMenu, 7);
    [Export] public static MenuItemDefinition ExportMenuItem = new CommandMenuItemDefinition<ExportCommandDefinition>(FileExportMenuGroup, 0);
}