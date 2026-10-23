
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

    // 현재 선택한 목표 Section
    private Transform selectedSection;

    // =========================================
    // 현재 선택한 Section 설정
    // =========================================
    public void SetSelectedSection(Transform section)
    {
        selectedSection = section;
        RefreshSummary();
    }

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

        // 현재 선택한 Section 연결 확인
        if (selectedSection == null)
        {
            SetEmptySummary();
            return;
        }

        int total = 0;
        int completed = 0;

        // =========================================
        // 선택한 Section의 Habit Item Toggle 상태 확인
        // =========================================
        for (int i = 0; i < selectedSection.childCount; i++)
        {
            Transform child = selectedSection.GetChild(i);

            if (child == null)
                continue;

            // 실제 Habit Item만 계산
            HabitItem habitItem =
                child.GetComponent<HabitItem>();

            if (habitItem == null)
                continue;

            Toggle toggle =
                child.GetComponentInChildren<Toggle>(true);

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
