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

    // =========================================
    // Summary 갱신
    // =========================================
    public void RefreshSummary()
    {
        // Content 연결 확인
        if (content == null)
        {
            Debug.LogWarning(
                "HabitSummaryManager의 Content가 연결되지 않았습니다."
            );

            SetEmptySummary();
            return;
        }

        int total = 0;
        int completed = 0;

        // =========================================
        // Habit Item의 Toggle 상태 확인
        // =========================================
        for (int i = 0; i < content.childCount; i++)
        {
            Transform child = content.GetChild(i);

            if (child == null)
                continue;

            Toggle toggle =
                child.GetComponentInChildren<Toggle>();

            // Toggle이 있는 실제 Habit Item만
            // 전체 Habit 개수에 포함
            if (toggle == null)
                continue;

            total++;

            if (toggle.isOn)
            {
                completed++;
            }
        }

        float progress =
            total == 0
                ? 0f
                : (float)completed / total;

        // =========================================
        // 완료 개수 표시
        // =========================================
        if (summaryCountText != null)
        {
            summaryCountText.text =
                $"{completed} / {total}";
        }

        // =========================================
        // Progress Bar 갱신
        // =========================================
        if (progressBar != null)
        {
            progressBar.value = progress;
        }
    }

    // =========================================
    // Habit이 없거나 Content 연결 실패 시
    // =========================================
    private void SetEmptySummary()
    {
        if (summaryCountText != null)
        {
            summaryCountText.text = "0 / 0";
        }

        if (progressBar != null)
        {
            progressBar.value = 0f;
        }
    }
}