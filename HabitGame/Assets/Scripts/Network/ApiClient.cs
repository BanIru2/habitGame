using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;  // UTF-8 ����Ʈ Ÿ������ ���� ���� �ʿ�
using System.Threading.Tasks;   // Task ���� ���� �ʿ�
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.SceneManagement;

public class ApiClient : Singleton<ApiClient>
{
    private const string BASE_URL = "http://localhost:8080";
    private const string LOGIN_SCENE_NAME = "LoginScene";
    private static readonly string[] REFRESH_EXCLUDED_PATHS =
    {
        "/auth/login",
        "/auth/register",
        "/auth/refresh",
        "/auth/logout"
    };

    private readonly object authenticationSync = new object();
    private string _jwt;               // accessToken
    private long _currentUserId;       // 현재 로그인 유저 ID
    private Func<Task> refreshAccessTokenAsync;
    private Action authenticationExpired;
    private Task currentRefreshTask;
    private long accessTokenVersion;
    private long refreshGeneration;
    private bool authenticationExpirationHandled;
    
    protected override void Awake()
    {
        base.Awake();

        // 로그인 UI가 붙기 전 임시 테스트용
        SetCurrentUserId(UserSession.TryRestore() ? UserSession.UserId : 0);
    }
    // LoginManager(����)���� ���
    public void SetAccessToken(string token)
    {
        lock (authenticationSync)
        {
            _jwt = token;
            accessTokenVersion++;

            if (!string.IsNullOrWhiteSpace(token))
                authenticationExpirationHandled = false;
        }
    }

    public void ConfigureAuthentication(
        Func<Task> refreshHandler,
        Action authenticationExpiredHandler)
    {
        lock (authenticationSync)
        {
            refreshAccessTokenAsync = refreshHandler;
            authenticationExpired = authenticationExpiredHandler;
        }
    }

    public void SetCurrentUserId(long userId)
    {
        _currentUserId = userId;
    }

    public long CurrentUserId => _currentUserId;

    // Task ȣ�� �� await�� �ٿ��� ȣ��
    /// <summary>
    /// GET ��û�� ���� �Լ� (Generic)
    /// </summary>
    /// <typeparam name="TResponse"></typeparam>
    /// <param name="path"></param>
    /// <returns></returns>
    public Task<TResponse> GetAsync<TResponse>(string path)
    {
        return SendAsync<TResponse>("GET", path, null);
    }
    /// <summary>
    /// ������ �����͸� ������ ���� �Լ� (�α��� ��û �� �α��ε����� ��)
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    /// <param name="path"></param>
    /// <param name="body"></param>
    /// <returns></returns>
    public Task<TResponse> PostAsync<TRequest, TResponse>(string path, TRequest body)
    {
        return SendAsync<TResponse>("POST", path, body);
    }
    /// <summary>
    /// ���� �����͸� �Ϻ� �����ϱ� ���� ������ ������ ������ ���� �Լ�
    /// </summary>
    /// <typeparam name="TRequest"></typeparam>
    /// <typeparam name="TResponse"></typeparam>
    /// <param name="path"></param>
    /// <param name="body"></param>
    /// <returns></returns>
    public Task<TResponse> PatchAsync<TRequest, TResponse>(string path, TRequest body)
    {
        return SendAsync<TResponse>("PATCH", path, body);
    }

    private async Task<TResponse> SendAsync<TResponse>(string method, string path, object body)
    {
        AuthenticationSnapshot authentication = CaptureAuthentication();

        try
        {
            return await SendOnceAsync<TResponse>(method, path, body, authentication.AccessToken);
        }
        catch (ApiException exception) when (
            exception.StatusCode == 401 && CanAttemptRefresh(path, authentication))
        {
            Task refreshTask = GetOrStartRefreshTask(authentication);

            try
            {
                await refreshTask;
            }
            catch (Exception refreshException)
            {
                if (IsAuthenticationExpired(refreshException))
                    HandleAuthenticationExpired();

                throw;
            }

            AuthenticationSnapshot retryAuthentication = CaptureAuthentication();

            try
            {
                return await SendOnceAsync<TResponse>(
                    method,
                    path,
                    body,
                    retryAuthentication.AccessToken
                );
            }
            catch (ApiException retryException) when (retryException.StatusCode == 401)
            {
                HandleAuthenticationExpired();
                throw;
            }
        }
    }

    private async Task<TResponse> SendOnceAsync<TResponse>(
        string method,
        string path,
        object body,
        string accessToken)
    {
        // ��û�� ���� �ּ� �ϼ���Ű��
        string url = BASE_URL + path;
        // HTTP ��û ��ü ����
        using UnityWebRequest request = CreateRequest(method, url, body);
        // ���� ��� ���̱�
        ApplyHeaders(request, accessToken);

        // ��û �߻�
        UnityWebRequestAsyncOperation operation = request.SendWebRequest();
        // ��û �� ���
        while (!operation.isDone)
            await Task.Yield();

        // ��û�� ���� ���� ������ (null�̸� �� ���ڿ�)
        string responseText = request.downloadHandler != null
            ? request.downloadHandler.text : string.Empty;

        // ��û ���� �� �α� ��� �� ���� ������ (��û ������ try-catch�� �ޱ�)
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[ApiClient] {method} {url} failed\nStatus: {request.responseCode}\nBody: {responseText}");
            throw ApiException.FromResponse(
                request.responseCode,
                request.error,
                responseText
            );
        }
        // ���� ������ Null�̰ų� Empty�ų� WhiteSpace �� �� �⺻��(TResponse Ÿ�Կ� ���� null/0/false etc) ��ȯ
        if (string.IsNullOrWhiteSpace(responseText))
            return default;

        // Json ���ڿ��� C#Ŭ����(��û���� Task<TResponse> Ÿ��)�� ������ȭ - await�� TResponse ���� ��� ����
        return JsonConvert.DeserializeObject<TResponse>(responseText);
    }

    private AuthenticationSnapshot CaptureAuthentication()
    {
        lock (authenticationSync)
        {
            return new AuthenticationSnapshot(_jwt, accessTokenVersion, refreshGeneration);
        }
    }

    private bool CanAttemptRefresh(string path, AuthenticationSnapshot authentication)
    {
        if (string.IsNullOrWhiteSpace(authentication.AccessToken)
            || IsRefreshExcludedPath(path))
        {
            return false;
        }

        lock (authenticationSync)
        {
            return refreshAccessTokenAsync != null && !authenticationExpirationHandled;
        }
    }

    private Task GetOrStartRefreshTask(AuthenticationSnapshot requestAuthentication)
    {
        lock (authenticationSync)
        {
            if (accessTokenVersion != requestAuthentication.AccessTokenVersion)
                return Task.CompletedTask;

            if (refreshGeneration != requestAuthentication.RefreshGeneration
                && currentRefreshTask != null)
            {
                return currentRefreshTask;
            }

            refreshGeneration++;

            try
            {
                currentRefreshTask = refreshAccessTokenAsync();
            }
            catch (Exception exception)
            {
                currentRefreshTask = Task.FromException(exception);
            }

            return currentRefreshTask;
        }
    }

    private void HandleAuthenticationExpired()
    {
        Action expirationHandler;

        lock (authenticationSync)
        {
            if (authenticationExpirationHandled)
                return;

            authenticationExpirationHandled = true;
            expirationHandler = authenticationExpired;
        }

        expirationHandler?.Invoke();
        ReturnToLoginScene();
    }

    public void ReturnToLoginScene()
    {
        if (SceneManager.GetActiveScene().name == LOGIN_SCENE_NAME)
            return;

        if (!Application.CanStreamedLevelBeLoaded(LOGIN_SCENE_NAME))
        {
            Debug.LogError($"[Auth] Scene '{LOGIN_SCENE_NAME}' is not available in Build Settings.");
            return;
        }

        SceneManager.LoadScene(LOGIN_SCENE_NAME);
    }

    private static bool IsRefreshExcludedPath(string path)
    {
        foreach (string excludedPath in REFRESH_EXCLUDED_PATHS)
        {
            if (string.Equals(path, excludedPath, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }

    private static bool IsAuthenticationExpired(Exception exception)
    {
        if (exception is InvalidOperationException)
            return true;

        if (exception is ApiException apiException)
        {
            return apiException.StatusCode == 400
                || apiException.StatusCode == 401
                || apiException.StatusCode == 403;
        }

        return false;
    }

    // ��û ��ü ����
    private UnityWebRequest CreateRequest(string method, string url, object body)
    {
        // GET ��û�� body�� �ʿ����� �ʾ� �ٷ� ����
        if (method == "GET")
            return UnityWebRequest.Get(url);

        // body�� Json���ڿ��� ����ȭ
        string json = body != null
            ? JsonConvert.SerializeObject(body) : "{}";
        // HTTP ��û�� ���� �����ʹ� ����Ʈ �迭�̾�� �ϱ� ������ UTF-8 ����Ʈ �迭�� ��ȯ
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        // ��û ��ü ����
        var request = new UnityWebRequest(url, method);
        // Json �ٵ� ��û�� �Ʊ�
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        // ���� ���� �غ� (�޾ƿ� ������ ������ ���� �Ҵ�)
        request.downloadHandler = new DownloadHandlerBuffer();

        return request;
    }
    // request ��ü�� ��� �߰�
    private void ApplyHeaders(UnityWebRequest request, string accessToken)
    {
        // json ���� ��û �Ѵٴ� ����
        request.SetRequestHeader("Accept", "application/json");
        // body�� json�������� �˸��� ����
        request.SetRequestHeader("Content-Type", "application/json");
        // ��û�� ������ ������ Ȯ���ϱ� ���� access token ����
        if (!string.IsNullOrEmpty(accessToken))
            request.SetRequestHeader("Authorization", "Bearer " + accessToken);
    }

    private readonly struct AuthenticationSnapshot
    {
        public AuthenticationSnapshot(
            string accessToken,
            long accessTokenVersion,
            long refreshGeneration)
        {
            AccessToken = accessToken;
            AccessTokenVersion = accessTokenVersion;
            RefreshGeneration = refreshGeneration;
        }

        public string AccessToken { get; }
        public long AccessTokenVersion { get; }
        public long RefreshGeneration { get; }
    }
}
