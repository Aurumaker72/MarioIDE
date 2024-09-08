using Caliburn.Micro;
using System.ComponentModel.Composition;

namespace Gemini.Modules.StatusBar.ViewModels
{
    [Export(typeof(IStatusBar))]
	public class StatusBarViewModel : PropertyChangedBase, IStatusBar
    {
        private string _text;
        public string Text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value;
                    NotifyOfPropertyChange(() => Text);
                }
            }
        }

        private int _progress;
        public int Progress
        {
            get => _progress;
            set
            {
                if (_progress != value)
                {
                    _progress = value;
                    NotifyOfPropertyChange(() => Progress);
                }
            }
        }
    }
}
