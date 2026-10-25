using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InventoryPanel : MonoBehaviour
{
    private void OnDisable()
    {
        InventoryManager.Instance.ClosePopup();
    }
}
