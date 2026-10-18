using UnityEngine;
using UnityEngine.UI;

public sealed class HabitItemPreviewController : MonoBehaviour
{
    [SerializeField] private HabitItemPreview[] previews;
    [SerializeField] private Button checkButton;
    [SerializeField] private Button numberAddButton;
    [SerializeField] private Button numberResetButton;
    [SerializeField] private Button photoButton;
    [SerializeField] private Button weeklyButton;
    [SerializeField] private Button resetAllButton;

    public void Configure(
        HabitItemPreview[] previewItems,
        Button check,
        Button numberAdd,
        Button numberReset,
        Button photo,
        Button weekly,
        Button resetAll)
    {
        previews = previewItems;
        checkButton = check;
        numberAddButton = numberAdd;
        numberResetButton = numberReset;
        photoButton = photo;
        weeklyButton = weekly;
        resetAllButton = resetAll;
    }

    private void Awake()
    {
        Bind(checkButton, ToggleCheck);
        Bind(numberAddButton, AddNumber);
        Bind(numberResetButton, ResetNumber);
        Bind(photoButton, TogglePhoto);
        Bind(weeklyButton, IncreaseWeeklyStreak);
        Bind(resetAllButton, ResetAll);
    }

    public void ToggleCheck() => Find(HabitItemPreview.PreviewKind.Check)?.ToggleComplete();
    public void AddNumber() => Find(HabitItemPreview.PreviewKind.Number)?.AddNumberStep();
    public void ResetNumber() => Find(HabitItemPreview.PreviewKind.Number)?.ResetPreview();
    public void TogglePhoto() => Find(HabitItemPreview.PreviewKind.Photo)?.TogglePhotoSuccess();
    public void IncreaseWeeklyStreak() => Find(HabitItemPreview.PreviewKind.Weekly)?.IncreaseStreak();

    public void ResetAll()
    {
        if (previews == null)
        {
            return;
        }

        foreach (HabitItemPreview preview in previews)
        {
            preview?.ResetPreview();
        }
    }

    private HabitItemPreview Find(HabitItemPreview.PreviewKind kind)
    {
        if (previews == null)
        {
            return null;
        }

        foreach (HabitItemPreview preview in previews)
        {
            if (preview != null && preview.Kind == kind)
            {
                return preview;
            }
        }

        return null;
    }

    private static void Bind(Button button, UnityEngine.Events.UnityAction action)
    {
        if (button == null)
        {
            return;
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(action);
    }
}
