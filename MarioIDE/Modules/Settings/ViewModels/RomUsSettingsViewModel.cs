using Gemini.Modules.Settings;
using System.ComponentModel.Composition;

namespace MarioIDE.Modules.Settings.ViewModels;

[Export(typeof(ISettingsEditor))]
[PartCreationPolicy(CreationPolicy.NonShared)]
public class RomUsSettingsViewModel : RomSettingsViewModel, ISettingsEditor
{
    public string SettingsPagePath => "Rom";
    public string SettingsPageName => "US";

    public void ApplyChanges()
    {

    }
}