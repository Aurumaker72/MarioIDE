using Caliburn.Micro;
using Gemini.Framework;
using Gemini.Framework.Attributes;
using Gemini.Framework.Services;
using MarioIDE.Framework.Models;
using MarioIDE.Framework.Views;
using System.ComponentModel.Composition;

namespace MarioIDE.Framework.ViewModels;

[UseView(typeof(GlView))]
[PartCreationPolicy(CreationPolicy.NonShared)]
public abstract class GlDocumentViewModel : PersistedDocument, IGlView
{
    public KeyboardState KeyboardState => _glViewModel.KeyboardState;
    public MouseState MouseState => _glViewModel.MouseState;
    public int Width => _glViewModel.Width;
    public int Height => _glViewModel.Height;

    private readonly GlViewModel _glViewModel;

    protected GlDocumentViewModel()
    {
        _glViewModel = new GlViewModel();
        _glViewModel.GotFocus += GlViewModelOnGotFocus;
        _glViewModel.OnRender += OnRender;
    }

    public virtual void OnRender()
    {
    }

    public void OnViewLoaded(GlControl control)
    {
        _glViewModel.OnViewLoaded(this, control);
    }

    public void OnViewUnloaded()
    {
        _glViewModel.OnViewUnloaded();
    }

    public void Render()
    {
        _glViewModel?.Render();
    }

    private void GlViewModelOnGotFocus()
    {
        IoC.Get<IShell>().ActiveLayoutItem = this;
    }
}