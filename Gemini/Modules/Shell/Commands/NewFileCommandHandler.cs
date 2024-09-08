using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Threading.Tasks;
using System.Windows;
using Caliburn.Micro;
using Gemini.Framework;
using Gemini.Framework.Commands;
using Gemini.Framework.Services;
using Gemini.Properties;

namespace Gemini.Modules.Shell.Commands
{
    [CommandHandler]
    public class NewFileCommandHandler : ICommandListHandler<NewFileCommandListDefinition>
    {
        private int _newFileCounter = 1;

        private readonly IShell _shell;
        private readonly IEditorProvider[] _editorProviders;

        [ImportingConstructor]
        public NewFileCommandHandler(
            IShell shell,
            [ImportMany] IEditorProvider[] editorProviders)
        {
            _shell = shell;
            _editorProviders = editorProviders;
        }

        public void Populate(Command command, List<Command> commands)
        {
            foreach (IEditorProvider editorProvider in _editorProviders)
            {
                if (!editorProvider.CanCreateNew)
                    continue;

                foreach (EditorFileType editorFileType in editorProvider.FileTypes)
                {
                    commands.Add(new Command(command.CommandDefinition)
                    {
                        Text = editorFileType.Name,
                        IconSource = editorFileType.IconSource,
                        Tag = new NewFileTag
                        {
                            EditorProvider = editorProvider,
                            FileType = editorFileType
                        }
                    });
                }
            }
        }

        public async Task Run(Command command)
        {
            NewFileTag tag = (NewFileTag) command.Tag;
            IDocument editor = tag.EditorProvider.Create();

            IViewAware viewAware = (IViewAware)editor;
            viewAware.ViewAttached += (sender, e) =>
            {
                FrameworkElement frameworkElement = (FrameworkElement)e.View;

                RoutedEventHandler loadedHandler = null;
                loadedHandler = async (sender2, e2) =>
                {
                    frameworkElement.Loaded -= loadedHandler;
                    await tag.EditorProvider.New(editor, string.Format(Resources.FileNewUntitled, (_newFileCounter++) + tag.FileType.FileExtension));
                };
                frameworkElement.Loaded += loadedHandler;
            };

            await _shell.OpenDocumentAsync(editor);
        }

        private class NewFileTag
        {
            public IEditorProvider EditorProvider;
            public EditorFileType FileType;
        };
    };
}
