using System.Threading.Tasks;

namespace Gemini.Framework
{
    public interface IPersistedDocument : IDocument
    {
        bool IsNew { get; }
        string FileName { get; }
        string FilePath { get; }

        Task New(string fileName);
        Task Load(string filePath, string newFilePath);
        Task Save(string filePath);
    }
}