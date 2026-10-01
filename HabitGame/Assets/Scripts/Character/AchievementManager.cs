using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class AchievementManager : MonoBehaviour
{
    [SerializeField]
    private Button achievementButton;
    [SerializeField]
    private GameObject achievementPopup;
    [SerializeField]
    private Transform slotParent;
    // 업적 항목 프리팹


    private void Awake()
    {
        achievementButton.onClick.AddListener(OnClickAchievementButton);
    }

    private void OnClickAchievementButton()
    {
        achievementPopup.SetActive(true);
    }

    public void ClosePopup()
    {
        achievementPopup.SetActive(false);
    }

}