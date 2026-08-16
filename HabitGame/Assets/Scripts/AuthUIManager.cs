using System;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 로그인/회원가입 패널과 인증 요청을 연결합니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class AuthUIManager : MonoBehaviour
{
    private const float LoginStatusY = -220f;
    private const float RegisterStatusY = -300f;
    private const string RememberLoginIdKey = "HabitPVP.RememberLoginId";
    private const string SavedLoginIdKey = "HabitPVP.SavedLoginId";

    [Header("Login")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private TMP_InputField loginEmailInput;
    [SerializeField] private TMP_InputField loginPasswordInput;
    [SerializeField] private Button loginButton;
    [SerializeField] private TMP_Text loginButtonText;
    [SerializeField] private Toggle rememberLoginIdToggle;

    [Header("Register")]
    [SerializeField] private GameObject registerPanel;
    [SerializeField] private TMP_InputField registerEmailInput;
    [SerializeField] private TMP_InputField registerPasswordInput;
    [SerializeField] private TMP_InputField registerPasswordConfirmationInput;
    [SerializeField] private TMP_InputField registerNicknameInput;
    [SerializeField] private Button registerButton;
    [SerializeField] private TMP_Text registerButtonText;

    [Header("Common")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private Button showLoginButton;
    [SerializeField] private Button showRegisterButton;
    [SerializeField] private string gameplaySceneName = "MainScene";

    private bool requestInProgress;

    private void Awake()
    {
        ConfigureEmailInput(loginEmailInput);
        ConfigureEmailInput(registerEmailInput);
        ConfigurePasswordInput(loginPasswordInput);
        ConfigurePasswordInput(registerPasswordInput);
        ConfigurePasswordInput(registerPasswordConfirmationInput);
        BindControls();
        WarnAboutMissingReferences();
    }

    private void Start()
    {
        ShowLoginPanel(clearStatus: true);
        RestoreRememberedLoginId();

        if (rememberLoginIdToggle != null && rememberLoginIdToggle.isOn)
            loginPasswordInput?.Select();
        else
            loginEmailInput?.Select();
    }

    private void OnDestroy()
    {
        UnbindControls();
    }

    private async void OnLoginClicked()
    {
        if (requestInProgress)
            return;

        string email = ReadTrimmed(loginEmailInput);
        string password = ReadPassword(loginPasswordInput);
        string validationMessage = ValidateAccountInput(email, password);

        if (validationMessage != null)
        {
            ShowStatus(validationMessage);
            return;
        }

        ShowStatus(string.Empty);
        SetBusy(AuthRequest.Login);

        try
        {
            LoginResponse response = await GetAuthService().LoginAsync(new LoginRequest
            {
                Email = email,
                Password = password
            });

            if (this == null)
                return;

            UserSession.SetUser(response);
            EnsureSessionMatches(response.UserId);
            ApplyRememberedLoginId(email);
            ClearPasswordInputs();
            LoadGameplayScene();
        }
        catch (ApiException exception)
        {
            if (this != null)
            {
                Debug.LogWarning($"[Auth] Login request failed ({exception.StatusCode}): {exception.Message}", this);
                ShowStatus(GetApiErrorMessage(exception, isRegistration: false));
            }
        }
        catch (Exception exception)
        {
            if (this != null)
            {
                Debug.LogError($"[Auth] Login request failed: {exception}", this);
                ShowStatus("로그인 중 오류가 발생했습니다.");
            }
        }
        finally
        {
            if (this != null)
                SetBusy(AuthRequest.None);
        }
    }

    private async void OnRegisterClicked()
    {
        if (requestInProgress)
            return;

        string email = ReadTrimmed(registerEmailInput);
        string password = ReadPassword(registerPasswordInput);
        string passwordConfirmation = ReadPassword(registerPasswordConfirmationInput);
        string nickname = ReadTrimmed(registerNicknameInput);

        string validationMessage = ValidateRegistrationInput(
            email,
            password,
            passwordConfirmation,
            nickname
        );

        if (validationMessage != null)
        {
            ShowStatus(validationMessage);
            return;
        }

        ShowStatus(string.Empty);
        SetBusy(AuthRequest.Register);

        try
        {
            await GetAuthService().RegisterAsync(new RegisterRequest
            {
                Email = email,
                Password = password,
                Nickname = nickname
            });

            if (this == null)
                return;

            ClearRegistrationInputs();
            loginEmailInput.text = email;
            loginPasswordInput.text = string.Empty;
            ShowLoginPanel(clearStatus: false);
            ShowStatus("회원가입이 완료되었습니다.", isError: false);
            loginPasswordInput.Select();
        }
        catch (ApiException exception)
        {
            if (this != null)
            {
                Debug.LogWarning($"[Auth] Register request failed ({exception.StatusCode}): {exception.Message}", this);
                ShowStatus(GetApiErrorMessage(exception, isRegistration: true));
            }
        }
        catch (Exception exception)
        {
            if (this != null)
            {
                Debug.LogError($"[Auth] Register request failed: {exception}", this);
                ShowStatus("회원가입 중 오류가 발생했습니다.");
            }
        }
        finally
        {
            if (this != null)
                SetBusy(AuthRequest.None);
        }
    }

    private void OnShowLoginClicked()
    {
        if (!requestInProgress)
            ShowLoginPanel(clearStatus: true);
    }

    private void OnShowRegisterClicked()
    {
        if (requestInProgress)
            return;

        SetActive(loginPanel, false);
        SetActive(registerPanel, true);
        SetStatusPosition(RegisterStatusY);
        ShowStatus(string.Empty);
        registerEmailInput?.Select();
    }

    private void OnLoginPasswordSubmitted(string _)
    {
        OnLoginClicked();
    }

    private void OnRegisterPasswordConfirmationSubmitted(string _)
    {
        OnRegisterClicked();
    }

    private void ShowLoginPanel(bool clearStatus)
    {
        SetActive(loginPanel, true);
        SetActive(registerPanel, false);
        SetStatusPosition(LoginStatusY);

        if (clearStatus)
            ShowStatus(string.Empty);
    }

    private static AuthService GetAuthService()
    {
        ServiceRegistry registry = ServiceRegistry.Instance;
        if (registry == null || registry.Auth == null)
            throw new InvalidOperationException("인증 서비스를 사용할 수 없습니다.");

        return registry.Auth;
    }

    private static void EnsureSessionMatches(long expectedUserId)
    {
        if (!UserSession.IsLoggedIn || UserSession.UserId != expectedUserId)
            throw new InvalidOperationException("로그인 사용자 정보를 저장하지 못했습니다.");

        ApiClient client = ApiClient.Instance;
        if (client == null || client.CurrentUserId != expectedUserId)
            throw new InvalidOperationException("사용자 ID를 API 클라이언트에 적용하지 못했습니다.");
    }

    private void LoadGameplayScene()
    {
        if (string.IsNullOrWhiteSpace(gameplaySceneName)
            || !Application.CanStreamedLevelBeLoaded(gameplaySceneName))
        {
            Debug.LogError($"[Auth] Scene '{gameplaySceneName}' is not available in Build Settings.", this);
            ShowStatus("게임 화면을 불러올 수 없습니다. 빌드 설정을 확인해주세요.");
            return;
        }

        SceneManager.LoadScene(gameplaySceneName);
    }

    private void SetBusy(AuthRequest request)
    {
        bool busy = request != AuthRequest.None;
        requestInProgress = busy;
        SetInteractable(loginButton, !busy);
        SetInteractable(registerButton, !busy);
        SetInteractable(showLoginButton, !busy);
        SetInteractable(showRegisterButton, !busy);
        SetInteractable(rememberLoginIdToggle, !busy);

        if (loginButtonText != null)
            loginButtonText.text = request == AuthRequest.Login ? "로그인 중..." : "로그인";

        if (registerButtonText != null)
            registerButtonText.text = request == AuthRequest.Register ? "가입 중..." : "회원가입";
    }

    private void ClearRegistrationInputs()
    {
        registerEmailInput.text = string.Empty;
        registerPasswordInput.text = string.Empty;
        registerPasswordConfirmationInput.text = string.Empty;
        registerNicknameInput.text = string.Empty;
    }

    private void RestoreRememberedLoginId()
    {
        bool rememberLoginId = PlayerPrefs.GetInt(RememberLoginIdKey, 0) == 1;
        string savedLoginId = rememberLoginId
            ? PlayerPrefs.GetString(SavedLoginIdKey, string.Empty).Trim()
            : string.Empty;
        bool hasSavedLoginId = !string.IsNullOrWhiteSpace(savedLoginId);

        if (rememberLoginIdToggle != null)
            rememberLoginIdToggle.isOn = hasSavedLoginId;

        if (loginEmailInput != null)
            loginEmailInput.text = hasSavedLoginId ? savedLoginId : string.Empty;

        if (loginPasswordInput != null)
            loginPasswordInput.text = string.Empty;
    }

    private void ApplyRememberedLoginId(string email)
    {
        bool rememberLoginId = rememberLoginIdToggle != null && rememberLoginIdToggle.isOn;
        bool changed;

        if (rememberLoginId)
        {
            changed = PlayerPrefs.GetInt(RememberLoginIdKey, 0) != 1
                || !string.Equals(
                    PlayerPrefs.GetString(SavedLoginIdKey, string.Empty),
                    email,
                    StringComparison.Ordinal
                );

            if (!changed)
                return;

            PlayerPrefs.SetInt(RememberLoginIdKey, 1);
            PlayerPrefs.SetString(SavedLoginIdKey, email);
        }
        else
        {
            changed = PlayerPrefs.HasKey(RememberLoginIdKey)
                || PlayerPrefs.HasKey(SavedLoginIdKey);

            if (!changed)
                return;

            PlayerPrefs.DeleteKey(RememberLoginIdKey);
            PlayerPrefs.DeleteKey(SavedLoginIdKey);
        }

        PlayerPrefs.Save();
    }

    private void ClearPasswordInputs()
    {
        if (loginPasswordInput != null)
            loginPasswordInput.text = string.Empty;

        if (registerPasswordInput != null)
            registerPasswordInput.text = string.Empty;

        if (registerPasswordConfirmationInput != null)
            registerPasswordConfirmationInput.text = string.Empty;
    }

    private void ShowStatus(string message, bool isError = true)
    {
        if (statusText == null)
            return;

        statusText.text = message ?? string.Empty;
        statusText.color = isError
            ? new Color32(211, 47, 47, 255)
            : new Color32(30, 136, 229, 255);
        statusText.gameObject.SetActive(!string.IsNullOrEmpty(message));
    }

    private void SetStatusPosition(float y)
    {
        if (statusText == null)
            return;

        RectTransform rectTransform = statusText.rectTransform;
        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, y);
    }

    private static string ValidateAccountInput(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email))
            return "이메일을 입력해주세요.";

        if (string.IsNullOrEmpty(password))
            return "비밀번호를 입력해주세요.";

        return null;
    }

    private static string ValidateRegistrationInput(
        string email,
        string password,
        string passwordConfirmation,
        string nickname)
    {
        string accountValidation = ValidateAccountInput(email, password);
        if (accountValidation != null)
            return accountValidation;

        if (string.IsNullOrEmpty(passwordConfirmation))
            return "비밀번호 확인을 입력해주세요.";

        if (!string.Equals(password, passwordConfirmation, StringComparison.Ordinal))
            return "비밀번호가 일치하지 않습니다.";

        if (string.IsNullOrWhiteSpace(nickname))
            return "닉네임을 입력해주세요.";

        return null;
    }

    private static string GetApiErrorMessage(ApiException exception, bool isRegistration)
    {
        if (exception.StatusCode <= 0)
        {
            return exception.Message.IndexOf("timed out", StringComparison.OrdinalIgnoreCase) >= 0
                ? "요청 시간이 초과되었습니다. 네트워크를 확인하고 다시 시도해주세요."
                : "서버에 연결할 수 없습니다.\n잠시 후 다시 시도해주세요.";
        }

        string detail = RemoveHttpPrefix(exception.Message, exception.StatusCode);

        if (isRegistration
            && (exception.StatusCode == 409 || ContainsDuplicateAccountMessage(detail)))
            return "이미 사용 중인 이메일입니다.";

        if (!isRegistration && (exception.StatusCode == 401
            || exception.StatusCode == 403
            || exception.StatusCode == 404
            || detail.Contains("존재하지 않는 이메일")
            || detail.Contains("비밀번호가 일치하지 않습니다")))
        {
            return "아이디 또는 비밀번호를 확인해주세요.";
        }

        if (exception.StatusCode >= 500)
            return "서버 오류가 발생했습니다.\n잠시 후 다시 시도해주세요.";

        return isRegistration
            ? "입력 정보를 확인하고 다시 시도해주세요."
            : "아이디 또는 비밀번호를 확인해주세요.";
    }

    private static bool ContainsDuplicateAccountMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return false;

        return message.Contains("이미 사용 중")
            || message.Contains("중복")
            || message.IndexOf("duplicate", StringComparison.OrdinalIgnoreCase) >= 0
            || message.IndexOf("already exists", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string RemoveHttpPrefix(string message, long statusCode)
    {
        if (string.IsNullOrWhiteSpace(message))
            return string.Empty;

        string prefix = $"HTTP {statusCode}: ";
        return message.StartsWith(prefix, StringComparison.Ordinal)
            ? message.Substring(prefix.Length)
            : message;
    }

    private static string ReadTrimmed(TMP_InputField input)
    {
        return input == null ? string.Empty : input.text.Trim();
    }

    private static string ReadPassword(TMP_InputField input)
    {
        return input == null ? string.Empty : input.text;
    }

    private static void ConfigureEmailInput(TMP_InputField input)
    {
        if (input == null)
            return;

        input.contentType = TMP_InputField.ContentType.EmailAddress;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.ForceLabelUpdate();
    }

    private static void ConfigurePasswordInput(TMP_InputField input)
    {
        if (input == null)
            return;

        input.contentType = TMP_InputField.ContentType.Password;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.ForceLabelUpdate();
    }

    private void BindControls()
    {
        AddListener(loginButton, OnLoginClicked);
        AddListener(registerButton, OnRegisterClicked);
        AddListener(showLoginButton, OnShowLoginClicked);
        AddListener(showRegisterButton, OnShowRegisterClicked);

        loginPasswordInput?.onSubmit.AddListener(OnLoginPasswordSubmitted);
        registerPasswordConfirmationInput?.onSubmit.AddListener(OnRegisterPasswordConfirmationSubmitted);
    }

    private void UnbindControls()
    {
        RemoveListener(loginButton, OnLoginClicked);
        RemoveListener(registerButton, OnRegisterClicked);
        RemoveListener(showLoginButton, OnShowLoginClicked);
        RemoveListener(showRegisterButton, OnShowRegisterClicked);

        loginPasswordInput?.onSubmit.RemoveListener(OnLoginPasswordSubmitted);
        registerPasswordConfirmationInput?.onSubmit.RemoveListener(OnRegisterPasswordConfirmationSubmitted);
    }

    private void WarnAboutMissingReferences()
    {
        string missing = string.Empty;
        AppendMissingReference(ref missing, loginPanel, nameof(loginPanel));
        AppendMissingReference(ref missing, loginEmailInput, nameof(loginEmailInput));
        AppendMissingReference(ref missing, loginPasswordInput, nameof(loginPasswordInput));
        AppendMissingReference(ref missing, loginButton, nameof(loginButton));
        AppendMissingReference(ref missing, loginButtonText, nameof(loginButtonText));
        AppendMissingReference(ref missing, rememberLoginIdToggle, nameof(rememberLoginIdToggle));
        AppendMissingReference(ref missing, registerPanel, nameof(registerPanel));
        AppendMissingReference(ref missing, registerEmailInput, nameof(registerEmailInput));
        AppendMissingReference(ref missing, registerPasswordInput, nameof(registerPasswordInput));
        AppendMissingReference(ref missing, registerPasswordConfirmationInput, nameof(registerPasswordConfirmationInput));
        AppendMissingReference(ref missing, registerNicknameInput, nameof(registerNicknameInput));
        AppendMissingReference(ref missing, registerButton, nameof(registerButton));
        AppendMissingReference(ref missing, registerButtonText, nameof(registerButtonText));
        AppendMissingReference(ref missing, statusText, nameof(statusText));
        AppendMissingReference(ref missing, showLoginButton, nameof(showLoginButton));
        AppendMissingReference(ref missing, showRegisterButton, nameof(showRegisterButton));

        if (!string.IsNullOrEmpty(missing))
            Debug.LogError($"[Auth] Inspector reference missing: {missing}.", this);
    }

    private static void AppendMissingReference(ref string missing, UnityEngine.Object value, string fieldName)
    {
        if (value == null)
            missing += string.IsNullOrEmpty(missing) ? fieldName : $", {fieldName}";
    }

    private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
    {
        button?.onClick.AddListener(action);
    }

    private static void RemoveListener(Button button, UnityEngine.Events.UnityAction action)
    {
        button?.onClick.RemoveListener(action);
    }

    private static void SetInteractable(Selectable selectable, bool interactable)
    {
        if (selectable != null)
            selectable.interactable = interactable;
    }

    private static void SetActive(GameObject target, bool active)
    {
        target?.SetActive(active);
    }

    private enum AuthRequest
    {
        None,
        Login,
        Register
    }
}
