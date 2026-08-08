using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OpenRecordPanel : MonoBehaviour
{
    [SerializeField] private GameObject topTab;
    [SerializeField] private GameObject spendPanel;
    [SerializeField] private GameObject recordPanel;

    public void OpenRecord()
    {
        topTab.SetActive(false);

        spendPanel.SetActive(false);
        recordPanel.SetActive(true);
    }

    public void BackToSpend()
    {
        topTab.SetActive(true);

        recordPanel.SetActive(false);
        spendPanel.SetActive(true);
    }
}
