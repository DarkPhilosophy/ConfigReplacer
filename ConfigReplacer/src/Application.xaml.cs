using System;
using System.Windows;
using Common;

namespace ConfigReplacer
{
    /// <summary>
    /// Interaction logic for Application.xaml
    /// </summary>
    public partial class Application : System.Windows.Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                base.OnStartup(e);

                // Log application start
                Common.Logger.Instance.LogInfo("Application starting...", true);

                // Initialize Common components
                Common.Logger.Instance.LogInfo("Initializing language manager...", true);
                Common.LanguageManager.Instance.Initialize("ConfigReplacer");

                // Register common sounds
                Common.Logger.Instance.LogInfo("Registering common sounds...", true);
                Common.SoundPlayer.RegisterCommonSounds("assets/Sounds");

                // Log application start
                Common.Logger.Instance.LogInfo("Application started successfully", true);
            }
            catch (Exception ex)
            {
                // Log the error
                Common.Logger.Instance.LogError($"Error during application startup: {ex.Message}");
                Common.Logger.Instance.LogError($"Stack trace: {ex.StackTrace}");

                // Show error message
                MessageBox.Show($"Error during application startup: {ex.Message}\n\nStack trace: {ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                // Shutdown the application
                Current.Shutdown();
            }
        }
    }
}
