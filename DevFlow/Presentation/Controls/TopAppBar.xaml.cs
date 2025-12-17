using DevFlow.Presentation.ViewModels;
using DevFlow.Services.Auth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace DevFlow.Presentation.Controls;

/// <summary>
/// Top app bar control with search and profile sections.
/// </summary>
public sealed partial class TopAppBar : UserControl
{
    private AuthViewModel? _viewModel;

    /// <summary>
    /// Event raised when the profile menu item is clicked.
    /// </summary>
    public event EventHandler? ProfileRequested;

    public TopAppBar()
    {
        this.InitializeComponent();
        this.Loaded += TopAppBar_Loaded;
        Console.WriteLine("[TopAppBar] Constructor called");
    }

    private async void TopAppBar_Loaded(object sender, RoutedEventArgs e)
    {
        Console.WriteLine("[TopAppBar] Loaded event fired");
        try
        {
            // Get the auth service from DI - may need to wait for Host to be available
            var app = Application.Current as App;
            
            // Wait for Host to be available (it's set after NavigateAsync completes)
            int retries = 0;
            while (app?.Host?.Services == null && retries < 10)
            {
                Console.WriteLine($"[TopAppBar] Waiting for Host... (attempt {retries + 1})");
                await Task.Delay(100);
                retries++;
            }
            
            Console.WriteLine($"[TopAppBar] App={app != null}, Host={app?.Host != null}, Services={app?.Host?.Services != null}");
            
            if (app?.Host?.Services != null)
            {
                var authService = app.Host.Services.GetService<ISupabaseAuthService>();
                Console.WriteLine($"[TopAppBar] AuthService found: {authService != null}");
                
                if (authService != null)
                {
                    Console.WriteLine("[TopAppBar] Initializing auth service...");
                    await authService.InitializeAsync();
                    
                    _viewModel = new AuthViewModel(authService);
                    _viewModel.PropertyChanged += ViewModel_PropertyChanged;
                    Console.WriteLine($"[TopAppBar] ViewModel created, IsAuthenticated={_viewModel.IsAuthenticated}");
                    
                    // Update UI from initial state
                    UpdateUI();
                    Console.WriteLine("[TopAppBar] UI updated");
                }
                else
                {
                    Console.WriteLine("[TopAppBar] ERROR: AuthService is null!");
                }
            }
            else
            {
                Console.WriteLine("[TopAppBar] ERROR: Host or Services not available after retries!");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[TopAppBar] ERROR: {ex.Message}");
            Console.WriteLine($"[TopAppBar] StackTrace: {ex.StackTrace}");
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        // Ensure UI updates happen on the UI thread
        DispatcherQueue.TryEnqueue(() => UpdateUI());
    }

    private void UpdateUI()
    {
        if (_viewModel == null) return;

        // Toggle visibility based on auth state
        SignInButton.Visibility = _viewModel.IsAuthenticated ? Visibility.Collapsed : Visibility.Visible;
        ProfileButton.Visibility = _viewModel.IsAuthenticated ? Visibility.Visible : Visibility.Collapsed;
        
        // Update loading state
        LoadingRing.IsActive = _viewModel.IsLoading;
        LoadingRing.Visibility = _viewModel.IsLoading ? Visibility.Visible : Visibility.Collapsed;
        
        if (_viewModel.IsLoading)
        {
            SignInButton.Visibility = Visibility.Collapsed;
        }

        // Update profile info
        if (_viewModel.IsAuthenticated)
        {
            UserNameText.Text = _viewModel.DisplayName ?? "User";
            InitialsText.Text = _viewModel.Initials;

            // Update avatar if available
            if (!string.IsNullOrEmpty(_viewModel.AvatarUrl))
            {
                try
                {
                    AvatarImageBrush.ImageSource = new BitmapImage(new Uri(_viewModel.AvatarUrl));
                    AvatarEllipse.Visibility = Visibility.Visible;
                }
                catch
                {
                    AvatarEllipse.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                AvatarEllipse.Visibility = Visibility.Collapsed;
            }
        }
    }

    private async void SignInButton_Click(object sender, RoutedEventArgs e)
    {
        Console.WriteLine("[TopAppBar] SignInButton clicked!");
        if (_viewModel != null)
        {
            Console.WriteLine("[TopAppBar] Calling SignInWithGoogleAsync...");
            await _viewModel.SignInWithGoogleAsync();
            Console.WriteLine("[TopAppBar] SignInWithGoogleAsync completed");
        }
        else
        {
            Console.WriteLine("[TopAppBar] ERROR: ViewModel is null!");
        }
    }

    private void ProfileButton_Click(object sender, RoutedEventArgs e)
    {
        // The flyout will automatically show
    }

    private void ProfileMenuItem_Click(object sender, RoutedEventArgs e)
    {
        ProfileRequested?.Invoke(this, EventArgs.Empty);
    }

    private async void SignOutMenuItem_Click(object sender, RoutedEventArgs e)
    {
        Console.WriteLine("[TopAppBar] SignOut clicked");
        if (_viewModel != null)
        {
            await _viewModel.SignOutAsync();
        }
    }
}
