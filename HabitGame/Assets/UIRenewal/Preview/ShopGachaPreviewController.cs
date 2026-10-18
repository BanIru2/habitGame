using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// UITestScene-only local preview for the renewed shop shell.
/// It only switches prebuilt local panels and never accesses ShopUIManager, GachaManager, or an API.
/// </summary>
public sealed class ShopGachaPreviewController : MonoBehaviour
{
    [SerializeField] private Button consumableButton;
    [SerializeField] private Button equipmentGachaButton;
    [SerializeField] private Image consumableButtonImage;
    [SerializeField] private Image equipmentGachaButtonImage;
    [SerializeField] private TMP_Text consumableButtonText;
    [SerializeField] private TMP_Text equipmentGachaButtonText;
    [SerializeField] private GameObject consumablePanel;
    [SerializeField] private GameObject gachaPanel;
    [SerializeField] private Button oneGachaButton;
    [SerializeField] private Button tenGachaButton;
    [SerializeField] private Button probabilityButton;
    [SerializeField] private GameObject probabilityPopup;
    [SerializeField] private Button probabilityCloseButton;
    [SerializeField] private TMP_Text gachaInputStatusText;
    [SerializeField] private GameObject gachaResultPopup;
    [SerializeField] private Button gachaResultDimButton;
    [SerializeField] private Button gachaResultCloseButton;
    [SerializeField] private GridLayoutGroup gachaResultGrid;
    [SerializeField] private GameObject[] gachaResultCards;
    [SerializeField] private TMP_Text[] gachaResultIconTexts;
    [SerializeField] private TMP_Text[] gachaResultNameTexts;
    [SerializeField] private TMP_Text[] gachaResultTierTexts;
    [SerializeField] private ScrollRect consumableScroll;
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private Button lowBalanceButton;
    [SerializeField] private Button[] productButtons;
    [SerializeField] private TMP_Text[] productStatusTexts;
    [SerializeField] private GameObject detailPopup;
    [SerializeField] private TMP_Text detailIconText;
    [SerializeField] private TMP_Text detailNameText;
    [SerializeField] private TMP_Text detailDescriptionText;
    [SerializeField] private TMP_Text detailPriceText;
    [SerializeField] private TMP_Text detailPurchaseStatusText;
    [SerializeField] private Button buyButton;
    [SerializeField] private Button detailCloseButton;
    [SerializeField] private GameObject purchaseFailPopup;
    [SerializeField] private Button purchaseFailCloseButton;
    [SerializeField] private Sprite selectedTabSprite;
    [SerializeField] private Sprite normalTabSprite;
    [SerializeField] private Color selectedTextColor = new Color32(0x5B, 0x36, 0x39, 0xFF);
    [SerializeField] private Color normalTextColor = new Color32(0x86, 0x68, 0x5D, 0xFF);
    [SerializeField] private Behaviour[] isolatedBehaviours;
    [SerializeField] private bool[] isolatedBehaviourEnabledStates;
    [SerializeField] private GameObject[] isolatedGameObjects;
    [SerializeField] private bool[] isolatedGameObjectActiveStates;

    private static readonly string[] ProductNames =
    {
        "작은 회복 물약", "집중의 차", "불꽃 영약", "맑은 샘물",
        "행운 쿠키", "방어 비약", "신속의 깃털", "별가루 병"
    };
    private static readonly string[] ProductDescriptions =
    {
        "HP를 조금 회복하는 기본 물약", "전투 준비 중 집중력을 높여주는 차",
        "불 속성의 힘을 일시적으로 높인다", "지친 모험가의 기운을 맑게 한다",
        "작은 행운이 담긴 바삭한 간식", "전투 준비 중 방어 능력을 보조한다",
        "가벼운 움직임을 돕는 마법 깃털", "반짝이는 별가루를 담은 작은 병"
    };
    private static readonly string[] ProductIcons = { "회", "집", "불", "샘", "행", "방", "신", "별" };
    private static readonly int[] ProductPrices = { 120, 180, 350, 220, 280, 320, 400, 500 };
    private readonly bool[] purchased = new bool[8];
    private static readonly string[] PreviewEquipmentNames =
    {
        "훈련용 목검", "견습 모험가의 방패", "바람 깃든 장갑", "은빛 수호 투구", "황금 별 지팡이",
        "달빛 여행자 장화", "붉은 용사의 갑옷", "고대 룬 목걸이", "새벽빛 활", "왕실 기사 검"
    };
    private static readonly string[] PreviewEquipmentIcons = { "검", "방", "장", "투", "지", "화", "갑", "룬", "활", "검" };
    private static readonly string[] PreviewEquipmentTiers =
    {
        "일반", "일반", "희귀", "희귀", "영웅", "일반", "영웅", "전설", "희귀", "영웅"
    };
    private static readonly Color[] PreviewTierColors =
    {
        new Color32(0x6E, 0x4B, 0x45, 0xFF), new Color32(0x6E, 0x4B, 0x45, 0xFF),
        new Color32(0x37, 0x78, 0x9E, 0xFF), new Color32(0x37, 0x78, 0x9E, 0xFF),
        new Color32(0x8B, 0x4A, 0xA8, 0xFF), new Color32(0x6E, 0x4B, 0x45, 0xFF),
        new Color32(0x8B, 0x4A, 0xA8, 0xFF), new Color32(0xD0, 0x86, 0x16, 0xFF),
        new Color32(0x37, 0x78, 0x9E, 0xFF), new Color32(0x8B, 0x4A, 0xA8, 0xFF)
    };
    private int selectedIndex = -1;
    private int localGold = 12450;
    private bool isGachaResultOpen;
    private bool isGachaRevealPlaying;
    private Coroutine gachaRevealRoutine;

    private void Awake()
    {
        Add(consumableButton, ShowConsumables);
        Add(equipmentGachaButton, ShowEquipmentGacha);
        Add(oneGachaButton, () => RunLocalGacha(1));
        Add(tenGachaButton, () => RunLocalGacha(10));
        Add(probabilityButton, OpenProbabilityPopup);
        Add(probabilityCloseButton, CloseProbabilityPopup);
        Add(gachaResultDimButton, OnGachaResultDimTouched);
        Add(gachaResultCloseButton, CloseGachaResult);
        Add(lowBalanceButton, SetLowBalanceForTest);
        Add(buyButton, BuySelectedItem);
        Add(detailCloseButton, CloseDetailPopup);
        Add(purchaseFailCloseButton, ClosePurchaseFailPopup);
        if (productButtons != null)
        {
            for (int index = 0; index < productButtons.Length; index++)
            {
                int capturedIndex = index;
                Add(productButtons[index], () => OpenDetail(capturedIndex));
            }
        }
    }

    private void Start()
    {
        ShowConsumables();
        RefreshGold();
    }

    public void ShowConsumables()
    {
        SetActive(consumablePanel, true);
        SetActive(gachaPanel, false);
        SetActive(probabilityPopup, false);
        CloseGachaResult();
        SetTab(consumableButtonImage, consumableButtonText, true);
        SetTab(equipmentGachaButtonImage, equipmentGachaButtonText, false);

        if (consumableScroll != null)
        {
            Canvas.ForceUpdateCanvases();
            if (consumableScroll.content != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(consumableScroll.content);
            }
            consumableScroll.StopMovement();
            consumableScroll.verticalNormalizedPosition = 1f;
        }
    }

    public void ShowEquipmentGacha()
    {
        SetActive(consumablePanel, false);
        SetActive(gachaPanel, true);
        SetTab(consumableButtonImage, consumableButtonText, false);
        SetTab(equipmentGachaButtonImage, equipmentGachaButtonText, true);
    }

    private void RunLocalGacha(int count)
    {
        if (isGachaResultOpen) return;

        int cost = count == 1 ? 500 : 5000;
        if (localGold < cost)
        {
            SetText(gachaInputStatusText, "보유 골드가 부족합니다 · 가챠 결과는 생성되지 않았습니다.");
            return;
        }

        localGold -= cost;
        RefreshGold();
        SetText(gachaInputStatusText, count == 1
            ? "500 G 사용 · Preview 가상 장비 1개"
            : "5,000 G 사용 · Preview 가상 장비 10개");
        ShowGachaResult(count);
    }

    private void ShowGachaResult(int count)
    {
        isGachaResultOpen = true;
        isGachaRevealPlaying = true;
        SetGachaButtonsInteractable(false);
        SetActive(probabilityPopup, false);
        SetActive(gachaResultPopup, true);

        if (gachaResultGrid != null)
        {
            bool single = count == 1;
            gachaResultGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gachaResultGrid.constraintCount = single ? 1 : 5;
            gachaResultGrid.cellSize = single ? new Vector2(360f, 470f) : new Vector2(164f, 330f);
            gachaResultGrid.spacing = single ? Vector2.zero : new Vector2(16f, 22f);
        }

        for (int index = 0; index < gachaResultCards.Length; index++)
        {
            bool active = index < count;
            SetActive(gachaResultCards[index], active);
            if (!active) continue;
            int dataIndex = count == 1 ? 7 : index;
            SetText(gachaResultIconTexts[index], PreviewEquipmentIcons[dataIndex]);
            SetText(gachaResultNameTexts[index], PreviewEquipmentNames[dataIndex]);
            SetText(gachaResultTierTexts[index], PreviewEquipmentTiers[dataIndex] + " 등급");
            gachaResultTierTexts[index].color = PreviewTierColors[dataIndex];
            gachaResultCards[index].transform.localScale = Vector3.zero;
        }

        if (gachaRevealRoutine != null) StopCoroutine(gachaRevealRoutine);
        gachaRevealRoutine = StartCoroutine(RevealGachaCards(count));
    }

    private IEnumerator RevealGachaCards(int count)
    {
        for (int index = 0; index < count; index++)
        {
            float elapsed = 0f;
            while (elapsed < 0.16f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 0.16f));
                gachaResultCards[index].transform.localScale = Vector3.one * t;
                yield return null;
            }
            gachaResultCards[index].transform.localScale = Vector3.one;
            if (index + 1 < count) yield return new WaitForSecondsRealtime(0.05f);
        }
        gachaRevealRoutine = null;
        isGachaRevealPlaying = false;
    }

    private void OnGachaResultDimTouched()
    {
        if (isGachaRevealPlaying)
        {
            CompleteGachaReveal();
            return;
        }
        CloseGachaResult();
    }

    private void CompleteGachaReveal()
    {
        if (gachaRevealRoutine != null)
        {
            StopCoroutine(gachaRevealRoutine);
            gachaRevealRoutine = null;
        }
        if (gachaResultCards != null)
        {
            foreach (GameObject card in gachaResultCards)
            {
                if (card != null && card.activeSelf) card.transform.localScale = Vector3.one;
            }
        }
        isGachaRevealPlaying = false;
    }

    private void CloseGachaResult()
    {
        CompleteGachaReveal();
        SetActive(gachaResultPopup, false);
        isGachaResultOpen = false;
        SetGachaButtonsInteractable(true);
    }

    private void SetGachaButtonsInteractable(bool interactable)
    {
        if (oneGachaButton != null) oneGachaButton.interactable = interactable;
        if (tenGachaButton != null) tenGachaButton.interactable = interactable;
    }

    private void OpenProbabilityPopup()
    {
        if (isGachaResultOpen) return;
        SetActive(probabilityPopup, true);
    }

    private void CloseProbabilityPopup()
    {
        SetActive(probabilityPopup, false);
    }

    private void OpenDetail(int index)
    {
        if (index < 0 || index >= ProductNames.Length) return;
        selectedIndex = index;
        SetText(detailIconText, ProductIcons[index]);
        SetText(detailNameText, ProductNames[index]);
        SetText(detailDescriptionText, ProductDescriptions[index]);
        SetText(detailPriceText, ProductPrices[index].ToString("N0") + " G");
        RefreshPurchaseState();
        SetActive(purchaseFailPopup, false);
        SetActive(detailPopup, true);
    }

    private void BuySelectedItem()
    {
        if (selectedIndex < 0 || selectedIndex >= ProductPrices.Length || purchased[selectedIndex]) return;
        int price = ProductPrices[selectedIndex];
        if (localGold < price)
        {
            SetActive(detailPopup, false);
            SetActive(purchaseFailPopup, true);
            return;
        }
        localGold -= price;
        purchased[selectedIndex] = true;
        RefreshGold();
        if (productStatusTexts != null && selectedIndex < productStatusTexts.Length)
        {
            SetText(productStatusTexts[selectedIndex], "구매 완료");
        }
        RefreshPurchaseState();
    }

    private void SetLowBalanceForTest()
    {
        localGold = 50;
        RefreshGold();
        CloseDetailPopup();
        SetActive(purchaseFailPopup, false);
    }

    private void RefreshGold()
    {
        SetText(goldText, localGold.ToString("N0") + " G");
    }

    private void RefreshPurchaseState()
    {
        bool isPurchased = selectedIndex >= 0 && purchased[selectedIndex];
        SetText(detailPurchaseStatusText, isPurchased ? "구매 완료 · Preview 로컬 상태" : "구매 가능 · Preview 로컬 테스트");
        if (buyButton != null) buyButton.interactable = !isPurchased;
    }

    private void CloseDetailPopup()
    {
        SetActive(detailPopup, false);
    }

    private void ClosePurchaseFailPopup()
    {
        SetActive(purchaseFailPopup, false);
        selectedIndex = -1;
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

    private static void SetActive(GameObject target, bool active)
    {
        if (target != null)
        {
            target.SetActive(active);
        }
    }

    private static void SetText(TMP_Text target, string value)
    {
        if (target != null) target.text = value;
    }
}
