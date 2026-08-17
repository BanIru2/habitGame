using UnityEngine;

public class HabitDetailManager : MonoBehaviour
{
    [SerializeField] private GameObject detailPanel;
    [SerializeField] private GameObject lifePanel;
    [SerializeField] private GameObject topTab;

    public void OpenDetail()
    {
        detailPanel.SetActive(true);
        lifePanel.SetActive(false);
        topTab.SetActive(false);
    }

    public void CloseDetail()
    {
        detailPanel.SetActive(false);
        lifePanel.SetActive(true);
        topTab.SetActive(true);
    }
}