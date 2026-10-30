using UnityEngine;

[DisallowMultipleComponent]
public class SpendGoalPreviewEmptyState : MonoBehaviour
{
    [SerializeField] private Transform previewContent;
    [SerializeField] private GameObject emptyState;
    [SerializeField] private GameObject previewTemplate;

    public void Bind(Transform content, GameObject state, GameObject template)
    {
        previewContent = content;
        emptyState = state;
        previewTemplate = template;
        Refresh();
    }

    private void OnEnable()
    {
        Refresh();
    }

    private void LateUpdate()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (previewContent == null || emptyState == null)
            return;

        bool shouldShow = true;
        for (int index = 0; index < previewContent.childCount; index++)
        {
            GameObject child = previewContent.GetChild(index).gameObject;
            if (child != emptyState && child != previewTemplate && child.activeSelf)
            {
                shouldShow = false;
                break;
            }
        }
        if (emptyState.activeSelf != shouldShow)
            emptyState.SetActive(shouldShow);
    }
}
