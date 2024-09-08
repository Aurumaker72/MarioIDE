using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.ComponentModel.Composition.ReflectionModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using Caliburn.Micro;
using Gemini.Framework.Attributes;
using Gemini.Framework.Services;
using Gemini.Properties;

namespace Gemini
{
    public class AppBootstrapper : BootstrapperBase
    {
        private List<Assembly> _priorityAssemblies;

        public static CompositionContainer Container { get; set; }

        internal IList<Assembly> PriorityAssemblies
            => _priorityAssemblies;

        /// <summary>
        /// Override this to true if your main application properly handles PublishSingleFile cases.
        /// Otherwise, an exception in <see cref="Configure"/> is thrown if Gemini detects
        /// telltale signs of an PublishSingleFile environment under .NET5+.
        /// </summary>
        public virtual bool IsPublishSingleFileHandled => false;

        [DllImport("kernel32")]
        private static extern bool AllocConsole();
        
        public AppBootstrapper()
        {
            Func<Type, DependencyObject, object, Type> existingViewLocator = ViewLocator.LocateTypeForModelType;
            ViewLocator.LocateTypeForModelType = (modelType, displayLocation, context) =>
            {
                Type targetType = existingViewLocator(modelType, displayLocation, context);

                if (targetType == null && modelType.GetCustomAttributes<UseViewAttribute>().Any())
                {
                    UseViewAttribute attribute = modelType.GetCustomAttribute<UseViewAttribute>();
                    targetType = attribute?.ViewType;
                }

                if (targetType == null && modelType.GetCustomAttributes<UseViewOfAttribute>().Any())
                {
                    UseViewOfAttribute attribute = modelType.GetCustomAttribute<UseViewOfAttribute>();
                    targetType = existingViewLocator(attribute?.SelectedType, displayLocation, context);
                }

                return targetType;
            };

            PreInitialize();
            Initialize();
        }

        protected virtual void PreInitialize()
        {
            string code = Settings.Default.LanguageCode;

            if (!string.IsNullOrWhiteSpace(code))
            {
                CultureInfo culture = CultureInfo.GetCultureInfo(code);
                Thread.CurrentThread.CurrentUICulture = culture;
                Thread.CurrentThread.CurrentCulture = culture;
            }
        }

        /// <summary>
        /// By default, we are configured to use MEF
        /// </summary>
        protected override void Configure()
        {
            if (CheckIfGeminiAppearsPublishedToSingleFile())
            {
                if (!IsPublishSingleFileHandled)
                {
                    const string fullMethodName =
                        nameof(Gemini) + "." +
                        nameof(AppBootstrapper) + "." +
                        nameof(Configure);

                    string exceptionMessage =
                        "Gemini appears to be loaded by a .NET5+ app that was deployed with PublishSingleFile (.pubxml), or possibly loaded from memory. " +
                        $"Set {nameof(IsPublishSingleFileHandled)} to true if you expect this and are handling it in your app. " +
                        $"Otherwise, {fullMethodName} and MEF may not find your assemblies with exports.";

                    // Need to show a message, else the program dies without any information to the user
                    if (!Debugger.IsAttached)
                    {
                        MessageBox.Show(exceptionMessage, "GeminiWpf");
                    }

                    InvalidOperationException exception = new InvalidOperationException(exceptionMessage)
                    {
                        HelpLink = "https://docs.microsoft.com/en-us/dotnet/core/deploying/single-file",
                    };

                    throw exception;
                }

                // First, add the assemblies which DirectoryCatalog can't find because they are embedded in the app.
                // Unlike the "directoryCatalog.Parts" LINQ below, we don't try to filter out duplicate assemblies here.
                AssemblySource.Instance.AddRange(PublishSingleFileBypassAssemblies);
            }

            // If these paths are different, it suggests this is a .netcoreapp3.1 PublishSingleFile,
            // which extracts files to the Temp directory (AppContext.BaseDirectory).
            // In .NET5+, the files are NOT extracted, unless IncludeAllContentForSelfExtract is set in the .pubxml.
            // See https://docs.microsoft.com/en-us/dotnet/core/deploying/single-file#other-considerations
            string currentWorkingDir = Path.GetDirectoryName(Path.GetFullPath(@"./"));
            string baseDirectory = Path.GetDirectoryName(Path.GetFullPath(AppContext.BaseDirectory));

            // Add all assemblies to AssemblySource (using a temporary DirectoryCatalog).
            PopulateAssemblySourceUsingDirectoryCatalog(currentWorkingDir);
            if (currentWorkingDir != baseDirectory)
            {
                PopulateAssemblySourceUsingDirectoryCatalog(baseDirectory);
            }

            // Prioritise the executable assembly. This allows the client project to override exports, including IShell.
            // The client project can override SelectAssemblies to choose which assemblies are prioritised.
            _priorityAssemblies = SelectAssemblies().ToList();
            AggregateCatalog priorityCatalog = new AggregateCatalog(_priorityAssemblies.Select(x => new AssemblyCatalog(x)));
            CatalogExportProvider priorityProvider = new CatalogExportProvider(priorityCatalog);

            // Now get all other assemblies (excluding the priority assemblies).
            AggregateCatalog mainCatalog = new AggregateCatalog(
                AssemblySource.Instance
                    .Where(assembly => !_priorityAssemblies.Contains(assembly))
                    .Select(x => new AssemblyCatalog(x)));
            CatalogExportProvider mainProvider = new CatalogExportProvider(mainCatalog);

            Container = new CompositionContainer(priorityProvider, mainProvider);
            priorityProvider.SourceProvider = Container;
            mainProvider.SourceProvider = Container;

            CompositionBatch batch = new CompositionBatch();

            BindServices(batch);
            batch.AddExportedValue(mainCatalog);

            Container.Compose(batch);
        }

        protected void PopulateAssemblySourceUsingDirectoryCatalog(string path)
        {
            DirectoryCatalog directoryCatalog = new DirectoryCatalog(path);
            AssemblySource.Instance.AddRange(
                directoryCatalog.Parts
                    .Select(part => ReflectionModelServices.GetPartType(part).Value.Assembly)
                    .Where(assembly => !AssemblySource.Instance.Contains(assembly)));
        }

        /// <summary>
        /// Does a best-guess check to determine if Gemini was deployed under an app with
        /// a PublishSingleFile configuration in .NET5+ environments.
        /// </summary>
        /// <returns></returns>
        protected virtual bool CheckIfGeminiAppearsPublishedToSingleFile()
        {
            Assembly geminiAssembly = Assembly.GetAssembly(typeof(AppBootstrapper));
            // https://github.com/dotnet/runtime/issues/36590 "Support Single-File Apps in .NET 5"
            // https://github.com/dotnet/designs/blob/main/accepted/2020/single-file/design.md#assemblylocation
            // "Proposed solution is for Assembly.Location to return the empty-string for bundled assemblies, which is the default behavior for assemblies loaded from memory."
            // https://docs.microsoft.com/en-us/dotnet/core/deploying/single-file#api-incompatibility
            // I suppose it may be possible that some .NET obfuscators and "protectors" would lead to this behavior, so this should
            return geminiAssembly.Location == string.Empty;
        }

        /// <summary>
        /// When your application is deployed using PublishSingleFile under .NET5+, override
        /// this to explicitly list the assemblies that MEF needs to search exports for.
        /// </summary>
        protected virtual IEnumerable<Assembly> PublishSingleFileBypassAssemblies => Enumerable.Empty<Assembly>();

        protected virtual void BindServices(CompositionBatch batch)
        {
            batch.AddExportedValue<IWindowManager>(new WindowManager());
            batch.AddExportedValue<IEventAggregator>(new EventAggregator());
            batch.AddExportedValue(Container);
            batch.AddExportedValue(this);
        }

        private static object Lock { get; } = new();

        protected override object GetInstance(Type serviceType, string key)
        {
            Monitor.Enter(Lock);

            string contract = string.IsNullOrEmpty(key) ? AttributedModelServices.GetContractName(serviceType) : key;
            Lazy<object>[] exports = Container.GetExports<object>(contract).ToArray();

            if (!exports.Any())
            {
                throw new Exception($"Could not locate any instances of contract {contract}.");
            }

            object result = exports.First().Value;

            Monitor.Exit(Lock);

            return result;
        }

        protected override IEnumerable<object> GetAllInstances(Type serviceType)
        {
            Monitor.Enter(Lock);

            IEnumerable<object> result = Container.GetExportedValues<object>(AttributedModelServices.GetContractName(serviceType)).ToArray();

            Monitor.Exit(Lock);

            return result;
        }

        protected override void BuildUp(object instance)
        {
            Monitor.Enter(Lock);

            Container.SatisfyImportsOnce(instance);

            Monitor.Exit(Lock);
        }

        protected override void OnStartup(object sender, StartupEventArgs e)
        {
            base.OnStartup(sender, e);
            DisplayRootViewFor<IMainWindow>();
        }

        protected override IEnumerable<Assembly> SelectAssemblies() => new[] { Assembly.GetEntryAssembly() };
    }
}
