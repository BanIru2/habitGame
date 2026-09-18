using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

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
    [SerializeField]
    private GameObject dim;

    [Header("결과 UI")]
    [SerializeField]
    private GameObject gachaResultPopup;
    [SerializeField]
    private RectTransform resultContainer;
    [SerializeField]
    private GridLayoutGroup resultGridLayout;
    [SerializeField]
    private GameObject resultItemPrefab;

    // 비용 임시값(G)
    // 1회 뽑기 비용
    private const int gachaCost = 500;
    // 10회 뽑기 비용
    private const int tenGachaCost = 5000;

    private List<EquipmentDataSO> canGetList;
    private int totalWeight;

    // 결과 출력용 슬롯 풀링 변수들
    private GameObject[] gachaSlotPool = new GameObject[10];
    private int poolPointer = 0;

    private ShopBackendManager shopBackendManager;

    private void Awake()
    {
        oneGachaButton.onClick.AddListener(() => DoGacha(1));
        tenGachaButton.onClick.AddListener(() => DoGacha(10));

        shopBackendManager = FindObjectOfType<ShopBackendManager>();
    }

    // 가챠탭(상점)이 열릴 때 호출
    public void OpenGachaPanel()
    {
        gachaPanel.SetActive(true);
        GetList();
        CalcWeight();
    }

    // 가챠탭 닫기
    public void CloseGachaPanel()
    {
        gachaPanel.SetActive(false);
    }

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
        // 골드 조건 검사
        int cost = (count == 1) ? gachaCost : tenGachaCost;
        var charData = CharacterManager.Instance.characterStatusData;


        if (charData == null)
        {
            Debug.LogError("캐릭터 데이터가 없습니다");
            return;
        }
        if (charData.Gold < cost)
        {
            Debug.LogWarning("골드가 부족합니다");
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
        foreach(Transform child in resultContainer)
        {
            if(child.gameObject.activeSelf)
                child.gameObject.SetActive(false);
        }

        gachaResultPopup.SetActive(true);
        dim.SetActive(true);

        float availableWidth = resultContainer.rect.width - (resultGridLayout.padding.left + resultGridLayout.padding.right);
        float cellWidthWithSpacing = resultGridLayout.cellSize.x + resultGridLayout.spacing.x;

        // 기기 해상도에 따라 첫 줄에 최대로 들어갈 수 있는 개수
        int maxColumns = Mathf.FloorToInt((availableWidth + resultGridLayout.spacing.x) / cellWidthWithSpacing);
        maxColumns = Mathf.Max(1, maxColumns);    // 최소 개수 1개로 지정
        
        // 10연챠면 화면 내 한줄 최대 개수 / 단챠면 1(result.count)
        int finalColumns = Mathf.Min(results.Count, maxColumns);

        // GridLayoutGroup 컴포넌트 세팅 조정 (constraint 방식 및 값)
        resultGridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        resultGridLayout.constraintCount = finalColumns;

        // 프리팹 생성
        RenderItems(results);
    }
    
    private void RenderItems(List<EquipmentDataSO> results)
    {
        poolPointer = 0;
        foreach(var item in results)
        {
            if (gachaSlotPool[poolPointer] == null)
            {
                gachaSlotPool[poolPointer] = Instantiate(resultItemPrefab, resultContainer);
            }

            if (!gachaSlotPool[poolPointer].activeSelf) gachaSlotPool[poolPointer].SetActive(true);

            Image resultIcon = gachaSlotPool[poolPointer].GetComponent<Image>();
            resultIcon.sprite = item.icon;

            poolPointer++;
        }
    }

    // Dim 부분 터치를 통해 결과창 닫도록 인스펙터 수준에서 연결 중
    public void OnTouchedDim()
    {
        CloseResult();
    }

    private void CloseResult()
    {
        gachaResultPopup.SetActive(false);
        dim.SetActive(false);
    }
}
