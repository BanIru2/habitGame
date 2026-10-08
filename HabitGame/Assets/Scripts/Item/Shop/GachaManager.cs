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
    private Button probabilityCloseButton;
    [SerializeField]
    private TextMeshProUGUI tier1Text;
    [SerializeField]
    private TextMeshProUGUI tier2Text;
    [SerializeField]
    private TextMeshProUGUI tier3Text;
    [SerializeField]
    private TextMeshProUGUI tier4Text;

    // 비용 임시값(G)
    // 1회 뽑기 비용
    private const int gachaCost = 500;
    // 10회 뽑기 비용
    private const int tenGachaCost = 5000;

    private List<EquipmentDataSO> canGetList;
    private int totalWeight;

    private ShopBackendManager shopBackendManager;

    private void Awake()
    {
        oneGachaButton.onClick.AddListener(() => DoGacha(1));
        tenGachaButton.onClick.AddListener(() => DoGacha(10));
        shopBackendManager = FindObjectOfType<ShopBackendManager>();

        probabilityButton.onClick.AddListener(OpenProbabilityPopup);
        probabilityCloseButton.onClick.AddListener(CloseProbabilityPopup);

    }

    // 가챠탭(상점)이 열릴 때 호출
    public void OpenGachaPanel()
    {
        gachaPanel.SetActive(true);
        GetList();
        CalcWeight();
    }

    // 가챠탭 닫기
    public void CloseGachaPanel() => gachaPanel.SetActive(false);

    // 획득 가능 장비 리스트 세팅
    private void GetList()
    {
        canGetList = ShopUIManager.Instance.GetUnlockedEquipmnetList();
    }

    private void CalcWeight()
    {
        totalWeight = 0;
        foreach (var item in canGetList)
        {
            totalWeight += GetItemWeight(item);
        }
    }

    // --------------------------- 뽑기 및 가중치 --------------------------------
    private int GetItemWeight(EquipmentDataSO item)
    {
        if (item == null) return 0;

        int reqLevel = item.unlockCondition.requiredAttributeLevel;
        // 요구 레벨이 높을수록 가중치를 낮게 하여 확률 낮추기
        if (reqLevel >= 7) return 5;
        if (reqLevel >= 4) return 20;
        if (reqLevel >= 1) return 50;
        return 100;
    }

    private EquipmentDataSO PickOneItem()
    {
        // 0 ~ totalWeight 사이 랜덤 숫자 뽑기
        int randomValue = Random.Range(0, totalWeight);
        // 당첨 아이템 찾기 (누적 가중치 차감 방식)
        foreach (var item in canGetList)
        {
            int weight = GetItemWeight(item);
            if (randomValue < weight)
            {
                return item;    // 결정된 아이템
            }
            randomValue -= weight; // 랜덤 값에서 이번 순서 아이템 가중치를 빼고 다음으로 넘김
        }
        return canGetList[0];
    }

    public async void DoGacha(int count)
    {
        if (canGetList == null || canGetList.Count == 0)
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

        oneGachaButton.interactable = false;
        tenGachaButton.interactable = false;

        try
        {
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
        UpdateProbabilityTexts();

        probabilityPopup.SetActive(true);
    }

    public void CloseProbabilityPopup()
    {
        probabilityPopup.SetActive(false);
    }

    private void UpdateProbabilityTexts()
    {
        // 최신 해금 장비 목록 및 총 가중치 갱신
        GetList();
        CalcWeight();

        if (totalWeight <= 0)
        {
            tier1Text.SetText("1티어 (기본)  : 0.00%");
            tier2Text.SetText("2티어 (Lv.1+) : 0.00%");
            tier3Text.SetText("3티어 (Lv.4+) : 0.00%");
            tier4Text.SetText("4티어 (Lv.7+) : 0.00%");
            return;
        }

        // 현재 풀(canGetList)에 있는 각 티어별 가중치 합산
        int tier1Weight = 0;
        int tier2Weight = 0;
        int tier3Weight = 0;
        int tier4Weight = 0;

        foreach (var item in canGetList)
        {
            if (item == null) continue;
            int reqLevel = item.unlockCondition.requiredAttributeLevel;

            if (reqLevel >= 7) tier4Weight += 5;
            else if (reqLevel >= 4) tier3Weight += 20;
            else if (reqLevel >= 1) tier2Weight += 50;
            else tier1Weight += 100;
        }

        // 각 티어별 총 확률(%) 계산
        float tier4Rate = (float)tier4Weight / totalWeight * 100f;
        float tier3Rate = (float)tier3Weight / totalWeight * 100f;
        float tier2Rate = (float)tier2Weight / totalWeight * 100f;
        float tier1Rate = (float)tier1Weight / totalWeight * 100f;

        // 텍스트 반영 ({0:2}는 소수점 둘째 자리까지 표기)
        tier4Text.SetText("4티어 (Lv.7+) : {0:2}%", tier4Rate);
        tier3Text.SetText("3티어 (Lv.4+) : {0:2}%", tier3Rate);
        tier2Text.SetText("2티어 (Lv.1+) : {0:2}%", tier2Rate);
        tier1Text.SetText("1티어 (기본)  : {0:2}%", tier1Rate);
    }
}
