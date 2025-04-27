using System;
using System.IO;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using System.Collections.Generic;
using System.Windows.Controls;
using System.Windows.Media.Animation;
using System.Linq;
using Common;

namespace ConfigReplacer
{
    public partial class MainWindow : Window
    {
        // Configuration
#if NET6_0_OR_GREATER
        private AppConfig _config = null!;
#else
        private AppConfig _config;
#endif

        // File paths stored in config
        private List<string> _configFilePaths = new List<string>();

        // Replacement strings stored in config
        private string BERString = string.Empty;
        private string SCHString = string.Empty;

        // Button colors - reference from AnimationManager
        private static readonly SolidColorBrush BlueColor = AnimationManager.BlueColor;
        private static readonly SolidColorBrush RedColor = AnimationManager.RedColor;
        private static readonly SolidColorBrush YellowColor = AnimationManager.YellowColor;
        private static readonly SolidColorBrush GrayColor = AnimationManager.GrayColor;
        private static readonly SolidColorBrush GreenColor = AnimationManager.GreenColor;

        // File status
        private enum FileStatus
        {
            Unknown,
            ContainsBER,
            ContainsSCH,
            NotFound,
            Error
        }

        private Dictionary<string, FileStatus> _fileStatuses = new Dictionary<string, FileStatus>();

        // Cache UI element references for better performance
#if NET6_0_OR_GREATER
        private Button? _btnReplaceBERtoSCH;
        private Button? _btnReplaceSCHtoBER;
        private TextBox? _txtLog;
        private ScrollViewer? _logScrollViewer;
#else
        private Button _btnReplaceBERtoSCH;
        private Button _btnReplaceSCHtoBER;
        private TextBox _txtLog;
        private ScrollViewer _logScrollViewer;
#endif

        // We'll use the Tag property to track which buttons are red

        public MainWindow()
        {
            try
            {
                // Log initialization
                Common.Logger.Instance.LogInfo("Initializing MainWindow...", true);

                InitializeComponent();
                Loaded += MainWindow_Loaded;

                // Load configuration
                Common.Logger.Instance.LogInfo("Loading configuration...", true);
                _config = AppConfig.Load();
                _configFilePaths = _config.ConfigFilePaths;
                BERString = _config.OldString;
                SCHString = _config.NewString;
                Common.Logger.Instance.LogInfo($"Configuration loaded: {_configFilePaths.Count} file paths", true);

                // Initialize language
                Common.Logger.Instance.LogInfo("Initializing language...", true);
                InitializeLanguage();
                Common.Logger.Instance.LogInfo($"Language initialized: {_config.Language}", true);

                // Event handlers will be added in MainWindow_Loaded
                Common.Logger.Instance.LogInfo("MainWindow constructor completed successfully", true);
            }
            catch (Exception ex)
            {
                // Log the error
                Common.Logger.Instance.LogError($"Error in MainWindow constructor: {ex.Message}");
                Common.Logger.Instance.LogError($"Stack trace: {ex.StackTrace}");

                // Show error message
                MessageBox.Show($"Error initializing application: {ex.Message}\n\nStack trace: {ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);

                // Shutdown the application
                Application.Current.Shutdown();
            }
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                // Log initialization
                Common.Logger.Instance.LogInfo("MainWindow_Loaded started...", true);

                // Initialize UI element references
                Common.Logger.Instance.LogInfo("Initializing UI element references...", true);
                _btnReplaceBERtoSCH = (Button)FindName("btnReplaceBERtoSCH");
                _btnReplaceSCHtoBER = (Button)FindName("btnReplaceSCHtoBER");
                _txtLog = (TextBox)FindName("txtLog");
                _logScrollViewer = (ScrollViewer)FindName("logScrollViewer");
                Common.Logger.Instance.LogInfo("UI element references initialized successfully", true);

            // Initialize the Logger's OnLogMessage event
            Common.Logger.Instance.OnLogMessage += (message, isError, isWarning, isSuccess, isInfo, consoleOnly) =>
            {
                if (!consoleOnly)
                {
                    // Update the UI log
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (_txtLog != null)
                        {
                            _txtLog.Text += message + Environment.NewLine;
                            _logScrollViewer?.ScrollToEnd();
                        }
                    });
                }
            };

            // Center the window horizontally on the screen and add a small gap at the top
            this.Left = (SystemParameters.PrimaryScreenWidth - this.Width) / 2;
            this.Top = 20; // 20 pixels gap from the top of the screen

            // Wait for the visual tree to be fully loaded
            await Task.Delay(100);

            // Add event handlers for red button hover behavior
            if (_btnReplaceBERtoSCH != null && _btnReplaceSCHtoBER != null)
            {
                _btnReplaceBERtoSCH.MouseEnter += RedButton_MouseEnter;
                _btnReplaceBERtoSCH.MouseLeave += RedButton_MouseLeave;
                _btnReplaceBERtoSCH.PreviewMouseDown += Button_PreviewMouseDown;
                // Add event handlers for blue button hover behavior
                _btnReplaceBERtoSCH.MouseEnter += BlueButton_MouseEnter;
                _btnReplaceBERtoSCH.MouseLeave += BlueButton_MouseLeave;

                _btnReplaceSCHtoBER.MouseEnter += RedButton_MouseEnter;
                _btnReplaceSCHtoBER.MouseLeave += RedButton_MouseLeave;
                _btnReplaceSCHtoBER.PreviewMouseDown += Button_PreviewMouseDown;
                // Add event handlers for blue button hover behavior
                _btnReplaceSCHtoBER.MouseEnter += BlueButton_MouseEnter;
                _btnReplaceSCHtoBER.MouseLeave += BlueButton_MouseLeave;
            }

            // Try to find the elements using different methods
            var txtAdBanner = (TextBlock)FindName("txtAdBanner");
            var adContainer = (Grid)FindName("adContainer");
            var adBannerContainer = (Border)FindName("adBannerContainer");

            // If elements are not found, create them manually
            if (txtAdBanner == null || adContainer == null || adBannerContainer == null)
            {
                LogMessage("Ad elements not found in XAML, creating them manually", isInfo: true);

                // Find the main grid to add our elements to
                var mainGrid = (Grid)FindName("MainGrid");
                if (mainGrid == null)
                {
                    // Try to find the main grid in the visual tree
                    mainGrid = FindElementByName<Grid>(this, "MainGrid");

                    // If still not found, use the root grid
                    if (mainGrid == null)
                    {
                        // Get the root grid (first grid in the window)
                        mainGrid = this.Content as Grid;
                        if (mainGrid == null)
                        {
                            LogMessage("Cannot find main grid to add ad elements", isError: true);
                            return;
                        }
                    }
                }

                // Create the ad banner container if it doesn't exist
                if (adBannerContainer == null)
                {
                    adBannerContainer = new Border
                    {
                        Name = "adBannerContainer",
                        Background = new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                        Padding = new Thickness(10, 5, 10, 5),
                        Margin = new Thickness(0, 0, 0, 5),
                        BorderThickness = new Thickness(0, 0, 0, 1),
                        BorderBrush = new SolidColorBrush(Color.FromRgb(221, 221, 221)),
                        Visibility = Visibility.Visible
                    };

                    // Add it to the main grid in row 4 (footer)
                    Grid.SetRow(adBannerContainer, 4);
                    mainGrid.Children.Add(adBannerContainer);
                    LogMessage("Created adBannerContainer and added to main grid", isInfo: true);
                }

                // Create the text ad banner if it doesn't exist
                if (txtAdBanner == null)
                {
                    txtAdBanner = new TextBlock
                    {
                        Name = "txtAdBanner",
                        FontSize = 16,
                        FontWeight = FontWeights.SemiBold,
                        VerticalAlignment = VerticalAlignment.Center,
                        HorizontalAlignment = HorizontalAlignment.Left,
                        Padding = new Thickness(0),
                        Margin = new Thickness(0),
                        Foreground = new SolidColorBrush(Color.FromRgb(51, 51, 51)),
                        TextWrapping = TextWrapping.NoWrap,
                        Height = 22,
                        Visibility = Visibility.Visible,
                        Text = "Loading ads...",
                        RenderTransform = new TranslateTransform(0, 0)
                    };

                    // Add it to the ad banner container
                    adBannerContainer.Child = txtAdBanner;
                    LogMessage("Created txtAdBanner and added to adBannerContainer", isInfo: true);
                }

                // Create the image ad container if it doesn't exist
                if (adContainer == null)
                {
                    adContainer = new Grid
                    {
                        Name = "adContainer",
                        Margin = new Thickness(0, 0, 0, 3),
                        Height = 65,
                        VerticalAlignment = VerticalAlignment.Bottom,
                        Visibility = Visibility.Collapsed
                    };

                    // Add it to the main grid in row 4 (footer)
                    Grid.SetRow(adContainer, 4);
                    mainGrid.Children.Add(adContainer);
                    LogMessage("Created adContainer and added to main grid", isInfo: true);
                }
            }

            if (txtAdBanner != null && adContainer != null)
            {
                // Don't create ads directory - only use it if it already exists
                // Make sure to use consoleOnly=true for ad-related messages
                LogMessage("Using network paths for ads, local paths only as fallback if they exist", consoleOnly: true);

                // Initialize the ad system asynchronously to prevent UI freezing
                await Task.Run(() => {
                    // Initialize the UniversalAdLoader with the log callback
                    UniversalAdLoader.Instance.Initialize(LogMessage);
                });

                // Initialize the AdManager on the UI thread
                Common.AdManager.Instance.Initialize(txtAdBanner, adContainer, LogMessage, UniversalAdLoader.Instance);

                // Set the initial language for the AdManager
                Common.AdManager.Instance.SwitchLanguage(_config.Language);

                LogMessage("Ad system initialized asynchronously", consoleOnly: true);
            }
            else
            {
                LogMessage("Failed to initialize ad system: UI elements not found", isError: true);
            }

            // Update UI with config file paths
            Common.Logger.Instance.LogInfo("Updating file paths display...", true);
            UpdateFilePathsDisplay();

            // Check status of all files
            Common.Logger.Instance.LogInfo("Checking files status...", true);
            await CheckFilesStatus();

            // Display the default greeting message
            if (_config.Language == "Romanian")
            {
                LogMessage("Gata pentru procesarea fișierelor. Faceți clic pe unul dintre butoanele de mai jos pentru a începe.");
            }
            else
            {
                LogMessage("Ready to process files. Click one of the buttons below to start.");
            }

            Common.Logger.Instance.LogInfo("MainWindow_Loaded completed successfully", true);
            }
            catch (Exception ex)
            {
                // Log the error
                Common.Logger.Instance.LogError($"Error in MainWindow_Loaded: {ex.Message}");
                Common.Logger.Instance.LogError($"Stack trace: {ex.StackTrace}");

                // Show error message
                MessageBox.Show($"Error loading application: {ex.Message}\n\nStack trace: {ex.StackTrace}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void InitializeLanguage()
        {
            // Initialize the language manager with the saved language
            Common.LanguageManager.Instance.LoadLanguageFromConfig(_config.Language);
        }

        private void btnSwitchLanguage_Click(object sender, RoutedEventArgs e)
        {
            // Play button click sound
            Common.SoundPlayer.PlayButtonClickSound();

            // Get the next language
            string nextLanguage = Common.LanguageManager.Instance.GetNextLanguage(_config.Language);

            // Switch to the new language
            Common.LanguageManager.Instance.SwitchLanguage(nextLanguage);

            // Update the config
            _config.Language = nextLanguage;
            _config.Save();

            // Log the language change
            string langChangedMsg = FindResource("LanguageChanged")?.ToString() ?? $"Language changed to {nextLanguage}";
            LogMessage(string.Format(langChangedMsg, nextLanguage), isInfo: true);
        }

        private void UpdateFilePathsDisplay()
        {
            // Find the file info stack panel
            var fileInfoStackPanel = (StackPanel)FindName("fileInfoPanel");
            if (fileInfoStackPanel == null) return;

            // Get the file info container
            var fileInfoContainer = fileInfoStackPanel.Children.OfType<Border>().FirstOrDefault();
            if (fileInfoContainer == null) return;

            // Get the stack panel inside the border
            var fileListPanel = fileInfoContainer.Child as StackPanel;
            if (fileListPanel == null) return;

            // Clear existing items
            fileListPanel.Children.Clear();

            // Add each file path
            for (int i = 0; i < _configFilePaths.Count; i++)
            {
                var textBlock = new TextBlock
                {
                    Text = $"{i + 1}. {_configFilePaths[i]}",
                    TextWrapping = TextWrapping.Wrap,
                    Margin = new Thickness(0, i > 0 ? 5 : 0, 0, 0)
                };

                fileListPanel.Children.Add(textBlock);
            }
        }

        private async void btnReplaceBERtoSCH_Click(object sender, RoutedEventArgs e)
        {
            // Play button click sound
            SoundPlayer.PlayButtonClickSound();

            await ReplaceInFiles(BERString, SCHString);
            await CheckFilesStatus();
        }

        private async void btnReplaceSCHtoBER_Click(object sender, RoutedEventArgs e)
        {
            // Play button click sound
            SoundPlayer.PlayButtonClickSound();

            await ReplaceInFiles(SCHString, BERString);
            await CheckFilesStatus();
        }

        private async Task CheckFilesStatus()
        {
            _fileStatuses.Clear();

            // Check all config files
            foreach (var filePath in _configFilePaths)
            {
                var status = await CheckFileStatus(filePath);
                _fileStatuses[filePath] = status;
                LogMessage($"File {filePath} status: {status}", consoleOnly: true);
            }

            // Update UI based on file statuses
            UpdateButtonColors();
            UpdateStatusDisplay();
        }

#if NET6_0_OR_GREATER
        private async Task<FileStatus> CheckFileStatus(string filePath)
#else
        private Task<FileStatus> CheckFileStatus(string filePath)
#endif
        {
            try
            {
                if (!File.Exists(filePath))
                {
#if NET6_0_OR_GREATER
                    return FileStatus.NotFound;
#else
                    return Task.FromResult(FileStatus.NotFound);
#endif
                }

#if NET6_0_OR_GREATER
                string content = await File.ReadAllTextAsync(filePath);
#else
                string content = File.ReadAllText(filePath);
#endif

                if (content.Contains(BERString))
                {
#if NET6_0_OR_GREATER
                    return FileStatus.ContainsBER;
#else
                    return Task.FromResult(FileStatus.ContainsBER);
#endif
                }
                else if (content.Contains(SCHString))
                {
#if NET6_0_OR_GREATER
                    return FileStatus.ContainsSCH;
#else
                    return Task.FromResult(FileStatus.ContainsSCH);
#endif
                }
                else
                {
#if NET6_0_OR_GREATER
                    return FileStatus.Unknown;
#else
                    return Task.FromResult(FileStatus.Unknown);
#endif
                }
            }
            catch (Exception)
            {
#if NET6_0_OR_GREATER
                return FileStatus.Error;
#else
                return Task.FromResult(FileStatus.Error);
#endif
            }
        }

        // Event handlers for red button hover behavior
        private void RedButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Button button && button.Tag as string == "RedButton")
            {
                // Store the fact that this button is in "disabled visual state" in a custom property
                button.SetValue(IsRedButtonHoveredProperty, true);

                // Start the corruption animation when mouse enters
                AnimationManager.Instance.StartRedButtonCorruptionAnimation(button);
            }
        }

        private void RedButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Button button && button.Tag as string == "RedButton")
            {
                // Clear the custom property
                button.SetValue(IsRedButtonHoveredProperty, false);

                // Stop the corruption animation when mouse leaves
                // This will restore the button's original appearance but keep the red glow
                AnimationManager.Instance.StopRedButtonCorruptionAnimation(button);

                // Make sure the button is red
                button.Background = RedColor;

                // Ensure the red glow is still applied
                AnimationManager.Instance.ApplyRedButtonGlow(button);
            }
        }

        // Custom attached property to track if a button should be pulsing
        public static readonly DependencyProperty ShouldPulseProperty =
            DependencyProperty.RegisterAttached(
                "ShouldPulse",
                typeof(bool),
                typeof(MainWindow),
                new PropertyMetadata(false));

        public static void SetShouldPulse(UIElement element, bool value)
        {
            element.SetValue(ShouldPulseProperty, value);
        }

        public static bool GetShouldPulse(UIElement element)
        {
            return (bool)element.GetValue(ShouldPulseProperty);
        }

        // Event handlers for blue button hover behavior
        private void BlueButton_MouseEnter(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (sender is Button button && button.Tag as string == "BlueButton")
            {
                // Mark that this button should be pulsing when mouse leaves
                SetShouldPulse(button, true);

                // Stop any active animation
                if (Common.AnimationManager.Instance._activeAnimations.TryGetValue(button, out var animation))
                {
                    try
                    {
                        // Handle different animation types
                        if (animation is System.Windows.Media.Animation.Storyboard storyboard)
                        {
                            storyboard.Stop();
                        }
                        else if (animation is System.Windows.Threading.DispatcherTimer timer)
                        {
                            timer.Stop();
                        }
                        Common.AnimationManager.Instance._activeAnimations.Remove(button);
                        LogMessage("Stopped pulsing animation on mouse enter", consoleOnly: true);
                    }
                    catch (Exception ex)
                    {
                        LogMessage($"Error stopping animation: {ex.Message}", isError: true, consoleOnly: true);
                    }
                }

                // Let the XAML HoverAnimation take over
                // The animation will make the button slightly larger (1.05 scale)
                // When the mouse leaves, we'll restart our custom pulsing animation
            }
        }

        private void BlueButton_MouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            // Check if this is a blue button by looking at the background color
            if (sender is Button button)
            {
                LogMessage($"BlueButton_MouseLeave called for button with Tag={button.Tag}", consoleOnly: true);

                // Check if this is a blue button by looking at the background color
                bool isBlueButton = false;

                if (button.Tag as string == "BlueButton")
                {
                    isBlueButton = true;
                }
                else if (button.Background is SolidColorBrush brush &&
                         brush.Color.R == BlueColor.Color.R &&
                         brush.Color.G == BlueColor.Color.G &&
                         brush.Color.B == BlueColor.Color.B)
                {
                    isBlueButton = true;
                    // If it has the blue color but not the tag, set the tag
                    button.Tag = "BlueButton";
                    LogMessage("Button has blue color but no BlueButton tag, setting tag", consoleOnly: true);
                }

                if (isBlueButton)
                {
                    // Mark that this button should be pulsing
                    SetShouldPulse(button, true);
                    LogMessage($"SetShouldPulse set to true for button", consoleOnly: true);

                    // Capture the current button reference to use in the task
                    Button capturedButton = button;

                    // Wait for the UnhoverAnimation to complete before starting our pulsing animation
                    // This ensures the button is back to its normal size before we start pulsing
                    LogMessage("Starting delay before restarting animation", consoleOnly: true);
                    Task.Delay(150).ContinueWith(_ =>
                    {
                        LogMessage("Delay completed, invoking on UI thread", consoleOnly: true);
                        // We need to use the Dispatcher to update UI from a different thread
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            // Force the tag to be BlueButton
                            capturedButton.Tag = "BlueButton";
                            LogMessage($"Now on UI thread, forcing button Tag={capturedButton.Tag}", consoleOnly: true);

                            LogMessage("Restarting animation", consoleOnly: true);
                            try
                            {
                                // Use the ForceRestartPulsingAnimation method to restart the pulsing animation
                                Common.AnimationManager.Instance.ForceRestartPulsingAnimation(capturedButton);
                                LogMessage("Restarted pulsing animation using ForceRestartPulsingAnimation", consoleOnly: true);
                            }
                            catch (Exception ex)
                            {
                                LogMessage($"Error restarting pulsing animation: {ex.Message}", isError: true, consoleOnly: true);
                            }
                        });
                    });
                }
            }
        }

        // Custom attached property to track if a red button is being hovered
        public static readonly DependencyProperty IsRedButtonHoveredProperty =
            DependencyProperty.RegisterAttached(
                "IsRedButtonHovered",
                typeof(bool),
                typeof(MainWindow),
                new PropertyMetadata(false));

        public static void SetIsRedButtonHovered(UIElement element, bool value)
        {
            element.SetValue(IsRedButtonHoveredProperty, value);
        }

        public static bool GetIsRedButtonHovered(UIElement element)
        {
            return (bool)element.GetValue(IsRedButtonHoveredProperty);
        }

        // Event handler to prevent clicks on red buttons
        private void Button_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is Button button && button.Tag as string == "RedButton")
            {
                // Prevent the click by marking the event as handled
                e.Handled = true;
            }
        }

        private void UpdateButtonColors()
        {
            // Default to green for both buttons until configuration is detected
            SolidColorBrush berToSchColor = GreenColor;
            SolidColorBrush schToBerColor = GreenColor;
            bool enableButtons = true;

            // Check if files have different statuses
            bool hasMixedStatus = false;
            bool hasFileNotFound = false;
            bool hasError = false;
            bool allBER = true;
            bool allSCH = true;

            foreach (var status in _fileStatuses.Values)
            {
                if (status != FileStatus.ContainsBER)
                {
                    allBER = false;
                }

                if (status != FileStatus.ContainsSCH)
                {
                    allSCH = false;
                }

                if (status == FileStatus.Unknown)
                {
                    hasMixedStatus = true;
                }

                if (status == FileStatus.NotFound)
                {
                    hasFileNotFound = true;
                }

                if (status == FileStatus.Error)
                {
                    hasError = true;
                }
            }

            // Get references to the buttons if not already cached
            if (_btnReplaceBERtoSCH == null || _btnReplaceSCHtoBER == null)
            {
                _btnReplaceBERtoSCH = (Button)FindName("btnReplaceBERtoSCH");
                _btnReplaceSCHtoBER = (Button)FindName("btnReplaceSCHtoBER");

                if (_btnReplaceBERtoSCH == null || _btnReplaceSCHtoBER == null)
                {
                    return; // Can't find buttons
                }
            }

            // Stop any existing animations
            StopPulsingBorderAnimation(_btnReplaceBERtoSCH);
            StopPulsingBorderAnimation(_btnReplaceSCHtoBER);

            // No need to reset tags as we're using styles

            // If any file is not found or has an error, disable both buttons
            if (hasFileNotFound || hasError)
            {
                berToSchColor = GrayColor;
                schToBerColor = GrayColor;
                enableButtons = false;
                LogMessage(FindResource("ButtonsDisabled")?.ToString() ?? "One or more files not found or have errors. Buttons disabled.", isWarning: true);
            }
            // If one file has BER and one has SCH
            else if (!allBER && !allSCH && !hasMixedStatus)
            {
                berToSchColor = YellowColor;
                schToBerColor = YellowColor;
            }
            // If all files have BER
            else if (allBER)
            {
                berToSchColor = BlueColor; // Enable BER to SCH
                schToBerColor = RedColor;  // Disable SCH to BER

                // Red button style will be applied later

                // Start pulsing animation on the active button
                StartPulsingBorderAnimation(_btnReplaceBERtoSCH);
            }
            // If all files have SCH
            else if (allSCH)
            {
                berToSchColor = RedColor;  // Disable BER to SCH
                schToBerColor = BlueColor; // Enable SCH to BER

                // Red button style will be applied later

                // Start pulsing animation on the active button
                StartPulsingBorderAnimation(_btnReplaceSCHtoBER);
            }

            // Don't reset button tags here as it can interfere with animations
            // We'll set them appropriately below

            // Update button colors and enabled state
            _btnReplaceBERtoSCH.Background = berToSchColor;
            _btnReplaceSCHtoBER.Background = schToBerColor;
            _btnReplaceBERtoSCH.IsEnabled = enableButtons;
            _btnReplaceSCHtoBER.IsEnabled = enableButtons;

            // Mark blue buttons with a tag and apply blue glow
            if (berToSchColor == BlueColor)
            {
                _btnReplaceBERtoSCH.Tag = "BlueButton";
                _btnReplaceBERtoSCH.Background = BlueColor;

                // Mark that this button should pulse
                SetShouldPulse(_btnReplaceBERtoSCH, true);

                // Always apply the blue glow to blue buttons
                AnimationManager.Instance.ApplyBlueButtonGlow(_btnReplaceBERtoSCH);

                LogMessage("Marked btnReplaceBERtoSCH as BlueButton", consoleOnly: true);
            }
            else
            {
                // Make sure we don't pulse non-blue buttons
                SetShouldPulse(_btnReplaceBERtoSCH, false);
            }

            if (schToBerColor == BlueColor)
            {
                _btnReplaceSCHtoBER.Tag = "BlueButton";
                _btnReplaceSCHtoBER.Background = BlueColor;

                // Mark that this button should pulse
                SetShouldPulse(_btnReplaceSCHtoBER, true);

                // Always apply the blue glow to blue buttons
                Common.AnimationManager.Instance.ApplyBlueButtonGlow(_btnReplaceSCHtoBER);

                LogMessage("Marked btnReplaceSCHtoBER as BlueButton", consoleOnly: true);
            }
            else
            {
                // Make sure we don't pulse non-blue buttons
                SetShouldPulse(_btnReplaceSCHtoBER, false);
            }

            // Mark red buttons with a tag and always start the red glow animation
            if (berToSchColor == RedColor)
            {
                _btnReplaceBERtoSCH.Tag = "RedButton";
                _btnReplaceBERtoSCH.Background = RedColor;

                // Always apply the red glow to red buttons
                Common.AnimationManager.Instance.ApplyRedButtonGlow(_btnReplaceBERtoSCH);

                // Only start the corruption animation if the mouse is already over the button
                if (_btnReplaceBERtoSCH.IsMouseOver)
                {
                    Common.AnimationManager.Instance.StartRedButtonCorruptionAnimation(_btnReplaceBERtoSCH);
                }
            }
            else
            {
                // Stop any corruption animation if the button is no longer red
                Common.AnimationManager.Instance.StopRedButtonCorruptionAnimation(_btnReplaceBERtoSCH);

                // Only change the tag if it's not already a BlueButton
                if (_btnReplaceBERtoSCH.Tag as string != "BlueButton")
                {
                    _btnReplaceBERtoSCH.Tag = null;
                }

                // Restore the normal background color
                _btnReplaceBERtoSCH.Background = berToSchColor;
            }

            if (schToBerColor == RedColor)
            {
                _btnReplaceSCHtoBER.Tag = "RedButton";
                _btnReplaceSCHtoBER.Background = RedColor;

                // Always apply the red glow to red buttons
                Common.AnimationManager.Instance.ApplyRedButtonGlow(_btnReplaceSCHtoBER);

                // Only start the corruption animation if the mouse is already over the button
                if (_btnReplaceSCHtoBER.IsMouseOver)
                {
                    Common.AnimationManager.Instance.StartRedButtonCorruptionAnimation(_btnReplaceSCHtoBER);
                }
            }
            else
            {
                // Stop any corruption animation if the button is no longer red
                Common.AnimationManager.Instance.StopRedButtonCorruptionAnimation(_btnReplaceSCHtoBER);
                _btnReplaceSCHtoBER.Tag = null;

                // Restore the normal background color
                _btnReplaceSCHtoBER.Background = schToBerColor;
            }
        }

        private void StartPulsingBorderAnimation(Button button)
        {
            LogMessage($"StartPulsingBorderAnimation called for button with Tag={button.Tag}", consoleOnly: true);
            // For blue buttons, use ForceRestartPulsingAnimation
            if (button.Tag as string == "BlueButton")
            {
                try
                {
                    // Mark that this button should be pulsing
                    SetShouldPulse(button, true);

                    // Use the ForceRestartPulsingAnimation method to start the pulsing animation
                    Common.AnimationManager.Instance.ForceRestartPulsingAnimation(button);
                    LogMessage("Started pulsing animation using ForceRestartPulsingAnimation", consoleOnly: true);
                }
                catch (Exception ex)
                {
                    LogMessage($"Error starting size pulsing animation: {ex.Message}", isError: true, consoleOnly: true);
                    // Fall back to the old method
                    Common.AnimationManager.Instance.StartPulsingBorderAnimation(button, Resources);
                }
            }
            else
            {
                // For other buttons, use the original method
                Common.AnimationManager.Instance.StartPulsingBorderAnimation(button, Resources);
            }
        }

        private void StopPulsingBorderAnimation(Button button)
        {
            Common.AnimationManager.Instance.StopPulsingBorderAnimation(button);
        }

        // Helper method to find all named elements in the visual tree
        private void FindAllNamedElements(DependencyObject parent)
        {
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is FrameworkElement element && !string.IsNullOrEmpty(element.Name))
                {
                    LogMessage($"Found element: {element.Name} (Type: {element.GetType().Name})", isInfo: true);
                }

                FindAllNamedElements(child);
            }
        }

        // Helper method to find an element by name in the visual tree
#if NET6_0_OR_GREATER
        private T? FindElementByName<T>(DependencyObject parent, string name) where T : FrameworkElement
#else
        private T FindElementByName<T>(DependencyObject parent, string name) where T : FrameworkElement
#endif
        {
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T element && element.Name == name)
                {
                    return element;
                }

                var result = FindElementByName<T>(child, name);
                if (result != null)
                {
                    return result;
                }
            }

            return null;
        }

        private void UpdateStatusDisplay()
        {
            // Find the file info stack panel
            var fileInfoStackPanel = (StackPanel)FindName("fileInfoPanel");
            if (fileInfoStackPanel == null)
            {
                // If we can't find the panel, we'll just log the status instead
                LogMessage("File Status: " + GetStatusSummary(), isInfo: true);
                return;
            }

            // Get the file info container
            var fileInfoContainer = fileInfoStackPanel.Children.OfType<Border>().FirstOrDefault();
            if (fileInfoContainer == null)
            {
                LogMessage("Could not find file info container", isError: true);
                return;
            }

            // Get the stack panel inside the border
            var fileListPanel = fileInfoContainer.Child as StackPanel;
            if (fileListPanel == null)
            {
                LogMessage("Could not find file list panel", isError: true);
                return;
            }

            // Clear existing items
            fileListPanel.Children.Clear();

            // Add file items with status
            for (int i = 0; i < _configFilePaths.Count; i++)
            {
                string filePath = _configFilePaths[i];
                AddFileWithStatus(fileListPanel, $"{i + 1}. {filePath}",
                    _fileStatuses.ContainsKey(filePath) ? _fileStatuses[filePath] : FileStatus.Unknown);
            }
        }

        private void AddFileWithStatus(StackPanel panel, string filePathText, FileStatus status)
        {
            // Create a grid for the file item
            var grid = new Grid();
            grid.Margin = new Thickness(0, 0, 0, panel.Children.Count == 0 ? 5 : 0);
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            // Add the file path
            var pathText = new TextBlock
            {
                Text = filePathText,
                TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(pathText, 0);

            // Add the status indicator
            var statusText = new TextBlock
            {
                Text = " → " + GetStatusText(status),
                FontWeight = FontWeights.Bold,
                Foreground = GetStatusColor(status),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(5, 0, 0, 0)
            };
            Grid.SetColumn(statusText, 1);

            // Add the elements to the grid
            grid.Children.Add(pathText);
            grid.Children.Add(statusText);

            // Add the grid to the panel
            panel.Children.Add(grid);
        }

        private string GetStatusText(FileStatus status)
        {
            switch (status)
            {
                case FileStatus.ContainsBER:
                    return "FFTesterBER";
                case FileStatus.ContainsSCH:
                    return "FFTesterSCH";
                case FileStatus.NotFound:
                    return "File Not Found";
                case FileStatus.Error:
                    return "Error Reading File";
                case FileStatus.Unknown:
                default:
                    return "Unknown Status";
            }
        }

        private SolidColorBrush GetStatusColor(FileStatus status)
        {
            switch (status)
            {
                case FileStatus.ContainsBER:
                    return new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)); // Green
                case FileStatus.ContainsSCH:
                    return new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)); // Green
                case FileStatus.NotFound:
                case FileStatus.Error:
                    return new SolidColorBrush(Color.FromRgb(0xF4, 0x43, 0x36)); // Red
                case FileStatus.Unknown:
                default:
                    return new SolidColorBrush(Color.FromRgb(0x75, 0x75, 0x75)); // Gray
            }
        }

        private string GetStatusSummary()
        {
            int berCount = 0;
            int schCount = 0;
            int errorCount = 0;

            foreach (var status in _fileStatuses.Values)
            {
                if (status == FileStatus.ContainsBER)
                    berCount++;
                else if (status == FileStatus.ContainsSCH)
                    schCount++;
                else if (status == FileStatus.Error || status == FileStatus.NotFound)
                    errorCount++;
            }

            string statusSummaryMsg = FindResource("FileStatusSummary")?.ToString() ?? $"{berCount} files with BER, {schCount} files with SCH, {errorCount} files with errors";
            return string.Format(statusSummaryMsg, berCount, schCount, errorCount);
        }

        private async Task ReplaceInFiles(string oldValue, string newValue)
        {
            // Get references to the buttons if not already cached
            if (_btnReplaceBERtoSCH == null || _btnReplaceSCHtoBER == null)
            {
                _btnReplaceBERtoSCH = (Button)FindName("btnReplaceBERtoSCH");
                _btnReplaceSCHtoBER = (Button)FindName("btnReplaceSCHtoBER");

                if (_btnReplaceBERtoSCH == null || _btnReplaceSCHtoBER == null)
                {
                    return; // Can't find buttons
                }
            }

            // Disable buttons during processing
            _btnReplaceBERtoSCH.IsEnabled = false;
            _btnReplaceSCHtoBER.IsEnabled = false;

            try
            {
                ClearLog();
                string startingMsg = FindResource("StartingReplacement")?.ToString() ?? $"Starting replacement of '{oldValue}' with '{newValue}'...";
                LogMessage(string.Format(startingMsg, oldValue, newValue));

                int totalReplacements = 0;

                // Process all files from config
                foreach (var filePath in _configFilePaths)
                {
                    int fileReplacements = await ProcessFile(filePath, oldValue, newValue);
                    totalReplacements += fileReplacements;
                }

                // Show summary
                string completedMsg = FindResource("CompletedReplacements")?.ToString() ?? $"Completed! Total replacements made: {totalReplacements}";
                LogMessage(string.Format(completedMsg, totalReplacements), isSuccess: true);
            }
            catch (Exception ex)
            {
                string errorMsg = FindResource("ErrorMessage")?.ToString() ?? $"Error: {ex.Message}";
                LogMessage(string.Format(errorMsg, ex.Message), isError: true);
            }
            finally
            {
                // Re-enable buttons
                _btnReplaceBERtoSCH.IsEnabled = true;
                _btnReplaceSCHtoBER.IsEnabled = true;
            }
        }

#if NET6_0_OR_GREATER
        private async Task<int> ProcessFile(string filePath, string oldValue, string newValue)
#else
        private Task<int> ProcessFile(string filePath, string oldValue, string newValue)
#endif
        {
            string processingMsg = FindResource("ProcessingFile")?.ToString() ?? $"Processing file: {filePath}";
            LogMessage(string.Format(processingMsg, filePath));

            try
            {
                // Check if file exists
                if (!File.Exists(filePath))
                {
                    string notFoundMsg = FindResource("FileNotFound")?.ToString() ?? $"File not found: {filePath}";
                    LogMessage(string.Format(notFoundMsg, filePath), isError: true);
#if NET6_0_OR_GREATER
                    return 0;
#else
                    return Task.FromResult(0);
#endif
                }

                // Read file content
#if NET6_0_OR_GREATER
                string content = await File.ReadAllTextAsync(filePath);
#else
                string content = File.ReadAllText(filePath);
#endif
                string readSuccessMsg = FindResource("FileReadSuccess")?.ToString() ?? "File read successfully.";
                LogMessage(readSuccessMsg);

                // Validate JSON
                try
                {
                    JObject.Parse(content);
                    string jsonValidMsg = FindResource("JsonValidationSuccess")?.ToString() ?? "JSON validation successful.";
                    LogMessage(jsonValidMsg);
                }
                catch (Newtonsoft.Json.JsonException)
                {
                    string invalidJsonMsg = FindResource("InvalidJsonWarning")?.ToString() ?? "Warning: File contains invalid JSON. Proceeding with text replacement only.";
                    LogMessage(invalidJsonMsg, isWarning: true);
                }

                // Count occurrences before replacement
                int count = CountOccurrences(content, oldValue);

                if (count == 0)
                {
                    string noOccurrencesMsg = FindResource("NoOccurrencesFound")?.ToString() ?? $"No occurrences of '{oldValue}' found in this file.";
                    LogMessage(string.Format(noOccurrencesMsg, oldValue));
#if NET6_0_OR_GREATER
                    return 0;
#else
                    return Task.FromResult(0);
#endif
                }

                // Perform replacement
                string newContent = content.Replace(oldValue, newValue);

                // Write back to file
#if NET6_0_OR_GREATER
                await File.WriteAllTextAsync(filePath, newContent);
#else
                File.WriteAllText(filePath, newContent);
#endif

                string replacedMsg = FindResource("ReplacedOccurrences")?.ToString() ?? $"Replaced {count} occurrence(s) of '{oldValue}' with '{newValue}'";
                LogMessage(string.Format(replacedMsg, count, oldValue, newValue), isSuccess: true);
#if NET6_0_OR_GREATER
                return count;
#else
                return Task.FromResult(count);
#endif
            }
            catch (Exception ex)
            {
                string errorProcessingMsg = FindResource("ErrorProcessing")?.ToString() ?? $"Error processing {filePath}: {ex.Message}";
                LogMessage(string.Format(errorProcessingMsg, filePath, ex.Message), isError: true);
                throw;
            }
        }

        private int CountOccurrences(string text, string pattern)
        {
            int count = 0;
            int index = 0;

            while ((index = text.IndexOf(pattern, index, StringComparison.Ordinal)) != -1)
            {
                count++;
                index += pattern.Length;
            }

            return count;
        }

        private void ClearLog()
        {
            // Get the log text box if not already cached
            if (_txtLog == null)
            {
                _txtLog = (TextBox)FindName("txtLog");
                if (_txtLog == null) return;
            }

            _txtLog.Text = string.Empty;
        }

        private bool _loggerSubscribed = false;



        private void LogMessage(string message, bool isError = false, bool isWarning = false, bool isSuccess = false, bool isInfo = false, bool consoleOnly = false)
        {
            // Use the Common Logger
            Common.Logger.Instance.LogMessage(message, isError, isWarning, isSuccess, isInfo, consoleOnly);

            // Subscribe to the Logger's OnLogMessage event if we haven't already
            if (!_loggerSubscribed)
            {
                Common.Logger.Instance.OnLogMessage += (formattedMessage, error, warning, success, info, console) => {
                    if (!console)
                    {
                        // Get the log text box if not already cached
                        if (_txtLog == null)
                        {
                            _txtLog = (TextBox)FindName("txtLog");
                            if (_txtLog == null) return;
                        }

                        // Get the log scroll viewer if not already cached
                        if (_logScrollViewer == null)
                        {
                            _logScrollViewer = (ScrollViewer)FindName("logScrollViewer");
                        }

                        // Clear the log and rebuild it from the buffer to ensure consistent formatting
                        _txtLog.Clear();

                        // Get the log buffer from the Logger and display it
                        string logBuffer = Common.Logger.Instance.GetLogBuffer();
                        _txtLog.Text = logBuffer.TrimEnd(); // Remove any trailing newlines

                        // Scroll to the end
                        _txtLog.ScrollToEnd();

                        // Ensure the ScrollViewer scrolls to the end as well
                        if (_logScrollViewer != null)
                        {
                            _logScrollViewer.ScrollToEnd();

                            // Force UI update to ensure scrolling happens immediately
                            _logScrollViewer.UpdateLayout();
                        }
                    }
                };
                _loggerSubscribed = true;
            }
        }
    }
}
