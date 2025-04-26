using System;
using System.Windows;
using Common;

namespace ConfigReplacer
{
    public partial class WelcomeScreen : Window
    {
        private AppConfig _config;

        public WelcomeScreen()
        {
            InitializeComponent();
            _config = AppConfig.Load();
            
            // Set the checkbox based on the config
            chkShowOnStartup.IsChecked = _config.ShowWelcomeScreen;
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            // Save the checkbox state to config
            _config.ShowWelcomeScreen = chkShowOnStartup.IsChecked ?? true;
            _config.Save();
            
            this.Close();
        }
    }
}
