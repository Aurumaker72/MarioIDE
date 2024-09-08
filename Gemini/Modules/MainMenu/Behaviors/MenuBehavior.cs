using System.Linq;
using System.Windows;
using System.Windows.Controls;
using Caliburn.Micro;
using Gemini.Framework.Commands;

namespace Gemini.Modules.MainMenu.Behaviors
{
    public class MenuBehavior : DependencyObject
    {
        public static readonly DependencyProperty UpdateCommandUiItemsProperty = DependencyProperty.RegisterAttached(
            "UpdateCommandUiItems", typeof(bool), typeof(MenuBehavior), new PropertyMetadata(false, OnUpdateCommandUiItemsChanged));

        public static bool GetUpdateCommandUiItems(DependencyObject control)
        {
            return (bool) control.GetValue(UpdateCommandUiItemsProperty);
        }

        public static void SetUpdateCommandUiItems(DependencyObject control, bool value)
        {
            control.SetValue(UpdateCommandUiItemsProperty, value);
        }

        private static void OnUpdateCommandUiItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            MenuItem menuItem = (MenuItem) d;
            menuItem.SubmenuOpened += OnSubmenuOpened;
            if (menuItem.IsSubmenuOpen)
                OnSubmenuOpened(menuItem, new RoutedEventArgs());
        }

        private static void OnSubmenuOpened(object sender, RoutedEventArgs e)
        {
            ICommandRouter commandRouter = IoC.Get<ICommandRouter>();
            MenuItem menuItem = (MenuItem) sender;
            foreach (ICommandUiItem item in menuItem.Items.OfType<ICommandUiItem>().ToList())
                item.Update(commandRouter.GetCommandHandler(item.CommandDefinition));
        }
    }
}