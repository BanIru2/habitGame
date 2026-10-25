using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Button))]
[DisallowMultipleComponent]
public sealed class GachaResultCloseButton : MonoBehaviour
{
    private Button closeButton;
    private GachaResultPopupManager popupManager;

    private void Awake()
    {
        closeButton = GetComponent<Button>();
        popupManager = FindObjectOfType<GachaResultPopupManager>(true);
        if (popupManager != null) closeButton.onClick.AddListener(popupManager.Close);
    }

    private void OnDestroy()
    {
        if (closeButton != null && popupManager != null)
            closeButton.onClick.RemoveListener(popupManager.Close);
    }
}
