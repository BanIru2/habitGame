using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public static class HabitItemPrefabIntegrationEditor
{
    private const string HabitItemPath = "Assets/Resources/Prefabs/HabitItem.prefab";
    private const string RenewalPath = "Assets/Resources/Prefabs/HabitItem_UIRenewal.prefab";
    private const string CanvasPath = "Assets/Resources/UIPrefabs/Canvas.prefab";
    private const string FontPath = "Assets/Login/Fonts/NotoSansKR Login SDF.asset";
    private const string MenuRoot = "Tools/HabitPVP/UI Renewal/MainScene/";
    private const string ValidateMenu = MenuRoot + "Validate HabitItem Prefab";
    private const string DryRunMenu = MenuRoot + "Dry Run HabitItem Prefab";
    private const string ApplyMenu = MenuRoot + "Apply HabitItem Prefab";

    private static readonly string[] CorePaths =
    {
        "Label", "ProgressBar", "ProgressBar/Background", "ProgressBar/Fill Area",
        "ProgressBar/Fill Area/Fill", "ProgressBar/Handle Slide Area",
        "ProgressBar/Handle Slide Area/Handle", "ProgressText", "Background", "Background/Checkmark"
    };

    [MenuItem(ValidateMenu, false, 2210)]
    private static void ValidateHabitItemPrefab()
    {
        Validation validation = Validate(true);
        if (validation.IsValid)
        {
            Debug.Log("[HabitPVP HabitItem] 검증 성공. Prefab 파일은 변경하지 않았습니다.");
        }
    }

    [MenuItem(DryRunMenu, false, 2211)]
    private static void DryRunHabitItemPrefab()
    {
        Validation validation = Validate(true);
        if (!validation.IsValid)
        {
            Debug.LogError("[HabitPVP HabitItem] Dry Run 중단: 검증 오류를 해결해 주세요.");
            return;
        }

        Debug.Log(
            "[HabitPVP HabitItem] Dry Run (파일 변경 없음)\n" +
            "- 원본 GUID 유지: " + AssetDatabase.AssetPathToGUID(HabitItemPath) + "\n" +
            "- 기존 HabitItem/Toggle/Button/Slider와 모든 기존 SerializeField 참조 및 UnityEvent 보존\n" +
            "- Renewal의 크림색 골드 카드, NotoSansKR 브라운 텍스트, 진행률 바 스타일 적용\n" +
            "- 카드 높이 196px 및 LayoutElement 적용\n" +
            "- 누락된 StreakText/StreakFlame과 비활성 CompletionBadge만 자식으로 추가\n" +
            "- 기존 GameObject 삭제/이동 없음, Scene/Canvas/Renewal Prefab 변경 없음");
    }

    [MenuItem(ApplyMenu, false, 2212)]
    private static void ApplyHabitItemPrefab()
    {
        Validation validation = Validate(true);
        if (!validation.IsValid)
        {
            Debug.LogError("[HabitPVP HabitItem] Apply 중단: 검증 오류를 해결해 주세요.");
            return;
        }

        string guidBefore = AssetDatabase.AssetPathToGUID(HabitItemPath);
        GameObject targetRoot = null;
        GameObject renewalRoot = null;

        try
        {
            targetRoot = PrefabUtility.LoadPrefabContents(HabitItemPath);
            renewalRoot = PrefabUtility.LoadPrefabContents(RenewalPath);
            ProtectedSnapshot snapshot = ProtectedSnapshot.Capture(targetRoot);

            ApplyVisuals(targetRoot, renewalRoot);
            snapshot.AssertPreserved(targetRoot);
            AssertAppliedState(targetRoot);

            PrefabUtility.SaveAsPrefabAsset(targetRoot, HabitItemPath, out bool saved);
            if (!saved)
            {
                throw new InvalidOperationException("Prefab 저장 API가 실패를 반환했습니다.");
            }
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Debug.LogError(
                "[HabitPVP HabitItem] Apply 실패. Save 이전 검증 실패 시 원본은 저장되지 않습니다. " +
                "저장 중 예외가 발생했다면 Version Control diff를 확인해 주세요.");
            return;
        }
        finally
        {
            if (renewalRoot != null) PrefabUtility.UnloadPrefabContents(renewalRoot);
            if (targetRoot != null) PrefabUtility.UnloadPrefabContents(targetRoot);
        }

        AssetDatabase.ImportAsset(HabitItemPath, ImportAssetOptions.ForceUpdate);
        Validation after = Validate(true);
        string guidAfter = AssetDatabase.AssetPathToGUID(HabitItemPath);
        if (!after.IsValid || guidBefore != guidAfter)
        {
            Debug.LogError("[HabitPVP HabitItem] 적용 후 검증 실패. 즉시 Git diff를 확인해 주세요.");
            return;
        }

        Debug.Log("[HabitPVP HabitItem] RPG 디자인 적용 및 적용 후 참조 검증 완료. GUID: " + guidAfter);
    }

    [MenuItem(ApplyMenu, true)]
    private static bool CanApplyHabitItemPrefab()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode;
    }

    private static Validation Validate(bool log)
    {
        Validation result = new Validation();
        GameObject originalAsset = AssetDatabase.LoadAssetAtPath<GameObject>(HabitItemPath);
        GameObject renewalAsset = AssetDatabase.LoadAssetAtPath<GameObject>(RenewalPath);
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        if (originalAsset == null) result.Errors.Add("실제 HabitItem Prefab이 없습니다: " + HabitItemPath);
        if (renewalAsset == null) result.Errors.Add("Renewal Prefab이 없습니다: " + RenewalPath);
        if (font == null) result.Errors.Add("NotoSansKR TMP Font Asset이 없습니다: " + FontPath);
        if (originalAsset == null || renewalAsset == null) return Finish(result, log);

        ValidateRoot(originalAsset, "원본", result);
        ValidateRoot(renewalAsset, "Renewal", result);
        ValidateMainReference(originalAsset, result);

        Image renewalCard = renewalAsset.GetComponent<Image>();
        if (renewalCard == null || renewalCard.sprite == null)
            result.Errors.Add("Renewal 루트 RPG 카드 Image/Sprite가 없습니다.");
        if (renewalAsset.GetComponent<Shadow>() == null)
            result.Errors.Add("Renewal 루트 카드 Shadow가 없습니다.");
        if (originalAsset.transform.Find("CardBackground")?.GetComponent<Image>() == null)
            result.Errors.Add("원본 CardBackground Image가 없습니다.");
        if (renewalAsset.transform.Find("StreakText") == null)
            result.Errors.Add("Renewal의 StreakText가 없습니다.");
        if (renewalAsset.transform.Find("CompletionBadge") == null)
            result.Errors.Add("Renewal의 CompletionBadge가 없습니다.");

        if (AssetDatabase.AssetPathToGUID(HabitItemPath) == AssetDatabase.AssetPathToGUID(RenewalPath))
            result.Errors.Add("원본과 Renewal Prefab GUID가 같아서는 안 됩니다.");

        return Finish(result, log);
    }

    private static void ValidateRoot(GameObject root, string label, Validation result)
    {
        HabitItem item = root.GetComponent<HabitItem>();
        Toggle toggle = root.GetComponent<Toggle>();
        LayoutElement layout = root.GetComponent<LayoutElement>();
        if (item == null) result.Errors.Add(label + " 루트에 HabitItem이 없습니다.");
        if (toggle == null) result.Errors.Add(label + " 루트에 Toggle이 없습니다.");
        if (layout == null) result.Errors.Add(label + " 루트에 LayoutElement가 없습니다.");

        foreach (string path in CorePaths)
        {
            if (root.transform.Find(path) == null) result.Errors.Add(label + " 핵심 경로 누락: " + path);
        }

        if (item == null) return;
        SerializedObject serialized = new SerializedObject(item);
        RequireReference(serialized, "completeToggle", toggle, label, result);
        RequireReference(serialized, "labelButton", root.transform.Find("Label")?.GetComponent<Button>(), label, result);
        RequireReference(serialized, "progressBar", root.transform.Find("ProgressBar")?.GetComponent<Slider>(), label, result);
        RequireReference(serialized, "progressText", root.transform.Find("ProgressText")?.GetComponent<TMP_Text>(), label, result);
        if (serialized.FindProperty("streakText") == null || serialized.FindProperty("streakRewardPopup") == null)
            result.Errors.Add(label + " HabitItem의 최신 Streak SerializeField가 없습니다.");
    }

    private static void ValidateMainReference(GameObject originalAsset, Validation result)
    {
        GameObject canvas = AssetDatabase.LoadAssetAtPath<GameObject>(CanvasPath);
        if (canvas == null)
        {
            result.Errors.Add("MainScene Canvas Prefab이 없습니다: " + CanvasPath);
            return;
        }

        HabitListManager[] managers = canvas.GetComponentsInChildren<HabitListManager>(true);
        if (managers.Length != 1)
        {
            result.Errors.Add("Canvas의 HabitListManager 수가 1이 아닙니다: " + managers.Length);
            return;
        }

        SerializedProperty prefab = new SerializedObject(managers[0]).FindProperty("habitItemPrefab");
        if (prefab == null || prefab.objectReferenceValue != originalAsset)
            result.Errors.Add("MainScene Canvas의 HabitListManager가 실제 HabitItem.prefab을 참조하지 않습니다.");
    }

    private static void RequireReference(SerializedObject owner, string propertyName, UnityEngine.Object expected,
        string label, Validation result)
    {
        SerializedProperty property = owner.FindProperty(propertyName);
        if (property == null || expected == null || property.objectReferenceValue != expected)
            result.Errors.Add(label + " SerializeField 연결 불일치: " + propertyName);
    }

    private static Validation Finish(Validation result, bool log)
    {
        if (log && !result.IsValid)
            Debug.LogError("[HabitPVP HabitItem] 검증 실패\n- " + string.Join("\n- ", result.Errors));
        return result;
    }

    private static void ApplyVisuals(GameObject target, GameObject renewal)
    {
        RectTransform targetRect = target.GetComponent<RectTransform>();
        RectTransform renewalRect = renewal.GetComponent<RectTransform>();
        targetRect.sizeDelta = new Vector2(targetRect.sizeDelta.x, renewalRect.sizeDelta.y);

        LayoutElement targetLayout = target.GetComponent<LayoutElement>();
        LayoutElement renewalLayout = renewal.GetComponent<LayoutElement>();
        targetLayout.minHeight = renewalLayout.minHeight;
        targetLayout.preferredHeight = renewalLayout.preferredHeight;
        targetLayout.flexibleWidth = renewalLayout.flexibleWidth;
        targetLayout.flexibleHeight = renewalLayout.flexibleHeight;

        Transform card = target.transform.Find("CardBackground");
        ApplyImage(card.GetComponent<Image>(), renewal.GetComponent<Image>(), false);
        Shadow sourceShadow = renewal.GetComponent<Shadow>();
        Shadow targetShadow = card.GetComponent<Shadow>() ?? card.gameObject.AddComponent<Shadow>();
        targetShadow.effectColor = sourceShadow.effectColor;
        targetShadow.effectDistance = sourceShadow.effectDistance;
        targetShadow.useGraphicAlpha = sourceShadow.useGraphicAlpha;
        card.SetAsFirstSibling();

        ApplyTextAndRect(target.transform.Find("Label"), renewal.transform.Find("Label"));
        ApplyTextAndRect(target.transform.Find("ProgressText"), renewal.transform.Find("ProgressText"));
        ApplyRect(target.transform.Find("ProgressBar") as RectTransform,
            renewal.transform.Find("ProgressBar") as RectTransform);
        ApplyImageAt(target, renewal, "ProgressBar/Background", false);
        ApplyImageAt(target, renewal, "ProgressBar/Fill Area/Fill", false);
        ApplyImageAt(target, renewal, "ProgressBar/Handle Slide Area/Handle", false);
        ApplyImageAt(target, renewal, "Background", true);
        ApplyImageAt(target, renewal, "Background/Checkmark", false);

        Transform streak = target.transform.Find("StreakText");
        if (streak == null) streak = CloneChild(renewal.transform.Find("StreakText"), target.transform);
        Transform badge = target.transform.Find("CompletionBadge");
        if (badge == null) badge = CloneChild(renewal.transform.Find("CompletionBadge"), target.transform);
        badge.gameObject.SetActive(false);

        foreach (Graphic graphic in new[] { streak, badge }.SelectMany(t => t.GetComponentsInChildren<Graphic>(true)))
            graphic.raycastTarget = false;

        SerializedObject item = new SerializedObject(target.GetComponent<HabitItem>());
        SerializedProperty streakProperty = item.FindProperty("streakText");
        if (streakProperty.objectReferenceValue == null)
        {
            streakProperty.objectReferenceValue = streak.GetComponent<TMP_Text>();
            item.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    private static Transform CloneChild(Transform source, Transform parent)
    {
        if (source == null) throw new InvalidOperationException("Renewal 장식 원본이 없습니다.");
        GameObject clone = UnityEngine.Object.Instantiate(source.gameObject, parent, false);
        clone.name = source.name;
        return clone.transform;
    }

    private static void ApplyImageAt(GameObject target, GameObject renewal, string path, bool preserveRaycast)
    {
        Transform targetTransform = target.transform.Find(path);
        Transform renewalTransform = renewal.transform.Find(path);
        ApplyRect(targetTransform as RectTransform, renewalTransform as RectTransform);
        ApplyImage(targetTransform.GetComponent<Image>(), renewalTransform.GetComponent<Image>(), preserveRaycast);
    }

    private static void ApplyImage(Image target, Image source, bool preserveRaycast)
    {
        bool raycast = target.raycastTarget;
        target.sprite = source.sprite;
        target.type = source.type;
        target.color = source.color;
        target.preserveAspect = source.preserveAspect;
        target.pixelsPerUnitMultiplier = source.pixelsPerUnitMultiplier;
        target.raycastTarget = preserveRaycast ? raycast : false;
    }

    private static void ApplyTextAndRect(Transform target, Transform source)
    {
        ApplyRect(target as RectTransform, source as RectTransform);
        TMP_Text targetText = target.GetComponent<TMP_Text>();
        TMP_Text sourceText = source.GetComponent<TMP_Text>();
        targetText.font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        targetText.color = sourceText.color;
        targetText.fontSize = sourceText.fontSize;
        targetText.enableAutoSizing = sourceText.enableAutoSizing;
        targetText.fontSizeMin = sourceText.fontSizeMin;
        targetText.fontSizeMax = sourceText.fontSizeMax;
        targetText.fontStyle = sourceText.fontStyle;
        targetText.alignment = sourceText.alignment;
        targetText.overflowMode = sourceText.overflowMode;
        targetText.raycastTarget = sourceText.raycastTarget;
    }

    private static void ApplyRect(RectTransform target, RectTransform source)
    {
        if (target == null || source == null) throw new InvalidOperationException("RectTransform 비교 대상이 없습니다.");
        target.anchorMin = source.anchorMin;
        target.anchorMax = source.anchorMax;
        target.anchoredPosition = source.anchoredPosition;
        target.sizeDelta = source.sizeDelta;
        target.pivot = source.pivot;
        target.localRotation = source.localRotation;
        target.localScale = source.localScale;
    }

    private static void AssertAppliedState(GameObject root)
    {
        HabitItem item = root.GetComponent<HabitItem>();
        SerializedObject serialized = new SerializedObject(item);
        if (serialized.FindProperty("streakText").objectReferenceValue == null)
            throw new InvalidOperationException("적용 후 streakText 연결이 없습니다.");
        if (root.transform.Find("CardBackground")?.GetComponent<Image>()?.sprite == null)
            throw new InvalidOperationException("적용 후 RPG 카드 Sprite가 없습니다.");
        foreach (Transform decorative in new[] { root.transform.Find("StreakText"), root.transform.Find("CompletionBadge") })
        foreach (Graphic graphic in decorative.GetComponentsInChildren<Graphic>(true))
            if (graphic.raycastTarget) throw new InvalidOperationException("장식 Graphic이 입력을 차단합니다: " + graphic.name);
    }

    private sealed class ProtectedSnapshot
    {
        private readonly Dictionary<string, UnityEngine.Object> references;
        private readonly EventSnapshot toggleEvent;
        private readonly EventSnapshot buttonEvent;
        private readonly Toggle toggle;
        private readonly Button button;
        private readonly Slider slider;
        private readonly Component[] originalComponents;

        private ProtectedSnapshot(GameObject root)
        {
            HabitItem item = root.GetComponent<HabitItem>();
            SerializedObject serialized = new SerializedObject(item);
            references = new Dictionary<string, UnityEngine.Object>();
            foreach (string name in new[] { "completeToggle", "labelButton", "progressBar", "progressText", "streakRewardPopup" })
                references[name] = serialized.FindProperty(name).objectReferenceValue;
            toggle = root.GetComponent<Toggle>();
            button = root.transform.Find("Label").GetComponent<Button>();
            slider = root.transform.Find("ProgressBar").GetComponent<Slider>();
            originalComponents = root.GetComponentsInChildren<Component>(true);
            toggleEvent = EventSnapshot.Capture(toggle.onValueChanged);
            buttonEvent = EventSnapshot.Capture(button.onClick);
        }

        public static ProtectedSnapshot Capture(GameObject root) => new ProtectedSnapshot(root);

        public void AssertPreserved(GameObject root)
        {
            if (root.GetComponent<Toggle>() != toggle || root.transform.Find("Label").GetComponent<Button>() != button ||
                root.transform.Find("ProgressBar").GetComponent<Slider>() != slider)
                throw new InvalidOperationException("핵심 Toggle/Button/Slider 컴포넌트가 교체되었습니다.");
            Component[] afterComponents = root.GetComponentsInChildren<Component>(true);
            if (originalComponents.Any(component => component == null || !afterComponents.Contains(component)))
                throw new InvalidOperationException("기존 GameObject 또는 컴포넌트가 삭제/이동/교체되었습니다.");

            SerializedObject serialized = new SerializedObject(root.GetComponent<HabitItem>());
            foreach (KeyValuePair<string, UnityEngine.Object> pair in references)
                if (serialized.FindProperty(pair.Key).objectReferenceValue != pair.Value)
                    throw new InvalidOperationException("기존 SerializeField 참조가 변경되었습니다: " + pair.Key);
            toggleEvent.AssertUnchanged(toggle.onValueChanged, "Toggle.onValueChanged");
            buttonEvent.AssertUnchanged(button.onClick, "Label.Button.onClick");
        }
    }

    private sealed class EventSnapshot
    {
        private readonly string[] calls;
        private EventSnapshot(string[] calls) { this.calls = calls; }

        public static EventSnapshot Capture(UnityEventBase unityEvent)
        {
            string[] values = Enumerable.Range(0, unityEvent.GetPersistentEventCount())
                .Select(i => (unityEvent.GetPersistentTarget(i) == null ? "null" : unityEvent.GetPersistentTarget(i).GetInstanceID().ToString()) +
                             "|" + unityEvent.GetPersistentMethodName(i)).ToArray();
            return new EventSnapshot(values);
        }

        public void AssertUnchanged(UnityEventBase unityEvent, string label)
        {
            string[] after = Capture(unityEvent).calls;
            if (!calls.SequenceEqual(after)) throw new InvalidOperationException(label + " persistent call이 변경되었습니다.");
        }
    }

    private sealed class Validation
    {
        public readonly List<string> Errors = new List<string>();
        public bool IsValid => Errors.Count == 0;
    }
}
