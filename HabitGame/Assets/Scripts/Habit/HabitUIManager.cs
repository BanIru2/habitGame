using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HabitUIManager : MonoBehaviour
{
    [SerializeField] private GameObject topTab;

    [SerializeField] private GameObject lifePanel;
    [SerializeField] private GameObject spendPanel;

    [SerializeField] private GameObject addHabitPanel;
    [SerializeField] private GameObject addSpendPanel;

    private void Start()
    {
        OpenLife();
    }

    // 생활습관 화면
    public void OpenLife()
    {
        topTab.SetActive(true);

        lifePanel.SetActive(true);
        spendPanel.SetActive(false);

        addHabitPanel.SetActive(false);
        addSpendPanel.SetActive(false);
    }

    // 소비습관 화면
    public void OpenSpend()
    {
        topTab.SetActive(true);

        lifePanel.SetActive(false);
        spendPanel.SetActive(true);

        addHabitPanel.SetActive(false);
        addSpendPanel.SetActive(false);
    }

    // 생활습관 추가
    public void OpenAddHabit()
    {
        topTab.SetActive(false);

        lifePanel.SetActive(false);
        spendPanel.SetActive(false);

        addHabitPanel.SetActive(true);
    }

    // 소비습관 추가
    public void OpenAddSpend()
    {
        topTab.SetActive(false);

        lifePanel.SetActive(false);
        spendPanel.SetActive(false);

        addSpendPanel.SetActive(true);
    }

    // 뒤로가기
    public void BackToLife()
    {
        OpenLife();
    }

    // 뒤로가기
    public void BackToSpend()
    {
        OpenSpend();
    }
}
