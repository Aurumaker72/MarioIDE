using Gemini.Framework;
using Gemini.Framework.Services;
using MarioIDE.Modules.Timeline.ViewModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace MarioIDE.Modules.Timeline.EditorProvider;

[Export(typeof(IEditorProvider))]
public class UsProjectFileEditorProvider : IEditorProvider
{
    private static readonly List<EditorFileType> Extensions = new()
    {
        new EditorFileType("US Project File (.mide)" , ".mide")
    };

    public IEnumerable<EditorFileType> FileTypes => Extensions;
    public bool CanCreateNew => true;
    public bool Handles(string path) => Extensions.Any(e => e.FileExtension.Equals(Path.GetExtension(path), StringComparison.InvariantCultureIgnoreCase));
    public IDocument Create() => new TimelineViewModel(GameVersion.US);
    public async Task New(IDocument document, string name) => await ((TimelineViewModel)document).New(name);
    public async Task Open(IDocument document, string path) => await ((TimelineViewModel)document).Load(path, path);
}