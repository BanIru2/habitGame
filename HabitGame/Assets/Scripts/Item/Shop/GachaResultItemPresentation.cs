using TMPro;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public sealed class GachaResultItemPresentation : MonoBehaviour
{
    [SerializeField] private Sprite frameSprite;
    [SerializeField] private Image itemIcon;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text itemTierText;

    private static EquipmentDataSO[] equipmentCatalog;
    private Image sourceImage;

    private void Awake()
    {
        sourceImage = GetComponent<Image>();
        if (equipmentCatalog == null)
            equipmentCatalog = Resources.LoadAll<EquipmentDataSO>("Data/EquipmentSO");
    }

    private void LateUpdate()
    {
        if (sourceImage != null && itemIcon != null)
            itemIcon.color = sourceImage.color;

        if (sourceImage == null || sourceImage.sprite == null || sourceImage.sprite == frameSprite)
            return;

        Sprite displayedItemSprite = sourceImage.sprite;
        if (itemIcon != null)
        {
            itemIcon.sprite = displayedItemSprite;
            itemIcon.preserveAspect = true;
        }

        EquipmentDataSO item = FindItem(displayedItemSprite);
        if (itemNameText != null)
            itemNameText.text = item != null ? item.displayName : displayedItemSprite.name;
        if (itemTierText != null)
        {
            itemTierText.text = item != null ? $"{item.Tier}티어" : string.Empty;
            itemTierText.color = item != null ? item.TierColor : Color.white;
        }

        sourceImage.sprite = frameSprite;
    }

    private static EquipmentDataSO FindItem(Sprite icon)
    {
        if (equipmentCatalog == null) return null;
        foreach (EquipmentDataSO item in equipmentCatalog)
        {
            if (item != null && item.icon == icon) return item;
        }
        return null;
    }
}
