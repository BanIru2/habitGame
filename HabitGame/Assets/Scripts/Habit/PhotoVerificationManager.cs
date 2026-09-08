using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PhotoVerificationManager : MonoBehaviour
{
    [Header("사진 인증 팝업")]
    [SerializeField]
    private GameObject photoVerificationPopup;
    [SerializeField]
    private RawImage photoView;
    [SerializeField]
    private Button photoButton;
    [SerializeField]
    private Button verifyButton;
    [SerializeField]
    private Button cancelButton;

    [Header("결과 팝업 UI")]
    [SerializeField]
    private GameObject resultPopup;
    [SerializeField]
    private TextMeshProUGUI resultText;
    [SerializeField]
    private TextMeshProUGUI reasonText;
    [SerializeField]
    private TextMeshProUGUI detectedActionText;
    [SerializeField]
    private TextMeshProUGUI attrText;
    [SerializeField]
    private TextMeshProUGUI expText;
    [SerializeField]
    private TextMeshProUGUI streakText;
    [SerializeField]
    private Button checkVerificationResultButton;


    private HabitGoalResponse habitData;
    private long goalId;
    private byte[] photoBytes;

    // 팝업 활성화
    public void ActivatePhotoVerificationPopup()
    {
        photoButton.onClick.RemoveAllListeners();
        verifyButton.onClick.RemoveAllListeners();
        cancelButton.onClick.RemoveAllListeners();
        photoVerificationPopup.SetActive(true);
        InitPopup();
    }

    // 팝업 활성화 시 버튼 리스너 연결 등 초기 세팅 기능
    private void InitPopup()
    {
        photoButton.onClick.AddListener(ShootPhoto);
        verifyButton.onClick.AddListener(Verifying);
        cancelButton.onClick.AddListener(ExitPopup);
    }

    // 인증 팝업을 열기 위한 버튼 클릭 시 데이터 전달 받기
    public void GetHabitData(HabitGoalResponse data)
    {
        habitData = data;
        goalId = data.Id;
    }

    // 사진 촬영 버튼 클릭 시 촬영 기능 열기
    private void ShootPhoto()
    {

    }

    // 응답 결과에 따른 처리
    private void HandleResult(HabitVerifyResponse response)
    {
        string status = response.Status;
        checkVerificationResultButton.onClick.RemoveAllListeners();
        switch (status)
        {
            case "success":
                // 성공 팝업 띄우기, 성공했으니 성공 팝업 확인 누를 시 인증 팝업 닫기
                Debug.Log("인증 성공!");
                resultText.text = "인증에 성공하였습니다";
                reasonText.text = response.Reason;
                detectedActionText.text = response.DetectedAction;
                int beforeExp = response.TotalExp - response.RewardExp; 
                switch (response.RewardAttribute)
                {
                    case "Fire":
                        attrText.text = "불 속성 경험치 증가";
                        break;
                    case "Water":
                        attrText.text = "물 속성 경험치 증가";
                        break;
                    case "Grass":
                        attrText.text = "풀 속성 경험치 증가";
                        break;
                    case "Aurora":
                        attrText.text = "오로라 속성 경험치 증가";
                        break;
                }
                expText.text = response.TotalExp + " (" + beforeExp + " + " + response.RewardExp + ")";
                streakText.text = "연속 달성 일수 : " + response.CurrentStreak + "일";
                attrText.gameObject.SetActive(true);
                expText.gameObject.SetActive(true);
                streakText.gameObject.SetActive(true);
                resultPopup.SetActive(true);
                checkVerificationResultButton.onClick.AddListener(() =>
                {
                    resultPopup.SetActive(false);
                    ExitPopup();
                    InitResultPopupData();
                });
                break;
            case "false":
            case "retry":
                // 실패 팝업 띄우기, 실패했으니 실패 팝업 확인 누를 시 인증 팝업으로 돌아가기 위해 실패 팝업만 닫고 인증팝업은 활성화된 그대로 유지
                // ==> 성공에서 ExitPopup()만 안하면 됨
                Debug.Log(status == "false" ? "사진이 습관과 관련이 없습니다." : "AI 신뢰도가 낮습니다");
                resultText.text = "인증에 실패하였습니다\n다시시도해주세요";
                reasonText.text = response.Reason;
                detectedActionText.text = response.DetectedAction;
                attrText.gameObject.SetActive(false);
                expText.gameObject.SetActive(false);
                streakText.gameObject.SetActive(false);
                resultPopup.SetActive(true);
                checkVerificationResultButton.onClick.AddListener(() =>
                {
                    resultPopup.SetActive(false);
                    InitResultPopupData();
                });
                break;
        }
    }

    private void InitResultPopupData()
    {
        resultText.text = "";
        reasonText.text = "";
        detectedActionText.text = "";
        attrText.text = "";
        expText.text = "";
        streakText.text = "";
    }

    // 입력 받은 사진 데이터로 서버->LLM 인증 요청
    private async void Verifying()
    {
        if (photoBytes == null || photoBytes.Length == 0) return;

        verifyButton.interactable = false;

        try
        {
            HabitVerifyResponse response = await ServiceRegistry.Instance.Habit.VerifyHabitPhotoAsync(goalId, photoBytes);
            if(response != null) HandleResult(response);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"인증 통신 오류: {e.Message}");
        }
        finally
        {
            verifyButton.interactable = true;
        }
    }

    // 팝업 닫기
    private void ExitPopup()
    {
        photoVerificationPopup.SetActive(false);
        habitData = null;
        goalId = -1;
        photoBytes = null;
    }

}
