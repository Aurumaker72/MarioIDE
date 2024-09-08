using System.ComponentModel.Composition;
using Gemini.Framework.Menus;
using Gemini.Properties;

namespace Gemini.Modules.MainMenu
{
    public static class MenuDefinitions
    {
        [Export]
        public static MenuBarDefinition MainMenuBar = new();

        [Export]
        public static MenuDefinition FileMenu = new(MainMenuBar, 0, Resources.FileMenuText);

        [Export]
        public static MenuItemGroupDefinition FileNewOpenMenuGroup = new(FileMenu, 0);

        [Export]
        public static MenuItemGroupDefinition FileCloseMenuGroup = new(FileMenu, 3);

        [Export]
        public static MenuItemGroupDefinition FileSaveMenuGroup = new(FileMenu, 6);

        [Export]
        public static MenuItemGroupDefinition FileExitOpenMenuGroup = new(FileMenu, 10);

        [Export]
        public static MenuDefinition EditMenu = new(MainMenuBar, 1, Resources.EditMenuText);

        [Export]
        public static MenuItemGroupDefinition EditUndoRedoMenuGroup = new(EditMenu, 0);

        [Export]
        public static MenuDefinition ViewMenu = new(MainMenuBar, 2, Resources.ViewMenuText);

        [Export]
        public static MenuItemGroupDefinition ViewToolsMenuGroup = new(ViewMenu, 0);

        [Export]
        public static MenuItemGroupDefinition ViewPropertiesMenuGroup = new(ViewMenu, 100);

        [Export]
        public static MenuDefinition ToolsMenu = new(MainMenuBar, 10, Resources.ToolsMenuText);

        [Export]
        public static MenuItemGroupDefinition ToolsOptionsMenuGroup = new(ToolsMenu, 100);

        [Export]
        public static MenuItemGroupDefinition EditOptionsMenuGroup = new(EditMenu, 1000);

        //[Export]
        //public static MenuDefinition WindowMenu = new MenuDefinition(MainMenuBar, 20, Resources.WindowMenuText);

        //[Export]
        //public static MenuItemGroupDefinition WindowDocumentListMenuGroup = new MenuItemGroupDefinition(WindowMenu, 10);

        [Export]
        public static MenuDefinition HelpMenu = new(MainMenuBar, 30, Resources.HelpMenuText);
    }
}