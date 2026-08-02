using System.Globalization;
using UnityEngine;

/// <summary>
/// 현재 자체 로그인 사용자의 비민감 식별 정보만 관리합니다.
/// 비밀번호와 요청 JSON은 저장하지 않습니다.
/// </summary>
public static class UserSession
{
    private const string UserIdKey = "HabitPVP.Auth.UserId";
    private const string NicknameKey = "HabitPVP.Auth.Nickname";
    private const string EmailKey = "HabitPVP.Auth.Email";
    private const string IsLoggedInKey = "HabitPVP.Auth.IsLoggedIn";

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

    public static bool TryRestore()
    {
        if (PlayerPrefs.GetInt(IsLoggedInKey, 0) != 1)
        {
            ClearMemory();
            DeleteStoredSession();
            return false;
        }

        string storedUserId = PlayerPrefs.GetString(UserIdKey, string.Empty);
        string storedNickname = PlayerPrefs.GetString(NicknameKey, string.Empty);
        string storedEmail = PlayerPrefs.GetString(EmailKey, string.Empty);

        bool valid = long.TryParse(
            storedUserId,
            NumberStyles.None,
            CultureInfo.InvariantCulture,
            out long userId
        ) && userId > 0
          && !string.IsNullOrWhiteSpace(storedNickname)
          && !string.IsNullOrWhiteSpace(storedEmail);

        if (!valid)
        {
            Logout();
            return false;
        }

        UserId = userId;
        Nickname = storedNickname;
        Email = storedEmail;
        IsLoggedIn = true;
        ApplyCurrentUserId();
        return true;
    }

    public static void Logout()
    {
        ClearMemory();
        DeleteStoredSession();
        ApplyCurrentUserId();
    }

    private static void DeleteStoredSession()
    {
        PlayerPrefs.DeleteKey(UserIdKey);
        PlayerPrefs.DeleteKey(NicknameKey);
        PlayerPrefs.DeleteKey(EmailKey);
        PlayerPrefs.DeleteKey(IsLoggedInKey);
        PlayerPrefs.Save();
    }

    private static void SetUser(long userId, string email, string nickname)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(nickname))
            throw new System.ArgumentException("이메일과 닉네임이 모두 필요합니다.");

        UserId = userId;
        Email = email;
        Nickname = nickname;
        IsLoggedIn = true;

        PlayerPrefs.SetString(UserIdKey, userId.ToString(CultureInfo.InvariantCulture));
        PlayerPrefs.SetString(EmailKey, email);
        PlayerPrefs.SetString(NicknameKey, nickname);
        PlayerPrefs.SetInt(IsLoggedInKey, 1);
        PlayerPrefs.Save();
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
