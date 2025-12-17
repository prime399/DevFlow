using Microsoft.Extensions.Configuration;
using Supabase;
using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;
using GotrueConstants = Supabase.Gotrue.Constants;

namespace DevFlow.Services.Auth;

/// <summary>
/// Supabase authentication service implementation with Google OAuth support.
/// </summary>
public class SupabaseAuthService : ISupabaseAuthService
{
    private readonly IConfiguration _configuration;
    private Supabase.Client? _supabaseClient;
    private bool _isInitialized;
    
    // Store the PKCE verifier for completing the OAuth flow
    private string? _pkceVerifier;

    public User? CurrentUser => _supabaseClient?.Auth?.CurrentUser;
    public bool IsAuthenticated => CurrentUser != null;
    
    public string? DisplayName => CurrentUser?.UserMetadata?.TryGetValue("full_name", out var name) == true 
        ? name?.ToString() 
        : CurrentUser?.UserMetadata?.TryGetValue("name", out var n) == true 
            ? n?.ToString() 
            : null;

    public string? Email => CurrentUser?.Email;

    public string? AvatarUrl => CurrentUser?.UserMetadata?.TryGetValue("avatar_url", out var url) == true 
        ? url?.ToString() 
        : CurrentUser?.UserMetadata?.TryGetValue("picture", out var pic) == true 
            ? pic?.ToString() 
            : null;

    public event EventHandler<AuthState>? AuthStateChanged;

    public SupabaseAuthService(IConfiguration configuration)
    {
        _configuration = configuration;
        Console.WriteLine("[SupabaseAuth] Constructor called");
    }

    public async Task InitializeAsync()
    {
        Console.WriteLine($"[SupabaseAuth] InitializeAsync called, already initialized: {_isInitialized}");
        
        if (_isInitialized) return;

        var supabaseUrl = _configuration["Supabase:Url"];
        var supabaseKey = _configuration["Supabase:AnonKey"];

        Console.WriteLine($"[SupabaseAuth] URL: {supabaseUrl?.Substring(0, Math.Min(40, supabaseUrl?.Length ?? 0))}...");
        Console.WriteLine($"[SupabaseAuth] Key: {(string.IsNullOrEmpty(supabaseKey) ? "EMPTY" : "SET")}");

        if (string.IsNullOrEmpty(supabaseUrl) || supabaseUrl == "YOUR_SUPABASE_PROJECT_URL" ||
            string.IsNullOrEmpty(supabaseKey) || supabaseKey == "YOUR_SUPABASE_ANON_KEY")
        {
            Console.WriteLine("[SupabaseAuth] ERROR: Configuration not set. Authentication disabled.");
            _isInitialized = true;
            return;
        }

        try
        {
            Console.WriteLine("[SupabaseAuth] Creating Supabase client...");
            
            var options = new SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = false
            };

            _supabaseClient = new Supabase.Client(supabaseUrl, supabaseKey, options);
            
            Console.WriteLine("[SupabaseAuth] Setting up persistence...");
            _supabaseClient.Auth.SetPersistence(new SessionPersistence());
            
            // Listen for auth state changes
            _supabaseClient.Auth.AddStateChangedListener((sender, state) =>
            {
                Console.WriteLine($"[SupabaseAuth] Auth state changed to: {state}");
                var authState = state switch
                {
                    GotrueConstants.AuthState.SignedIn => AuthState.SignedIn,
                    GotrueConstants.AuthState.SignedOut => AuthState.SignedOut,
                    _ => AuthState.SignedOut
                };
                AuthStateChanged?.Invoke(this, authState);
            });

            // Load existing session
            _supabaseClient.Auth.LoadSession();

            Console.WriteLine("[SupabaseAuth] Initializing async...");
            await _supabaseClient.InitializeAsync();

            _isInitialized = true;
            Console.WriteLine($"[SupabaseAuth] Initialized! IsAuthenticated: {IsAuthenticated}");
            
            AuthStateChanged?.Invoke(this, IsAuthenticated ? AuthState.SignedIn : AuthState.SignedOut);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SupabaseAuth] ERROR during init: {ex.Message}");
            Console.WriteLine($"[SupabaseAuth] StackTrace: {ex.StackTrace}");
            AuthStateChanged?.Invoke(this, AuthState.Error);
        }
    }

    public async Task<bool> SignInWithGoogleAsync()
    {
        Console.WriteLine("[SupabaseAuth] SignInWithGoogleAsync called");
        
        if (_supabaseClient?.Auth == null)
        {
            Console.WriteLine("[SupabaseAuth] ERROR: Client not initialized!");
            AuthStateChanged?.Invoke(this, AuthState.Error);
            return false;
        }

        try
        {
            AuthStateChanged?.Invoke(this, AuthState.Loading);
            
            // Use PKCE flow with custom redirect URL for desktop apps
            var redirectUrl = "devflow://auth/callback";
            Console.WriteLine($"[SupabaseAuth] Using redirect URL: {redirectUrl}");

            var signInOptions = new SignInOptions
            {
                FlowType = GotrueConstants.OAuthFlowType.PKCE,
                RedirectTo = redirectUrl
            };

            var providerAuthState = await _supabaseClient.Auth.SignIn(GotrueConstants.Provider.Google, signInOptions);
            
            // Store PKCE verifier for later code exchange
            _pkceVerifier = providerAuthState?.PKCEVerifier;
            Console.WriteLine($"[SupabaseAuth] PKCE verifier stored: {!string.IsNullOrEmpty(_pkceVerifier)}");

            Console.WriteLine($"[SupabaseAuth] Provider auth state received");
            Console.WriteLine($"[SupabaseAuth] URI: {providerAuthState?.Uri}");

            if (providerAuthState?.Uri != null)
            {
                var oauthUrl = providerAuthState.Uri.ToString();
                Console.WriteLine($"[SupabaseAuth] Opening browser: {oauthUrl.Substring(0, Math.Min(100, oauthUrl.Length))}...");
                
                try
                {
                    // Try Windows.System.Launcher first
                    var launched = await Windows.System.Launcher.LaunchUriAsync(new Uri(oauthUrl));
                    Console.WriteLine($"[SupabaseAuth] Launcher.LaunchUriAsync result: {launched}");
                    
                    if (!launched)
                    {
                        Console.WriteLine("[SupabaseAuth] Launcher failed, using Process.Start...");
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = oauthUrl,
                            UseShellExecute = true
                        });
                    }
                    
                    return true;
                }
                catch (Exception launchEx)
                {
                    Console.WriteLine($"[SupabaseAuth] Launcher exception: {launchEx.Message}");
                    
                    try
                    {
                        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                        {
                            FileName = oauthUrl,
                            UseShellExecute = true
                        });
                        Console.WriteLine("[SupabaseAuth] Process.Start succeeded");
                        return true;
                    }
                    catch (Exception processEx)
                    {
                        Console.WriteLine($"[SupabaseAuth] Process.Start also failed: {processEx.Message}");
                    }
                }
            }
            else
            {
                Console.WriteLine("[SupabaseAuth] ERROR: No URI returned from SignIn!");
            }

            AuthStateChanged?.Invoke(this, AuthState.Error);
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SupabaseAuth] ERROR: {ex.Message}");
            Console.WriteLine($"[SupabaseAuth] StackTrace: {ex.StackTrace}");
            AuthStateChanged?.Invoke(this, AuthState.Error);
            return false;
        }
    }

    public async Task<bool> HandleOAuthCallbackAsync(string code)
    {
        Console.WriteLine($"[SupabaseAuth] HandleOAuthCallbackAsync called");
        
        if (_supabaseClient?.Auth == null || string.IsNullOrEmpty(_pkceVerifier))
        {
            Console.WriteLine("[SupabaseAuth] Cannot handle callback - missing client or verifier");
            return false;
        }

        try
        {
            AuthStateChanged?.Invoke(this, AuthState.Loading);
            var session = await _supabaseClient.Auth.ExchangeCodeForSession(_pkceVerifier, code);
            
            if (session != null)
            {
                Console.WriteLine($"[SupabaseAuth] Session obtained!");
                AuthStateChanged?.Invoke(this, AuthState.SignedIn);
                return true;
            }
            
            Console.WriteLine("[SupabaseAuth] No session returned");
            AuthStateChanged?.Invoke(this, AuthState.Error);
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SupabaseAuth] Failed to exchange code: {ex.Message}");
            AuthStateChanged?.Invoke(this, AuthState.Error);
            return false;
        }
    }

    public async Task<bool> SetSessionFromUrlAsync(string callbackUrl)
    {
        Console.WriteLine($"[SupabaseAuth] SetSessionFromUrlAsync called");
        Console.WriteLine($"[SupabaseAuth] URL: {callbackUrl.Substring(0, Math.Min(50, callbackUrl.Length))}...");
        
        if (_supabaseClient?.Auth == null)
        {
            Console.WriteLine("[SupabaseAuth] Cannot set session - client not initialized");
            return false;
        }

        try
        {
            AuthStateChanged?.Invoke(this, AuthState.Loading);
            
            // Parse the URL fragment to extract tokens
            var fragmentIndex = callbackUrl.IndexOf('#');
            if (fragmentIndex == -1)
            {
                Console.WriteLine("[SupabaseAuth] No fragment found in URL");
                AuthStateChanged?.Invoke(this, AuthState.Error);
                return false;
            }

            var fragment = callbackUrl.Substring(fragmentIndex + 1);
            var parameters = System.Web.HttpUtility.ParseQueryString(fragment);
            
            var accessToken = parameters["access_token"];
            var refreshToken = parameters["refresh_token"];
            
            Console.WriteLine($"[SupabaseAuth] Access token found: {!string.IsNullOrEmpty(accessToken)}");
            Console.WriteLine($"[SupabaseAuth] Refresh token found: {!string.IsNullOrEmpty(refreshToken)}");

            if (string.IsNullOrEmpty(accessToken))
            {
                Console.WriteLine("[SupabaseAuth] No access token in URL");
                AuthStateChanged?.Invoke(this, AuthState.Error);
                return false;
            }

            // Set the session using the tokens
            var session = await _supabaseClient.Auth.SetSession(accessToken, refreshToken ?? "");
            
            if (session != null)
            {
                Console.WriteLine($"[SupabaseAuth] Session set successfully!");
                Console.WriteLine($"[SupabaseAuth] User: {session.User?.Email}");
                AuthStateChanged?.Invoke(this, AuthState.SignedIn);
                return true;
            }
            
            Console.WriteLine("[SupabaseAuth] SetSession returned null");
            AuthStateChanged?.Invoke(this, AuthState.Error);
            return false;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SupabaseAuth] Failed to set session: {ex.Message}");
            Console.WriteLine($"[SupabaseAuth] StackTrace: {ex.StackTrace}");
            AuthStateChanged?.Invoke(this, AuthState.Error);
            return false;
        }
    }

    public async Task SignOutAsync()
    {
        Console.WriteLine("[SupabaseAuth] SignOutAsync called");
        
        if (_supabaseClient?.Auth == null) return;

        try
        {
            await _supabaseClient.Auth.SignOut();
            AuthStateChanged?.Invoke(this, AuthState.SignedOut);
            Console.WriteLine("[SupabaseAuth] Signed out successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SupabaseAuth] Sign out failed: {ex.Message}");
        }
    }
}
