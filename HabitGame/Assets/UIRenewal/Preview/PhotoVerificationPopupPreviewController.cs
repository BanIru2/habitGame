using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UITestScene-only local preview for the PHOTO verification popup.
/// It never opens NativeCamera and never calls an API service.
/// </summary>
public sealed class PhotoVerificationPopupPreviewController : MonoBehaviour
{
    [SerializeField] private GameObject popupRoot;
    [SerializeField] private GameObject photoPopup;
    [SerializeField] private RawImage photoView;
    [SerializeField] private Button photoButton;
    [SerializeField] private Button verifyButton;
    [SerializeField] private Button cancelButton;
    [SerializeField] private GameObject noticePopup;
    [SerializeField] private TMP_Text noticeText;
    [SerializeField] private GameObject resultPopup;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text reasonText;
    [SerializeField] private TMP_Text detectedActionText;
    [SerializeField] private TMP_Text attrText;
    [SerializeField] private TMP_Text expText;
    [SerializeField] private TMP_Text streakText;
    [SerializeField] private Button resultConfirmButton;
    [SerializeField] private Button selectStateButton;
    [SerializeField] private Button loadingStateButton;
    [SerializeField] private Button successStateButton;
    [SerializeField] private Button failureStateButton;
    [SerializeField] private Button resetStateButton;
    [SerializeField] private TMP_Text previewStatusText;
    [SerializeField] private Behaviour[] isolatedBehaviours;
    [SerializeField] private bool[] isolatedBehaviourEnabledStates;
    [SerializeField] private GameObject[] isolatedGameObjects;
    [SerializeField] private bool[] isolatedGameObjectActiveStates;

    private Texture2D virtualPhoto;

    private void Awake()
    {
        AddListeners();
    }

    private void Start()
    {
        ShowInitialState();
    }

    private void AddListeners()
    {
        Add(photoButton, ShowSelectedState);
        Add(verifyButton, ShowLoadingState);
        Add(cancelButton, ShowInitialState);
        Add(resultConfirmButton, ShowSelectedState);
        Add(selectStateButton, ShowSelectedState);
        Add(loadingStateButton, ShowLoadingState);
        Add(successStateButton, ShowSuccessState);
        Add(failureStateButton, ShowFailureState);
        Add(resetStateButton, ShowInitialState);
    }

    private static void Add(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
        {
            button.onClick.AddListener(action);
        }
    }

    public void ShowInitialState()
    {
        SetActive(popupRoot, true);
        SetActive(photoPopup, true);
        SetActive(noticePopup, false);
        SetActive(resultPopup, false);
        ReleaseVirtualPhoto();

        if (photoView != null)
        {
            photoView.texture = null;
        }

        if (verifyButton != null)
        {
            verifyButton.interactable = false;
        }

        SetStatus("[가상 테스트 데이터] 초기 상태 · 실제 카메라/API 호출 없음");
    }

    public void ShowSelectedState()
    {
        SetActive(popupRoot, true);
        SetActive(photoPopup, true);
        SetActive(noticePopup, false);
        SetActive(resultPopup, false);

        if (photoView != null)
        {
            if (virtualPhoto == null)
            {
                virtualPhoto = CreateVirtualPhoto();
            }

            photoView.texture = virtualPhoto;
        }

        if (verifyButton != null)
        {
            verifyButton.interactable = true;
        }

        SetStatus("[가상 테스트 데이터] 사진 선택 완료 · 인증 버튼 활성화");
    }

    public void ShowLoadingState()
    {
        if (virtualPhoto == null)
        {
            ShowSelectedState();
        }

        SetActive(popupRoot, true);
        SetActive(photoPopup, true);
        SetActive(resultPopup, false);
        SetActive(noticePopup, true);
        SetText(noticeText, "가상 사진을 확인하는 중입니다…\n실제 서버 요청은 전송되지 않습니다.");
        SetStatus("[가상 테스트 데이터] 로딩 상태 · 네트워크 요청 없음");
    }

    public void ShowSuccessState()
    {
        SetActive(popupRoot, true);
        SetActive(photoPopup, true);
        SetActive(noticePopup, false);
        SetActive(resultPopup, true);
        SetText(resultText, "사진 인증에 성공했습니다");
        SetText(reasonText, "가상 테스트 사진이 목표 행동과 일치합니다.");
        SetText(detectedActionText, "인식된 행동\n아침 스트레칭");
        SetText(attrText, "불 속성 경험치 획득");
        SetText(expText, "1,250 (1,150 + 100)");
        SetText(streakText, "연속 달성 일수: 7일");
        SetActive(attrText != null ? attrText.gameObject : null, true);
        SetActive(expText != null ? expText.gameObject : null, true);
        SetActive(streakText != null ? streakText.gameObject : null, true);
        SetStatus("[가상 테스트 데이터] 성공 결과 · 보상 값은 Preview 전용");
    }

    public void ShowFailureState()
    {
        SetActive(popupRoot, true);
        SetActive(photoPopup, true);
        SetActive(noticePopup, false);
        SetActive(resultPopup, true);
        SetText(resultText, "사진 인증에 실패했습니다\n다시 시도해주세요");
        SetText(reasonText, "가상 테스트 사진에서 목표 행동을 확인하지 못했습니다.");
        SetText(detectedActionText, "인식된 행동\n판별 불가");
        SetActive(attrText != null ? attrText.gameObject : null, false);
        SetActive(expText != null ? expText.gameObject : null, false);
        SetActive(streakText != null ? streakText.gameObject : null, false);
        SetStatus("[가상 테스트 데이터] 실패 결과 · Habit 기록은 변경되지 않음");
    }

    private static Texture2D CreateVirtualPhoto()
    {
        const int size = 256;
        Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
        {
            name = "PHOTO Preview Virtual Test Image",
            wrapMode = TextureWrapMode.Clamp,
            filterMode = FilterMode.Bilinear
        };

        Color32[] pixels = new Color32[size * size];
        Color32 sky = new Color32(0xA7, 0xD8, 0xE8, 0xFF);
        Color32 ground = new Color32(0x84, 0xB6, 0x73, 0xFF);
        Color32 path = new Color32(0xE8, 0xD4, 0xA7, 0xFF);
        Color32 subject = new Color32(0xA6, 0x45, 0x35, 0xFF);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Color32 color = y > 106 ? sky : ground;
                if (y < 106 && Mathf.Abs(x - size / 2f) < 22f + (106 - y) * 0.35f)
                {
                    color = path;
                }

                float dx = x - 128f;
                float dy = y - 142f;
                if (dx * dx + dy * dy < 23f * 23f ||
                    (x > 112 && x < 144 && y > 64 && y < 142))
                {
                    color = subject;
                }

                pixels[y * size + x] = color;
            }
        }

        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        return texture;
    }

    private void SetStatus(string value)
    {
        SetText(previewStatusText, value);
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null)
        {
            target.text = value;
        }
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }

    private void ReleaseVirtualPhoto()
    {
        if (virtualPhoto == null)
        {
            return;
        }

        if (Application.isPlaying)
        {
            Destroy(virtualPhoto);
        }
        else
        {
            DestroyImmediate(virtualPhoto);
        }

        virtualPhoto = null;
    }

    private void OnDestroy()
    {
        ReleaseVirtualPhoto();
    }
}
