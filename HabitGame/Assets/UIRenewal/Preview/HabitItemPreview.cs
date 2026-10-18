using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class HabitItemPreview : MonoBehaviour
{
    public enum PreviewKind
    {
        Check,
        Number,
        Photo,
        LongTitle,
        Weekly
    }

    [SerializeField] private PreviewKind kind;
    [SerializeField] private Toggle completeToggle;
    [SerializeField] private TextMeshProUGUI label;
    [SerializeField] private Slider progressBar;
    [SerializeField] private TextMeshProUGUI progressText;
    [SerializeField] private TextMeshProUGUI streakText;
    [SerializeField] private GameObject completionBadge;
    [SerializeField] private int initialAmount;
    [SerializeField] private int targetAmount = 1;
    [SerializeField] private string unit = "";

    private int currentAmount;
    private int streak = 1;

    public PreviewKind Kind => kind;

    public void Configure(
        PreviewKind previewKind,
        string title,
        int initial,
        int target,
        string progressUnit,
        Toggle toggle,
        TextMeshProUGUI titleLabel,
        Slider slider,
        TextMeshProUGUI progress,
        TextMeshProUGUI streakLabel,
        GameObject badge)
    {
        kind = previewKind;
        initialAmount = Mathf.Max(0, initial);
        targetAmount = Mathf.Max(1, target);
        unit = progressUnit ?? string.Empty;
        completeToggle = toggle;
        label = titleLabel;
        progressBar = slider;
        progressText = progress;
        streakText = streakLabel;
        completionBadge = badge;

        if (label != null)
        {
            label.text = title;
            label.enableAutoSizing = true;
            label.fontSizeMin = 20f;
            label.overflowMode = TextOverflowModes.Ellipsis;
        }

        ResetPreview();
    }

    private void Awake()
    {
        if (completeToggle != null)
        {
            completeToggle.onValueChanged.RemoveAllListeners();
            completeToggle.onValueChanged.AddListener(OnToggleChanged);
        }

        Refresh();
    }

    public void ToggleComplete()
    {
        SetCompleted(currentAmount < targetAmount);
    }

    public void AddNumberStep()
    {
        if (kind != PreviewKind.Number)
        {
            return;
        }

        currentAmount = Mathf.Min(targetAmount, currentAmount + 500);
        Refresh();
    }

    public void TogglePhotoSuccess()
    {
        if (kind == PreviewKind.Photo)
        {
            SetCompleted(currentAmount < targetAmount);
        }
    }

    public void IncreaseStreak()
    {
        if (kind == PreviewKind.Weekly)
        {
            streak++;
            Refresh();
        }
    }

    public void ResetPreview()
    {
        currentAmount = initialAmount;
        streak = 1;
        Refresh();
    }

    private void OnToggleChanged(bool isOn)
    {
        SetCompleted(isOn);
    }

    private void SetCompleted(bool completed)
    {
        currentAmount = completed ? targetAmount : initialAmount;
        Refresh();
    }

    private void Refresh()
    {
        bool completed = currentAmount >= targetAmount;

        if (completeToggle != null)
        {
            completeToggle.SetIsOnWithoutNotify(completed);
            completeToggle.interactable = true;
        }

        if (progressBar != null)
        {
            progressBar.minValue = 0f;
            progressBar.maxValue = targetAmount;
            progressBar.SetValueWithoutNotify(Mathf.Clamp(currentAmount, 0, targetAmount));
            progressBar.interactable = false;
        }

        if (progressText != null)
        {
            string suffix = string.IsNullOrEmpty(unit) ? string.Empty : " " + unit;
            progressText.text = $"{currentAmount} / {targetAmount}{suffix}";
        }

        if (streakText != null)
        {
            bool isWeekly = kind == PreviewKind.Weekly;
            streakText.gameObject.SetActive(isWeekly);
            if (isWeekly)
            {
                streakText.text = $"Weekly Streak {streak}";
            }
        }

        if (completionBadge != null)
        {
            completionBadge.SetActive(completed);
        }
    }
}
