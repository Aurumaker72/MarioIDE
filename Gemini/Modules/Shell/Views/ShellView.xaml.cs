using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using AvalonDock.Controls;
using Gemini.Framework;
using Gemini.Modules.Shell.ViewModels;

namespace Gemini.Modules.Shell.Views
{
    public partial class ShellView : IShellView
    {
        public ShellView()
        {
            InitializeComponent();
        }

        public void LoadLayout(Stream stream, Action<ITool> addToolCallback, Action<IDocument> addDocumentCallback,
                               Dictionary<string, ILayoutItem> itemsState)
        {
            LayoutUtility.LoadLayout(Manager, stream, addDocumentCallback, addToolCallback, itemsState);
        }

        public void SaveLayout(Stream stream)
        {
            LayoutUtility.SaveLayout(Manager, stream);
        }

        private void OnManagerLayoutUpdated(object sender, EventArgs e)
        {
            UpdateFloatingWindows();
        }

        public void UpdateFloatingWindows()
        {
            if (DataContext == null)
                return;

            Window mainWindow = Window.GetWindow(this);
            ImageSource mainWindowIcon = (mainWindow != null) ? mainWindow.Icon : null;
            bool showFloatingWindowsInTaskbar = ((ShellViewModel)DataContext).ShowFloatingWindowsInTaskbar;
            foreach (LayoutFloatingWindowControl window in Manager.FloatingWindows)
            {
                window.Icon = mainWindowIcon;
                window.ShowInTaskbar = showFloatingWindowsInTaskbar;
            }
        }
    };
}
