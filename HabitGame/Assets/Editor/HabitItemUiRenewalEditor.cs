using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class HabitItemUiRenewalEditor
{
    private const string ScenePath = "Assets/Scenes/UITestScene.unity";
    private const string CanvasPrefabPath = "Assets/Resources/UIPrefabs/Canvas.prefab";
    private const string SourcePrefabPath = "Assets/Resources/Prefabs/HabitItem.prefab";
    private const string RenewalPrefabPath = "Assets/Resources/Prefabs/HabitItem_UIRenewal.prefab";
    private const string GeneratedRoot = "Assets/UIRenewal/Generated/FreeCasualGUI/HabitItem/";
    private const string CardSpritePath = GeneratedRoot + "panel_rpg_card.png";
    private const string ToggleSpritePath = GeneratedRoot + "Button_square_rpg.png";
    private const string CheckSpritePath = GeneratedRoot + "icon_check_brown.png";
    private const string BadgeSpritePath = GeneratedRoot + "badge_complete.png";
    private const string FlameSpritePath = GeneratedRoot + "icon_streak_flame.png";
    private const string BarSpritePath =
        "Assets/Skyden_Games/Free_Casual_GUI/Demo/Sprites/Bars/Bar.png";
    private const string FillSpritePath =
        "Assets/Skyden_Games/Free_Casual_GUI/Demo/Sprites/Bars/Bar_Fill_02.png";
    private const string ValidateMenu = "Tools/HabitPVP/UI Renewal/Validate HabitItem";
    private const string ApplyMenu = "Tools/HabitPVP/UI Renewal/Apply HabitItem";
    private const string RemoveLegacyMenu =
        "Tools/HabitPVP/UI Renewal/Remove Legacy HabitItem From UITestScene";
    private const float CardHeight = 196f;

    private static readonly Color32 Brown = new Color32(0x5B, 0x36, 0x39, 0xFF);
    private static readonly Color32 MutedBrown = new Color32(0x89, 0x68, 0x6A, 0xFF);
    private static readonly Color32 Blue = new Color32(0x4B, 0x8D, 0xE3, 0xFF);
    private static readonly Color32 Green = new Color32(0x6F, 0xAF, 0x78, 0xFF);
    private static readonly Color32 Orange = new Color32(0xD9, 0x82, 0x36, 0xFF);

    [MenuItem(ValidateMenu, false, 2020)]
    private static void ValidateHabitItem()
    {
        List<string> messages = new List<string>();
        HabitItemContext context = ValidateProject(messages, false);
        if (context == null)
        {
            return;
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(RenewalPrefabPath) == null)
        {
            messages.Add("테스트 Prefab은 아직 생성되지 않았습니다. Apply HabitItem 실행 시 생성됩니다.");
        }
        else
        {
            ValidateRenewalPrefab(messages);
        }

        LogReport("Validate HabitItem 성공", context, messages);
    }

    [MenuItem(ApplyMenu, false, 2021)]
    private static void ApplyHabitItem()
    {
        List<string> messages = new List<string>();
        HabitItemContext context = ValidateProject(messages, true);
        if (context == null)
        {
            return;
        }

        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply HabitItem UI Renewal");
        UnityEngine.Object previousPrefab = context.PrefabProperty.objectReferenceValue;
        try
        {
            EnsureGeneratedSprites();
            CreateOrRefreshRenewalPrefab();

            GameObject renewalPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RenewalPrefabPath);
            if (renewalPrefab == null)
            {
                throw new InvalidOperationException("생성된 HabitItem 테스트 Prefab을 불러올 수 없습니다.");
            }

            ValidateRenewalPrefab(messages);
            Undo.RecordObject(context.Manager, "Change HabitItem test prefab");
            context.PrefabProperty.objectReferenceValue = renewalPrefab;
            context.ManagerObject.ApplyModifiedProperties();
            PrefabUtility.RecordPrefabInstancePropertyModifications(context.Manager);
            EditorUtility.SetDirty(context.Manager);
            EditorSceneManager.MarkSceneDirty(context.Scene);

            string afterPath = AssetDatabase.GetAssetPath(
                context.ManagerObject.FindProperty("habitItemPrefab").objectReferenceValue);
            messages.Add($"Prefab 참조 변경: {DisplayPath(context.BeforePrefabPath)} -> {afterPath}");
            messages.Add("HabitListManager 변경은 UITestScene의 Canvas Prefab 인스턴스 Override로 기록되었습니다.");

            if (!EditorSceneManager.SaveScene(context.Scene))
            {
                throw new InvalidOperationException("UITestScene 저장에 실패했습니다.");
            }

            Undo.CollapseUndoOperations(undoGroup);
            AssetDatabase.SaveAssets();
            LogReport("Apply HabitItem 성공", context, messages);
        }
        catch (Exception exception)
        {
            context.ManagerObject.Update();
            SerializedProperty property = context.ManagerObject.FindProperty("habitItemPrefab");
            property.objectReferenceValue = previousPrefab;
            context.ManagerObject.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.RecordPrefabInstancePropertyModifications(context.Manager);
            Undo.RevertAllDownToGroup(undoGroup);
            Debug.LogError("[HabitPVP UI Renewal] Apply HabitItem 실패. Scene Prefab 참조를 롤백했습니다.\n" + exception);
        }
    }

    [MenuItem(ApplyMenu, true)]
    private static bool CanApplyHabitItem()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode &&
               SceneManager.GetActiveScene().path == ScenePath;
    }

    [MenuItem(RemoveLegacyMenu, false, 2022)]
    private static void RemoveLegacyHabitItemFromUiTestScene()
    {
        List<string> messages = new List<string>();
        HabitItemContext context = ValidateProject(messages, true);
        if (context == null)
        {
            return;
        }

        string canvasPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(context.Manager.gameObject);
        if (!string.Equals(canvasPath, CanvasPrefabPath, StringComparison.OrdinalIgnoreCase))
        {
            Debug.LogError($"[HabitPVP UI Renewal] HabitListManager가 예상한 Canvas Prefab 인스턴스에 속하지 않습니다.\n" +
                           $"Expected: {CanvasPrefabPath}\nActual: {DisplayPath(canvasPath)}");
            return;
        }

        GameObject sourcePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath);
        if (sourcePrefab == null)
        {
            Debug.LogError($"[HabitPVP UI Renewal] 원본 Prefab이 없습니다: {SourcePrefabPath}");
            return;
        }

        List<GameObject> before = FindDirectPrefabInstances(context.Content, sourcePrefab);
        Debug.Log($"[HabitPVP UI Renewal] Legacy HabitItem 제거 전\n" +
                  $"Scene: {context.Scene.path}\n" +
                  $"Content: {DescribeHierarchy(context.Content)}\n" +
                  $"Matched instances: {before.Count}" + DescribeMatchedInstances(before));

        if (before.Count == 0)
        {
            Debug.Log("[HabitPVP UI Renewal] 기존 HabitItem.prefab 인스턴스가 이미 제거되어 있습니다. 변경하지 않습니다.\n" +
                      "Legacy HabitItem 제거 후: 0");
            return;
        }

        if (before.Count > 1)
        {
            Debug.LogError($"[HabitPVP UI Renewal] 직접 자식에서 기존 HabitItem.prefab 인스턴스를 {before.Count}개 발견했습니다. " +
                           "예상하지 않은 객체 삭제를 막기 위해 작업을 중단합니다." + DescribeMatchedInstances(before));
            return;
        }

        GameObject legacyInstance = before[0];
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Remove Legacy HabitItem From UITestScene");
        Undo.DestroyObjectImmediate(legacyInstance);
        EditorSceneManager.MarkSceneDirty(context.Scene);
        Undo.CollapseUndoOperations(undoGroup);

        List<GameObject> after = FindDirectPrefabInstances(context.Content, sourcePrefab);
        if (after.Count != 0)
        {
            Debug.LogError("[HabitPVP UI Renewal] Legacy HabitItem 제거 Override 기록을 확인하지 못했습니다. " +
                           "Scene을 저장하지 않았습니다." + DescribeMatchedInstances(after));
            return;
        }

        if (!EditorSceneManager.SaveScene(context.Scene))
        {
            Debug.LogError("[HabitPVP UI Renewal] Legacy HabitItem 제거 후 UITestScene 저장에 실패했습니다.");
            return;
        }

        Debug.Log($"[HabitPVP UI Renewal] Legacy HabitItem 제거 후\n" +
                  $"Scene: {context.Scene.path}\n" +
                  $"Matched instances: {after.Count}\n" +
                  "Canvas.prefab 원본에는 Apply하지 않았으며, UITestScene의 Prefab Instance Override로 기록했습니다.");
    }

    [MenuItem(RemoveLegacyMenu, true)]
    private static bool CanRemoveLegacyHabitItemFromUiTestScene()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode &&
               SceneManager.GetActiveScene().path == ScenePath;
    }

    private static List<GameObject> FindDirectPrefabInstances(Transform content, GameObject sourcePrefab)
    {
        List<GameObject> matches = new List<GameObject>();
        for (int index = 0; index < content.childCount; index++)
        {
            Transform child = content.GetChild(index);
            GameObject childObject = child.gameObject;
            if (EditorUtility.IsPersistent(childObject) || !PrefabUtility.IsPartOfPrefabInstance(childObject))
            {
                continue;
            }

            // Canvas.prefab 안에 중첩된 HabitItem은 Scene에서 가장 가까운 Prefab 경로가
            // Canvas.prefab으로 보고된다. 원본 계보까지 따라가야 실제 HabitItem 인스턴스를
            // 식별할 수 있으며, 이렇게 찾은 직접 자식을 삭제하면 Scene removal override만 남는다.
            GameObject originalSource = PrefabUtility.GetCorrespondingObjectFromOriginalSource(childObject);
            if (originalSource == sourcePrefab)
            {
                matches.Add(childObject);
            }
        }
        return matches;
    }

    private static string DescribeMatchedInstances(IEnumerable<GameObject> matches)
    {
        string[] paths = matches.Select(item => DescribeHierarchy(item.transform)).ToArray();
        return paths.Length == 0 ? string.Empty : "\n- " + string.Join("\n- ", paths);
    }

    private static HabitItemContext ValidateProject(List<string> messages, bool requirePrefabInstance)
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
        {
            Debug.LogError($"[HabitPVP UI Renewal] {ScenePath}을 활성 Scene으로 연 뒤 실행하세요.");
            return null;
        }

        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefabPath);
        if (source == null)
        {
            Debug.LogError($"[HabitPVP UI Renewal] 원본 Prefab이 없습니다: {SourcePrefabPath}");
            return null;
        }

        HabitListManager[] managers = UnityEngine.Object.FindObjectsOfType<HabitListManager>(true)
            .Where(manager => manager.gameObject.scene == scene).ToArray();
        if (managers.Length != 1)
        {
            Debug.LogError($"[HabitPVP UI Renewal] UITestScene의 HabitListManager가 1개여야 합니다. 현재: {managers.Length}");
            return null;
        }

        HabitListManager manager = managers[0];
        if (requirePrefabInstance && !PrefabUtility.IsPartOfPrefabInstance(manager))
        {
            Debug.LogError("[HabitPVP UI Renewal] HabitListManager가 Prefab 인스턴스가 아니므로 Scene Override만 기록할 수 없습니다.");
            return null;
        }

        SerializedObject managerObject = new SerializedObject(manager);
        SerializedProperty prefabProperty = managerObject.FindProperty("habitItemPrefab");
        SerializedProperty contentProperty = managerObject.FindProperty("content");
        if (prefabProperty == null || contentProperty == null)
        {
            Debug.LogError("[HabitPVP UI Renewal] HabitListManager SerializeField를 찾을 수 없습니다.");
            return null;
        }

        Transform content = contentProperty.objectReferenceValue as Transform;
        if (content == null)
        {
            Debug.LogError("[HabitPVP UI Renewal] HabitListManager.content가 연결되지 않았습니다.");
            return null;
        }

        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();
        if (layout == null || fitter == null)
        {
            Debug.LogError("[HabitPVP UI Renewal] Content에 VerticalLayoutGroup과 ContentSizeFitter가 모두 필요합니다.");
            return null;
        }

        string beforePath = AssetDatabase.GetAssetPath(prefabProperty.objectReferenceValue);
        messages.Add($"Hierarchy: {DescribeHierarchy(source.transform)}");
        messages.Add($"Content layout: spacing={layout.spacing}, childControlHeight={layout.childControlHeight}, " +
                     $"childForceExpandHeight={layout.childForceExpandHeight}, fitter.verticalFit={fitter.verticalFit}");
        messages.Add($"현재 HabitListManager Prefab: {DisplayPath(beforePath)}");
        return new HabitItemContext(scene, manager, content, managerObject, prefabProperty, beforePath);
    }

    private static void CreateOrRefreshRenewalPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(SourcePrefabPath);
        try
        {
            FunctionalSnapshot before = FunctionalSnapshot.Capture(root);
            ApplyStyle(root);
            before.AssertPreserved(root);

            GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, RenewalPrefabPath);
            if (saved == null)
            {
                throw new InvalidOperationException("HabitItem_UIRenewal.prefab 저장에 실패했습니다.");
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void ApplyStyle(GameObject root)
    {
        Sprite cardSprite = LoadSprite(CardSpritePath);
        Sprite toggleSprite = LoadSprite(ToggleSpritePath);
        Sprite checkSprite = LoadSprite(CheckSpritePath);
        Sprite badgeSprite = LoadSprite(BadgeSpritePath);
        Sprite flameSprite = LoadSprite(FlameSpritePath);
        Sprite barSprite = LoadSprite(BarSpritePath);
        Sprite fillSprite = LoadSprite(FillSpritePath);

        RectTransform rootRect = Require<RectTransform>(root.transform, "HabitItem");
        rootRect.sizeDelta = new Vector2(rootRect.sizeDelta.x, CardHeight);

        Image card = root.GetComponent<Image>();
        if (card == null)
        {
            card = root.AddComponent<Image>();
        }
        card.sprite = cardSprite;
        card.type = Image.Type.Sliced;
        card.color = Color.white;
        card.raycastTarget = false;

        Shadow shadow = root.GetComponent<Shadow>();
        if (shadow == null)
        {
            shadow = root.AddComponent<Shadow>();
        }
        shadow.effectColor = new Color32(0x55, 0x32, 0x27, 0x54);
        shadow.effectDistance = new Vector2(0f, -7f);
        shadow.useGraphicAlpha = true;

        LayoutElement layout = root.GetComponent<LayoutElement>();
        if (layout == null)
        {
            layout = root.AddComponent<LayoutElement>();
        }
        layout.minHeight = CardHeight;
        layout.preferredHeight = CardHeight;
        layout.flexibleHeight = 0f;
        layout.flexibleWidth = 1f;

        Transform toggleBackground = RequireChild(root.transform, "Background");
        RectTransform toggleRect = Require<RectTransform>(toggleBackground, "Background");
        SetRect(toggleRect, Vector2.up, Vector2.up, new Vector2(66f, -66f), new Vector2(80f, 80f), new Vector2(0.5f, 0.5f));
        Image toggleImage = Require<Image>(toggleBackground, "Background");
        toggleImage.sprite = toggleSprite;
        toggleImage.type = Image.Type.Sliced;
        toggleImage.color = Color.white;

        Transform checkmark = RequireChild(toggleBackground, "Checkmark");
        Image checkImage = Require<Image>(checkmark, "Checkmark");
        checkImage.sprite = checkSprite;
        checkImage.type = Image.Type.Simple;
        checkImage.preserveAspect = true;
        checkImage.color = Green;
        Require<RectTransform>(checkmark, "Checkmark").offsetMin = new Vector2(14f, 14f);
        Require<RectTransform>(checkmark, "Checkmark").offsetMax = new Vector2(-14f, -14f);

        Transform labelTransform = RequireChild(root.transform, "Label");
        RectTransform labelRect = Require<RectTransform>(labelTransform, "Label");
        SetOffsets(labelRect, new Vector2(0f, 1f), Vector2.one,
            new Vector2(154f, -110f), new Vector2(-240f, -18f), new Vector2(0f, 1f));
        TextMeshProUGUI label = Require<TextMeshProUGUI>(labelTransform, "Label");
        label.color = Brown;
        label.fontStyle = FontStyles.Bold;
        label.alignment = TextAlignmentOptions.TopLeft;
        label.enableAutoSizing = true;
        label.fontSizeMin = 28f;
        label.fontSizeMax = 40f;
        label.enableWordWrapping = true;
        label.overflowMode = TextOverflowModes.Ellipsis;
        label.maxVisibleLines = 2;
        label.margin = new Vector4(4f, 2f, 8f, 2f);

        Transform progressTransform = RequireChild(root.transform, "ProgressBar");
        RectTransform progressRect = Require<RectTransform>(progressTransform, "ProgressBar");
        SetRect(progressRect, new Vector2(0.15f, 0f), new Vector2(0.72f, 0f),
            new Vector2(0f, 61f), new Vector2(0f, 42f), new Vector2(0.5f, 0f));
        Slider slider = Require<Slider>(progressTransform, "ProgressBar");
        Image progressBackground = Require<Image>(RequireChild(progressTransform, "Background"), "ProgressBar/Background");
        progressBackground.sprite = barSprite;
        progressBackground.type = Image.Type.Sliced;
        progressBackground.color = Color.white;
        Image progressFill = Require<Image>(RequireChild(RequireChild(progressTransform, "Fill Area"), "Fill"), "ProgressBar/Fill Area/Fill");
        progressFill.sprite = fillSprite;
        progressFill.type = Image.Type.Sliced;
        progressFill.color = Blue;
        slider.interactable = false;

        Transform progressTextTransform = RequireChild(root.transform, "ProgressText");
        RectTransform progressTextRect = Require<RectTransform>(progressTextTransform, "ProgressText");
        SetRect(progressTextRect, new Vector2(1f, 0f), new Vector2(1f, 0f),
            new Vector2(-30f, 49f), new Vector2(240f, 54f), new Vector2(1f, 0f));
        TextMeshProUGUI progressText = Require<TextMeshProUGUI>(progressTextTransform, "ProgressText");
        progressText.color = Brown;
        progressText.fontStyle = FontStyles.Bold;
        progressText.fontSize = 30f;
        progressText.enableAutoSizing = true;
        progressText.fontSizeMin = 22f;
        progressText.fontSizeMax = 30f;
        progressText.alignment = TextAlignmentOptions.MidlineRight;
        progressText.enableWordWrapping = false;
        progressText.overflowMode = TextOverflowModes.Ellipsis;
        progressText.margin = new Vector4(4f, 0f, 4f, 0f);

        Transform streakTransform = RequireChild(root.transform, "StreakText");
        RectTransform streakRect = Require<RectTransform>(streakTransform, "StreakText");
        SetRect(streakRect, new Vector2(0f, 0f), new Vector2(0f, 0f),
            new Vector2(202f, 9f), new Vector2(360f, 38f), Vector2.zero);
        TextMeshProUGUI streakText = Require<TextMeshProUGUI>(streakTransform, "StreakText");
        streakText.color = Orange;
        streakText.fontStyle = FontStyles.Bold;
        streakText.fontSize = 24f;
        streakText.enableAutoSizing = true;
        streakText.fontSizeMin = 19f;
        streakText.fontSizeMax = 25f;
        streakText.alignment = TextAlignmentOptions.MidlineLeft;

        GameObject flame = CreateImageChild(streakTransform, "StreakFlame", flameSprite);
        SetRect(flame.GetComponent<RectTransform>(), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
            new Vector2(-24f, 0f), new Vector2(32f, 38f), new Vector2(0.5f, 0.5f));

        GameObject badge = CreateImageChild(root.transform, "CompletionBadge", badgeSprite);
        SetRect(badge.GetComponent<RectTransform>(), Vector2.one, Vector2.one,
            new Vector2(-30f, -24f), new Vector2(172f, 50f), Vector2.one);
        GameObject badgeLabelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
        badgeLabelObject.transform.SetParent(badge.transform, false);
        RectTransform badgeLabelRect = badgeLabelObject.GetComponent<RectTransform>();
        badgeLabelRect.anchorMin = Vector2.zero;
        badgeLabelRect.anchorMax = Vector2.one;
        badgeLabelRect.offsetMin = new Vector2(16f, 4f);
        badgeLabelRect.offsetMax = new Vector2(-16f, -4f);
        TextMeshProUGUI badgeLabel = badgeLabelObject.GetComponent<TextMeshProUGUI>();
        badgeLabel.text = "CLEAR";
        badgeLabel.color = Color.white;
        badgeLabel.fontStyle = FontStyles.Bold;
        badgeLabel.fontSize = 25f;
        badgeLabel.alignment = TextAlignmentOptions.Center;
        badgeLabel.raycastTarget = false;
        badge.SetActive(false);
    }

    private static GameObject CreateImageChild(Transform parent, string name, Sprite sprite)
    {
        Transform existing = parent.Find(name);
        GameObject child = existing == null
            ? new GameObject(name, typeof(RectTransform), typeof(Image))
            : existing.gameObject;
        child.transform.SetParent(parent, false);
        Image image = child.GetComponent<Image>();
        image.sprite = sprite;
        image.type = sprite.border.sqrMagnitude > 0f ? Image.Type.Sliced : Image.Type.Simple;
        image.preserveAspect = true;
        image.raycastTarget = false;
        return child;
    }

    private static void ValidateRenewalPrefab(List<string> messages)
    {
        GameObject source = PrefabUtility.LoadPrefabContents(SourcePrefabPath);
        GameObject renewal = PrefabUtility.LoadPrefabContents(RenewalPrefabPath);
        try
        {
            FunctionalSnapshot.Capture(source).AssertEquivalent(renewal);
            LayoutElement layout = renewal.GetComponent<LayoutElement>();
            Image card = renewal.GetComponent<Image>();
            Transform badge = renewal.transform.Find("CompletionBadge");
            Transform streak = renewal.transform.Find("StreakText");
            Transform flame = streak == null ? null : streak.Find("StreakFlame");
            if (layout == null || !Mathf.Approximately(layout.preferredHeight, CardHeight) ||
                card == null || card.sprite == null || card.type != Image.Type.Sliced ||
                badge == null || badge.gameObject.activeSelf || flame == null)
            {
                throw new InvalidOperationException(
                    "Renewal Prefab의 카드, LayoutElement, 완료 배지 또는 Streak 장식 설정이 올바르지 않습니다.");
            }
            messages.Add("원본/갱신 Prefab의 HabitItem SerializeField, Toggle, Label Button, Slider 참조와 이벤트가 일치합니다.");
            messages.Add($"Renewal layout: preferredHeight={layout.preferredHeight}, card={card.sprite.name}, sliced={card.type}");
            messages.Add("CompletionBadge는 기본 비활성이고 StreakFlame은 StreakText 표시 상태를 따릅니다.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(source);
            PrefabUtility.UnloadPrefabContents(renewal);
        }
    }

    private static void EnsureGeneratedSprites()
    {
        string absoluteRoot = AssetAbsolutePath(GeneratedRoot);
        Directory.CreateDirectory(absoluteRoot);
        WriteIfMissing(CardSpritePath, CreateRoundedSprite(512, 196, 50,
            new Color32(0xF5, 0xE6, 0xD0, 0xFF), new Color32(0xC9, 0xA5, 0x74, 0xFF), 14));
        WriteIfMissing(ToggleSpritePath, CreateRoundedSprite(128, 128, 42,
            new Color32(0xFF, 0xF8, 0xE9, 0xFF), new Color32(0xC9, 0xA5, 0x74, 0xFF), 10));
        WriteIfMissing(CheckSpritePath, CreateCheckSprite(128, Brown));
        WriteIfMissing(BadgeSpritePath, CreateRoundedSprite(256, 80, 30,
            Green, new Color32(0xE7, 0xB8, 0x5B, 0xFF), 8));
        WriteIfMissing(FlameSpritePath, CreateFlameSprite(128));
        AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        ConfigureSprite(CardSpritePath, new Vector4(64f, 64f, 64f, 64f), 100f);
        ConfigureSprite(ToggleSpritePath, new Vector4(48f, 48f, 48f, 48f), 100f);
        ConfigureSprite(CheckSpritePath, Vector4.zero, 100f);
        ConfigureSprite(BadgeSpritePath, new Vector4(34f, 34f, 34f, 34f), 100f);
        ConfigureSprite(FlameSpritePath, Vector4.zero, 100f);
    }

    private static Texture2D CreateFlameSprite(int size)
    {
        Texture2D texture = NewTexture(size, size);
        Color32[] pixels = new Color32[size * size];
        Vector2 center = new Vector2(size * 0.5f, size * 0.45f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float nx = (x + 0.5f - center.x) / (size * 0.31f);
                float ny = (y + 0.5f - center.y) / (size * 0.43f);
                float taper = Mathf.Lerp(1f, 0.2f, Mathf.Clamp01(ny));
                bool outer = ny > -1f && ny < 1.25f && Mathf.Abs(nx) < taper &&
                             nx * nx + (ny + 0.35f) * (ny + 0.35f) < 1.25f;
                bool inner = outer && ny < 0.2f && nx * nx + (ny + 0.45f) * (ny + 0.45f) < 0.32f;
                pixels[y * size + x] = !outer
                    ? new Color32(0, 0, 0, 0)
                    : inner ? new Color32(0xFF, 0xD7, 0x66, 0xFF) : Orange;
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        return texture;
    }

    private static Texture2D CreateRoundedSprite(int width, int height, float radius,
        Color32 fill, Color32 border, float borderWidth)
    {
        Texture2D texture = NewTexture(width, height);
        Color32[] pixels = new Color32[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                float distance = RoundedRectDistance(x + 0.5f, y + 0.5f, width, height, radius);
                pixels[y * width + x] = distance > 0f
                    ? new Color32(0, 0, 0, 0)
                    : distance > -borderWidth ? border : fill;
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        return texture;
    }

    private static Texture2D CreateCheckSprite(int size, Color32 color)
    {
        Texture2D texture = NewTexture(size, size);
        Color32[] pixels = new Color32[size * size];
        Vector2 a = new Vector2(25f, 65f);
        Vector2 b = new Vector2(53f, 36f);
        Vector2 c = new Vector2(105f, 94f);
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                Vector2 point = new Vector2(x + 0.5f, y + 0.5f);
                bool inside = DistanceToSegment(point, a, b) <= 10f ||
                              DistanceToSegment(point, b, c) <= 10f;
                pixels[y * size + x] = inside ? color : new Color32(0, 0, 0, 0);
            }
        }
        texture.SetPixels32(pixels);
        texture.Apply(false, false);
        return texture;
    }

    private static Texture2D NewTexture(int width, int height)
    {
        Texture2D texture = new Texture2D(width, height, TextureFormat.RGBA32, false, false);
        texture.name = "HabitItem UI Renewal generated sprite";
        return texture;
    }

    private static void WriteIfMissing(string assetPath, Texture2D texture)
    {
        try
        {
            string absolutePath = AssetAbsolutePath(assetPath);
            if (!File.Exists(absolutePath))
            {
                File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    private static void ConfigureSprite(string path, Vector4 border, float pixelsPerUnit)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            throw new InvalidOperationException($"TextureImporter를 찾을 수 없습니다: {path}");
        }
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.spriteBorder = border;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
    }

    private static float RoundedRectDistance(float x, float y, float width, float height, float radius)
    {
        Vector2 point = new Vector2(Mathf.Abs(x - width * 0.5f), Mathf.Abs(y - height * 0.5f));
        Vector2 half = new Vector2(width * 0.5f - radius, height * 0.5f - radius);
        Vector2 delta = point - half;
        return new Vector2(Mathf.Max(delta.x, 0f), Mathf.Max(delta.y, 0f)).magnitude +
               Mathf.Min(Mathf.Max(delta.x, delta.y), 0f) - radius;
    }

    private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        Vector2 segment = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(point - a, segment) / segment.sqrMagnitude);
        return Vector2.Distance(point, a + segment * t);
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 position, Vector2 size, Vector2 pivot)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.pivot = pivot;
    }

    private static void SetOffsets(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax,
        Vector2 offsetMin, Vector2 offsetMax, Vector2 pivot)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.pivot = pivot;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static Sprite LoadSprite(string path)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
        {
            throw new InvalidOperationException($"Sprite를 불러올 수 없습니다: {path}");
        }
        return sprite;
    }

    private static Transform RequireChild(Transform parent, string name)
    {
        Transform child = parent.Find(name);
        if (child == null)
        {
            throw new InvalidOperationException($"Hierarchy를 찾을 수 없습니다: {GetPath(parent)}/{name}");
        }
        return child;
    }

    private static T Require<T>(Transform transform, string label) where T : Component
    {
        T component = transform.GetComponent<T>();
        if (component == null)
        {
            throw new InvalidOperationException($"{label}에 {typeof(T).Name} 컴포넌트가 없습니다.");
        }
        return component;
    }

    private static string DescribeHierarchy(Transform root)
    {
        return string.Join(", ", root.GetComponentsInChildren<Transform>(true)
            .Select(GetPath));
    }

    private static string GetPath(Transform transform)
    {
        List<string> parts = new List<string>();
        while (transform != null)
        {
            parts.Add(transform.name);
            transform = transform.parent;
        }
        parts.Reverse();
        return string.Join("/", parts);
    }

    private static string RelativePath(Transform root, UnityEngine.Object value)
    {
        Component component = value as Component;
        if (component == null)
        {
            return value == null ? "<null>" : value.name;
        }
        return AnimationUtility.CalculateTransformPath(component.transform, root) + ":" + component.GetType().FullName;
    }

    private static string DisplayPath(string path) => string.IsNullOrEmpty(path) ? "<null>" : path;

    private static string AssetAbsolutePath(string assetPath)
    {
        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot))
        {
            throw new InvalidOperationException("Unity 프로젝트 루트를 확인할 수 없습니다.");
        }
        return Path.GetFullPath(Path.Combine(projectRoot, assetPath));
    }

    private static void LogReport(string title, HabitItemContext context, IEnumerable<string> messages)
    {
        Debug.Log($"[HabitPVP UI Renewal] {title}\n" +
                  $"Scene: {context.Scene.path}\n" + string.Join("\n", messages.Select(item => "- " + item)));
    }

    private sealed class HabitItemContext
    {
        public readonly Scene Scene;
        public readonly HabitListManager Manager;
        public readonly Transform Content;
        public readonly SerializedObject ManagerObject;
        public readonly SerializedProperty PrefabProperty;
        public readonly string BeforePrefabPath;

        public HabitItemContext(Scene scene, HabitListManager manager, Transform content, SerializedObject managerObject,
            SerializedProperty prefabProperty, string beforePrefabPath)
        {
            Scene = scene;
            Manager = manager;
            Content = content;
            ManagerObject = managerObject;
            PrefabProperty = prefabProperty;
            BeforePrefabPath = beforePrefabPath;
        }
    }

    private sealed class FunctionalSnapshot
    {
        private readonly Dictionary<string, string> habitItemReferences;
        private readonly SelectableSnapshot toggle;
        private readonly SelectableSnapshot labelButton;
        private readonly SliderSnapshot slider;

        private FunctionalSnapshot(GameObject root)
        {
            HabitItem habitItem = root.GetComponent<HabitItem>();
            if (habitItem == null)
            {
                throw new InvalidOperationException("Prefab 루트에 HabitItem 컴포넌트가 없습니다.");
            }
            SerializedObject serialized = new SerializedObject(habitItem);
            habitItemReferences = new[]
                {
                    "completeToggle", "labelButton", "progressBar", "progressText", "streakText", "streakRewardPopup"
                }
                .ToDictionary(name => name, name =>
                {
                    SerializedProperty property = serialized.FindProperty(name);
                    if (property == null)
                    {
                        throw new InvalidOperationException($"HabitItem.{name} SerializeField를 찾을 수 없습니다.");
                    }
                    return RelativePath(root.transform, property.objectReferenceValue);
                });
            toggle = SelectableSnapshot.Capture(root.GetComponent<Toggle>(), root.transform, true);
            labelButton = SelectableSnapshot.Capture(root.transform.Find("Label")?.GetComponent<Button>(), root.transform, false);
            slider = SliderSnapshot.Capture(root.transform.Find("ProgressBar")?.GetComponent<Slider>(), root.transform);
        }

        public static FunctionalSnapshot Capture(GameObject root) => new FunctionalSnapshot(root);

        public void AssertPreserved(GameObject root)
        {
            AssertEquivalent(root);
        }

        public void AssertEquivalent(GameObject root)
        {
            FunctionalSnapshot other = new FunctionalSnapshot(root);
            foreach (KeyValuePair<string, string> pair in habitItemReferences)
            {
                if (!other.habitItemReferences.TryGetValue(pair.Key, out string value) || value != pair.Value)
                {
                    throw new InvalidOperationException($"HabitItem.{pair.Key} 참조가 변경되었습니다: {pair.Value} -> {value}");
                }
            }
            toggle.AssertEquivalent(other.toggle, "Toggle");
            labelButton.AssertEquivalent(other.labelButton, "Label Button");
            slider.AssertEquivalent(other.slider);
        }
    }

    private sealed class SelectableSnapshot
    {
        private readonly string targetGraphic;
        private readonly string secondaryGraphic;
        private readonly bool interactable;
        private readonly bool toggleIsOn;
        private readonly string[] persistentCalls;

        private SelectableSnapshot(Selectable selectable, Transform root, bool isToggle)
        {
            if (selectable == null)
            {
                throw new InvalidOperationException("보호할 Selectable 컴포넌트가 없습니다.");
            }
            targetGraphic = RelativePath(root, selectable.targetGraphic);
            interactable = selectable.interactable;
            Toggle toggle = selectable as Toggle;
            secondaryGraphic = isToggle ? RelativePath(root, toggle?.graphic) : string.Empty;
            toggleIsOn = toggle != null && toggle.isOn;
            if (toggle != null)
            {
                persistentCalls = Enumerable.Range(0, toggle.onValueChanged.GetPersistentEventCount())
                    .Select(index => DescribePersistentCall(toggle.onValueChanged.GetPersistentTarget(index),
                        toggle.onValueChanged.GetPersistentMethodName(index))).ToArray();
            }
            else
            {
                Button button = (Button)selectable;
                persistentCalls = Enumerable.Range(0, button.onClick.GetPersistentEventCount())
                    .Select(index => DescribePersistentCall(button.onClick.GetPersistentTarget(index),
                        button.onClick.GetPersistentMethodName(index))).ToArray();
            }
        }

        public static SelectableSnapshot Capture(Selectable selectable, Transform root, bool isToggle) =>
            new SelectableSnapshot(selectable, root, isToggle);

        public void AssertEquivalent(SelectableSnapshot other, string label)
        {
            if (targetGraphic != other.targetGraphic || secondaryGraphic != other.secondaryGraphic ||
                interactable != other.interactable || toggleIsOn != other.toggleIsOn ||
                !persistentCalls.SequenceEqual(other.persistentCalls))
            {
                throw new InvalidOperationException($"{label} 참조 또는 persistent event가 변경되었습니다.");
            }
        }

        private static string DescribePersistentCall(UnityEngine.Object target, string method)
        {
            return (target == null ? "<null>" : target.GetType().FullName) + "." + method;
        }
    }

    private sealed class SliderSnapshot
    {
        private readonly string targetGraphic;
        private readonly string fillRect;
        private readonly string handleRect;
        private readonly float minValue;
        private readonly float maxValue;
        private readonly float value;
        private readonly bool wholeNumbers;
        private readonly bool interactable;
        private readonly Slider.Direction direction;
        private readonly string[] persistentCalls;

        private SliderSnapshot(Slider slider, Transform root)
        {
            if (slider == null)
            {
                throw new InvalidOperationException("ProgressBar Slider가 없습니다.");
            }
            targetGraphic = RelativePath(root, slider.targetGraphic);
            fillRect = RelativePath(root, slider.fillRect);
            handleRect = RelativePath(root, slider.handleRect);
            minValue = slider.minValue;
            maxValue = slider.maxValue;
            value = slider.value;
            wholeNumbers = slider.wholeNumbers;
            interactable = slider.interactable;
            direction = slider.direction;
            persistentCalls = Enumerable.Range(0, slider.onValueChanged.GetPersistentEventCount())
                .Select(index => slider.onValueChanged.GetPersistentMethodName(index)).ToArray();
        }

        public static SliderSnapshot Capture(Slider slider, Transform root) => new SliderSnapshot(slider, root);

        public void AssertEquivalent(SliderSnapshot other)
        {
            if (targetGraphic != other.targetGraphic || fillRect != other.fillRect || handleRect != other.handleRect ||
                !Mathf.Approximately(minValue, other.minValue) || !Mathf.Approximately(maxValue, other.maxValue) ||
                !Mathf.Approximately(value, other.value) || wholeNumbers != other.wholeNumbers ||
                interactable != other.interactable || direction != other.direction ||
                !persistentCalls.SequenceEqual(other.persistentCalls))
            {
                throw new InvalidOperationException("Slider 값, 참조 또는 persistent event가 변경되었습니다.");
            }
        }
    }
}
