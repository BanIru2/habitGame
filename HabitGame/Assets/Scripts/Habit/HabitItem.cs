using UnityEngine;
using UnityEngine.UI;

public class HabitItem : MonoBehaviour
{
    [Header("Toggle")]
    [SerializeField] private Toggle completeToggle;

    [Header("Detail")]
    [SerializeField] private Button labelButton;

    private HabitSummaryManager summaryManager;
    private HabitDetailManager detailManager;

    // 이 항목의 습관 데이터
    private HabitGoalResponse habitData;

    private void Start()
    {
        summaryManager = FindObjectOfType<HabitSummaryManager>();
        detailManager = FindObjectOfType<HabitDetailManager>();

        // 체크박스 이벤트
        if (completeToggle != null)
        {
            completeToggle.onValueChanged.AddListener(OnToggleChanged);
        }

        // Label 클릭 이벤트
        if (labelButton != null)
        {
            labelButton.onClick.AddListener(OnClickLabel);
        }
    }

    // 이 HabitItem에 실제 습관 데이터 저장
    public void SetData(HabitGoalResponse data)
    {
        habitData = data;
    }

    private void OnToggleChanged(bool isOn)
    {
        if (summaryManager != null)
        {
            summaryManager.RefreshSummary();
        }
    }

    // Label 클릭
    private void OnClickLabel()
    {
        if (habitData == null)
        {
            Debug.LogWarning(
                "HabitItem에 습관 데이터가 연결되지 않았습니다."
            );
            return;
        }

        if (detailManager == null)
        {
            Debug.LogWarning(
                "HabitDetailManager를 찾을 수 없습니다."
            );
            return;
        }

        detailManager.OpenDetail(habitData);
    }

    private void OnDestroy()
    {
        if (completeToggle != null)
        {
            completeToggle.onValueChanged.RemoveListener(OnToggleChanged);
        }

        if (labelButton != null)
        {
            labelButton.onClick.RemoveListener(OnClickLabel);
        }
    }
}