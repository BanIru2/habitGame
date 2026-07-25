using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Shop/Config", fileName = "ShopConfig")]
public class ShopConfigSO : ScriptableObject
{
    public List<EquipmentDataSO> equipmentItems;
    public List<ConsumableDataSO> consumableItems;
}
