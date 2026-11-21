using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class GachaManager : MonoBehaviour
{
    [SerializeField]
    private GameObject gachaPanel;
    [SerializeField]
    private GameObject gachaMainImage;
    [SerializeField]
    private Button oneGachaButton;
    [SerializeField]
    private Button tenGachaButton;
    [SerializeField]
    private Button probabilityButton;

    [Header("결과 UI")]
    [SerializeField]
    private GachaResultPopupManager resultPopupManager;

    [Header("확률 팝업 UI")]
    [SerializeField]
    private GameObject probabilityPopup;
    [SerializeField]
    private GameObject dim;
    [SerializeField]
    private Button probabilityCloseButton;
    [SerializeField]
    private TextMeshProUGUI tier1Text;
    [SerializeField]
    private TextMeshProUGUI tier2Text;
    [SerializeField]
    private TextMeshProUGUI tier3Text;
    [SerializeField]
    private TextMeshProUGUI tier4Text;

    [SerializeField]
    private Button fireTabButton;
    [SerializeField]
    private Button waterTabButton;
    [SerializeField]
    private Button grassTabButton;
    [SerializeField]
    private Button auroraTabButton;
    [SerializeField]
    private Button armorTabButton;
    [SerializeField]
    private Color selectedTabColor = new Color32(255, 220, 140, 255);
    [SerializeField] 
    private Color normalTabColor = Color.white;

    private enum GachaProbabilityGroup
    {
        FireWeapon = 0,
        WaterWeapon = 1,
        GrassWeapon = 2,
        AuroraWeapon = 3,
        Armor = 4
    }

    private const int GroupCount = 5;
    private const int TierCount = 4;

    private Button[] probabilityTabButtons;
    private readonly List<EquipmentDataSO> gachaPool = new List<EquipmentDataSO>();
    private int totalWeight;
    private readonly int[,] groupTierWeights = new int[GroupCount, TierCount];
    private readonly int[] groupTotalWeights = new int[GroupCount];
    private GachaProbabilityGroup selectedProbabilityGroup = GachaProbabilityGroup.FireWeapon;


    // 비용 임시값(G)
    // 1회 뽑기 비용
    private const int gachaCost = 500;
    // 10회 뽑기 비용
    private const int tenGachaCost = 5000;

    private ShopBackendManager shopBackendManager;

    private void Awake()
    {
        oneGachaButton.onClick.AddListener(() => DoGacha(1));
        tenGachaButton.onClick.AddListener(() => DoGacha(10));
        shopBackendManager = FindObjectOfType<ShopBackendManager>();

        probabilityButton.onClick.AddListener(OpenProbabilityPopup);
        probabilityCloseButton.onClick.AddListener(CloseProbabilityPopup);

        probabilityTabButtons = new[]
        {
            fireTabButton,
            waterTabButton,
            grassTabButton,
            auroraTabButton,
            armorTabButton
        };

        for (int i = 0; i < probabilityTabButtons.Length; i++)
        {
            if (probabilityTabButtons[i] == null) continue;
            GachaProbabilityGroup group = (GachaProbabilityGroup)i;
            probabilityTabButtons[i].onClick.AddListener(() => SelectProbabilityGroup(group));
        }
    }

    // 가챠탭(상점)이 열릴 때 호출
    public void OpenGachaPanel()
    {
        gachaPanel.SetActive(true);
        RebuildGachaData();
    }

    public void CloseGachaPanel()
    {
        ClosePopups();
        gachaPanel.SetActive(false);
    }

    private void RebuildGachaData()
    {
        gachaPool.Clear();
        totalWeight = 0;
        System.Array.Clear(groupTierWeights, 0, groupTierWeights.Length);
        System.Array.Clear(groupTotalWeights, 0, groupTotalWeights.Length);

        List<EquipmentDataSO> unlockedList = ShopUIManager.Instance.GetUnlockedEquipmnetList();
        if (unlockedList == null) return;

        foreach (var item in unlockedList)
        {
            if (item == null) continue;
            if (!TryGetProbabilityGroup(item, out GachaProbabilityGroup group)) continue;

            int weight = GetTierWeight(item.Tier);
            int g = (int)group;
            int t = item.Tier - 1;    // 인덱스 0-based로 맞추기 위해 -1

            gachaPool.Add(item);
            totalWeight += weight;
            groupTierWeights[g, t] += weight;
            groupTotalWeights[g] += weight;
        }
    }

    public void ClosePopups()
    {
        CloseProbabilityPopup();
        resultPopupManager.Close();
    }

    // --------------------------- 뽑기 및 가중치 --------------------------------
    private static int GetTierWeight(int tier) => tier switch
    {
        4 => 5,
        3 => 20,
        2 => 50,
        _ => 100
    };
    private static bool TryGetProbabilityGroup(EquipmentDataSO item, out GachaProbabilityGroup group)
    {
        group = default;
        if (item.equipmentType == EquipmentType.Clothes || item.equipmentType == EquipmentType.Shoes || item.equipmentType == EquipmentType.Hat)
        {
            group = GachaProbabilityGroup.Armor;
            return true;
        }
        if (item.equipmentType != EquipmentType.Weapon)
        {
            Debug.LogWarning($"[GachaManager] 지원하지 않는 장비 타입이라 가챠 풀에서 제외됨: {item.itemId} ({item.equipmentType})");
            return false;
        }
        if (item.unlockCondition == null)
        {
            Debug.LogWarning($"[GachaManager] unlockCondition이 비어 있어 가챠 풀에서 제외됨: {item.itemId}");
            return false;
        }
        switch (item.unlockCondition.requiredAttribute)
        {
            case AttributeType.Fire: 
                group = GachaProbabilityGroup.FireWeapon; 
                return true;
            case AttributeType.Water: 
                group = GachaProbabilityGroup.WaterWeapon; 
                return true;
            case AttributeType.Grass: 
                group = GachaProbabilityGroup.GrassWeapon; 
                return true;
            case AttributeType.Aurora: 
                group = GachaProbabilityGroup.AuroraWeapon; 
                return true;
            default:
                Debug.LogWarning($"[GachaManager] 무기 속성이 유효하지 않아 가챠 풀에서 제외됨: {item.itemId} ({item.unlockCondition.requiredAttribute})");
                return false;
        }
    }
    private EquipmentDataSO PickOneItem()
    {
        int randomValue = Random.Range(0, totalWeight);
        foreach (var item in gachaPool)
        {
            int weight = GetTierWeight(item.Tier);
            if (randomValue < weight)
                return item;
            randomValue -= weight;
        }

        // 가중치 총합에 대해 로직이 실패하는 예외상황 알림
        throw new System.InvalidOperationException($"가챠 가중치 불일치 (totalWeight={totalWeight}, count={gachaPool.Count})");
    }

    public async void DoGacha(int count)
    {
        if (gachaPool.Count == 0 || totalWeight <= 0)
        {
            Debug.LogWarning("획득 가능한 장비 목록이 없습니다.");
            ErrorPopupManager.Instance.ShowMessage("장비 목록을 불러오는 중입니다. 잠시 후 다시 시도해주세요.");
            return;
        }

        // 골드 조건 검사
        int cost = (count == 1) ? gachaCost : tenGachaCost;
        var charData = CharacterManager.Instance.characterStatusData;


        if (charData == null)
        {
            Debug.LogError("캐릭터 데이터가 없습니다");
            ErrorPopupManager.Instance.ShowMessage("캐릭터 정보를 불러오지 못했습니다.");
            return;
        }
        if (charData.Gold < cost)
        {
            Debug.LogWarning("골드가 부족합니다");
            ErrorPopupManager.Instance.ShowMessage("골드가 부족합니다.");
            return;
        }

        if (!MainTabManager.Instance.TryLockNavigation()) return;

        oneGachaButton.interactable = false;
        tenGachaButton.interactable = false;

        try
        {
            dim.SetActive(true);

            List<EquipmentDataSO> results = new List<EquipmentDataSO>(count);
            List<string> itemIds = new List<string>(count);
            // 가챠돌리기
            for (int i = 0; i < count; i++)
            {
                EquipmentDataSO pickedItem = PickOneItem();
                results.Add(pickedItem);
                itemIds.Add(pickedItem.itemId);
            }

            // 백엔드 저장 요청
            GachaResponse response = await shopBackendManager.GachaAsync(count, itemIds);

            if (response == null)
            {
                Debug.LogError("가챠 응답 데이터가 null입니다.");
                return;
            }

            // 골드 동기화 (캐릭터 전체 재요청 X, 골드 변수 하나만 갱신)
            charData.Gold = response.RemainingGold;
            ShopUIManager.Instance.UpdateGoldUI(response.RemainingGold);

            // 결과 팝업 화면에 띄우기
            ShowGachaResult(results);
        }
        catch (ApiException e)
        {
            Debug.LogError($"가챠 API 실패 ({e.StatusCode}): {e.Message}");
            ErrorPopupManager.Instance.ShowApiError(e);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"가챠 시스템 오류: {e.Message}");
            ErrorPopupManager.Instance.ShowSystemError();
        }
        finally
        {
            dim.SetActive(false);

            MainTabManager.Instance.UnlockNavigation();

            oneGachaButton.interactable = true;
            tenGachaButton.interactable = true;
        }
    }

    // --------------------------- 결과 처리 --------------------------------
    private void ShowGachaResult(List<EquipmentDataSO> results)
    {
        resultPopupManager.Show(results);
    }

    // --------------------------- 확률 팝업 --------------------------------
    public void OpenProbabilityPopup()
    {
        RebuildGachaData();
        SelectProbabilityGroup(GachaProbabilityGroup.FireWeapon);
        probabilityPopup.SetActive(true);
        dim.SetActive(true);
    }

    public void CloseProbabilityPopup()
    {
        probabilityPopup.SetActive(false);
        dim.SetActive(false);
    }

    private void SelectProbabilityGroup(GachaProbabilityGroup group)
    {
        selectedProbabilityGroup = group;
        UpdateTabButtonVisuals();
        RenderSelectedGroupProbabilities();
    }

    private void UpdateTabButtonVisuals()
    {
        if (probabilityTabButtons == null) return;
        for (int i = 0; i < probabilityTabButtons.Length; i++)
        {
            Button btn = probabilityTabButtons[i];
            if (btn == null || btn.image == null) continue;
            btn.image.color = (i == (int)selectedProbabilityGroup) ? selectedTabColor : normalTabColor;
        }
    }

    private void RenderSelectedGroupProbabilities()
    {
        int g = (int)selectedProbabilityGroup;
        int groupWeight = groupTotalWeights[g];

        if (groupWeight <= 0)
        {
            tier1Text.SetText("1티어 : 0.00%");
            tier2Text.SetText("2티어 : 0.00%");
            tier3Text.SetText("3티어 : 0.00%");
            tier4Text.SetText("4티어 : 0.00%");
            return;
        }

        float tier1Rate = (float)groupTierWeights[g, 0] / groupWeight * 100f;
        float tier2Rate = (float)groupTierWeights[g, 1] / groupWeight * 100f;
        float tier3Rate = (float)groupTierWeights[g, 2] / groupWeight * 100f;
        float tier4Rate = (float)groupTierWeights[g, 3] / groupWeight * 100f;

        tier1Text.SetText("1티어 : {0:2}%", tier1Rate);
        tier2Text.SetText("2티어 : {0:2}%", tier2Rate);
        tier3Text.SetText("3티어 : {0:2}%", tier3Rate);
        tier4Text.SetText("4티어 : {0:2}%", tier4Rate);
    }
}
