using System;
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
            SetStatus("저장된 로그인 정보를 복원했습니다.");
        }
        else
        {
            ShowLoginPanel();
            UpdateCurrentUserText();
            SetStatus(string.Empty);
        }
    }

    private void OnDestroy()
    {
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
            SetStatus(validationMessage);
            return;
        }

        SetBusy(true);
        SetStatus("로그인 요청 중...");

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
            SetStatus("로그인되었습니다.");
        }
        catch (ApiException exception)
        {
            if (this != null)
                SetStatus(GetApiErrorMessage(exception, isRegistration: false));
        }
        catch (Exception exception)
        {
            if (this != null)
            {
                Debug.LogException(exception, this);
                SetStatus("로그인 중 예상하지 못한 오류가 발생했습니다.");
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
            validationMessage = "닉네임을 입력해주세요.";

        if (validationMessage != null)
        {
            SetStatus(validationMessage);
            return;
        }

        SetBusy(true);
        SetStatus("회원가입 요청 중...");
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

            SetStatus("회원가입 완료. 로그인 중...");

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
            SetStatus("회원가입 및 로그인에 성공했습니다.");
        }
        catch (ApiException exception)
        {
            if (this != null)
                SetStatus(GetApiErrorMessage(exception, isRegistration: !registrationCompleted));
        }
        catch (Exception exception)
        {
            if (this != null)
            {
                Debug.LogException(exception, this);
                SetStatus("회원가입 중 예상하지 못한 오류가 발생했습니다.");
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
        SetStatus(string.Empty);
    }

    private void OnShowRegisterClicked()
    {
        if (requestInProgress)
            return;

        SetActive(loginPanel, false);
        SetActive(registerPanel, true);
        SetStatus(string.Empty);
    }

    private void OnLogoutClicked()
    {
        if (requestInProgress)
            return;

        UserSession.Logout();
        ClearPasswordInputs();
        UpdateCurrentUserText();
        ShowLoginPanel();
        SetStatus("로그아웃되었습니다.");
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
            throw new InvalidOperationException("로그인 정보를 저장하지 못했습니다.");

        ApiClient client = ApiClient.Instance;
        if (client == null || client.CurrentUserId != expectedUserId)
            throw new InvalidOperationException("현재 사용자 ID를 적용하지 못했습니다.");
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
            ? $"사용자 ID: {UserSession.UserId}\n이메일: {UserSession.Email}\n닉네임: {UserSession.Nickname}"
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

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message ?? string.Empty;
    }

    private static string ValidateAccountInput(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email))
            return "이메일을 입력해주세요.";

        if (!email.Contains("@"))
            return "올바른 이메일 형식을 입력해주세요.";

        if (string.IsNullOrWhiteSpace(password))
            return "비밀번호를 입력해주세요.";

        return null;
    }

    private static string GetApiErrorMessage(ApiException exception, bool isRegistration)
    {
        if (exception.StatusCode <= 0)
            return "서버에 연결할 수 없습니다. 네트워크와 서버 실행 상태를 확인해주세요.";

        string detail = RemoveHttpPrefix(exception.Message, exception.StatusCode);

        if (ContainsKnownAuthMessage(detail))
            return detail;

        if (isRegistration && exception.StatusCode >= 500)
            return "이미 사용 중인 이메일이거나 서버 오류가 발생했습니다.";

        if (exception.StatusCode == 401 || exception.StatusCode == 403)
            return "이메일 또는 비밀번호가 올바르지 않습니다.";

        if (exception.StatusCode == 404)
            return "존재하지 않는 계정입니다.";

        if (exception.StatusCode >= 500)
            return "서버 오류가 발생했습니다. 잠시 후 다시 시도해주세요.";

        if (!string.IsNullOrWhiteSpace(detail))
            return detail;

        return "요청을 처리하지 못했습니다. 입력값을 확인해주세요.";
    }

    private static bool ContainsKnownAuthMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return false;

        return message.Contains("존재하지 않는 이메일")
               || message.Contains("비밀번호가 일치하지 않습니다")
               || message.Contains("이미 사용 중")
               || message.Contains("중복");
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
        if (loginPanel == null || registerPanel == null
            || loginEmailInput == null || loginPasswordInput == null
            || registerEmailInput == null || registerPasswordInput == null
            || registerNicknameInput == null || loginButton == null
            || registerButton == null || statusText == null
            || currentUserText == null || showLoginButton == null
            || showRegisterButton == null || logoutButton == null)
        {
            Debug.LogWarning("[AuthUIManager] Inspector의 UI 참조가 일부 연결되지 않았습니다.", this);
        }
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
