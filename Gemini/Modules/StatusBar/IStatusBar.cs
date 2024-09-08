namespace Gemini.Modules.StatusBar
{
    public interface IStatusBar
    {
        string Text { get; set; }
        int Progress { get; set; }
    }
}
