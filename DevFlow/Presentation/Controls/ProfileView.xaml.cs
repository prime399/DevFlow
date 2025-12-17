using DevFlow.Presentation.ViewModels;
using DevFlow.Services.Auth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;

namespace DevFlow.Presentation.Controls;

/// <summary>
/// Profile view for managing user authentication and account settings.
/// </summary>
public sealed partial class ProfileView : UserControl
{
    private AuthViewModel? _viewModel;
    private ISupabaseAuthService? _authService;

    public ProfileView()
    {
        this.InitializeComponent();
        this.Loaded += ProfileView_Loaded;
    }

    private async void ProfileView_Loaded(object sender, RoutedEventArgs e)
    {
        Console.WriteLine("[ProfileView] Loaded event fired");
        try
        {
            var app = Application.Current as App;
            
            // Wait for Host to be available (it's set after NavigateAsync completes)
            int retries = 0;
            while (app?.Host?.Services == null && retries < 10)
            {
                Console.WriteLine($"[ProfileView] Waiting for Host... (attempt {retries + 1})");
                await Task.Delay(100);
                retries++;
            }
            
            if (app?.Host?.Services != null)
            {
                _authService = app.Host.Services.GetService<ISupabaseAuthService>();
                Console.WriteLine($"[ProfileView] AuthService found: {_authService != null}");
                
                if (_authService != null)
                {
                    await _authService.InitializeAsync();
                    
                    _viewModel = new AuthViewModel(_authService);
                    _viewModel.PropertyChanged += ViewModel_PropertyChanged;
                    
                    UpdateUI();
                    Console.WriteLine("[ProfileView] Initialized successfully");
                }
            }
            else
            {
                Console.WriteLine("[ProfileView] ERROR: Host not available after retries");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[ProfileView] ERROR: {ex.Message}");
        }
    }

    private void ViewModel_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        DispatcherQueue.TryEnqueue(() => UpdateUI());
    }

    private void UpdateUI()
    {
        if (_viewModel == null) return;

        // Toggle panels based on auth state
        SignedOutPanel.Visibility = _viewModel.IsAuthenticated ? Visibility.Collapsed : Visibility.Visible;
        SignedInPanel.Visibility = _viewModel.IsAuthenticated ? Visibility.Visible : Visibility.Collapsed;

        // Update loading state
        LoadingOverlay.Visibility = _viewModel.IsLoading ? Visibility.Visible : Visibility.Collapsed;

        // Hide callback input when authenticated
        CallbackInputSection.Visibility = _viewModel.IsAuthenticated ? Visibility.Collapsed : CallbackInputSection.Visibility;

        // Update profile info
        if (_viewModel.IsAuthenticated)
        {
            ProfileDisplayName.Text = _viewModel.DisplayName ?? "User";
            ProfileEmail.Text = _viewModel.Email ?? "";
            ProfileInitials.Text = _viewModel.Initials;

            // Update avatar
            if (!string.IsNullOrEmpty(_viewModel.AvatarUrl))
            {
                try
                {
                    ProfileAvatarBrush.ImageSource = new BitmapImage(new Uri(_viewModel.AvatarUrl));
                    ProfileAvatarEllipse.Visibility = Visibility.Visible;
                }
                catch
                {
                    ProfileAvatarEllipse.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                ProfileAvatarEllipse.Visibility = Visibility.Collapsed;
            }
        }
    }

    private async void GoogleSignInButton_Click(object sender, RoutedEventArgs e)
    {
        Console.WriteLine("[ProfileView] GoogleSignInButton clicked!");
        if (_viewModel != null)
        {
            Console.WriteLine("[ProfileView] Calling SignInWithGoogleAsync...");
            await _viewModel.SignInWithGoogleAsync();
            Console.WriteLine("[ProfileView] SignInWithGoogleAsync completed");
            
            // Show the callback input section after initiating OAuth (as fallback)
            CallbackInputSection.Visibility = Visibility.Visible;
        }
        else
        {
            Console.WriteLine("[ProfileView] ERROR: ViewModel is null!");
        }
    }

    private async void SubmitCallbackButton_Click(object sender, RoutedEventArgs e)
    {
        Console.WriteLine("[ProfileView] SubmitCallbackButton clicked!");
        
        var callbackUrl = CallbackUrlTextBox.Text?.Trim();
        
        if (string.IsNullOrEmpty(callbackUrl))
        {
            Console.WriteLine("[ProfileView] Callback URL is empty");
            return;
        }

        if (_authService != null)
        {
            Console.WriteLine("[ProfileView] Setting session from URL...");
            var success = await _authService.SetSessionFromUrlAsync(callbackUrl);
            Console.WriteLine($"[ProfileView] SetSessionFromUrlAsync result: {success}");
            
            if (success)
            {
                CallbackUrlTextBox.Text = "";
                CallbackInputSection.Visibility = Visibility.Collapsed;
            }
        }
    }

    private async void SignOutButton_Click(object sender, RoutedEventArgs e)
    {
        Console.WriteLine("[ProfileView] SignOutButton clicked");
        if (_viewModel != null)
        {
            await _viewModel.SignOutAsync();
        }
    }
}
