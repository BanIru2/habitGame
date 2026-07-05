using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;  // UTF-8 ����Ʈ Ÿ������ ���� ���� �ʿ�
using System.Threading.Tasks;   // Task ���� ���� �ʿ�
using UnityEngine;
using UnityEngine.Networking;

public class ApiClient : Singleton<ApiClient>
{
    private static string BASE_URL;    // server api 입력 필요
    private string _jwt;               // accessToken
    private long _currentUserId;       // 현재 로그인 유저 ID
    
    protected override void Awake()
    {
        base.Awake();

        // 로그인 UI가 붙기 전 임시 테스트용
        SetCurrentUserId(1);
    }
    // LoginManager(����)���� ���
    public void SetAccessToken(string token)
    {
        _jwt = token;
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
        // ��û�� ���� �ּ� �ϼ���Ű��
        string url = BASE_URL + path;
        // HTTP ��û ��ü ����
        using UnityWebRequest request = CreateRequest(method, url, body);
        // ���� ��� ���̱�
        ApplyHeaders(request);

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
            throw new Exception($"API request failed: {request.responseCode} {request.error}");
        }
        // ���� ������ Null�̰ų� Empty�ų� WhiteSpace �� �� �⺻��(TResponse Ÿ�Կ� ���� null/0/false etc) ��ȯ
        if (string.IsNullOrWhiteSpace(responseText))
            return default;

        // Json ���ڿ��� C#Ŭ����(��û���� Task<TResponse> Ÿ��)�� ������ȭ - await�� TResponse ���� ��� ����
        return JsonConvert.DeserializeObject<TResponse>(responseText);
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
    private void ApplyHeaders(UnityWebRequest request)
    {
        // json ���� ��û �Ѵٴ� ����
        request.SetRequestHeader("Accept", "application/json");
        // body�� json�������� �˸��� ����
        request.SetRequestHeader("Content-Type", "application/json");
        // ��û�� ������ ������ Ȯ���ϱ� ���� access token ����
        if (!string.IsNullOrEmpty(_jwt))
            request.SetRequestHeader("Authorization", "Bearer " + _jwt);
    }
}
