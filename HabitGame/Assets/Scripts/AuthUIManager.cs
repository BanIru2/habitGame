using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// 자체 계정 로그인/회원가입 화면을 관리합니다.
/// HTTP 통신은 기존 AuthService에 위임하고, 인증 정보는 현재 실행 세션에만 보관합니다.
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
    [SerializeField] private TMP_Text loginButtonText;

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
    [SerializeField] private string gameplaySceneName = "JS";

    private bool requestInProgress;
    private Coroutine statusClearCoroutine;
    private Font runtimeKoreanSourceFont;
    private TMP_FontAsset runtimeKoreanFontAsset;

    private void Awake()
    {
        EnsureKoreanFontCoverage();
        ConfigureEmailInput(loginEmailInput);
        ConfigureEmailInput(registerEmailInput);
        ConfigurePasswordInput(loginPasswordInput);
        ConfigurePasswordInput(registerPasswordInput);
        BindControls();
        WarnAboutMissingReferences();
    }

    private void Start()
    {
        ShowLoginPanel();
        UpdateCurrentUserText();
        ShowStatus(string.Empty);

        if (loginEmailInput != null)
            loginEmailInput.Select();
    }

    private void OnDisable()
    {
        bool hadTemporaryStatus = statusClearCoroutine != null;
        CancelStatusClear();

        if (hadTemporaryStatus)
            ShowStatus(string.Empty);
    }

    private void OnDestroy()
    {
        CancelStatusClear();
        UnbindControls();

        if (runtimeKoreanFontAsset != null)
            Destroy(runtimeKoreanFontAsset);

        if (runtimeKoreanSourceFont != null)
            Destroy(runtimeKoreanSourceFont);
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

        SetBusy(true);
        ShowStatus("로그인 중...", isError: false);

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
            LoadGameplayScene();
        }
        catch (ApiException exception)
        {
            if (this != null)
            {
                Debug.LogWarning($"[AuthUIManager] Login failed ({exception.StatusCode}): {exception.Message}", this);
                ShowStatus(GetApiErrorMessage(exception, isRegistration: false));
            }
        }
        catch (Exception exception)
        {
            if (this != null)
            {
                Debug.LogException(exception, this);
                ShowStatus("예상하지 못한 오류가 발생했습니다. 잠시 후 다시 시도해주세요.");
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
        string password = ReadPassword(registerPasswordInput);
        string nickname = ReadTrimmed(registerNicknameInput);

        string validationMessage = ValidateAccountInput(email, password);
        if (validationMessage == null && string.IsNullOrWhiteSpace(nickname))
            validationMessage = "닉네임을 입력해주세요.";

        if (validationMessage != null)
        {
            ShowStatus(validationMessage);
            return;
        }

        SetBusy(true);
        ShowStatus("계정을 만드는 중...", isError: false);
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
            ShowStatus("가입 완료. 로그인 중...", isError: false);

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
            LoadGameplayScene();
        }
        catch (ApiException exception)
        {
            if (this != null)
            {
                Debug.LogWarning($"[AuthUIManager] Registration failed ({exception.StatusCode}): {exception.Message}", this);
                ShowStatus(GetApiErrorMessage(exception, isRegistration: !registrationCompleted));
            }
        }
        catch (Exception exception)
        {
            if (this != null)
            {
                Debug.LogException(exception, this);
                ShowStatus("계정을 만드는 중 오류가 발생했습니다. 잠시 후 다시 시도해주세요.");
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

        if (registerEmailInput != null)
            registerEmailInput.Select();
    }

    private void OnLogoutClicked()
    {
        if (requestInProgress)
            return;

        UserSession.Logout();
        ClearPasswordInputs();
        UpdateCurrentUserText();
        ShowLoginPanel();
        ShowStatus("로그아웃되었습니다.", isError: false, autoClear: true);
    }

    private void OnPasswordSubmitted(string _)
    {
        OnLoginClicked();
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
            throw new InvalidOperationException("현재 사용자 ID를 API 클라이언트에 적용하지 못했습니다.");
    }

    private void LoadGameplayScene()
    {
        if (string.IsNullOrWhiteSpace(gameplaySceneName)
            || !Application.CanStreamedLevelBeLoaded(gameplaySceneName))
        {
            Debug.LogError($"[AuthUIManager] Scene '{gameplaySceneName}' is not available in Build Settings.", this);
            ShowStatus("게임 화면을 불러올 수 없습니다. 빌드 설정을 확인해주세요.");
            return;
        }

        SceneManager.LoadScene(gameplaySceneName);
    }

    private void ShowLoginPanel()
    {
        SetActive(loginPanel, true);
        SetActive(registerPanel, false);

        if (logoutButton != null)
            logoutButton.gameObject.SetActive(false);
    }

    private void UpdateCurrentUserText()
    {
        if (currentUserText == null)
            return;

        currentUserText.text = UserSession.IsLoggedIn
            ? $"{UserSession.Nickname} 님"
            : string.Empty;
        currentUserText.gameObject.SetActive(UserSession.IsLoggedIn);
    }

    private void SetBusy(bool busy)
    {
        requestInProgress = busy;
        SetInteractable(loginButton, !busy);
        SetInteractable(registerButton, !busy);
        SetInteractable(showLoginButton, !busy);
        SetInteractable(showRegisterButton, !busy);
        SetInteractable(logoutButton, !busy);

        if (loginButtonText != null)
            loginButtonText.text = busy ? "로그인 중..." : "로그인";
    }

    private void ClearPasswordInputs()
    {
        if (loginPasswordInput != null)
            loginPasswordInput.text = string.Empty;

        if (registerPasswordInput != null)
            registerPasswordInput.text = string.Empty;
    }

    private void ShowStatus(string message, bool isError = true, bool autoClear = false)
    {
        CancelStatusClear();

        if (statusText != null)
        {
            bool hasMessage = !string.IsNullOrEmpty(message);
            statusText.text = message ?? string.Empty;
            statusText.color = isError
                ? new Color32(229, 57, 53, 255)
                : new Color32(30, 136, 229, 255);
            statusText.gameObject.SetActive(hasMessage);
        }

        if (!autoClear || string.IsNullOrEmpty(message))
            return;

        if (isActiveAndEnabled)
            statusClearCoroutine = StartCoroutine(ClearStatusAfterDelay());
    }

    private IEnumerator ClearStatusAfterDelay()
    {
        yield return new WaitForSecondsRealtime(TemporaryStatusDuration);
        statusClearCoroutine = null;
        ShowStatus(string.Empty);
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
            return "아이디를 입력해주세요.";

        if (string.IsNullOrEmpty(password))
            return "비밀번호를 입력해주세요.";

        return null;
    }

    private static string GetApiErrorMessage(ApiException exception, bool isRegistration)
    {
        if (exception.StatusCode <= 0)
        {
            return exception.Message.IndexOf("timed out", StringComparison.OrdinalIgnoreCase) >= 0
                ? "요청 시간이 초과되었습니다. 네트워크를 확인하고 다시 시도해주세요."
                : "서버에 연결할 수 없습니다. 잠시 후 다시 시도해주세요.";
        }

        string detail = RemoveHttpPrefix(exception.Message, exception.StatusCode);

        if (!isRegistration
            && (detail.Contains("존재하지 않는 이메일")
                || detail.Contains("비밀번호가 일치하지 않습니다")))
        {
            return "아이디 또는 비밀번호가 올바르지 않습니다.";
        }

        if (isRegistration && ContainsDuplicateAccountMessage(detail))
            return "이미 사용 중인 이메일입니다.";

        if (exception.StatusCode == 401 || exception.StatusCode == 403 || exception.StatusCode == 404)
            return "아이디 또는 비밀번호가 올바르지 않습니다.";

        if (exception.StatusCode >= 500)
            return "서버에서 오류가 발생했습니다. 잠시 후 다시 시도해주세요.";

        return isRegistration
            ? "입력 정보를 확인하고 다시 시도해주세요."
            : "아이디 또는 비밀번호가 올바르지 않습니다.";
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

    private void EnsureKoreanFontCoverage()
    {
        TMP_Text[] labels = GetComponentsInChildren<TMP_Text>(true);
        if (labels.Length == 0 || (labels[0].font != null && labels[0].font.HasCharacter('가', true)))
            return;

        string[] installedFonts = Font.GetOSInstalledFontNames();
        string[] preferredFonts =
        {
            "Malgun Gothic",
            "Apple SD Gothic Neo",
            "Noto Sans CJK KR",
            "Noto Sans KR",
            "NanumGothic",
            "Arial Unicode MS"
        };

        string selectedFont = null;
        foreach (string preferredFont in preferredFonts)
        {
            selectedFont = Array.Find(
                installedFonts,
                installed => installed.IndexOf(preferredFont, StringComparison.OrdinalIgnoreCase) >= 0
            );

            if (!string.IsNullOrEmpty(selectedFont))
                break;
        }

        if (string.IsNullOrEmpty(selectedFont))
        {
            Debug.LogWarning("[AuthUIManager] 한글을 지원하는 시스템 폰트를 찾지 못했습니다.", this);
            return;
        }

        runtimeKoreanSourceFont = Font.CreateDynamicFontFromOSFont(selectedFont, 32);
        if (runtimeKoreanSourceFont == null)
            return;

        runtimeKoreanFontAsset = TMP_FontAsset.CreateFontAsset(runtimeKoreanSourceFont);
        if (runtimeKoreanFontAsset == null)
            return;

        foreach (TMP_Text label in labels)
            label.font = runtimeKoreanFontAsset;
    }

    private void BindControls()
    {
        AddListener(loginButton, OnLoginClicked);
        AddListener(registerButton, OnRegisterClicked);
        AddListener(showLoginButton, OnShowLoginClicked);
        AddListener(showRegisterButton, OnShowRegisterClicked);
        AddListener(logoutButton, OnLogoutClicked);

        if (loginPasswordInput != null)
            loginPasswordInput.onSubmit.AddListener(OnPasswordSubmitted);
    }

    private void UnbindControls()
    {
        RemoveListener(loginButton, OnLoginClicked);
        RemoveListener(registerButton, OnRegisterClicked);
        RemoveListener(showLoginButton, OnShowLoginClicked);
        RemoveListener(showRegisterButton, OnShowRegisterClicked);
        RemoveListener(logoutButton, OnLogoutClicked);

        if (loginPasswordInput != null)
            loginPasswordInput.onSubmit.RemoveListener(OnPasswordSubmitted);
    }

    private void WarnAboutMissingReferences()
    {
        string missing = string.Empty;
        AppendMissingReference(ref missing, loginPanel, nameof(loginPanel));
        AppendMissingReference(ref missing, loginEmailInput, nameof(loginEmailInput));
        AppendMissingReference(ref missing, loginPasswordInput, nameof(loginPasswordInput));
        AppendMissingReference(ref missing, loginButton, nameof(loginButton));
        AppendMissingReference(ref missing, loginButtonText, nameof(loginButtonText));
        AppendMissingReference(ref missing, registerPanel, nameof(registerPanel));
        AppendMissingReference(ref missing, registerEmailInput, nameof(registerEmailInput));
        AppendMissingReference(ref missing, registerPasswordInput, nameof(registerPasswordInput));
        AppendMissingReference(ref missing, registerNicknameInput, nameof(registerNicknameInput));
        AppendMissingReference(ref missing, registerButton, nameof(registerButton));
        AppendMissingReference(ref missing, statusText, nameof(statusText));
        AppendMissingReference(ref missing, showLoginButton, nameof(showLoginButton));
        AppendMissingReference(ref missing, showRegisterButton, nameof(showRegisterButton));

        if (!string.IsNullOrEmpty(missing))
            Debug.LogError($"[AuthUIManager] Inspector 연결 누락: {missing}.", this);
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
