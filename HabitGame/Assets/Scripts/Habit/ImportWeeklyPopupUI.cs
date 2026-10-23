
using UnityEngine;

public class ImportWeeklyPopupUI : MonoBehaviour
{
    [SerializeField] private GameObject importWeeklyPopup;

    // 지난주 목표 가져오기 팝업 열기
    public void OpenPopup()
    {
        if (importWeeklyPopup != null)
        {
            importWeeklyPopup.SetActive(true);
        }
    }

    // 팝업 닫기
    public void ClosePopup()
    {
        if (importWeeklyPopup != null)
        {
            importWeeklyPopup.SetActive(false);
        }
    }
}

