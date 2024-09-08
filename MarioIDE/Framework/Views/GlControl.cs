using System.Windows.Forms;

namespace MarioIDE.Framework.Views;

public class GlControl : Control
{
    protected override CreateParams CreateParams
    {
        get
        {
            CreateParams cp = base.CreateParams;
            cp.ClassStyle |= 0x1 | 0x2 | 0x20;
            return cp;
        }
    }
}