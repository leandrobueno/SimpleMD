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

        /// <summary>
        /// Gets the current main window.
        /// </summary>
        public static MainWindow? MainWindow { get; private set; }

        /// <summary>
        /// Initializes the singleton application object.  This is the first line of authored code
        /// executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            InitializeComponent();
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
            _window = new MainWindow();
            MainWindow = _window as MainWindow;

            // If we have a file to open, notify the main window
            if (!string.IsNullOrEmpty(_launchFilePath) && MainWindow != null)
            {
                MainWindow.SetInitialFile(_launchFilePath);
            }

            _window.Activate();
        }


    }
}
