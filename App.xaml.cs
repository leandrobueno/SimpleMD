using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Shapes;
using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SimpleMD.Helpers;
using SimpleMD.Services;
using SimpleMD.ViewModels;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace SimpleMD
{
    /// <summary>
    /// Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    public partial class App : Application
    {
        private Window? _window;
        private string? _launchFilePath;
        private IHost? _host;

        /// <summary>
        /// Gets the current main window.
        /// </summary>
        public static MainWindow? MainWindow { get; private set; }
        
        /// <summary>
        /// Gets the current App instance.
        /// </summary>
        public static new App Current => (App)Application.Current;
        
        /// <summary>
        /// Gets the service provider.
        /// </summary>
        public IServiceProvider Services => _host?.Services ?? throw new InvalidOperationException("Services not initialized");

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            try
            {
                // Log startup information for diagnostics
                LogStartupInfo();
                
                InitializeComponent();
                
                // Build the host
                _host = Host.CreateDefaultBuilder()
                    .ConfigureServices((context, services) =>
                    {
                        // Add our services
                        services.AddSimpleMdServices();
                    })
                    .Build();
            }
            catch (Exception ex)
            {
                // Log the error for debugging
                System.Diagnostics.Debug.WriteLine($"App initialization error: {ex}");
                
                // Create a minimal host to prevent crashes
                _host = new HostBuilder().Build();
            }
        }

        /// <summary>
        /// Invoked when the application is launched.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            // Check if we're being activated with a file
            var activatedEventArgs = Microsoft.Windows.AppLifecycle.AppInstance.GetCurrent().GetActivatedEventArgs();

            if (activatedEventArgs != null && activatedEventArgs.Kind == Microsoft.Windows.AppLifecycle.ExtendedActivationKind.File)
            {
                var fileArgs = activatedEventArgs.Data as Windows.ApplicationModel.Activation.FileActivatedEventArgs;
                if (fileArgs?.Files.Count > 0)
                {
                    var file = fileArgs.Files[0] as StorageFile;
                    _launchFilePath = file?.Path;
                }
            }

            CreateAndActivateMainWindow();
        }

        private void CreateAndActivateMainWindow()
        {
            try
            {
                // Get MainWindow from DI container
                _window = Services.GetRequiredService<MainWindow>();
                MainWindow = _window as MainWindow;
                
                // Initialize services that need the window
                if (MainWindow != null)
                {
                    var dialogService = Services.GetRequiredService<IDialogService>() as DialogService;
                    dialogService?.SetXamlRoot(MainWindow.Content.XamlRoot);
                }
                
                var themeService = Services.GetRequiredService<IThemeService>();
                themeService.Initialize(_window);

                _window.Activate();

                // If we have a file to open, set it after activation and ensure WebView is ready
                if (!string.IsNullOrEmpty(_launchFilePath) && MainWindow != null)
                {
                    MainWindow.SetInitialFile(_launchFilePath);
                }
            }
            catch (Exception ex)
            {
                // If DI fails, create window manually as a fallback
                System.Diagnostics.Debug.WriteLine($"Window creation error: {ex}");
                
                try
                {
                    // Create services manually
                    var markdownService = new MarkdownService();
                    var themeService = new ThemeService();
                    var fileService = new FileService();
                    var dialogService = new DialogService();
                    
                    // Create view model
                    var viewModel = new MainViewModel(markdownService, themeService, fileService, dialogService);
                    
                    // Create window
                    _window = new MainWindow(viewModel, dialogService);
                    MainWindow = _window as MainWindow;
                    
                    // Initialize theme
                    themeService.Initialize(_window);
                    
                    _window.Activate();
                }
                catch
                {
                    // Last resort - create a basic error window
                    _window = new Window();
                    _window.Content = new TextBlock 
                    { 
                        Text = "SimpleMD failed to initialize properly. Please ensure Windows App Runtime is installed.",
                        HorizontalAlignment = HorizontalAlignment.Center,
                        VerticalAlignment = VerticalAlignment.Center
                    };
                    _window.Activate();
                }
            }
        }
        
        /// <summary>
        /// Gets a service of the specified type.
        /// </summary>
        public T GetService<T>() where T : class
        {
            return Services.GetRequiredService<T>();
        }
        
        private void LogStartupInfo()
        {
            try
            {
                var logPath = System.IO.Path.Combine(ApplicationData.Current.LocalFolder.Path, "startup.log");
                var info = new System.Text.StringBuilder();
                info.AppendLine($"SimpleMD Startup Log - {DateTime.Now}");
                info.AppendLine($"OS Version: {Environment.OSVersion}");
                info.AppendLine($".NET Version: {Environment.Version}");
                info.AppendLine($"App Version: 1.0.4.0");
                info.AppendLine($"Working Directory: {Environment.CurrentDirectory}");
                info.AppendLine($"App Location: {System.Reflection.Assembly.GetExecutingAssembly().Location}");
                
                System.IO.File.WriteAllText(logPath, info.ToString());
            }
            catch
            {
                // Ignore logging errors
            }
        }
    }
}
