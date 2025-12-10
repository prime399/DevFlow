using DevFlow.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace DevFlow.Controls;

public sealed partial class AuthorizationEditorControl : UserControl
{
    public static readonly DependencyProperty AuthorizationProperty =
        DependencyProperty.Register(nameof(Authorization), typeof(AuthorizationConfig), typeof(AuthorizationEditorControl),
            new PropertyMetadata(null, OnAuthorizationChanged));

    public static readonly DependencyProperty AuthTypesProperty =
        DependencyProperty.Register(nameof(AuthTypes), typeof(IReadOnlyList<AuthTypeOption>), typeof(AuthorizationEditorControl),
            new PropertyMetadata(AuthTypeInfo.AllTypes, OnAuthTypesChanged));

    public static readonly DependencyProperty ApiKeyLocationsProperty =
        DependencyProperty.Register(nameof(ApiKeyLocations), typeof(IReadOnlyList<ApiKeyLocationOption>), typeof(AuthorizationEditorControl),
            new PropertyMetadata(ApiKeyLocationInfo.AllLocations, OnApiKeyLocationsChanged));

    private bool _isUpdating;
    private FrameworkElement[] _authPanels = null!;

    public AuthorizationEditorControl()
    {
        this.InitializeComponent();
        _authPanels = new FrameworkElement[] { AuthNonePanel, AuthBasicPanel, AuthBearerPanel, AuthApiKeyPanel, AuthOAuth2Panel };
        
        AuthTypeComboBox.ItemsSource = AuthTypes;
        AuthTypeComboBox.SelectedIndex = 0;
        ApiKeyLocationComboBox.ItemsSource = ApiKeyLocations;
        ApiKeyLocationComboBox.SelectedIndex = 0;
    }

    public AuthorizationConfig? Authorization
    {
        get => (AuthorizationConfig?)GetValue(AuthorizationProperty);
        set => SetValue(AuthorizationProperty, value);
    }

    public IReadOnlyList<AuthTypeOption> AuthTypes
    {
        get => (IReadOnlyList<AuthTypeOption>)GetValue(AuthTypesProperty);
        set => SetValue(AuthTypesProperty, value);
    }

    public IReadOnlyList<ApiKeyLocationOption> ApiKeyLocations
    {
        get => (IReadOnlyList<ApiKeyLocationOption>)GetValue(ApiKeyLocationsProperty);
        set => SetValue(ApiKeyLocationsProperty, value);
    }

    public event EventHandler? AuthorizationChanged;

    private static void OnAuthorizationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AuthorizationEditorControl control)
        {
            control.LoadFromAuthorization();
        }
    }

    private static void OnAuthTypesChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AuthorizationEditorControl control)
        {
            control.AuthTypeComboBox.ItemsSource = e.NewValue as IReadOnlyList<AuthTypeOption>;
        }
    }

    private static void OnApiKeyLocationsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is AuthorizationEditorControl control)
        {
            control.ApiKeyLocationComboBox.ItemsSource = e.NewValue as IReadOnlyList<ApiKeyLocationOption>;
        }
    }

    private void LoadFromAuthorization()
    {
        if (Authorization == null) return;

        _isUpdating = true;

        // Set auth type
        var authTypeIndex = AuthTypes.ToList().FindIndex(t => t.Type == Authorization.AuthType);
        if (authTypeIndex >= 0)
        {
            AuthTypeComboBox.SelectedIndex = authTypeIndex;
        }

        AuthEnabledCheckbox.IsChecked = Authorization.IsEnabled;
        BasicUsernameInput.Text = Authorization.Username;
        BasicPasswordInput.Password = Authorization.Password;
        BearerTokenInput.Text = Authorization.BearerToken;
        ApiKeyNameInput.Text = Authorization.ApiKeyName;
        ApiKeyValueInput.Password = Authorization.ApiKeyValue;
        OAuth2AccessTokenInput.Text = Authorization.AccessToken;
        OAuth2TokenTypeInput.Text = Authorization.TokenType;

        var locationIndex = ApiKeyLocations.ToList().FindIndex(l => l.Location == Authorization.ApiKeyLocation);
        if (locationIndex >= 0)
        {
            ApiKeyLocationComboBox.SelectedIndex = locationIndex;
        }

        SwitchToAuthPanel(Authorization.AuthType);

        _isUpdating = false;
    }

    private void SwitchToAuthPanel(AuthorizationType authType)
    {
        var panelIndex = authType switch
        {
            AuthorizationType.None => 0,
            AuthorizationType.BasicAuth => 1,
            AuthorizationType.BearerToken => 2,
            AuthorizationType.ApiKey => 3,
            AuthorizationType.OAuth2 => 4,
            _ => 0
        };

        for (int i = 0; i < _authPanels.Length; i++)
        {
            _authPanels[i].Visibility = i == panelIndex ? Visibility.Visible : Visibility.Collapsed;
        }
    }

    private void AuthType_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdating || Authorization == null) return;

        if (AuthTypeComboBox.SelectedItem is AuthTypeOption option)
        {
            Authorization.AuthType = option.Type;
            Authorization.IsEnabled = option.Type != AuthorizationType.None;
            AuthEnabledCheckbox.IsChecked = Authorization.IsEnabled;
            SwitchToAuthPanel(option.Type);
            AuthorizationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void AuthEnabled_Changed(object sender, RoutedEventArgs e)
    {
        if (_isUpdating || Authorization == null) return;

        Authorization.IsEnabled = AuthEnabledCheckbox.IsChecked ?? false;
        AuthorizationChanged?.Invoke(this, EventArgs.Empty);
    }

    private void BasicAuth_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdating || Authorization == null) return;

        Authorization.Username = BasicUsernameInput.Text;
        AuthorizationChanged?.Invoke(this, EventArgs.Empty);
    }

    private void BasicAuth_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_isUpdating || Authorization == null) return;

        Authorization.Password = BasicPasswordInput.Password;
        AuthorizationChanged?.Invoke(this, EventArgs.Empty);
    }

    private void BearerToken_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdating || Authorization == null) return;

        Authorization.BearerToken = BearerTokenInput.Text;
        AuthorizationChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ApiKey_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdating || Authorization == null) return;

        Authorization.ApiKeyName = ApiKeyNameInput.Text;
        AuthorizationChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ApiKeyValue_PasswordChanged(object sender, RoutedEventArgs e)
    {
        if (_isUpdating || Authorization == null) return;

        Authorization.ApiKeyValue = ApiKeyValueInput.Password;
        AuthorizationChanged?.Invoke(this, EventArgs.Empty);
    }

    private void ApiKeyLocation_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isUpdating || Authorization == null) return;

        if (ApiKeyLocationComboBox.SelectedItem is ApiKeyLocationOption option)
        {
            Authorization.ApiKeyLocation = option.Location;
            AuthorizationChanged?.Invoke(this, EventArgs.Empty);
        }
    }

    private void OAuth2_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdating || Authorization == null) return;

        Authorization.AccessToken = OAuth2AccessTokenInput.Text;
        AuthorizationChanged?.Invoke(this, EventArgs.Empty);
    }

    private void OAuth2TokenType_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isUpdating || Authorization == null) return;

        Authorization.TokenType = OAuth2TokenTypeInput.Text;
        AuthorizationChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Clear_Click(object sender, RoutedEventArgs e)
    {
        if (Authorization == null) return;

        _isUpdating = true;

        Authorization.Username = string.Empty;
        Authorization.Password = string.Empty;
        Authorization.BearerToken = string.Empty;
        Authorization.ApiKeyName = string.Empty;
        Authorization.ApiKeyValue = string.Empty;
        Authorization.AccessToken = string.Empty;
        Authorization.TokenType = "Bearer";

        BasicUsernameInput.Text = string.Empty;
        BasicPasswordInput.Password = string.Empty;
        BearerTokenInput.Text = string.Empty;
        ApiKeyNameInput.Text = string.Empty;
        ApiKeyValueInput.Password = string.Empty;
        OAuth2AccessTokenInput.Text = string.Empty;
        OAuth2TokenTypeInput.Text = "Bearer";

        _isUpdating = false;
        AuthorizationChanged?.Invoke(this, EventArgs.Empty);
    }
}
