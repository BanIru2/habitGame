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

    [SerializeField]
    private GameObject noticePopup;
    [SerializeField]
    private TextMeshProUGUI noticeText;

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

    private HabitItem habit;
    private long goalId;
    private byte[] photoBytes;
    private int achievedAmount;

    // 1. 팝업 활성화
    // HabitItem에서 호출하면서 저장하고 있는 각 객체의 데이터 받기
    public void ActivatePhotoVerificationPopup(HabitItem item, long id, int achieved)
    {
        GetHabitData(item, id, achieved);
        photoButton.onClick.RemoveAllListeners();
        verifyButton.onClick.RemoveAllListeners();
        cancelButton.onClick.RemoveAllListeners();
        verifyButton.interactable = false;
        photoVerificationPopup.SetActive(true);
        InitPopup();
    }

    // 인증 팝업을 열기 위한 버튼 클릭 시 데이터 전달 받기
    private void GetHabitData(HabitItem item, long id, int achieved)
    {
        habit = item;
        goalId = id;
        achievedAmount = achieved;
    }

    // 팝업 활성화 시 버튼 리스너 연결 등 초기 세팅 기능
    private void InitPopup()
    {
        photoButton.onClick.AddListener(ShootPhoto);
        verifyButton.onClick.AddListener(Verifying);
        cancelButton.onClick.AddListener(ExitPopup);
    }

    // 2. 사진 촬영 버튼 클릭 시 촬영 기능 열기
    // NativeCamera 플러그인 사용
    private void ShootPhoto()
    {
        NativeCamera.TakePicture((path) =>
        {
            if (string.IsNullOrEmpty(path))
            {
                Debug.Log("사진 촬영이 취소되었습니다");
                return;
            }

            Debug.Log($"사진 촬영 완료 경로: {path}");

            // 이전 데이터가 남아있다면 메모리 해제
            if(photoView.texture != null)
            {
                Destroy(photoView.texture);
                photoView.texture = null;
            }

            // 플러그인을 통해 텍스처 로드 (자동 회전 보정 및 1024px 리사이징)
            // markTextureNonReadable: false 로 설정해야 바이트 추출 가능
            Texture2D loadedTexture = NativeCamera.LoadImageAtPath(path, maxSize: 1024, markTextureNonReadable: false);

            if(loadedTexture == null)
            {
                Debug.LogError("사진 텍스처 로드에 실패했습니다.");
                return;
            }

            // UI(RawImage) 연결
            photoView.texture = loadedTexture;

            // 서버 제출용 바이트배열 추출 (품질80의 JPG)
            photoBytes = loadedTexture.EncodeToJPG(80);

            // 사진이 업로드되면 인증버튼 활성화
            verifyButton.interactable = true;
        }, maxSize: 1024);
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
                detectedActionText.text = "인식된 대상 :\n" + response.DetectedAction;
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
                _ = CharacterManager.Instance.RefreshCharacterAsync();
                resultPopup.SetActive(true);
                checkVerificationResultButton.onClick.AddListener(() =>
                {
                    resultPopup.SetActive(false);
                    ExitPopup();
                    habit.GetSuccessOrFailToVerify(true, response.CurrentStreak);
                    InitResultPopupData();
                });
                break;
            case "false":
            case "retry":
                // 실패 팝업 띄우기, 실패했으니 실패 팝업 확인 누를 시 인증 팝업으로 돌아가기 위해 실패 팝업만 닫고 인증팝업은 활성화된 그대로 유지
                Debug.Log(status == "false" ? "사진이 습관과 관련이 없습니다." : "AI 신뢰도가 낮습니다");
                resultText.text = "인증에 실패하였습니다\n다시시도해주세요";
                reasonText.text = response.Reason;
                detectedActionText.text = "인식된 대상 :\n" + response.DetectedAction;
                attrText.gameObject.SetActive(false);
                expText.gameObject.SetActive(false);
                streakText.gameObject.SetActive(false);
                resultPopup.SetActive(true);
                checkVerificationResultButton.onClick.AddListener(() =>
                {
                    resultPopup.SetActive(false);
                    habit.GetSuccessOrFailToVerify(false, response.CurrentStreak);
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

    // 3. 입력 받은 사진 데이터로 서버->LLM 인증 요청
    private async void Verifying()
    {
        if (photoBytes == null || photoBytes.Length == 0) return;

        verifyButton.interactable = false;
        cancelButton.interactable = false;
        photoButton.interactable = false;

        try
        {
            noticePopup.SetActive(true);
            noticeText.text = "인증이 진행중입니다...";
            HabitVerifyResponse response = await ServiceRegistry.Instance.Habit.VerifyHabitPhotoAsync(goalId, photoBytes, achievedAmount);
            if(response == null)
            {
                Debug.LogError("사진 인증 응답이 비어있습니다.");
                // 어떻게 처리해야할까 이건 참...
                return;
            }
            if (response != null)
            {
                Debug.Log("사진 인증 응답 수신 완료. 인증 성공!");
                HandleResult(response);
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"인증 통신 오류: {e.Message}");
        }
        finally
        {
            verifyButton.interactable = true;
            cancelButton.interactable = true;
            photoButton.interactable = true;
            noticePopup.SetActive(false);
        }
    }

    // 4. 팝업 닫기
    private void ExitPopup()
    {
        photoVerificationPopup.SetActive(false);
        goalId = -1;
        photoBytes = null;
        photoView.texture = null;
    }

}
