using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 자체 계정 로그인, 회원가입, 로컬 세션 UI를 관리합니다.
/// 인증 요청은 기존 AuthService에 위임하고 비밀번호는 저장하거나 로그에 남기지 않습니다.
/// </summary>
[DisallowMultipleComponent]
public sealed class AuthUIManager : MonoBehaviour
{
    private const float TemporaryStatusDuration = 2.5f;

    [Header("Login")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private TMP_InputField loginEmailInput;
    [SerializeField] private TMP_InputField loginPasswordInput;
    [SerializeField] private Button loginButton;

    [Header("Register")]
    [SerializeField] private GameObject registerPanel;
    [SerializeField] private TMP_InputField registerEmailInput;
    [SerializeField] private TMP_InputField registerPasswordInput;
    [SerializeField] private TMP_InputField registerNicknameInput;
    [SerializeField] private Button registerButton;

    [Header("Common")]
    [SerializeField] private TMP_Text statusText;
    [SerializeField] private TMP_Text currentUserText;
    [SerializeField] private Button showLoginButton;
    [SerializeField] private Button showRegisterButton;
    [SerializeField] private Button logoutButton;

    private bool requestInProgress;
    private Coroutine statusClearCoroutine;

    private void Awake()
    {
        ConfigurePasswordInput(loginPasswordInput);
        ConfigurePasswordInput(registerPasswordInput);
        BindButtons();
        WarnAboutMissingReferences();
    }

    private void Start()
    {
        // JWT 재인증이 아니라 PlayerPrefs에 저장된 비민감 사용자 정보의 로컬 복원입니다.
        // ApiClient가 먼저 복원했다면 다시 PlayerPrefs를 읽지 않습니다.
        bool restored = UserSession.IsLoggedIn || UserSession.TryRestore();

        if (restored)
        {
            ShowAuthenticatedState();
            ShowStatus("Saved session restored.", autoClear: true);
        }
        else
        {
            ShowLoginPanel();
            UpdateCurrentUserText();
            ShowStatus(string.Empty);
        }
    }

    private void OnDisable()
    {
        bool hadTemporaryStatus = statusClearCoroutine != null;
        CancelStatusClear();

        if (hadTemporaryStatus && statusText != null)
            statusText.text = string.Empty;
    }

    private void OnDestroy()
    {
        CancelStatusClear();
        UnbindButtons();
    }

    private async void OnLoginClicked()
    {
        if (requestInProgress)
            return;

        string email = ReadTrimmed(loginEmailInput);
        string password = ReadTrimmed(loginPasswordInput);

        string validationMessage = ValidateAccountInput(email, password);
        if (validationMessage != null)
        {
            ShowStatus(validationMessage);
            return;
        }

        SetBusy(true);
        ShowStatus("Signing in...");

        try
        {
            LoginResponse response = await GetAuthService().LoginAsync(
                new LoginRequest
                {
                    Email = email,
                    Password = password
                }
            );

            if (this == null)
                return;

            UserSession.SetUser(response);
            EnsureSessionMatches(response.UserId);
            ClearPasswordInputs();
            ShowAuthenticatedState();
            ShowStatus("Login successful.", autoClear: true);
        }
        catch (ApiException exception)
        {
            if (this != null)
                ShowStatus(GetApiErrorMessage(exception, isRegistration: false));
        }
        catch (Exception exception)
        {
            if (this != null)
            {
                Debug.LogException(exception, this);
                ShowStatus("An unexpected error occurred while signing in.");
            }
        }
        finally
        {
            if (this != null)
                SetBusy(false);
        }
    }

    private async void OnRegisterClicked()
    {
        if (requestInProgress)
            return;

        string email = ReadTrimmed(registerEmailInput);
        string password = ReadTrimmed(registerPasswordInput);
        string nickname = ReadTrimmed(registerNicknameInput);

        string validationMessage = ValidateAccountInput(email, password);
        if (validationMessage == null && string.IsNullOrWhiteSpace(nickname))
            validationMessage = "Enter a nickname.";

        if (validationMessage != null)
        {
            ShowStatus(validationMessage);
            return;
        }

        SetBusy(true);
        ShowStatus("Creating account...");
        bool registrationCompleted = false;

        try
        {
            AuthService authService = GetAuthService();
            await authService.RegisterAsync(
                new RegisterRequest
                {
                    Email = email,
                    Password = password,
                    Nickname = nickname
                }
            );
            registrationCompleted = true;

            if (this == null)
                return;

            ClearPasswordInputs();
            ShowStatus("Account created. Signing in...");

            LoginResponse loginResponse = await authService.LoginAsync(
                new LoginRequest
                {
                    Email = email,
                    Password = password
                }
            );

            if (this == null)
                return;

            UserSession.SetUser(loginResponse);
            EnsureSessionMatches(loginResponse.UserId);
            ClearPasswordInputs();
            ShowAuthenticatedState();
            ShowStatus("Registration and login successful.", autoClear: true);
        }
        catch (ApiException exception)
        {
            if (this != null)
                ShowStatus(GetApiErrorMessage(exception, isRegistration: !registrationCompleted));
        }
        catch (Exception exception)
        {
            if (this != null)
            {
                Debug.LogException(exception, this);
                ShowStatus("An unexpected error occurred while creating the account.");
            }
        }
        finally
        {
            if (this != null)
                SetBusy(false);
        }
    }

    private void OnShowLoginClicked()
    {
        if (requestInProgress)
            return;

        ShowLoginPanel();
        ShowStatus(string.Empty);
    }

    private void OnShowRegisterClicked()
    {
        if (requestInProgress)
            return;

        SetActive(loginPanel, false);
        SetActive(registerPanel, true);
        ShowStatus(string.Empty);
    }

    private void OnLogoutClicked()
    {
        if (requestInProgress)
            return;

        UserSession.Logout();
        ClearPasswordInputs();
        UpdateCurrentUserText();
        ShowLoginPanel();
        ShowStatus("Logged out.", autoClear: true);
    }

    private static AuthService GetAuthService()
    {
        ServiceRegistry registry = ServiceRegistry.Instance;
        if (registry == null || registry.Auth == null)
            throw new InvalidOperationException("Authentication service is unavailable.");

        return registry.Auth;
    }

    private static void EnsureSessionMatches(long expectedUserId)
    {
        if (!UserSession.IsLoggedIn || UserSession.UserId != expectedUserId)
            throw new InvalidOperationException("The signed-in user could not be saved.");

        ApiClient client = ApiClient.Instance;
        if (client == null || client.CurrentUserId != expectedUserId)
            throw new InvalidOperationException("The current user ID could not be applied.");
    }

    private void ShowAuthenticatedState()
    {
        SetActive(loginPanel, false);
        SetActive(registerPanel, false);
        UpdateCurrentUserText();

        if (logoutButton != null)
            logoutButton.gameObject.SetActive(true);
    }

    private void ShowLoginPanel()
    {
        SetActive(loginPanel, true);
        SetActive(registerPanel, false);

        if (logoutButton != null)
            logoutButton.gameObject.SetActive(UserSession.IsLoggedIn);
    }

    private void UpdateCurrentUserText()
    {
        if (currentUserText == null)
            return;

        currentUserText.text = UserSession.IsLoggedIn
            ? $"User ID: {UserSession.UserId}\nEmail: {UserSession.Email}\nNickname: {UserSession.Nickname}"
            : string.Empty;
    }

    private void SetBusy(bool busy)
    {
        requestInProgress = busy;
        SetInteractable(loginButton, !busy);
        SetInteractable(registerButton, !busy);
        SetInteractable(showLoginButton, !busy);
        SetInteractable(showRegisterButton, !busy);
        SetInteractable(logoutButton, !busy);
    }

    private void ClearPasswordInputs()
    {
        if (loginPasswordInput != null)
            loginPasswordInput.text = string.Empty;

        if (registerPasswordInput != null)
            registerPasswordInput.text = string.Empty;
    }

    private void ShowStatus(string message, bool autoClear = false)
    {
        CancelStatusClear();

        if (statusText != null)
            statusText.text = message ?? string.Empty;

        if (!autoClear || string.IsNullOrEmpty(message))
            return;

        if (!isActiveAndEnabled)
        {
            if (statusText != null)
                statusText.text = string.Empty;

            return;
        }

        statusClearCoroutine = StartCoroutine(ClearStatusAfterDelay());
    }

    private IEnumerator ClearStatusAfterDelay()
    {
        yield return new WaitForSecondsRealtime(TemporaryStatusDuration);

        statusClearCoroutine = null;

        if (statusText != null)
            statusText.text = string.Empty;
    }

    private void CancelStatusClear()
    {
        if (statusClearCoroutine == null)
            return;

        StopCoroutine(statusClearCoroutine);
        statusClearCoroutine = null;
    }

    private static string ValidateAccountInput(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email))
            return "Enter an email address.";

        if (!email.Contains("@"))
            return "Enter a valid email address containing @.";

        if (string.IsNullOrWhiteSpace(password))
            return "Enter a password.";

        return null;
    }

    private static string GetApiErrorMessage(ApiException exception, bool isRegistration)
    {
        if (exception.StatusCode <= 0)
            return "Could not connect to the server. Check your network connection and try again.";

        string detail = RemoveHttpPrefix(exception.Message, exception.StatusCode);

        if (detail.Contains("존재하지 않는 이메일"))
            return "No account exists for this email address.";

        if (detail.Contains("비밀번호가 일치하지 않습니다"))
            return "The password is incorrect.";

        if (isRegistration && ContainsDuplicateAccountMessage(detail))
            return "An account with this email address already exists.";

        if (isRegistration && exception.StatusCode >= 500)
            return "This email may already be registered, or the server could not create the account.";

        if (exception.StatusCode == 401 || exception.StatusCode == 403)
            return "The email address or password is incorrect.";

        if (exception.StatusCode == 404)
            return "No account exists for this email address.";

        if (exception.StatusCode >= 500)
            return "The server encountered an unexpected error. Try again later.";

        return "The request could not be completed. Check your input and try again.";
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

    private static void ConfigurePasswordInput(TMP_InputField input)
    {
        if (input == null)
            return;

        input.contentType = TMP_InputField.ContentType.Password;
        input.ForceLabelUpdate();
    }

    private void BindButtons()
    {
        AddListener(loginButton, OnLoginClicked);
        AddListener(registerButton, OnRegisterClicked);
        AddListener(showLoginButton, OnShowLoginClicked);
        AddListener(showRegisterButton, OnShowRegisterClicked);
        AddListener(logoutButton, OnLogoutClicked);
    }

    private void UnbindButtons()
    {
        RemoveListener(loginButton, OnLoginClicked);
        RemoveListener(registerButton, OnRegisterClicked);
        RemoveListener(showLoginButton, OnShowLoginClicked);
        RemoveListener(showRegisterButton, OnShowRegisterClicked);
        RemoveListener(logoutButton, OnLogoutClicked);
    }

    private void WarnAboutMissingReferences()
    {
        string missing = string.Empty;
        AppendMissingReference(ref missing, loginPanel, nameof(loginPanel));
        AppendMissingReference(ref missing, loginEmailInput, nameof(loginEmailInput));
        AppendMissingReference(ref missing, loginPasswordInput, nameof(loginPasswordInput));
        AppendMissingReference(ref missing, loginButton, nameof(loginButton));
        AppendMissingReference(ref missing, registerPanel, nameof(registerPanel));
        AppendMissingReference(ref missing, registerEmailInput, nameof(registerEmailInput));
        AppendMissingReference(ref missing, registerPasswordInput, nameof(registerPasswordInput));
        AppendMissingReference(ref missing, registerNicknameInput, nameof(registerNicknameInput));
        AppendMissingReference(ref missing, registerButton, nameof(registerButton));
        AppendMissingReference(ref missing, statusText, nameof(statusText));
        AppendMissingReference(ref missing, currentUserText, nameof(currentUserText));
        AppendMissingReference(ref missing, showLoginButton, nameof(showLoginButton));
        AppendMissingReference(ref missing, showRegisterButton, nameof(showRegisterButton));
        AppendMissingReference(ref missing, logoutButton, nameof(logoutButton));

        if (!string.IsNullOrEmpty(missing))
            Debug.LogError($"[AuthUIManager] Missing Inspector reference(s): {missing}.", this);
    }

    private static void AppendMissingReference(ref string missing, UnityEngine.Object value, string fieldName)
    {
        if (value == null)
            missing += string.IsNullOrEmpty(missing) ? fieldName : $", {fieldName}";
    }

    private static void AddListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
            button.onClick.AddListener(action);
    }

    private static void RemoveListener(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
            button.onClick.RemoveListener(action);
    }

    private static void SetInteractable(Button button, bool interactable)
    {
        if (button != null)
            button.interactable = interactable;
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }
}
