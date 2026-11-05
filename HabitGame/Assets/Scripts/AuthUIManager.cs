using System;
using System.Text;
using System.Threading.Tasks;
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
    private const float LoginStatusHeight = 100f;
    private const float RegisterStatusHeight = 72f;
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
    private float registerStatusY = -225f;

    private void Awake()
    {
        ConfigureLoginIdInput(loginEmailInput);
        ConfigureRegistrationIdInput(registerEmailInput);
        ConfigurePasswordInput(loginPasswordInput);
        ConfigurePasswordInput(registerPasswordInput);
        ConfigurePasswordInput(registerPasswordConfirmationInput);
        ConfigureIdPlaceholder(loginEmailInput);
        ConfigureIdPlaceholder(registerEmailInput);
        CreateRegistrationGuidanceTexts();
        BindControls();
        WarnAboutMissingReferences();
    }

    private async void Start()
    {
        ShowLoginPanel(clearStatus: true);
        RestoreRememberedLoginId();

        bool autoLoginSucceeded = await TryAutoLoginAsync();
        if (this == null || autoLoginSucceeded)
            return;

        if (rememberLoginIdToggle != null && rememberLoginIdToggle.isOn)
            loginPasswordInput?.Select();
        else
            loginEmailInput?.Select();
    }

    private async Task<bool> TryAutoLoginAsync()
    {
        if (requestInProgress || !TokenStorage.HasRefreshToken())
            return false;

        AuthService authService = null;
        SetBusy(AuthRequest.AutoLogin);
        ShowStatus("저장된 로그인 정보를 확인 중입니다.", isError: false);

        try
        {
            string storedRefreshToken = TokenStorage.LoadRefreshToken();
            if (string.IsNullOrWhiteSpace(storedRefreshToken))
                return false;

            authService = GetAuthService();
            AuthSession.SetRefreshToken(storedRefreshToken);

            await authService.RefreshAsync();
            MeResponse response = await authService.GetMeAsync();

            if (this == null)
                return false;

            UserSession.SetUser(response);
            EnsureSessionMatches(response.UserId);
            ClearPasswordInputs();
            ShowStatus(string.Empty);

            return LoadGameplayScene();
        }
        catch (ApiException exception)
        {
            if (IsTransientAuthenticationFailure(exception) && authService != null)
                authService.ClearRuntimeSession();
            else
                ClearFailedAutoLogin(authService);

            if (this != null)
            {
                Debug.LogWarning($"[Auth] Automatic login failed ({exception.StatusCode}): {exception.Message}", this);
                ShowStatus(GetAutoLoginErrorMessage(exception));
            }

            return false;
        }
        catch (Exception exception)
        {
            ClearFailedAutoLogin(authService);

            if (this != null)
            {
                Debug.LogError($"[Auth] Automatic login failed: {exception}", this);
                ShowStatus("자동 로그인에 실패했습니다. 다시 로그인해주세요.");
            }

            return false;
        }
        finally
        {
            if (this != null)
                SetBusy(AuthRequest.None);
        }
    }

    private void OnDestroy()
    {
        UnbindControls();
    }

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.Tab)
            || loginPanel == null
            || !loginPanel.activeInHierarchy)
            return;

        bool shiftPressed = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (loginEmailInput != null && loginEmailInput.isFocused && !shiftPressed)
        {
            FocusInput(loginPasswordInput);
            return;
        }

        if (loginPasswordInput != null && loginPasswordInput.isFocused && shiftPressed)
            FocusInput(loginEmailInput);
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
        SetStatusLayout(registerStatusY, RegisterStatusHeight);
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
        SetStatusLayout(LoginStatusY, LoginStatusHeight);

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

    private bool LoadGameplayScene()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        // 현재 실행 중인 씬과 이동하려는 씬이 같거나, gameplaySceneName이 비어있는 경우
        // (예: JS 작업 씬에 AuthCanvas를 직접 배치하여 테스트하는 경우)
        // 씬을 다시 로드하면 Start()가 다시 실행되어 무한 재로드 루프에 빠지므로
        // 씬을 다시 로드하지 않고 AuthCanvas UI를 비활성화
        if (string.IsNullOrWhiteSpace(gameplaySceneName)
            || string.Equals(currentSceneName, gameplaySceneName, StringComparison.OrdinalIgnoreCase))
        {
            Debug.Log($"[Auth] 현재 씬('{currentSceneName}')에서 직접 로그인되었습니다. 씬을 재로드하지 않고 AuthCanvas를 비활성화합니다.", this);
            gameObject.SetActive(false);
            return true;
        }

        // 기존 조건 분기 분리
        if (!Application.CanStreamedLevelBeLoaded(gameplaySceneName))
        {
            Debug.LogError($"[Auth] Scene '{gameplaySceneName}' is not available in Build Settings.", this);
            ShowStatus("게임 화면을 불러올 수 없습니다. 빌드 설정을 확인해주세요.");
            return false;
        }

        SceneManager.LoadScene(gameplaySceneName);
        return true;
    }

    private static void ClearFailedAutoLogin(AuthService authService)
    {
        if (authService != null)
        {
            authService.ClearLocalSession();
            return;
        }

        TokenStorage.ClearRefreshToken();
        AuthSession.ClearRefreshToken();
        UserSession.Logout();
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
        {
            loginButtonText.text = request == AuthRequest.AutoLogin
                ? "로그인 확인 중..."
                : request == AuthRequest.Login ? "로그인 중..." : "로그인";
        }

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

    private void SetStatusLayout(float y, float height)
    {
        if (statusText == null)
            return;

        RectTransform rectTransform = statusText.rectTransform;
        rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, y);
        rectTransform.sizeDelta = new Vector2(rectTransform.sizeDelta.x, height);
    }

    private static string ValidateAccountInput(string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email))
            return "아이디를 입력해주세요.";

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
        if (string.IsNullOrWhiteSpace(email))
            return "아이디를 입력해주세요.";

        if (string.IsNullOrEmpty(password))
            return "비밀번호를 입력해주세요.";

        if (string.IsNullOrEmpty(passwordConfirmation))
            return "비밀번호 확인을 입력해주세요.";

        if (string.IsNullOrWhiteSpace(nickname))
            return "닉네임을 입력해주세요.";

        if (email.Length < 4)
            return "아이디가 4자 미만입니다.";

        if (email.Length > 20)
            return "아이디는 20자 이하여야 합니다.";

        foreach (char character in email)
        {
            if (!IsAsciiLetter(character)
                && (character < '0' || character > '9')
                && character != '_')
            {
                return "아이디는 영문, 숫자, 밑줄(_)만 사용할 수 있습니다.";
            }
        }

        if (password.Length < 8)
            return "비밀번호는 8자 이상이어야 합니다.";

        if (password.Length > 64)
            return "비밀번호는 64자 이하여야 합니다.";

        if (string.Equals(password, email, StringComparison.Ordinal))
            return "비밀번호는 아이디와 같을 수 없습니다.";

        bool hasLetter = false;
        bool hasDigit = false;
        bool hasAsciiSpecial = false;

        foreach (char character in password)
        {
            if (char.IsWhiteSpace(character))
                return "비밀번호에는 공백을 사용할 수 없습니다.";

            hasLetter |= IsAsciiLetter(character);
            hasDigit |= character >= '0' && character <= '9';
            hasAsciiSpecial |= IsAsciiSpecial(character);
        }

        if (!hasLetter || !hasDigit || !hasAsciiSpecial)
            return "비밀번호에는 영문, 숫자, 특수문자가 각각 1개 이상 포함되어야 합니다.";

        if (Encoding.UTF8.GetByteCount(password) > 72)
            return "비밀번호는 UTF-8 기준 72바이트 이하여야 합니다.";

        if (!string.Equals(password, passwordConfirmation, StringComparison.Ordinal))
            return "비밀번호가 일치하지 않습니다.";

        return null;
    }

    private static bool IsAsciiLetter(char character)
    {
        return (character >= 'A' && character <= 'Z')
            || (character >= 'a' && character <= 'z');
    }

    private static bool IsAsciiSpecial(char character)
    {
        return (character >= '!' && character <= '/')
            || (character >= ':' && character <= '@')
            || (character >= '[' && character <= '`')
            || (character >= '{' && character <= '~');
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
            return "이미 사용 중인 아이디입니다.";

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

        if (isRegistration && exception.StatusCode == 400 && !string.IsNullOrWhiteSpace(detail))
            return detail;

        return isRegistration
            ? "입력 정보를 확인하고 다시 시도해주세요."
            : "아이디 또는 비밀번호를 확인해주세요.";
    }

    private static string GetAutoLoginErrorMessage(ApiException exception)
    {
        if (exception.StatusCode <= 0)
            return "서버에 연결할 수 없어 자동 로그인에 실패했습니다. 직접 로그인해주세요.";

        if (exception.StatusCode == 400
            || exception.StatusCode == 401
            || exception.StatusCode == 403)
        {
            return "로그인 유지 시간이 만료되었습니다. 다시 로그인해주세요.";
        }

        if (exception.StatusCode >= 500)
            return "서버 오류로 자동 로그인에 실패했습니다. 직접 로그인해주세요.";

        return "자동 로그인에 실패했습니다. 다시 로그인해주세요.";
    }

    private static bool IsTransientAuthenticationFailure(ApiException exception)
    {
        return exception.StatusCode <= 0 || exception.StatusCode >= 500;
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

    private static void ConfigureLoginIdInput(TMP_InputField input)
    {
        if (input == null)
            return;

        input.contentType = TMP_InputField.ContentType.EmailAddress;
        input.lineType = TMP_InputField.LineType.SingleLine;
        input.ForceLabelUpdate();
    }

    private static void ConfigureRegistrationIdInput(TMP_InputField input)
    {
        if (input == null)
            return;

        input.contentType = TMP_InputField.ContentType.Standard;
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

    private static void ConfigureIdPlaceholder(TMP_InputField input)
    {
        if (input != null && input.placeholder is TMP_Text placeholder)
            placeholder.text = "아이디";
    }

    private void CreateRegistrationGuidanceTexts()
    {
        if (registerPanel == null || statusText == null)
            return;

        TMP_Text idGuidance = GetOrCreateRegistrationGuidanceText(
            "IdGuidanceText",
            "아이디: 4~20자, 영문·숫자·_ 사용 가능",
            242f
        );
        TMP_Text passwordGuidance = GetOrCreateRegistrationGuidanceText(
            "PasswordGuidanceText",
            "비밀번호: 8~64자, 영문·숫자·특수문자 포함",
            107f
        );

        ConfigureRegistrationLayout(idGuidance, passwordGuidance);
    }

    private TMP_Text GetOrCreateRegistrationGuidanceText(
        string objectName,
        string message,
        float fallbackAnchoredY)
    {
        Transform existing = registerPanel.transform.Find(objectName);
        TMP_Text guidance = existing == null ? null : existing.GetComponent<TMP_Text>();

        if (guidance == null)
        {
            guidance = Instantiate(statusText, registerPanel.transform);
            guidance.name = objectName;
        }

        guidance.text = message;
        guidance.fontSize = 20f;
        guidance.fontStyle = FontStyles.Normal;
        guidance.alignment = TextAlignmentOptions.Left;
        guidance.enableWordWrapping = false;
        guidance.color = new Color32(107, 78, 50, 255);
        guidance.raycastTarget = false;
        guidance.gameObject.SetActive(true);

        RectTransform rectTransform = guidance.rectTransform;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = new Vector2(0f, fallbackAnchoredY);
        rectTransform.sizeDelta = new Vector2(720f, 30f);
        rectTransform.localScale = Vector3.one;

        return guidance;
    }

    private void ConfigureRegistrationLayout(TMP_Text idGuidance, TMP_Text passwordGuidance)
    {
        RectTransform panel = registerPanel.transform as RectTransform;
        RectTransform caption = panel == null
            ? null
            : panel.Find("RegisterCard/PanelHeader/Caption") as RectTransform;

        if (panel == null || caption == null)
        {
            Debug.LogWarning(
                "[Auth] Registration caption was not found; existing UI positions are preserved.",
                this
            );
            return;
        }

        float nextTop = GetBottomInParent(caption, panel) - 18f;
        nextTop = PlaceBelow(registerEmailInput?.transform as RectTransform, nextTop, 4f);
        nextTop = PlaceBelow(idGuidance?.rectTransform, nextTop, 12f);
        nextTop = PlaceBelow(registerPasswordInput?.transform as RectTransform, nextTop, 4f);
        nextTop = PlaceBelow(passwordGuidance?.rectTransform, nextTop, 12f);
        nextTop = PlaceBelow(
            registerPasswordConfirmationInput?.transform as RectTransform,
            nextTop,
            18f
        );
        nextTop = PlaceBelow(registerNicknameInput?.transform as RectTransform, nextTop, 8f);

        registerStatusY = nextTop - RegisterStatusHeight * 0.5f;
        nextTop -= RegisterStatusHeight + 8f;
        nextTop = PlaceBelow(registerButton?.transform as RectTransform, nextTop, 20f);
        PlaceBelow(showLoginButton?.transform as RectTransform, nextTop, 0f);
    }

    private static float PlaceBelow(RectTransform target, float top, float followingGap)
    {
        if (target == null)
            return top;

        float height = target.rect.height;
        Vector2 position = target.anchoredPosition;
        position.y = top - height * (1f - target.pivot.y);
        target.anchoredPosition = position;
        return top - height - followingGap;
    }

    private static float GetBottomInParent(RectTransform target, RectTransform parent)
    {
        Vector3 bottomCenter = new Vector3(target.rect.center.x, target.rect.yMin, 0f);
        return parent.InverseTransformPoint(target.TransformPoint(bottomCenter)).y;
    }

    private static void FocusInput(TMP_InputField input)
    {
        if (input == null || !input.IsInteractable())
            return;

        input.Select();
        input.ActivateInputField();
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
        AutoLogin,
        Login,
        Register
    }
}
