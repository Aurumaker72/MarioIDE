using Caliburn.Micro;
using Gemini.Framework.Commands;
using Gemini.Framework.Services;
using Gemini.Framework.Threading;
using Gemini.Framework.ToolBars;
using Gemini.Modules.Shell.Commands;
using Gemini.Modules.ToolBars;
using Gemini.Modules.ToolBars.Models;
using Gemini.Modules.UndoRedo;
using Gemini.Modules.UndoRedo.Commands;
using Gemini.Modules.UndoRedo.Services;
using Microsoft.Win32;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Gemini.Framework
{
    public abstract class Document : LayoutItemBase, IDocument,
        ICommandHandler<UndoCommandDefinition>,
        ICommandHandler<RedoCommandDefinition>,
        ICommandHandler<SaveFileCommandDefinition>,
        ICommandHandler<SaveFileAsCommandDefinition>
    {
        private IUndoRedoManager _undoRedoManager;
        public IUndoRedoManager UndoRedoManager
        {
            get { return _undoRedoManager ?? (_undoRedoManager = new UndoRedoManager()); }
        }

        private ICommand _closeCommand;
        public override ICommand CloseCommand
        {
            get { return _closeCommand ?? (_closeCommand = new AsyncCommand(() => TryCloseAsync(null))); }
        }

        private ToolBarDefinition _toolBarDefinition;
        public ToolBarDefinition ToolBarDefinition
        {
            get { return _toolBarDefinition; }
            protected set
            {
                _toolBarDefinition = value;
                NotifyOfPropertyChange(() => ToolBar);
                NotifyOfPropertyChange();
            }
        }

        private IToolBar _toolBar;
        public IToolBar ToolBar
        {
            get
            {
                if (_toolBar != null)
                    return _toolBar;

                if (ToolBarDefinition == null)
                    return null;

                IToolBarBuilder toolBarBuilder = IoC.Get<IToolBarBuilder>();
                _toolBar = new ToolBarModel();
                toolBarBuilder.BuildToolBar(ToolBarDefinition, _toolBar);
                return _toolBar;
            }
        }

        void ICommandHandler<UndoCommandDefinition>.Update(Command command)
        {
            command.Enabled = UndoRedoManager.CanUndo;
        }

        Task ICommandHandler<UndoCommandDefinition>.Run(Command command)
        {
            UndoRedoManager.Undo(1);
            return TaskUtility.Completed;
        }

        void ICommandHandler<RedoCommandDefinition>.Update(Command command)
        {
            command.Enabled = UndoRedoManager.CanRedo;
        }

        Task ICommandHandler<RedoCommandDefinition>.Run(Command command)
        {
            UndoRedoManager.Redo(1);
            return TaskUtility.Completed;
        }

        void ICommandHandler<SaveFileCommandDefinition>.Update(Command command)
        {
            command.Enabled = this is IPersistedDocument;
        }

        async Task ICommandHandler<SaveFileCommandDefinition>.Run(Command command)
        {
            IPersistedDocument persistedDocument = this as IPersistedDocument;
            if (persistedDocument == null)
                return;

            // If file has never been saved, show Save As dialog.
            if (persistedDocument.IsNew)
            {
                await DoSaveAs(persistedDocument);
                return;
            }

            // Save file.
            string filePath = persistedDocument.FilePath;
            await persistedDocument.Save(filePath);
        }

        void ICommandHandler<SaveFileAsCommandDefinition>.Update(Command command)
        {
            command.Enabled = this is IPersistedDocument;
        }

        async Task ICommandHandler<SaveFileAsCommandDefinition>.Run(Command command)
        {
            IPersistedDocument persistedDocument = this as IPersistedDocument;
            if (persistedDocument == null)
                return;

            await DoSaveAs(persistedDocument);
        }

        private static async Task DoSaveAs(IPersistedDocument persistedDocument)
        {
            // Show user dialog to choose filename.
            SaveFileDialog dialog = new SaveFileDialog();
            dialog.FileName = Path.GetFileNameWithoutExtension(persistedDocument.FileName) ?? string.Empty;
            string filter = string.Empty;

            string fileExtension = Path.GetExtension(persistedDocument.FileName);

            List<EditorFileType> fileTypes = IoC.GetAll<IEditorProvider>()
                .Where(e => e.CanCreateNew)
                .SelectMany(x => x.FileTypes).ToList();

            EditorFileType fileType = fileTypes.FirstOrDefault(x => x.FileExtension == fileExtension);

            int filterIndex = 0;
            for (int index = 0; index < fileTypes.Count; index++)
            {
                EditorFileType editorFileType = fileTypes[index];
                filter += editorFileType.Name + "|*" + editorFileType.FileExtension;
                if (index < fileTypes.Count - 1) filter += "|";
                if (editorFileType == fileType)
                {
                    filterIndex = index;
                }
            }

            dialog.Filter = filter;
            dialog.FilterIndex = filterIndex;

            if (dialog.ShowDialog() != true)
                return;

            string filePath = dialog.FileName;

            // Save file.
            await persistedDocument.Save(filePath);
        }
    }
}
