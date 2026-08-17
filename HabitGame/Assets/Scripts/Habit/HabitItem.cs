using UnityEngine;
using UnityEngine.UI;

public class HabitItem : MonoBehaviour
{
    [SerializeField] private Toggle completeToggle;

    private HabitSummaryManager summaryManager;

    private void Start()
    {
        summaryManager = FindObjectOfType<HabitSummaryManager>();

        if (completeToggle != null)
        {
            completeToggle.onValueChanged.AddListener(OnToggleChanged);
        }
    }

    private void OnToggleChanged(bool isOn)
    {
        if (summaryManager != null)
        {
            summaryManager.RefreshSummary();
        }
    }

    private void OnDestroy()
    {
        if (completeToggle != null)
        {
            completeToggle.onValueChanged.RemoveListener(OnToggleChanged);
        }
    }
}
