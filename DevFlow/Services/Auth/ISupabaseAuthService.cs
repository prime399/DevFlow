using Supabase.Gotrue;

namespace DevFlow.Services.Auth;

/// <summary>
/// Interface for Supabase authentication operations.
/// Provides optional authentication - users can access the app without signing in,
/// but certain features like cloud sync require authentication.
/// </summary>
public interface ISupabaseAuthService
{
    /// <summary>
    /// Gets the currently authenticated user, or null if not signed in.
    /// </summary>
    User? CurrentUser { get; }

    /// <summary>
    /// Gets whether a user is currently authenticated.
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    /// Gets the user's display name (from Google profile).
    /// </summary>
    string? DisplayName { get; }

    /// <summary>
    /// Gets the user's email address.
    /// </summary>
    string? Email { get; }

    /// <summary>
    /// Gets the user's avatar URL (from Google profile).
    /// </summary>
    string? AvatarUrl { get; }

    /// <summary>
    /// Event raised when the authentication state changes.
    /// </summary>
    event EventHandler<AuthState>? AuthStateChanged;

    /// <summary>
    /// Initializes the auth service and loads any persisted session.
    /// </summary>
    Task InitializeAsync();

    /// <summary>
    /// Initiates Google OAuth sign-in flow.
    /// </summary>
    /// <returns>True if sign-in was successful, false otherwise.</returns>
    Task<bool> SignInWithGoogleAsync();

    /// <summary>
    /// Sets the session from an OAuth callback URL containing tokens in the fragment.
    /// </summary>
    /// <param name="callbackUrl">The full callback URL with tokens (e.g., http://localhost:3000/#access_token=...)</param>
    /// <returns>True if session was set successfully.</returns>
    Task<bool> SetSessionFromUrlAsync(string callbackUrl);

    /// <summary>
    /// Handles the OAuth callback by exchanging the authorization code for a session (PKCE flow).
    /// </summary>
    /// <param name="code">The authorization code from the OAuth callback.</param>
    /// <returns>True if session was obtained successfully.</returns>
    Task<bool> HandleOAuthCallbackAsync(string code);

    /// <summary>
    /// Signs out the current user.
    /// </summary>
    Task SignOutAsync();
}

/// <summary>
/// Represents the current authentication state.
/// </summary>
public enum AuthState
{
    /// <summary>
    /// No user is signed in.
    /// </summary>
    SignedOut,

    /// <summary>
    /// A user is currently signed in.
    /// </summary>
    SignedIn,

    /// <summary>
    /// Authentication is in progress.
    /// </summary>
    Loading,

    /// <summary>
    /// An error occurred during authentication.
    /// </summary>
    Error
}
