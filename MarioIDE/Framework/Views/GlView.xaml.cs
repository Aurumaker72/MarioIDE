using System;
using MarioIDE.Framework.ViewModels;
using System.Windows;
using UserControl = System.Windows.Controls.UserControl;

namespace MarioIDE.Framework.Views;

/// <summary>
/// Interaction logic for GlView.xaml
/// </summary>
public partial class GlView : UserControl
{
    public GlView()
    {
        Loaded += OnLoaded;
        Unloaded += OnUnloaded;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is IGlView glView)
        {
            glView.OnViewLoaded(Host.Child as GlControl);
        }
        
        Console.WriteLine("OnLoaded: " + sender);
    }

    private void OnUnloaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is IGlView glView)
        {
            glView.OnViewUnloaded();
        }
    }
}