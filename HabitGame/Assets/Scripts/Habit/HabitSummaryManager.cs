using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HabitSummaryManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private TextMeshProUGUI summaryCountText;
    [SerializeField] private Slider progressBar;

    [Header("Habit List")]
    [SerializeField] private Transform content;

    private void Start()
    {
        RefreshSummary();
    }

    public void RefreshSummary()
    {
        int total = content.childCount;
        int completed = 0;

        for (int i = 0; i < total; i++)
        {
            Toggle toggle = content.GetChild(i)
                .GetComponentInChildren<Toggle>();

            if (toggle != null && toggle.isOn)
            {
                completed++; 
            }
        }

        float progress = total == 0
            ? 0f
            : (float)completed / total;

        summaryCountText.text = $"{completed} / {total}";
        progressBar.value = progress;
    }
}
