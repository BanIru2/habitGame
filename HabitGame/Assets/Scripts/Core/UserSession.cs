/// <summary>
/// 현재 실행 중인 게임 세션의 로그인 사용자 식별 정보만 관리합니다.
/// 인증 토큰의 메모리 및 영구 저장은 인증 전용 클래스에서 관리합니다.
/// </summary>
public static class UserSession
{
    public static bool IsLoggedIn { get; private set; }
    public static long UserId { get; private set; }
    public static string Nickname { get; private set; } = string.Empty;
    public static string Email { get; private set; } = string.Empty;

    public static void SetUser(LoginResponse response)
    {
        if (response == null || response.UserId <= 0)
            throw new System.ArgumentException("유효한 로그인 응답이 필요합니다.", nameof(response));

        SetUser(response.UserId, response.Email, response.Nickname);
    }

    public static void SetUser(RegisterResponse response)
    {
        if (response == null || response.Id <= 0)
            throw new System.ArgumentException("유효한 회원가입 응답이 필요합니다.", nameof(response));

        SetUser(response.Id, response.Email, response.Nickname);
    }

    public static void SetUser(MeResponse response)
    {
        if (response == null || response.UserId <= 0)
            throw new System.ArgumentException("유효한 현재 사용자 응답이 필요합니다.", nameof(response));

        SetUser(response.UserId, response.Email, response.Nickname);
    }

    /// <summary>
    /// 기존 ApiClient 호출과의 호환성을 유지하며 현재 메모리 세션만 다시 적용합니다.
    /// 저장 토큰을 사용한 비동기 복원은 로그인 화면의 인증 흐름에서 처리합니다.
    /// </summary>
    public static bool TryRestore()
    {
        ApplyCurrentUserId();
        return IsLoggedIn;
    }

    public static void Logout()
    {
        ClearMemory();
        ClearAccessToken();
        ApplyCurrentUserId();
    }

    private static void SetUser(long userId, string email, string nickname)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(nickname))
            throw new System.ArgumentException("이메일과 닉네임이 모두 필요합니다.");

        UserId = userId;
        Email = email;
        Nickname = nickname;
        IsLoggedIn = true;
        ApplyCurrentUserId();
    }

    private static void ClearMemory()
    {
        IsLoggedIn = false;
        UserId = 0;
        Nickname = string.Empty;
        Email = string.Empty;
    }

    private static void ApplyCurrentUserId()
    {
        ApiClient client = ApiClient.Instance;
        if (client != null)
            client.SetCurrentUserId(IsLoggedIn ? UserId : 0);
    }

    private static void ClearAccessToken()
    {
        ApiClient client = ApiClient.Instance;
        if (client != null)
            client.SetAccessToken(null);
    }
}
