/// <summary>
/// 현재 실행 중인 게임 세션의 로그인 사용자 식별 정보만 관리합니다.
/// Backend가 인증 토큰을 제공하지 않으므로 앱 종료 후 자동 로그인 정보는 저장하지 않습니다.
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

    /// <summary>
    /// 기존 ApiClient 호출과의 호환성을 유지합니다. 영구 인증 수단이 없어 복원하지 않습니다.
    /// </summary>
    public static bool TryRestore()
    {
        ApplyCurrentUserId();
        return IsLoggedIn;
    }

    public static void Logout()
    {
        ClearMemory();
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
}
