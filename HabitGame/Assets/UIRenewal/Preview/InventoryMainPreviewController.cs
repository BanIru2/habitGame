using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UITestScene-only local preview for the renewed inventory, item detail, and enhance screens.
/// It switches prebuilt virtual item cards and never accesses InventoryManager or an API.
/// </summary>
public sealed class InventoryMainPreviewController : MonoBehaviour
{
    [SerializeField] private Button equipmentButton;
    [SerializeField] private Button consumableButton;
    [SerializeField] private Image equipmentButtonImage;
    [SerializeField] private Image consumableButtonImage;
    [SerializeField] private TMP_Text equipmentButtonText;
    [SerializeField] private TMP_Text consumableButtonText;
    [SerializeField] private GameObject equipmentItemsRoot;
    [SerializeField] private GameObject consumableItemsRoot;
    [SerializeField] private Button[] equipmentItemButtons;
    [SerializeField] private Button[] consumableItemButtons;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private TMP_Text categoryStatusText;
    [SerializeField] private GameObject detailPopupRoot;
    [SerializeField] private TMP_Text detailIconText;
    [SerializeField] private TMP_Text detailNameText;
    [SerializeField] private TMP_Text detailDescriptionText;
    [SerializeField] private TMP_Text detailTypeText;
    [SerializeField] private Button detailEquipButton;
    [SerializeField] private Image detailEquipButtonImage;
    [SerializeField] private TMP_Text detailEquipButtonText;
    [SerializeField] private Button detailEnhanceButton;
    [SerializeField] private Button detailCloseButton;
    [SerializeField] private GameObject enhancePopupRoot;
    [SerializeField] private TMP_Text enhanceIconText;
    [SerializeField] private TMP_Text enhanceNameText;
    [SerializeField] private TMP_Text enhanceLevelText;
    [SerializeField] private TMP_Text enhanceExpText;
    [SerializeField] private Image enhanceExpFill;
    [SerializeField] private Button enhanceTestButton;
    [SerializeField] private Button enhanceCancelButton;
    [SerializeField] private GameObject consumableDetailDim;
    [SerializeField] private GameObject consumableDetailPopup;
    [SerializeField] private TMP_Text consumableDetailIconText;
    [SerializeField] private TMP_Text consumableDetailNameText;
    [SerializeField] private TMP_Text consumableDetailDescriptionText;
    [SerializeField] private Button consumableDetailCloseButton;
    [SerializeField] private GameObject functionalDetailPopup;
    [SerializeField] private TMP_Text functionalDetailIconText;
    [SerializeField] private TMP_Text functionalDetailNameText;
    [SerializeField] private TMP_Text functionalDetailDescriptionText;
    [SerializeField] private Button functionalDetailUseButton;
    [SerializeField] private Button functionalDetailCloseButton;
    [SerializeField] private Sprite selectedTabSprite;
    [SerializeField] private Sprite normalTabSprite;
    [SerializeField] private Color selectedTextColor = new Color32(0x5B, 0x36, 0x39, 0xFF);
    [SerializeField] private Color normalTextColor = new Color32(0x86, 0x68, 0x5D, 0xFF);
    [SerializeField] private Color equippedButtonColor = new Color32(0xD8, 0x78, 0x68, 0xFF);
    [SerializeField] private Color unequippedButtonColor = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
    [SerializeField] private Behaviour[] isolatedBehaviours;
    [SerializeField] private bool[] isolatedBehaviourEnabledStates;
    [SerializeField] private GameObject[] isolatedGameObjects;
    [SerializeField] private bool[] isolatedGameObjectActiveStates;

    private static readonly string[] EquipmentNames =
    {
        "새벽의 검", "숲지기 모자", "용기의 갑옷", "바람걸음 신발",
        "불꽃 장검", "별빛 후드", "수호자의 제복", "구름 발자국"
    };

    private static readonly string[] EquipmentDescriptions =
    {
        "ATK +18 · 모험의 시작을 함께한 검", "HP +120 · 잎사귀의 기운이 깃든 모자",
        "DEF +24 · 단단한 골드 테두리 갑옷", "SPD +9 · 가벼운 발걸음을 돕는 신발",
        "ATK +31 · 불 속성 숙련자를 위한 무기", "CRIT +6% · 밤하늘 빛을 머금은 모자",
        "DEF +42 · 오랜 습관을 증명하는 옷", "SPD +14 · 구름처럼 가벼운 신발"
    };

    private static readonly string[] EquipmentTypes =
    {
        "무기", "모자", "옷", "신발", "무기", "모자", "옷", "신발"
    };

    private static readonly string[] EquipmentIconLetters =
    {
        "검", "모", "갑", "신", "불", "별", "수", "구"
    };

    private readonly bool[] equippedStates = { true, false, true, false, false, false, false, false };
    private readonly int[] previewLevels = { 3, 2, 5, 1, 6, 4, 7, 2 };
    private readonly int[] previewExp = { 35, 72, 48, 15, 90, 26, 64, 8 };
    private readonly int[] previewRequiredExp = { 100, 90, 120, 80, 140, 110, 150, 95 };
    private static readonly string[] ConsumableNames =
    {
        "작은 회복 물약", "집중의 차", "불꽃 영약", "맑은 샘물", "행운 쿠키",
        "방어 비약", "신속의 깃털", "오로라 사탕", "튼튼한 도시락", "별가루 병"
    };
    private static readonly string[] ConsumableDescriptions =
    {
        "HP를 조금 회복하는 기본 물약", "전투 준비 중 집중력을 높여주는 차",
        "불 속성의 힘을 일시적으로 높인다", "지친 모험가의 기운을 맑게 한다",
        "작은 행운이 담긴 바삭한 간식", "전투 준비 중 방어 능력을 보조한다",
        "가벼운 움직임을 돕는 마법 깃털", "오로라 속성의 달콤한 사탕",
        "모험 전에 든든하게 먹는 도시락", "반짝이는 별가루를 담은 작은 병"
    };
    private static readonly string[] ConsumableIconLetters =
    {
        "회", "집", "불", "샘", "행", "방", "신", "오", "도", "별"
    };
    private static readonly bool[] IsFunctionalConsumable =
    {
        false, false, true, true, true, false, true, true, false, true
    };
    private readonly int[] consumableQuantities = { 12, 5, 3, 18, 7, 4, 9, 21, 2, 15 };
    private int selectedEquipmentIndex = -1;
    private int selectedConsumableIndex = -1;

    private void Awake()
    {
        Add(equipmentButton, ShowEquipmentItems);
        Add(consumableButton, ShowConsumableItems);
        Add(detailEquipButton, ToggleSelectedEquipmentState);
        Add(detailEnhanceButton, OpenEnhancePreview);
        Add(detailCloseButton, CloseDetailPopup);
        Add(enhanceTestButton, SimulateLocalEnhance);
        Add(enhanceCancelButton, ReturnToDetailPopup);
        Add(consumableDetailCloseButton, CloseConsumableDetailPopup);
        Add(functionalDetailUseButton, SimulateLocalConsumableUse);
        Add(functionalDetailCloseButton, CloseConsumableDetailPopup);

        if (equipmentItemButtons != null)
        {
            for (int index = 0; index < equipmentItemButtons.Length; index++)
            {
                int capturedIndex = index;
                Add(equipmentItemButtons[index], () => OpenEquipmentDetail(capturedIndex));
            }
        }

        if (consumableItemButtons != null)
        {
            for (int index = 0; index < consumableItemButtons.Length; index++)
            {
                int capturedIndex = index;
                Add(consumableItemButtons[index], () => OpenConsumableDetail(capturedIndex));
            }
        }
    }

    private void Start()
    {
        CloseDetailPopup();
        ShowEquipmentItems();
    }

    public void ShowEquipmentItems()
    {
        CloseDetailPopup();
        ApplyCategory(true);
    }

    public void ShowConsumableItems()
    {
        CloseDetailPopup();
        ApplyCategory(false);
    }

    private void OpenEquipmentDetail(int index)
    {
        if (index < 0 || index >= EquipmentNames.Length)
        {
            return;
        }

        selectedEquipmentIndex = index;
        SetText(detailIconText, EquipmentIconLetters[index]);
        SetText(detailNameText, EquipmentNames[index]);
        SetText(detailDescriptionText, EquipmentDescriptions[index]);
        SetText(detailTypeText, "장비 유형 · " + EquipmentTypes[index]);
        RefreshDetailEquipState();
        SetActive(enhancePopupRoot, false);
        SetActive(detailPopupRoot, true);
    }

    private void OpenConsumableDetail(int index)
    {
        if (index < 0 || index >= ConsumableNames.Length)
        {
            return;
        }

        selectedConsumableIndex = index;
        bool functional = IsFunctionalConsumable[index];
        TMP_Text icon = functional ? functionalDetailIconText : consumableDetailIconText;
        TMP_Text name = functional ? functionalDetailNameText : consumableDetailNameText;
        TMP_Text description = functional
            ? functionalDetailDescriptionText
            : consumableDetailDescriptionText;
        SetText(icon, ConsumableIconLetters[index]);
        SetText(name, ConsumableNames[index]);
        SetText(description, ConsumableDescriptions[index]);
        SetActive(consumableDetailPopup, !functional);
        SetActive(functionalDetailPopup, functional);
        if (functionalDetailUseButton != null)
        {
            functionalDetailUseButton.interactable = consumableQuantities[index] > 0;
        }
        SetActive(consumableDetailDim, true);
    }

    private void SimulateLocalConsumableUse()
    {
        if (selectedConsumableIndex < 0 || selectedConsumableIndex >= consumableQuantities.Length ||
            !IsFunctionalConsumable[selectedConsumableIndex] ||
            consumableQuantities[selectedConsumableIndex] <= 0)
        {
            return;
        }

        consumableQuantities[selectedConsumableIndex]--;
        RefreshConsumableCardQuantity(selectedConsumableIndex);
        if (functionalDetailUseButton != null)
        {
            functionalDetailUseButton.interactable = consumableQuantities[selectedConsumableIndex] > 0;
        }
    }

    private void RefreshConsumableCardQuantity(int index)
    {
        if (consumableItemButtons == null || index < 0 || index >= consumableItemButtons.Length ||
            consumableItemButtons[index] == null)
        {
            return;
        }

        Transform badge = consumableItemButtons[index].transform.Find(
            "QuantityOrTypeBackground/QuantityOrTypeText");
        SetText(badge != null ? badge.GetComponent<TMP_Text>() : null,
            "x" + consumableQuantities[index]);
    }

    private void OpenEnhancePreview()
    {
        if (!HasSelectedEquipment())
        {
            return;
        }

        RefreshEnhancePreview();
        SetActive(detailPopupRoot, false);
        SetActive(enhancePopupRoot, true);
    }

    private void SimulateLocalEnhance()
    {
        if (!HasSelectedEquipment())
        {
            return;
        }

        int index = selectedEquipmentIndex;
        previewExp[index] += 35;
        while (previewExp[index] >= previewRequiredExp[index])
        {
            previewExp[index] -= previewRequiredExp[index];
            previewLevels[index]++;
            previewRequiredExp[index] += 25;
        }
        RefreshEnhancePreview();
    }

    private void RefreshEnhancePreview()
    {
        if (!HasSelectedEquipment())
        {
            return;
        }

        int index = selectedEquipmentIndex;
        int requiredExp = Mathf.Max(1, previewRequiredExp[index]);
        float ratio = Mathf.Clamp01((float)previewExp[index] / requiredExp);
        SetText(enhanceIconText, EquipmentIconLetters[index]);
        SetText(enhanceNameText, EquipmentNames[index]);
        SetText(enhanceLevelText, "Lv. " + previewLevels[index]);
        SetText(enhanceExpText, previewExp[index] + " / " + requiredExp);
        if (enhanceExpFill != null)
        {
            Vector3 scale = enhanceExpFill.rectTransform.localScale;
            scale.x = ratio;
            enhanceExpFill.rectTransform.localScale = scale;
        }
    }

    private void ReturnToDetailPopup()
    {
        if (!HasSelectedEquipment())
        {
            CloseDetailPopup();
            return;
        }

        SetActive(enhancePopupRoot, false);
        SetActive(detailPopupRoot, true);
    }

    private void ToggleSelectedEquipmentState()
    {
        if (selectedEquipmentIndex < 0 || selectedEquipmentIndex >= equippedStates.Length)
        {
            return;
        }

        equippedStates[selectedEquipmentIndex] = !equippedStates[selectedEquipmentIndex];
        RefreshDetailEquipState();
        RefreshCardEquipState(selectedEquipmentIndex);
    }

    private void RefreshDetailEquipState()
    {
        if (selectedEquipmentIndex < 0 || selectedEquipmentIndex >= equippedStates.Length)
        {
            return;
        }

        bool equipped = equippedStates[selectedEquipmentIndex];
        SetText(detailEquipButtonText, equipped ? "해제하기" : "장착하기");
        if (detailEquipButtonImage != null)
        {
            detailEquipButtonImage.color = equipped ? equippedButtonColor : unequippedButtonColor;
        }
    }

    private void RefreshCardEquipState(int index)
    {
        if (equipmentItemButtons == null || index < 0 || index >= equipmentItemButtons.Length ||
            equipmentItemButtons[index] == null)
        {
            return;
        }

        Transform label = equipmentItemButtons[index].transform.Find("EquippedText");
        TMP_Text text = label != null ? label.GetComponent<TMP_Text>() : null;
        SetText(text, equippedStates[index] ? "장착 중" : string.Empty);
    }

    private void CloseDetailPopup()
    {
        selectedEquipmentIndex = -1;
        SetActive(detailPopupRoot, false);
        SetActive(enhancePopupRoot, false);
        CloseConsumableDetailPopup();
    }

    private void CloseConsumableDetailPopup()
    {
        selectedConsumableIndex = -1;
        SetActive(consumableDetailPopup, false);
        SetActive(functionalDetailPopup, false);
        SetActive(consumableDetailDim, false);
    }

    private bool HasSelectedEquipment()
    {
        return selectedEquipmentIndex >= 0 && selectedEquipmentIndex < EquipmentNames.Length;
    }

    private void ApplyCategory(bool showEquipment)
    {
        SetActive(equipmentItemsRoot, showEquipment);
        SetActive(consumableItemsRoot, !showEquipment);
        SetTab(equipmentButtonImage, equipmentButtonText, showEquipment);
        SetTab(consumableButtonImage, consumableButtonText, !showEquipment);

        if (categoryStatusText != null)
        {
            categoryStatusText.text = showEquipment
                ? "장비 8개 · 카드를 눌러 상세 팝업 확인 · LOCAL DATA"
                : "소비 아이템 10개 · 보유 수량 표시 · LOCAL DATA";
        }

        if (scrollRect != null)
        {
            GameObject activeRoot = showEquipment ? equipmentItemsRoot : consumableItemsRoot;
            scrollRect.content = activeRoot != null ? activeRoot.GetComponent<RectTransform>() : null;
            Canvas.ForceUpdateCanvases();
            if (scrollRect.content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(scrollRect.content);
            }
            scrollRect.StopMovement();
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void SetTab(Image image, TMP_Text text, bool selected)
    {
        if (image != null)
        {
            image.sprite = selected ? selectedTabSprite : normalTabSprite;
            image.color = selected ? Color.white : new Color32(0xFF, 0xF9, 0xEA, 0xFF);
            image.type = Image.Type.Sliced;
        }

        if (text != null)
        {
            text.color = selected ? selectedTextColor : normalTextColor;
            text.fontStyle = selected ? FontStyles.Bold : FontStyles.Normal;
        }
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
