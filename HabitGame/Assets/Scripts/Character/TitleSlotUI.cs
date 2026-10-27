using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TitleSlotUI : MonoBehaviour
{
    [SerializeField]
    private Image titleBadgeImage;     // 가로형 칭호 배지 이미지
    [SerializeField]
    private TextMeshProUGUI titleNameText;
    [SerializeField]
    private TextMeshProUGUI equippedStatusText;
    [SerializeField]
    private Image borderImage;         // 테두리 이미지 (색상 적용)
    [SerializeField]
    private Button slotButton;         // 슬롯 전체를 클릭할 버튼

    private TitleDataSO titleData;

    private static readonly Color32 EquippedColor = new Color32(217, 164, 65, 255);
    private static readonly Color32 NormalColor = new Color32(201, 168, 120, 140);

    public void LoadData(TitleDataSO data, bool isEquipped, Action<TitleDataSO> onClick)
    {
        ResetUI();

        titleData = data;
        if (titleData == null) return;

        // 배지 이미지 적용
        if (titleBadgeImage != null && titleData.displaySprite != null)
        {
            titleBadgeImage.sprite = titleData.displaySprite;
        }
        if (titleNameText != null)
        {
            titleNameText.text = titleData.displayName;
        }

        // 장착 중 테두리 표시/숨김
        SetEquipped(isEquipped);

        // 클릭 시 상세 팝업을 띄우도록 이벤트 연결
        if (slotButton != null)
        {
            slotButton.onClick.RemoveAllListeners();
            slotButton.onClick.AddListener(() => onClick?.Invoke(titleData));
        }
    }

    public void SetEquipped(bool isEquipped)
    {
        if (borderImage != null)
        {
            // 장착 여부에 따라 테두리 색상만 스위칭
            borderImage.color = isEquipped ? EquippedColor : NormalColor;
        }
        if (equippedStatusText != null)
        {
            equippedStatusText.gameObject.SetActive(isEquipped);
        }
    }

    // UI 데이터 초기화
    public void ResetUI()
    {
        titleData = null;
        if (titleBadgeImage != null)
        {
            titleBadgeImage.sprite = null;
        }
        if (titleNameText != null)
        {
            titleNameText.text = "";
        }
        if (borderImage != null)
        {
            borderImage.color = NormalColor;
        }
        if (equippedStatusText != null)
        {
            equippedStatusText.gameObject.SetActive(false);
        }
        if (slotButton != null)
        {
            slotButton.onClick.RemoveAllListeners();
        }
    }
}