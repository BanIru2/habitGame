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

    // 결과 출력용 슬롯 풀링 변수들
    private GameObject[] gachaSlotPool = new GameObject[10];
    private int poolPointer = 0;


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
