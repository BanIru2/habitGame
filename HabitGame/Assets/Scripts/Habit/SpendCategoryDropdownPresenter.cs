using System.Collections.Generic;
using TMPro;
using UnityEngine;

[RequireComponent(typeof(TMP_Dropdown))]
public class SpendCategoryDropdownPresenter : MonoBehaviour
{
    private static readonly IReadOnlyDictionary<string, string> DisplayLabels =
        new Dictionary<string, string>
        {
            { "Select Category", "카테고리를 선택하세요" },
            { "Food", "식비" },
            { "Cafe", "카페" },
            { "Transport", "교통" },
            { "Shopping", "쇼핑" },
            { "Living", "생활" },
            { "Education", "교육" },
            { "Other", "기타" }
        };

    private TMP_Dropdown dropdown;

    private void Awake()
    {
        dropdown = GetComponent<TMP_Dropdown>();
    }

    private void OnEnable()
    {
        if (dropdown == null)
            dropdown = GetComponent<TMP_Dropdown>();

        dropdown.onValueChanged.AddListener(UpdateCaption);
        UpdateCaption(dropdown.value);
    }

    private void OnDisable()
    {
        if (dropdown != null)
            dropdown.onValueChanged.RemoveListener(UpdateCaption);
    }

    private void LateUpdate()
    {
        Transform dropdownList = transform.Find("Dropdown List");
        if (dropdownList == null && transform.parent != null)
            dropdownList = transform.parent.Find("Dropdown List");
        if (dropdownList == null)
            return;

        foreach (TMP_Text label in dropdownList.GetComponentsInChildren<TMP_Text>(true))
            label.text = Localize(label.text);
    }

    private void UpdateCaption(int selectedIndex)
    {
        if (dropdown == null || dropdown.captionText == null ||
            selectedIndex < 0 || selectedIndex >= dropdown.options.Count)
            return;

        dropdown.captionText.text = Localize(dropdown.options[selectedIndex].text);
    }

    private static string Localize(string value)
    {
        return !string.IsNullOrEmpty(value) && DisplayLabels.TryGetValue(value, out string label)
            ? label
            : value;
    }
}
