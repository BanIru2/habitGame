using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ErrorPopupManager : Singleton<ErrorPopupManager>
{
    [SerializeField]
    private GameObject errorPopup;
    [SerializeField]
    private TextMeshProUGUI errorMessageText;
    [SerializeField]
    private Button closeButton;

    protected override void Awake()
    {
        base.Awake();
        closeButton.onClick.AddListener(ClosePopup);
    }

    private static string GetDisplayMessage(ApiException exception)
    {
        if (exception == null || string.IsNullOrWhiteSpace(exception.Message))
            return "요청 처리에 실패했습니다.";

        string prefix = $"HTTP {exception.StatusCode}: ";

        return exception.Message.StartsWith(prefix) ? exception.Message.Substring(prefix.Length) : exception.Message;
    }

    public void ShowApiError(ApiException exception)
    {
        errorMessageText.text = GetDisplayMessage(exception);
        errorPopup.SetActive(true);
    }

    public void ShowSystemError()
    {
        errorMessageText.text = "요청 처리 중 시스템 오류가 발생했습니다";
        errorPopup.SetActive(true);
    }

    private void ClosePopup()
    {
        errorMessageText.text = "";
        errorPopup.SetActive(false);
    }
}
