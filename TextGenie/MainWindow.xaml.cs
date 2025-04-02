using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Windows.Controls;
using System.Net.Http;
using TextGenie.Services;

namespace TextGenie
{
    public partial class MainWindow : Window
    {
        private const int HOTKEY_ID = 1;
        private const uint MOD_ALT = 0x0001;
        private const uint VK_T = 0x54;  // Virtual key code for 'T'
        private readonly TextProcessingServiceFactory _serviceFactory;
        private ITextProcessingService? _currentService;
        private bool _isProcessing;

        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        public MainWindow()
        {
            InitializeComponent();
            RegisterGlobalHotkey();
            _serviceFactory = new TextProcessingServiceFactory();
            ApiComboBox.SelectionChanged += ApiComboBox_SelectionChanged;
            
            // Select Ollama by default
            foreach (ComboBoxItem item in ApiComboBox.Items)
            {
                if (item.Content.ToString() == "Ollama")
                {
                    ApiComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void SetProcessingState(bool isProcessing)
        {
            _isProcessing = isProcessing;
            LoadingIndicator.Visibility = isProcessing ? Visibility.Visible : Visibility.Collapsed;
            
            // Disable buttons during processing
            foreach (var button in ButtonPanel.Children.OfType<Button>())
            {
                button.IsEnabled = !isProcessing;
            }
        }

        private async Task ProcessTextAsync(Func<string, Task<string>> operation, string errorMessage)
        {
            if (_currentService == null)
            {
                MessageBox.Show("Please select an API provider first.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var inputText = MainTextBox.Text;
            if (string.IsNullOrWhiteSpace(inputText))
            {
                MessageBox.Show("Please enter some text to process.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var prompt = PromptTextBox.Text;
            if (string.IsNullOrWhiteSpace(prompt))
            {
                MessageBox.Show("Please enter a prompt.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                SetProcessingState(true);
                System.Diagnostics.Debug.WriteLine("Starting text processing...");
                
                var result = await operation(prompt + "\n\n" + inputText);
                System.Diagnostics.Debug.WriteLine($"Received result: {result}");
                
                // Ensure we're on the UI thread when updating the text
                await Dispatcher.InvokeAsync(() =>
                {
                    System.Diagnostics.Debug.WriteLine("Updating text box on UI thread");
                    MainTextBox.Text = result;
                    System.Diagnostics.Debug.WriteLine("Text box updated");
                });
            }
            catch (HttpRequestException ex)
            {
                System.Diagnostics.Debug.WriteLine($"HTTP Error: {ex.Message}");
                MessageBox.Show($"{errorMessage}\n\nError: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"General Error: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack Trace: {ex.StackTrace}");
                MessageBox.Show($"{errorMessage}\n\nError: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                SetProcessingState(false);
            }
        }

        private async void ProcessButton_Click(object sender, RoutedEventArgs e)
        {
            await ProcessTextAsync(text => _currentService!.RewriteTextAsync(text), "Error processing text");
        }

        private void ApiComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ApiComboBox.SelectedItem is ComboBoxItem selectedItem)
            {
                try
                {
                    _currentService = _serviceFactory.GetService(selectedItem.Content.ToString() ?? "Ollama");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error initializing service: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void RegisterGlobalHotkey()
        {
            var helper = new WindowInteropHelper(this);
            RegisterHotKey(helper.Handle, HOTKEY_ID, MOD_ALT, VK_T);
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            HwndSource.FromHwnd(new WindowInteropHelper(this).Handle)?.AddHook(HwndHook);
        }

        private IntPtr HwndHook(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == 0x0312 && wParam.ToInt32() == HOTKEY_ID)
            {
                ToggleWindow();
                handled = true;
            }
            return IntPtr.Zero;
        }

        private void ToggleWindow()
        {
            if (IsVisible)
            {
                Hide();
            }
            else
            {
                Show();
                Activate();
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            UnregisterGlobalHotkey();
            Close();
        }

        private void UnregisterGlobalHotkey()
        {
            var helper = new WindowInteropHelper(this);
            UnregisterHotKey(helper.Handle, HOTKEY_ID);
        }

        private async void TestApiButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentService == null)
            {
                MessageBox.Show("Please select an API provider first.", "Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var testText = "Hello, this is a test message.";
                var result = await _currentService.RewriteTextAsync(testText);
                MessageBox.Show($"API Test Successful!\n\nResponse: {result}", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (HttpRequestException ex)
            {
                var errorMessage = $"API Test Failed!\n\nHTTP Error: {ex.Message}\n\n";
                if (ex.InnerException != null)
                {
                    errorMessage += $"Inner Exception: {ex.InnerException.Message}\n\n";
                }
                MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                var errorMessage = $"API Test Failed!\n\nError Type: {ex.GetType().Name}\n";
                errorMessage += $"Error Message: {ex.Message}\n\n";
                if (ex.InnerException != null)
                {
                    errorMessage += $"Inner Exception: {ex.InnerException.Message}\n\n";
                }
                MessageBox.Show(errorMessage, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
} 