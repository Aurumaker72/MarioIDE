using MarioIDE.Framework.Views;

namespace MarioIDE.Framework.ViewModels;

public interface IGlView
{
    void Render();
    void OnViewLoaded(GlControl control);
    void OnViewUnloaded();
}