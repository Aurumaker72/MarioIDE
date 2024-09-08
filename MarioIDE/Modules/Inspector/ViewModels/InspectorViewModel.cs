using Gemini.Framework.Services;
using ImGuiNET;
using MarioIDE.Core.Modules.Inspector;
using MarioIDE.Framework.ViewModels;
using System.ComponentModel.Composition;

namespace MarioIDE.Modules.Inspector.ViewModels;

[Export(typeof(IInspector))]
internal class InspectorViewModel: GlToolViewModel, IInspector
{
    public override PaneLocation PreferredLocation => PaneLocation.Right;
    public override string DisplayName => "Inspector";

    public override void OnRender()
    {
        ImGui.Text("Test");
    }
}