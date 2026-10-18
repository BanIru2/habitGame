using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UITestScene-only local preview for the renewed PvP lobby, ranking board, and battle HUD.
/// This component never accesses Photon, PvP managers, or an API.
/// </summary>
public sealed class PvpRankingPreviewController : MonoBehaviour
{
    [SerializeField] private Button matchButton;
    [SerializeField] private Button zeroRemainButton;
    [SerializeField] private Button popupCloseButton;
    [SerializeField] private GameObject limitPopup;
    [SerializeField] private TMP_Text remainText;
    [SerializeField] private TMP_Text localStatusText;
    [SerializeField] private Image remainCardImage;
    [SerializeField] private Image matchButtonImage;
    [SerializeField] private Sprite normalMatchSprite;
    [SerializeField] private Sprite disabledMatchSprite;
    [SerializeField] private ScrollRect rankingScroll;
    [SerializeField] private GameObject[] lobbyObjects;
    [SerializeField] private GameObject loadingPanel;
    [SerializeField] private Button participantChangeButton;
    [SerializeField] private Button cancelMatchButton;
    [SerializeField] private Button matchCompleteButton;
    [SerializeField] private TMP_Text participantCountText;
    [SerializeField] private GameObject readyPanel;
    [SerializeField] private Button[] attributeButtons;
    [SerializeField] private Button[] consumableButtons;
    [SerializeField] private Button[] equipmentButtons;
    [SerializeField] private GameObject equipmentSelectPanel;
    [SerializeField] private TMP_Text equipmentTypeText;
    [SerializeField] private Button[] equipmentItemButtons;
    [SerializeField] private TMP_Text[] equipmentItemNameTexts;
    [SerializeField] private TMP_Text[] equipmentItemStatTexts;
    [SerializeField] private TMP_Text[] equipmentItemStateTexts;
    [SerializeField] private Button equipmentSelectCloseButton;
    [SerializeField] private GameObject equipmentDetailPopup;
    [SerializeField] private TMP_Text equipmentDetailNameText;
    [SerializeField] private TMP_Text equipmentDetailStatText;
    [SerializeField] private TMP_Text equipmentDetailDescriptionText;
    [SerializeField] private Button equipmentActionButton;
    [SerializeField] private TMP_Text equipmentActionButtonText;
    [SerializeField] private Button equipmentDetailCloseButton;
    [SerializeField] private Button readyButton;
    [SerializeField] private Button battleTestButton;
    [SerializeField] private Button returnLobbyButton;
    [SerializeField] private TMP_Text myReadyText;
    [SerializeField] private TMP_Text opponentReadyText;
    [SerializeField] private TMP_Text readyGuideText;
    [SerializeField] private GameObject battlePanel;
    [SerializeField] private Button battleReturnLobbyButton;
    [SerializeField] private Button hpTestButton;
    [SerializeField] private Button turnTestButton;
    [SerializeField] private Button timerTestButton;
    [SerializeField] private Image myBattleHpBar;
    [SerializeField] private Image opponentBattleHpBar;
    [SerializeField] private TMP_Text myBattleHpText;
    [SerializeField] private TMP_Text opponentBattleHpText;
    [SerializeField] private TMP_Text battleTurnText;
    [SerializeField] private TMP_Text battleTimerText;
    [SerializeField] private Behaviour[] isolatedBehaviours;
    [SerializeField] private bool[] isolatedBehaviourEnabledStates;
    [SerializeField] private GameObject[] isolatedGameObjects;
    [SerializeField] private bool[] isolatedGameObjectActiveStates;

    private static readonly Color NormalRemainColor = new Color32(0x5B, 0x36, 0x39, 0xFF);
    private static readonly Color EmptyRemainColor = new Color32(0xA8, 0x3F, 0x35, 0xFF);
    private static readonly Color NormalCardColor = new Color32(0xFF, 0xF9, 0xEA, 0xFF);
    private static readonly Color EmptyCardColor = new Color32(0xFF, 0xE3, 0xD8, 0xFF);
    private static readonly Color SelectedColor = new Color32(0xFF, 0xD8, 0x79, 0xFF);
    private static readonly Color NormalSlotColor = new Color32(0xFF, 0xF9, 0xEA, 0xFF);
    private static readonly Color DisabledColor = new Color32(0xC9, 0xBE, 0xAA, 0xFF);

    private int remainingCount = 3;
    private int participantCount = 1;
    private int selectedAttribute = -1;
    private readonly bool[] selectedConsumables = new bool[3];
    private readonly bool[] selectedEquipment = new bool[4];
    private readonly int[] selectedEquipmentChoices = { -1, -1, -1, -1 };
    private int selectedEquipmentType = -1;
    private int selectedEquipmentItem = -1;
    private int battleHpStep;
    private int previewTurn = 1;
    private int previewSeconds = 60;

    private static readonly string[] EquipmentTypeNames = { "무기", "의류", "모자", "신발" };
    private static readonly string[,] EquipmentNames =
    {
        { "훈련용 장검", "황금 단검", "별빛 지팡이", "참나무 활", "용사의 대검" },
        { "견습 모험복", "수호 기사복", "달빛 로브", "숲지기 망토", "용린 갑옷" },
        { "가죽 모자", "용사 머리띠", "별무늬 후드", "현자의 모자", "왕실 투구" },
        { "여행자 장화", "질풍 부츠", "구름 슬리퍼", "숲길 신발", "용기사 장화" }
    };
    private static readonly string[,] EquipmentStats =
    {
        { "공격력 +12", "공격력 +18 · 속도 +3", "공격력 +15 · 집중 +5", "공격력 +14 · 속도 +4", "공격력 +24" },
        { "방어력 +10", "방어력 +17 · 체력 +20", "방어력 +12 · 집중 +6", "방어력 +14 · 속도 +2", "방어력 +22" },
        { "체력 +15", "공격력 +6 · 체력 +12", "집중 +9", "집중 +7 · 방어력 +5", "체력 +25" },
        { "속도 +5", "속도 +9 · 회피 +3", "체력 +10 · 속도 +4", "속도 +7 · 회피 +2", "방어력 +8 · 속도 +6" }
    };

    private void Awake()
    {
        Add(matchButton, OnMatchClicked);
        Add(zeroRemainButton, ToggleRemainingCount);
        Add(popupCloseButton, CloseLimitPopup);
        Add(participantChangeButton, ChangeParticipantCount);
        Add(cancelMatchButton, ShowLobby);
        Add(matchCompleteButton, ShowReady);
        Add(returnLobbyButton, ShowLobby);
        Add(readyButton, CompleteReady);
        Add(battleTestButton, ShowBattle);
        AddIndexed(attributeButtons, SelectAttribute);
        AddIndexed(consumableButtons, ToggleConsumable);
        AddIndexed(equipmentButtons, OpenEquipmentSelect);
        AddIndexed(equipmentItemButtons, OpenEquipmentDetail);
        Add(equipmentSelectCloseButton, CloseEquipmentSelect);
        Add(equipmentActionButton, ToggleSelectedEquipment);
        Add(equipmentDetailCloseButton, CloseEquipmentDetail);
        Add(battleReturnLobbyButton, ShowLobby);
        Add(hpTestButton, ChangeBattleHp);
        Add(turnTestButton, ChangeBattleTurn);
        Add(timerTestButton, ChangeBattleTimer);
    }

    private void Start()
    {
        SetRemainingCount(3);
        CloseLimitPopup();
        ShowLobby();
        if (rankingScroll != null)
        {
            Canvas.ForceUpdateCanvases();
            if (rankingScroll.content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(rankingScroll.content);
            }
            rankingScroll.StopMovement();
            rankingScroll.verticalNormalizedPosition = 1f;
        }
    }

    private void OnMatchClicked()
    {
        if (remainingCount <= 0)
        {
            SetActive(limitPopup, true);
            SetText(localStatusText, "오늘의 PvP 도전 횟수를 모두 사용했습니다.");
            return;
        }

        CloseLimitPopup();
        participantCount = 1;
        RefreshParticipantCount();
        SetObjectsActive(lobbyObjects, false);
        SetActive(loadingPanel, true);
        SetText(localStatusText, "Preview 안내 · 실제 매칭은 시작하지 않습니다.");
    }

    private void ToggleRemainingCount()
    {
        SetRemainingCount(remainingCount == 0 ? 3 : 0);
        CloseLimitPopup();
        SetText(localStatusText, remainingCount == 0
            ? "남은 횟수 0 상태 · 매칭 버튼으로 제한 팝업을 확인하세요."
            : "남은 횟수 복원 · 로컬 Preview 상태입니다.");
    }

    private void SetRemainingCount(int value)
    {
        remainingCount = Mathf.Max(0, value);
        bool empty = remainingCount == 0;
        SetText(remainText, remainingCount + " / 5");
        if (remainText != null) remainText.color = empty ? EmptyRemainColor : NormalRemainColor;
        if (remainCardImage != null) remainCardImage.color = empty ? EmptyCardColor : NormalCardColor;
        if (matchButtonImage != null)
        {
            matchButtonImage.sprite = empty ? disabledMatchSprite : normalMatchSprite;
            matchButtonImage.color = Color.white;
            matchButtonImage.type = Image.Type.Sliced;
        }
    }

    private void CloseLimitPopup()
    {
        SetActive(limitPopup, false);
    }

    private void ChangeParticipantCount()
    {
        participantCount = participantCount == 1 ? 2 : 1;
        RefreshParticipantCount();
    }

    private void RefreshParticipantCount()
    {
        SetText(participantCountText, participantCount + " / 2");
    }

    private void ShowLobby()
    {
        SetActive(loadingPanel, false);
        SetActive(readyPanel, false);
        SetActive(battlePanel, false);
        CloseEquipmentSelect();
        SetObjectsActive(lobbyObjects, true);
        CloseLimitPopup();
        SetText(localStatusText, "로컬 Preview · 실제 매칭과 서버 요청은 실행되지 않습니다.");
    }

    private void ShowReady()
    {
        SetActive(loadingPanel, false);
        SetObjectsActive(lobbyObjects, false);
        SetActive(readyPanel, true);
        SetActive(battlePanel, false);
        CloseEquipmentSelect();
        selectedAttribute = -1;
        System.Array.Clear(selectedConsumables, 0, selectedConsumables.Length);
        System.Array.Clear(selectedEquipment, 0, selectedEquipment.Length);
        for (int index = 0; index < selectedEquipmentChoices.Length; index++)
            selectedEquipmentChoices[index] = -1;
        SetText(myReadyText, "준비 중");
        SetText(opponentReadyText, "준비 완료");
        SetText(readyGuideText, "속성을 선택하면 준비 완료 버튼이 활성화됩니다.");
        RefreshReadySelections();
    }

    private void SelectAttribute(int index)
    {
        selectedAttribute = index;
        SetText(readyGuideText, "선택 완료 · 소비품과 장비는 로컬 Preview 상태입니다.");
        RefreshReadySelections();
    }

    private void ToggleConsumable(int index)
    {
        if (index >= 0 && index < selectedConsumables.Length)
            selectedConsumables[index] = !selectedConsumables[index];
        RefreshReadySelections();
    }

    private void OpenEquipmentSelect(int index)
    {
        if (index < 0 || index >= selectedEquipment.Length) return;
        selectedEquipmentType = index;
        selectedEquipmentItem = -1;
        SetText(equipmentTypeText, EquipmentTypeNames[index] + " 선택");
        SetActive(equipmentSelectPanel, true);
        SetActive(equipmentDetailPopup, false);
        RefreshEquipmentList();
    }

    private void OpenEquipmentDetail(int index)
    {
        if (selectedEquipmentType < 0 || index < 0 ||
            equipmentItemButtons == null || index >= equipmentItemButtons.Length) return;
        selectedEquipmentItem = index;
        SetText(equipmentDetailNameText, EquipmentNames[selectedEquipmentType, index]);
        SetText(equipmentDetailStatText, EquipmentStats[selectedEquipmentType, index]);
        SetText(equipmentDetailDescriptionText,
            "PvP 준비 Preview용 가상 장비입니다. 실제 인벤토리나 서버 데이터에는 반영되지 않습니다.");
        SetActive(equipmentDetailPopup, true);
        RefreshEquipmentDetail();
    }

    private void ToggleSelectedEquipment()
    {
        if (selectedEquipmentType < 0 || selectedEquipmentItem < 0) return;
        bool sameSelection = selectedEquipmentChoices[selectedEquipmentType] == selectedEquipmentItem;
        selectedEquipmentChoices[selectedEquipmentType] = sameSelection ? -1 : selectedEquipmentItem;
        selectedEquipment[selectedEquipmentType] = !sameSelection;
        RefreshEquipmentList();
        RefreshEquipmentDetail();
        RefreshReadySelections();
    }

    private void CloseEquipmentDetail()
    {
        selectedEquipmentItem = -1;
        SetActive(equipmentDetailPopup, false);
    }

    private void CloseEquipmentSelect()
    {
        CloseEquipmentDetail();
        selectedEquipmentType = -1;
        SetActive(equipmentSelectPanel, false);
    }

    private void RefreshEquipmentList()
    {
        if (selectedEquipmentType < 0) return;
        for (int index = 0; equipmentItemButtons != null && index < equipmentItemButtons.Length; index++)
        {
            bool equipped = selectedEquipmentChoices[selectedEquipmentType] == index;
            SetButtonColor(equipmentItemButtons[index], equipped ? SelectedColor : NormalSlotColor);
            if (equipmentItemNameTexts != null && index < equipmentItemNameTexts.Length)
                SetText(equipmentItemNameTexts[index], EquipmentNames[selectedEquipmentType, index]);
            if (equipmentItemStatTexts != null && index < equipmentItemStatTexts.Length)
                SetText(equipmentItemStatTexts[index], EquipmentStats[selectedEquipmentType, index]);
            if (equipmentItemStateTexts != null && index < equipmentItemStateTexts.Length)
                SetText(equipmentItemStateTexts[index], equipped ? "장착 중" : "상세 보기");
        }
    }

    private void RefreshEquipmentDetail()
    {
        bool equipped = selectedEquipmentType >= 0 &&
                        selectedEquipmentChoices[selectedEquipmentType] == selectedEquipmentItem;
        SetText(equipmentActionButtonText, equipped ? "해제하기" : "장착하기");
        SetButtonColor(equipmentActionButton, equipped ? EmptyCardColor : SelectedColor);
    }

    private void CompleteReady()
    {
        if (selectedAttribute < 0) return;
        SetText(myReadyText, "준비 완료");
        SetText(readyGuideText, "양측 준비 완료 · 전투 화면 테스트 버튼으로 로컬 HUD를 확인하세요.");
        if (readyButton != null) readyButton.interactable = false;
        SetButtonColor(readyButton, DisabledColor);
        if (battleTestButton != null) battleTestButton.interactable = true;
        SetButtonColor(battleTestButton, SelectedColor);
    }

    private void ShowBattle()
    {
        if (selectedAttribute < 0) return;
        CloseEquipmentSelect();
        SetObjectsActive(lobbyObjects, false);
        SetActive(loadingPanel, false);
        SetActive(readyPanel, false);
        SetActive(battlePanel, true);
        battleHpStep = 0;
        previewTurn = 1;
        previewSeconds = 60;
        RefreshBattlePreview();
    }

    private void ChangeBattleHp()
    {
        battleHpStep = (battleHpStep + 1) % 3;
        RefreshBattlePreview();
    }

    private void ChangeBattleTurn()
    {
        previewTurn = previewTurn % 9 + 1;
        SetText(battleTurnText, "TURN " + previewTurn);
    }

    private void ChangeBattleTimer()
    {
        previewSeconds = previewSeconds == 60 ? 30 : previewSeconds == 30 ? 10 : 60;
        SetText(battleTimerText, previewSeconds.ToString("00"));
        if (battleTimerText != null)
            battleTimerText.color = previewSeconds <= 10 ? EmptyRemainColor : NormalRemainColor;
    }

    private void RefreshBattlePreview()
    {
        int[] myHp = { 1200, 840, 360 };
        int[] opponentHp = { 1080, 756, 324 };
        SetHpFill(myBattleHpBar, myHp[battleHpStep], 1200);
        SetHpFill(opponentBattleHpBar, opponentHp[battleHpStep], 1080);
        SetText(myBattleHpText, myHp[battleHpStep] + " / 1200");
        SetText(opponentBattleHpText, opponentHp[battleHpStep] + " / 1080");
        SetText(battleTurnText, "TURN " + previewTurn);
        SetText(battleTimerText, previewSeconds.ToString("00"));
    }

    private static void SetHpFill(Image hpBar, int currentHp, int maxHp)
    {
        if (hpBar == null) return;
        hpBar.type = Image.Type.Filled;
        hpBar.fillMethod = Image.FillMethod.Horizontal;
        hpBar.fillOrigin = 0;
        hpBar.fillAmount = maxHp > 0
            ? Mathf.Clamp01((float)currentHp / maxHp)
            : 0f;
    }

    private void RefreshReadySelections()
    {
        for (int index = 0; attributeButtons != null && index < attributeButtons.Length; index++)
            SetButtonColor(attributeButtons[index], index == selectedAttribute ? SelectedColor : NormalSlotColor);
        for (int index = 0; consumableButtons != null && index < consumableButtons.Length; index++)
            SetButtonColor(consumableButtons[index], index < selectedConsumables.Length && selectedConsumables[index]
                ? SelectedColor : NormalSlotColor);
        for (int index = 0; equipmentButtons != null && index < equipmentButtons.Length; index++)
            SetButtonColor(equipmentButtons[index], index < selectedEquipment.Length && selectedEquipment[index]
                ? SelectedColor : NormalSlotColor);
        if (readyButton != null) readyButton.interactable = selectedAttribute >= 0;
        SetButtonColor(readyButton, selectedAttribute >= 0 ? SelectedColor : DisabledColor);
        if (battleTestButton != null) battleTestButton.interactable = false;
        SetButtonColor(battleTestButton, DisabledColor);
    }

    private static void SetObjectsActive(GameObject[] targets, bool active)
    {
        if (targets == null) return;
        foreach (GameObject target in targets) SetActive(target, active);
    }

    private static void Add(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button != null) button.onClick.AddListener(action);
    }

    private static void AddIndexed(Button[] buttons, System.Action<int> action)
    {
        if (buttons == null) return;
        for (int index = 0; index < buttons.Length; index++)
        {
            int capturedIndex = index;
            if (buttons[index] != null) buttons[index].onClick.AddListener(() => action(capturedIndex));
        }
    }

    private static void SetButtonColor(Button button, Color color)
    {
        if (button != null && button.image != null) button.image.color = color;
    }

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null) target.SetActive(active);
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null) target.text = value;
    }
}
