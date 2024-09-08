using Caliburn.Micro;
using Gemini.Framework.Attributes;
using MarioIDE.Modules.Settings.Views;
using System.Diagnostics;

namespace MarioIDE.Modules.Settings.ViewModels;

[UseView(typeof(RomSettingsView))]
public class RomSettingsViewModel : PropertyChangedBase
{
    private string _selectedPath;
    public string SelectedPath
    {
        get => _selectedPath;
        set
        {
            _selectedPath = value;
            Debug.WriteLine("SelectedPath: " + _selectedPath);
        }
    }
}