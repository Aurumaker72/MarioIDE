using MarioIDE.Framework.ViewModels;
using System.ComponentModel.Composition.Primitives;

namespace MarioIDE.Framework;

public interface IGlHandler
{
    void UnregisterGlView(ComposablePart part);
    ComposablePart RegisterGlView(IGlView glView);
}