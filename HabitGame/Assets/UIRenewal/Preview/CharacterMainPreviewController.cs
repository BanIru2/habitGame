using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UITestScene-only local preview for the renewed character main screen.
/// This component owns only virtual display data and never accesses a game manager or API.
/// </summary>
public sealed class CharacterMainPreviewController : MonoBehaviour
{
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private GameObject equippedTitleRoot;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text[] statValueTexts;
    [SerializeField] private TMP_Text[] attributeLevelTexts;
    [SerializeField] private TMP_Text[] attributeExpTexts;
    [SerializeField] private Image[] attributeExpFills;
    [SerializeField] private TMP_Text previewStatusText;
    [SerializeField] private Button defaultStateButton;
    [SerializeField] private Button highStatStateButton;
    [SerializeField] private Button noTitleStateButton;
    [SerializeField] private Button titlePopupButton;
    [SerializeField] private GameObject titlePopup;
    [SerializeField] private Button titlePopupCloseButton;
    [SerializeField] private Button[] titleOptionButtons;
    [SerializeField] private Image[] titleOptionCards;
    [SerializeField] private TMP_Text[] titleOptionStateTexts;
    [SerializeField] private Button achievementPopupButton;
    [SerializeField] private GameObject achievementPopup;
    [SerializeField] private Button achievementPopupCloseButton;
    [SerializeField] private Button[] achievementClaimButtons;
    [SerializeField] private Image[] achievementCards;
    [SerializeField] private TMP_Text[] achievementStateTexts;
    [SerializeField] private TMP_Text[] achievementClaimTexts;
    [SerializeField] private Behaviour[] isolatedBehaviours;
    [SerializeField] private bool[] isolatedBehaviourEnabledStates;
    [SerializeField] private GameObject[] isolatedGameObjects;
    [SerializeField] private bool[] isolatedGameObjectActiveStates;

    private static readonly string[] DefaultStats = { "128", "94", "1,460", "37", "12%" };
    private static readonly string[] HighStats = { "9,999", "8,750", "45,000", "420", "85%" };
    private static readonly int[] DefaultLevels = { 7, 8, 5, 9 };
    private static readonly int[] HighLevels = { 42, 38, 51, 29 };
    private static readonly int[] DefaultExp = { 700, 1200, 225, 780 };
    private static readonly int[] DefaultMaxExp = { 1000, 2000, 500, 3000 };
    private static readonly int[] HighExp = { 9300, 8750, 12400, 6800 };
    private static readonly int[] HighMaxExp = { 10000, 10000, 15000, 8000 };
    private static readonly string[] PreviewTitles =
        { "새벽을 여는 자", "꾸준한 모험가", "습관의 수호자", "불굴의 도전자" };
    private static readonly Color32 SelectedCardColor = new Color32(0xFF, 0xE7, 0xAD, 0xFF);
    private static readonly Color32 NormalCardColor = new Color32(0xFF, 0xF9, 0xEA, 0xFF);
    private static readonly Color32 CompletedCardColor = new Color32(0xFF, 0xEA, 0xB8, 0xFF);
    private static readonly Color32 ClaimedCardColor = new Color32(0xE4, 0xD8, 0xC4, 0xFF);
    private static readonly bool[] PreviewAchievementCompleted = { false, true, true, false, true };
    private readonly bool[] previewAchievementClaimed = new bool[5];
    private int equippedPreviewTitleIndex;

    private void Awake()
    {
        Add(defaultStateButton, ShowDefaultState);
        Add(highStatStateButton, ShowHighStatState);
        Add(noTitleStateButton, ShowNoTitleState);
        Add(titlePopupButton, OpenTitlePopup);
        Add(titlePopupCloseButton, CloseTitlePopup);
        Add(achievementPopupButton, OpenAchievementPopup);
        Add(achievementPopupCloseButton, CloseAchievementPopup);
        for (int index = 0; index < titleOptionButtons.Length; index++)
        {
            int capturedIndex = index;
            Add(titleOptionButtons[index], () => TogglePreviewTitle(capturedIndex));
        }
        for (int index = 0; index < achievementClaimButtons.Length; index++)
        {
            int capturedIndex = index;
            Add(achievementClaimButtons[index], () => ClaimPreviewAchievement(capturedIndex));
        }
    }

    private void Start()
    {
        ShowDefaultState();
        CloseTitlePopup();
        CloseAchievementPopup();
    }

    public void ShowDefaultState()
    {
        equippedPreviewTitleIndex = 0;
        ApplyState("용감한 모험가", "새벽을 여는 자", DefaultStats, DefaultLevels,
            DefaultExp, DefaultMaxExp, true,
            "[가상 테스트 데이터] 기본 상태 · 실제 Manager/API 호출 없음");
        RefreshTitleOptions();
    }

    public void ShowHighStatState()
    {
        equippedPreviewTitleIndex = 2;
        ApplyState("만렙을 향해서", "전설의 습관 수호자", HighStats, HighLevels,
            HighExp, HighMaxExp, true,
            "[가상 테스트 데이터] 높은 능력치/긴 숫자 레이아웃 확인");
        RefreshTitleOptions();
    }

    public void ShowNoTitleState()
    {
        equippedPreviewTitleIndex = -1;
        ApplyState("칭호없는모험가", string.Empty, DefaultStats, DefaultLevels,
            DefaultExp, DefaultMaxExp, false,
            "[가상 테스트 데이터] 칭호 미장착 상태 · 실제 저장 데이터 변경 없음");
        RefreshTitleOptions();
    }

    public void OpenTitlePopup()
    {
        CloseAchievementPopup();
        SetActive(titlePopup, true);
        RefreshTitleOptions();
        SetText(previewStatusText, "[가상 테스트 데이터] 칭호 팝업 · 실제 TitleManager/API 호출 없음");
    }

    public void CloseTitlePopup()
    {
        SetActive(titlePopup, false);
    }

    public void OpenAchievementPopup()
    {
        CloseTitlePopup();
        SetActive(achievementPopup, true);
        RefreshAchievementOptions();
        SetText(previewStatusText, "[가상 테스트 데이터] 업적 팝업 · 실제 AchievementManager/API 호출 없음");
    }

    public void CloseAchievementPopup()
    {
        SetActive(achievementPopup, false);
    }

    private void ClaimPreviewAchievement(int index)
    {
        if (index < 0 || index >= PreviewAchievementCompleted.Length ||
            !PreviewAchievementCompleted[index] || previewAchievementClaimed[index])
        {
            return;
        }

        previewAchievementClaimed[index] = true;
        RefreshAchievementOptions();
        SetText(previewStatusText, "[가상 테스트 데이터] 업적 보상 수령 상태만 변경 · API 호출 없음");
    }

    private void RefreshAchievementOptions()
    {
        int count = Mathf.Min(PreviewAchievementCompleted.Length, achievementCards.Length);
        for (int index = 0; index < count; index++)
        {
            bool completed = PreviewAchievementCompleted[index];
            bool claimed = previewAchievementClaimed[index];
            if (achievementCards[index] != null)
            {
                achievementCards[index].color = claimed
                    ? ClaimedCardColor
                    : completed ? CompletedCardColor : NormalCardColor;
            }

            if (index < achievementStateTexts.Length)
            {
                SetText(achievementStateTexts[index], claimed ? "수령 완료" : completed ? "달성 완료" : "진행 중");
            }

            if (index < achievementClaimButtons.Length && achievementClaimButtons[index] != null)
            {
                achievementClaimButtons[index].interactable = completed && !claimed;
            }

            if (index < achievementClaimTexts.Length)
            {
                SetText(achievementClaimTexts[index], claimed ? "수령 완료" : "보상 수령");
            }
        }
    }

    private void TogglePreviewTitle(int index)
    {
        if (index < 0 || index >= PreviewTitles.Length)
        {
            return;
        }

        equippedPreviewTitleIndex = equippedPreviewTitleIndex == index ? -1 : index;
        bool hasTitle = equippedPreviewTitleIndex >= 0;
        SetActive(equippedTitleRoot, hasTitle);
        SetText(titleText, hasTitle ? PreviewTitles[equippedPreviewTitleIndex] : string.Empty);
        SetText(previewStatusText, hasTitle
            ? $"[가상 테스트 데이터] '{PreviewTitles[equippedPreviewTitleIndex]}' 장착"
            : "[가상 테스트 데이터] 칭호 장착 해제");
        RefreshTitleOptions();
    }

    private void RefreshTitleOptions()
    {
        for (int index = 0; index < titleOptionCards.Length; index++)
        {
            bool isEquipped = index == equippedPreviewTitleIndex;
            if (titleOptionCards[index] != null)
            {
                titleOptionCards[index].color = isEquipped ? SelectedCardColor : NormalCardColor;
            }

            if (index < titleOptionStateTexts.Length)
            {
                SetText(titleOptionStateTexts[index], isEquipped ? "장착 중 · 다시 누르면 해제" : "선택하여 장착");
            }
        }
    }

    private void ApplyState(string nickname, string title, string[] stats, int[] levels,
        int[] exp, int[] maxExp, bool hasTitle, string status)
    {
        SetText(nameText, nickname);
        SetActive(equippedTitleRoot, hasTitle);
        SetText(titleText, title);

        for (int index = 0; index < statValueTexts.Length && index < stats.Length; index++)
        {
            SetText(statValueTexts[index], stats[index]);
        }

        int attributeCount = Mathf.Min(levels.Length,
            Mathf.Min(attributeLevelTexts.Length, attributeExpTexts.Length));
        for (int index = 0; index < attributeCount; index++)
        {
            SetText(attributeLevelTexts[index], $"Lv. {levels[index]}");
            SetText(attributeExpTexts[index], $"{exp[index]:N0} / {maxExp[index]:N0}");
            if (index < attributeExpFills.Length && attributeExpFills[index] != null)
            {
                attributeExpFills[index].fillAmount = maxExp[index] > 0
                    ? Mathf.Clamp01((float)exp[index] / maxExp[index])
                    : 0f;
            }
        }

        SetText(previewStatusText, status);
    }

    private static void Add(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null)
        {
            button.onClick.AddListener(action);
        }
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
}
