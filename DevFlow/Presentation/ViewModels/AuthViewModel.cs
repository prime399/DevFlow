using DevFlow.Services.Auth;
using Microsoft.UI.Xaml.Media.Imaging;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DevFlow.Presentation.ViewModels;

/// <summary>
/// ViewModel for authentication UI components.
/// Manages sign-in/sign-out state and user profile information.
/// </summary>
public partial class AuthViewModel : INotifyPropertyChanged
{
    private readonly ISupabaseAuthService _authService;
    
    private bool _isLoading;
    private bool _isAuthenticated;
    private string? _displayName;
    private string? _email;
    private string? _avatarUrl;
    private string? _errorMessage;

    public event PropertyChangedEventHandler? PropertyChanged;

    public bool IsLoading
    {
        get => _isLoading;
        private set => SetProperty(ref _isLoading, value);
    }

    public bool IsAuthenticated
    {
        get => _isAuthenticated;
        private set => SetProperty(ref _isAuthenticated, value);
    }

    public string? DisplayName
    {
        get => _displayName;
        private set => SetProperty(ref _displayName, value);
    }

    public string? Email
    {
        get => _email;
        private set => SetProperty(ref _email, value);
    }

    public string? AvatarUrl
    {
        get => _avatarUrl;
        private set => SetProperty(ref _avatarUrl, value);
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        private set => SetProperty(ref _errorMessage, value);
    }

    /// <summary>
    /// Gets the user's initials for display when no avatar is available.
    /// </summary>
    public string Initials
    {
        get
        {
            if (string.IsNullOrEmpty(DisplayName))
                return "?";

            var parts = DisplayName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length >= 2)
                return $"{parts[0][0]}{parts[1][0]}".ToUpperInvariant();
            if (parts.Length == 1 && parts[0].Length > 0)
                return parts[0][0].ToString().ToUpperInvariant();

            return "?";
        }
    }

    public AuthViewModel(ISupabaseAuthService authService)
    {
        _authService = authService;
        _authService.AuthStateChanged += OnAuthStateChanged;
        
        // Initialize from current state
        UpdateFromAuthService();
    }

    private void OnAuthStateChanged(object? sender, AuthState state)
    {
        IsLoading = state == AuthState.Loading;
        ErrorMessage = state == AuthState.Error ? "Authentication failed. Please try again." : null;
        UpdateFromAuthService();
    }

    private void UpdateFromAuthService()
    {
        IsAuthenticated = _authService.IsAuthenticated;
        DisplayName = _authService.DisplayName;
        Email = _authService.Email;
        AvatarUrl = _authService.AvatarUrl;
        OnPropertyChanged(nameof(Initials));
    }

    public async Task SignInWithGoogleAsync()
    {
        ErrorMessage = null;
        await _authService.SignInWithGoogleAsync();
    }

    public async Task SignOutAsync()
    {
        await _authService.SignOutAsync();
    }

    private bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
            return false;

        field = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
