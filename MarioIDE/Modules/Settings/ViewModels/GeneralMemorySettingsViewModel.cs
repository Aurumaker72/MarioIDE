using Caliburn.Micro;
using Gemini.Modules.Settings;
using System.ComponentModel.Composition;

namespace MarioIDE.Modules.Settings.ViewModels;

[Export(typeof(ISettingsEditor))]
[PartCreationPolicy(CreationPolicy.NonShared)]
public class GeneralMemorySettingsViewModel : PropertyChangedBase, ISettingsEditor
{
    public string SettingsPagePath => "General";
    public string SettingsPageName => "Memory";

    public int MaxMemory { get; set; }

    public void ApplyChanges()
    {

    }
}