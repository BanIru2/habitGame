using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SpendListManager : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private TMP_InputField goalNameInput;
    [SerializeField] private TMP_InputField goldInput;

    [Header("UI")]
    [SerializeField] private Transform content;
    [SerializeField] private GameObject spendItemPrefab;

    [Header("Reward")]
    [SerializeField] private int reward = 300;

    public void AddSpendGoal()
    {
        // 입력값 확인
        if (string.IsNullOrWhiteSpace(goalNameInput.text))
            return;

        if (string.IsNullOrWhiteSpace(goldInput.text))
            return;

        // Prefab 생성
        GameObject newItem = Instantiate(spendItemPrefab, content);

        // 제목
        newItem.transform.Find("Title")
            .GetComponent<TextMeshProUGUI>().text = goalNameInput.text;

        // 설명
        newItem.transform.Find("Description")
            .GetComponent<TextMeshProUGUI>().text =
            goldInput.text + "원 이하 사용";

        // 보상
        newItem.transform.Find("Reward")
            .GetComponent<TextMeshProUGUI>().text =
            "+" + reward;

        // Toggle 초기화
        Toggle toggle = newItem.GetComponent<Toggle>();
        if (toggle != null)
            toggle.isOn = false;

        // 입력창 비우기
        goalNameInput.text = "";
        goldInput.text = "";
    }
}
