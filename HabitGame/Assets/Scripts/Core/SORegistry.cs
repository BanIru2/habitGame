using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// SO에 접근하기 위한 통로
/// </summary>
public class SORegistry : Singleton<SORegistry>
{
    [SerializeField] private LifeStyleHabitCategorySO[] lifeStyleHabitCategories;
    [SerializeField] private ItemDataSO[] items;
    [SerializeField] private AttributeRuleSO attributeRule;
    [SerializeField] private BattleFormulaSO battleFormula;
    [SerializeField] private ShopConfigSO shopConfig;

    private readonly Dictionary<HabitCategory, LifeStyleHabitCategorySO> lifeStyleCategoryMap = new Dictionary<HabitCategory, LifeStyleHabitCategorySO>();
    private readonly Dictionary<string, ItemDataSO> itemMap = new Dictionary<string, ItemDataSO>();

    // 참조를 위한 프로퍼티
    public ShopConfigSO ShopConfig => shopConfig;
    public AttributeRuleSO AttributeRule => attributeRule;
    public BattleFormulaSO BattleFormula => battleFormula;

    protected override void Awake()
    {
        base.Awake();
        BuildMaps();
    }

    // Dictionary 채우기
    private void BuildMaps()
    {
        lifeStyleCategoryMap.Clear();
        itemMap.Clear();

        if (lifeStyleHabitCategories != null)
        {
            foreach (var categorySO in lifeStyleHabitCategories)
            {
                if (categorySO == null) continue;
                lifeStyleCategoryMap[categorySO.category] = categorySO;
            }
        }

        RegisterItems(items);

        if (shopConfig != null)
        {
            RegisterItems(shopConfig.equipmentItems);
            RegisterItems(shopConfig.consumableItems);
        }
    }

    private void RegisterItems(IEnumerable<ItemDataSO> source)
    {
        if (source == null) return;

        foreach (var item in source)
        {
            if (item == null || string.IsNullOrEmpty(item.itemId)) continue;
            // itemId 중복 방지
            if (itemMap.TryGetValue(item.itemId, out var existing))
            {
                if (existing != item)
                    Debug.LogWarning($"[SORegistry] Duplicate itemId found: {item.itemId}", this);
                continue;
            }
            itemMap[item.itemId] = item;
        }
    }

    // 참조를 위한 프로퍼티
    public LifeStyleHabitCategorySO GetLifeStyleHabitCategory(HabitCategory category)
    {
        return lifeStyleCategoryMap.TryGetValue(category, out var categorySO)
            ? categorySO
            : null;
    }
    public ItemDataSO GetItem(string itemId)
    {
        return itemMap.TryGetValue(itemId, out var item)
            ? item
            : null;
    }
}
