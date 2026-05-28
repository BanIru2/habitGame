using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;  // UTF-8 바이트 타입으로 변경 위해 필요
using System.Threading.Tasks;   // Task 선언 위해 필요
using UnityEngine;
using UnityEngine.Networking;

public class ApiClient : Singleton<ApiClient>
{
    private static string BASE_URL;    // server api 입력 필요
    private static string _jwt;     // accessToken

    // LoginManager(가명)에서 사용
    public void SetAccessToken(string token)
    {
        _jwt = token;
    }

    // Task 호출 시 await를 붙여서 호출
    /// <summary>
    /// GET 요청용 단축 함수 (Generic)
    /// </summary>
    /// <typeparam name="TResponse"></typeparam>
    /// <param name="path"></param>
    /// <returns></returns>
    public Task<TResponse> GetAsync<TResponse>(string path)
    {
        return SendAsync<TResponse>("GET", path, null);
    }
    /// <summary>
    /// 서버에 데이터를 보내기 위한 함수 (로그인 요청 시 로그인데이터 등)
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
    /// 기존 데이터를 일부 수정하기 위해 서버에 데이터 보내기 위한 함수
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
        // 요청할 서버 주소 완성시키기
        string url = BASE_URL + path;
        // HTTP 요청 객체 생성
        using UnityWebRequest request = CreateRequest(method, url, body);
        // 공통 헤더 붙이기
        ApplyHeaders(request);

        // 요청 발생
        UnityWebRequestAsyncOperation operation = request.SendWebRequest();
        // 요청 후 대기
        while (!operation.isDone)
            await Task.Yield();

        // 요청에 대한 응답 꺼내기 (null이면 빈 문자열)
        string responseText = request.downloadHandler != null
            ? request.downloadHandler.text : string.Empty;

        // 요청 실패 시 로그 찍기 및 예외 던지기 (요청 측에서 try-catch로 받기)
        if (request.result != UnityWebRequest.Result.Success)
        {
            Debug.LogError($"[ApiClient] {method} {url} failed\nStatus: {request.responseCode}\nBody: {responseText}");
            throw new Exception($"API request failed: {request.responseCode} {request.error}");
        }
        // 응답 내용이 Null이거나 Empty거나 WhiteSpace 일 때 기본값(TResponse 타입에 따라 null/0/false etc) 반환
        if (string.IsNullOrWhiteSpace(responseText))
            return default;

        // Json 문자열을 C#클래스(요청받은 Task<TResponse> 타입)로 역직렬화 - await로 TResponse 꺼내 사용 가능
        return JsonConvert.DeserializeObject<TResponse>(responseText);
    }

    // 요청 객체 생성
    private UnityWebRequest CreateRequest(string method, string url, object body)
    {
        // GET 요청은 body가 필요하지 않아 바로 생성
        if (method == "GET")
            return UnityWebRequest.Get(url);

        // body를 Json문자열로 직렬화
        string json = body != null
            ? JsonConvert.SerializeObject(body) : "{}";
        // HTTP 요청에 보낼 데이터는 바이트 배열이어야 하기 때문에 UTF-8 바이트 배열로 변환
        byte[] bodyRaw = Encoding.UTF8.GetBytes(json);

        // 요청 객체 생성
        var request = new UnityWebRequest(url, method);
        // Json 바디를 요청에 싣기
        request.uploadHandler = new UploadHandlerRaw(bodyRaw);
        // 응답 받을 준비 (받아온 응답을 저장할 공간 할당)
        request.downloadHandler = new DownloadHandlerBuffer();

        return request;
    }
    // request 객체에 헤더 추가
    private void ApplyHeaders(UnityWebRequest request)
    {
        // json 파일 요청 한다는 정보
        request.SetRequestHeader("Accept", "application/json");
        // body가 json형식임을 알리는 정보
        request.SetRequestHeader("Content-Type", "application/json");
        // 요청한 유저의 정보를 확인하기 위한 access token 정보
        if (!string.IsNullOrEmpty(_jwt))
            request.SetRequestHeader("Authorization", "Bearer " + _jwt);
    }
}
