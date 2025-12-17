using Supabase.Gotrue;
using Supabase.Gotrue.Interfaces;

namespace DevFlow.Services.Auth;

/// <summary>
/// Session persistence implementation for Supabase authentication.
/// Stores sessions in local storage to maintain login state across app restarts.
/// </summary>
public class SessionPersistence : IGotrueSessionPersistence<Session>
{
    private const string SessionKey = "supabase_session";
#pragma warning disable CS0169 // Field is assigned/used in conditional compilation blocks
    private Session? _cachedSession;
#pragma warning restore CS0169

    public void SaveSession(Session session)
    {
        try
        {
            var json = System.Text.Json.JsonSerializer.Serialize(session);
#if HAS_UNO_WINUI || WINDOWS
            Windows.Storage.ApplicationData.Current.LocalSettings.Values[SessionKey] = json;
#else
            // For WASM, we use a simple in-memory cache that will be persisted via JavaScript interop
            _cachedSession = session;
#endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to save session: {ex.Message}");
        }
    }

    public Session? LoadSession()
    {
        try
        {
#if HAS_UNO_WINUI || WINDOWS
            if (Windows.Storage.ApplicationData.Current.LocalSettings.Values.TryGetValue(SessionKey, out var value) 
                && value is string json)
            {
                return System.Text.Json.JsonSerializer.Deserialize<Session>(json);
            }
#else
            return _cachedSession;
#endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to load session: {ex.Message}");
        }

        return null;
    }

    public void DestroySession()
    {
        try
        {
#if HAS_UNO_WINUI || WINDOWS
            Windows.Storage.ApplicationData.Current.LocalSettings.Values.Remove(SessionKey);
#else
            _cachedSession = null;
#endif
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to destroy session: {ex.Message}");
        }
    }
}
