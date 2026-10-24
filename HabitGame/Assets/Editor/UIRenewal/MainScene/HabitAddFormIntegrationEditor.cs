using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class HabitAddFormIntegrationEditor
{
    private const string ScenePath = "Assets/Scenes/MainScene.unity";
    private const string CanvasPrefabPath = "Assets/Resources/UIPrefabs/Canvas.prefab";
    private const string FormPath = "HabitTap/LifeAdd_1";
    private const string FontPath = "Assets/Login/Fonts/NotoSansKR Login SDF.asset";
    private const string CardSpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/HabitItem/panel_rpg_card.png";
    private const string CreamButtonSpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/HabitItem/Button_square_cream.png";
    private const string GoldButtonSpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/HabitItem/Button_square_rpg.png";
    private const string MenuRoot = "Tools/HabitPVP/UI Renewal/MainScene/";
    private const float ScrollHorizontalInset = 18f;
    private const float ScrollEdgeGap = 18f;
    private const float BaseSectionSpacing = 18f;
    private const float MaximumResponsiveExpansion = 158f;
    private const float MinimumBottomBreathingRoom = 8f;

    private static readonly Color32 Cream = new Color32(0xF8, 0xEF, 0xD9, 0xFF);
    private static readonly Color32 Brown = new Color32(0x5B, 0x36, 0x39, 0xFF);
    private static readonly Color32 MutedBrown = new Color32(0x86, 0x68, 0x5D, 0xFF);
    private static readonly Color32 SoftGold = new Color32(0xF4, 0xD5, 0x8B, 0xFF);

    private static readonly string[] SectionPaths =
    {
        "HabitScroll View/Viewport/Content/Name",
        "HabitScroll View/Viewport/Content/Category",
        "HabitScroll View/Viewport/Content/RecordTypePanel",
        "HabitScroll View/Viewport/Content/Repeat",
        "HabitScroll View/Viewport/Content/DurationPanel",
        "HabitScroll View/Viewport/Content/Amount",
        "HabitScroll View/Viewport/Content/Reward",
        "HabitScroll View/Viewport/Content/Save"
    };

    private static readonly float[] BaseSectionHeights =
        { 188f, 282f, 216f, 216f, 188f, 232f, 124f, 136f };

    private static readonly float[] SectionHeightExpansion =
        { 18f, 28f, 18f, 18f, 18f, 24f, 12f, 22f };

    [MenuItem(MenuRoot + "Validate Habit Add Form", false, 2220)]
    private static void ValidateMenu()
    {
        Context context = Validate(true);
        if (context.IsValid)
        {
            Debug.Log("[HabitPVP MainScene] Habit Add Form 검증 성공. Scene과 Prefab은 변경하지 않았습니다.",
                context.FormRoot);
        }
    }

    [MenuItem(MenuRoot + "Dry Run Habit Add Form", false, 2221)]
    private static void DryRunMenu()
    {
        Context context = Validate(true);
        if (!context.IsValid)
        {
            Debug.LogError("[HabitPVP MainScene] Habit Add Form Dry Run 중단: 검증 오류를 해결해 주세요.");
            return;
        }

        Debug.Log(
            "[HabitPVP MainScene] Habit Add Form Dry Run (변경 없음)\n" +
            "대상: Canvas/" + FormPath + "\n" +
            "- LifeAdd_1: 기존 하단 TapBar 예약 180px 제거, Safe Area 내부 zero-offset full stretch\n" +
            "- 폼/헤더/8개 섹션: 크림 카드와 골드 계열 RPG 스타일\n" +
            "- NotoSansKR 브라운 텍스트, 입력창/Dropdown/선택 버튼/저장 버튼 스타일\n" +
            "- Content VerticalLayoutGroup + ContentSizeFitter, ScrollRect Clamped\n" +
            "- Viewport의 남는 높이를 입력/선택/보상/저장 영역과 간격에 최대 158px 반응형 배분\n" +
            "- Habit Add 전용 TapBar 숨김 정책에 맞춰 Header 아래부터 화면 하단까지 스크롤 영역 배치\n" +
            "- HabitGoalUI, HabitUIManager, MainTabManager 직렬화 참조와 모든 UnityEvent 보존\n" +
            "- Scene 자동 저장 및 Canvas.prefab Apply/Revert 없음",
            context.FormRoot);
    }

    [MenuItem(MenuRoot + "Apply Habit Add Form", false, 2222)]
    private static void ApplyMenu()
    {
        Context context = Validate(true);
        if (!context.IsValid)
        {
            Debug.LogError("[HabitPVP MainScene] Habit Add Form Apply 중단: 검증 오류를 해결해 주세요.");
            return;
        }

        ProtectionSnapshot snapshot = ProtectionSnapshot.Capture(context);
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply MainScene Habit Add Form UI Renewal");

        try
        {
            ApplyStyle(context);
            snapshot.AssertUnchanged(context);
            ValidateLayout(context);
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = context.FormRoot;
            Debug.Log(
                "[HabitPVP MainScene] Habit Add Form 적용 완료. Scene은 저장하지 않았습니다. " +
                "Hierarchy/Inspector 확인 후 사용자가 직접 저장해 주세요. Edit > Undo를 지원합니다.",
                context.FormRoot);
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(undoGroup);
            Debug.LogException(exception);
            Debug.LogError("[HabitPVP MainScene] Habit Add Form Apply 실패. 이번 Undo Group을 되돌렸습니다.");
        }
    }

    [MenuItem(MenuRoot + "Apply Habit Add Form", true)]
    private static bool CanApply()
    {
        Scene scene = SceneManager.GetActiveScene();
        return !EditorApplication.isPlayingOrWillChangePlaymode && scene.IsValid() && scene.isLoaded &&
               string.Equals(scene.path, ScenePath, StringComparison.Ordinal);
    }

    private static Context Validate(bool log)
    {
        Context context = new Context { Scene = SceneManager.GetActiveScene() };
        if (!context.Scene.IsValid() || !context.Scene.isLoaded ||
            !string.Equals(context.Scene.path, ScenePath, StringComparison.Ordinal))
        {
            context.Errors.Add("정확한 MainScene을 활성 Scene으로 연 Edit Mode에서만 실행할 수 있습니다.");
            return Finish(context, log);
        }
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            context.Errors.Add("Play Mode 또는 전환 중에는 실행할 수 없습니다.");
        }

        GameObject[] canvases = context.Scene.GetRootGameObjects().Where(item => item.name == "Canvas").ToArray();
        if (canvases.Length != 1)
        {
            context.Errors.Add("Scene 루트 Canvas가 정확히 하나여야 합니다. 현재: " + canvases.Length);
            return Finish(context, log);
        }

        context.Canvas = canvases[0];
        if (PrefabUtility.GetOutermostPrefabInstanceRoot(context.Canvas) != context.Canvas ||
            PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(context.Canvas) != CanvasPrefabPath)
        {
            context.Errors.Add("Canvas가 예상 Canvas.prefab의 최상위 Scene instance가 아닙니다.");
        }

        Transform form = context.Canvas.transform.Find(FormPath);
        context.FormRoot = form != null ? form.gameObject : null;
        context.GoalUi = context.FormRoot != null ? context.FormRoot.GetComponent<HabitGoalUI>() : null;
        context.HabitManager = FindUnique<HabitUIManager>(context);
        context.TabManager = FindUnique<MainTabManager>(context);
        context.TapBar = context.Canvas.transform.Find("TapBar") as RectTransform;
        if (context.FormRoot == null || context.GoalUi == null)
        {
            context.Errors.Add("Canvas/" + FormPath + "의 HabitGoalUI를 찾을 수 없습니다.");
            return Finish(context, log);
        }
        if (!PrefabUtility.IsPartOfPrefabInstance(context.FormRoot))
        {
            context.Errors.Add("LifeAdd_1이 Canvas Prefab instance에 속하지 않습니다.");
        }
        if (context.TapBar == null)
        {
            context.Errors.Add("Habit Add 화면에서 숨길 Canvas/TapBar가 없습니다.");
        }
        if (context.HabitManager != null &&
            context.HabitManager.GetComponentInParent<Canvas>()?.gameObject != context.Canvas)
        {
            context.Errors.Add("HabitUIManager가 MainScene Canvas 계층에 없어 TapBar를 안전하게 찾을 수 없습니다.");
        }

        ValidateObjectReferences(context.GoalUi, "HabitGoalUI", context);
        ValidateObjectReferences(context.HabitManager, "HabitUIManager", context);
        ValidateObjectReferences(context.TabManager, "MainTabManager", context);
        ValidateHierarchy(context);
        ValidateBindings(context);

        context.Font = Load<TMP_FontAsset>(FontPath, context);
        context.CardSprite = Load<Sprite>(CardSpritePath, context);
        context.CreamButtonSprite = Load<Sprite>(CreamButtonSpritePath, context);
        context.GoldButtonSprite = Load<Sprite>(GoldButtonSpritePath, context);
        return Finish(context, log);
    }

    private static void ValidateHierarchy(Context context)
    {
        Transform root = context.FormRoot.transform;
        string[] paths = SectionPaths.Concat(new[]
        {
            "Header/BackButton", "Header/title", "HabitScroll View", "HabitScroll View/Viewport",
            "HabitScroll View/Viewport/Content", "HabitScroll View/Viewport/Content/Name/HabitInput",
            "HabitScroll View/Viewport/Content/Category/CategoryGrid",
            "HabitScroll View/Viewport/Content/RecordTypePanel/ButtonGroup",
            "HabitScroll View/Viewport/Content/Repeat/ButtonGroup",
            "HabitScroll View/Viewport/Content/DurationPanel/DurationLabel",
            "HabitScroll View/Viewport/Content/DurationPanel/DurationInput",
            "HabitScroll View/Viewport/Content/DurationPanel/DaysText",
            "HabitScroll View/Viewport/Content/Amount/InputGroup",
            "HabitScroll View/Viewport/Content/Amount/InputGroup/MinusButton",
            "HabitScroll View/Viewport/Content/Amount/InputGroup/PlusButton",
            "HabitScroll View/Viewport/Content/Save/SaveButton"
        }).ToArray();
        foreach (string path in paths.Where(path => root.Find(path) == null))
        {
            context.Errors.Add("Preview와 동일한 기존 폼 경로가 없습니다: LifeAdd_1/" + path);
        }

        ScrollRect scroll = Find<ScrollRect>(root, "HabitScroll View");
        RectTransform viewport = Find<RectTransform>(root, "HabitScroll View/Viewport");
        RectTransform content = Find<RectTransform>(root, "HabitScroll View/Viewport/Content");
        if (scroll == null || viewport == null || content == null ||
            scroll.viewport != viewport || scroll.content != content)
        {
            context.Errors.Add("Habit Add Form ScrollRect의 viewport/content 연결이 예상 구조와 다릅니다.");
        }
    }

    private static void ValidateBindings(Context context)
    {
        HabitGoalUI goal = context.GoalUi;
        ValidateButton(goal.physicalButton, "SelectCategory", context);
        ValidateButton(goal.rhythmButton, "SelectCategory", context);
        ValidateButton(goal.ecoButton, "SelectCategory", context);
        ValidateButton(goal.growthButton, "SelectCategory", context);
        ValidateButton(goal.completeButton, "CompleteMode", context);
        ValidateButton(goal.valueButton, "ValueMode", context);
        ValidateButton(goal.dailyButton, "SelectPeriod", context);
        ValidateButton(goal.weeklyButton, "SelectPeriod", context);
        ValidateButton(goal.longTermButton, "SelectPeriod", context);
        Transform root = context.FormRoot.transform;
        ValidateButton(Find<Button>(root, "HabitScroll View/Viewport/Content/Amount/InputGroup/MinusButton"),
            "DecreaseAmount", context);
        ValidateButton(Find<Button>(root, "HabitScroll View/Viewport/Content/Amount/InputGroup/PlusButton"),
            "IncreaseAmount", context);
        ValidateButton(Find<Button>(root, "HabitScroll View/Viewport/Content/Save/SaveButton"),
            "SaveGoal", context);
        ValidateButton(Find<Button>(root, "Header/BackButton"), "BackToLife", context);
        ValidateButton(Find<Button>(context.Canvas.transform,
            "HabitTap/Life/HabitListCard/HabitListHeader/AddButton"), "OpenLifeAddPanel", context);
    }

    private static void ValidateObjectReferences(UnityEngine.Object target, string label, Context context)
    {
        if (target == null)
        {
            context.Errors.Add(label + "가 MainScene에 정확히 하나 있어야 합니다.");
            return;
        }
        SerializedObject serialized = new SerializedObject(target);
        SerializedProperty iterator = serialized.GetIterator();
        bool enterChildren = true;
        while (iterator.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (iterator.propertyType == SerializedPropertyType.ObjectReference &&
                iterator.objectReferenceValue == null && iterator.name != "m_Script")
            {
                context.Errors.Add(label + "." + iterator.propertyPath + " 참조가 비어 있습니다.");
            }
        }
    }

    private static T FindUnique<T>(Context context) where T : Component
    {
        T[] matches = UnityEngine.Object.FindObjectsOfType<T>(true)
            .Where(item => item.gameObject.scene == context.Scene).ToArray();
        if (matches.Length != 1)
        {
            context.Errors.Add(typeof(T).Name + " 수가 1이 아닙니다: " + matches.Length);
            return null;
        }
        return matches[0];
    }

    private static void ValidateButton(Button button, string method, Context context)
    {
        if (button == null || !Enumerable.Range(0, button.onClick.GetPersistentEventCount())
                .Any(index => button.onClick.GetPersistentMethodName(index) == method))
        {
            context.Errors.Add(method + " 기존 UnityEvent 바인딩을 찾을 수 없습니다.");
        }
    }

    private static T Load<T>(string path, Context context) where T : UnityEngine.Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) context.Errors.Add("UI Renewal 에셋을 불러올 수 없습니다: " + path);
        return asset;
    }

    private static Context Finish(Context context, bool log)
    {
        if (log)
        {
            foreach (string error in context.Errors) Debug.LogError("[HabitPVP MainScene] " + error);
        }
        return context;
    }

    private static void ApplyStyle(Context context)
    {
        Transform root = context.FormRoot.transform;
        ConfigureFormRoot(context.FormRoot.GetComponent<RectTransform>());
        StyleImage(GetOrAdd<Image>(context.FormRoot), context.CardSprite, Cream, false);

        RectTransform header = Require<RectTransform>(root, "Header");
        Record(header); header.anchorMin = new Vector2(0f, 1f); header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(0.5f, 1f); header.anchoredPosition = Vector2.zero;
        header.sizeDelta = new Vector2(0f, 160f);
        StyleImage(GetOrAdd<Image>(header.gameObject), context.CardSprite, SoftGold, false);

        RectTransform scrollArea = Require<RectTransform>(root, "HabitScroll View");
        ConfigureScrollArea(context, header, scrollArea);
        StyleImage(GetOrAdd<Image>(scrollArea.gameObject), context.CardSprite, Cream, true);
        RectTransform viewport = Require<RectTransform>(root, "HabitScroll View/Viewport");
        ConfigureViewport(viewport);
        RectTransform content = Require<RectTransform>(root, "HabitScroll View/Viewport/Content");
        ScrollRect scroll = Require<ScrollRect>(root, "HabitScroll View");
        Record(scroll); scroll.viewport = viewport; scroll.content = content; scroll.horizontal = false;
        scroll.vertical = true; scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = true; scroll.scrollSensitivity = 55f; scroll.enabled = true;

        Record(content); content.anchorMin = new Vector2(0f, 1f); content.anchorMax = new Vector2(1f, 1f);
        content.pivot = new Vector2(0.5f, 1f); content.anchoredPosition = Vector2.zero;
        VerticalLayoutGroup vertical = GetOrAdd<VerticalLayoutGroup>(content.gameObject);
        Record(vertical); vertical.padding = new RectOffset(24, 24, 24, 32);
        vertical.spacing = BaseSectionSpacing;
        vertical.childAlignment = TextAnchor.UpperCenter; vertical.childControlWidth = true;
        vertical.childControlHeight = true; vertical.childForceExpandWidth = true;
        vertical.childForceExpandHeight = false;
        ContentSizeFitter fitter = GetOrAdd<ContentSizeFitter>(content.gameObject);
        Record(fitter); fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        for (int index = 0; index < SectionPaths.Length; index++)
            StyleSection(root, SectionPaths[index], BaseSectionHeights[index], context.CardSprite);

        foreach (TMP_Text text in context.FormRoot.GetComponentsInChildren<TMP_Text>(true))
        {
            Record(text); text.font = context.Font; text.color = Brown; text.enableAutoSizing = true;
            text.fontSizeMin = 26f; text.fontSizeMax = 40f; text.margin = new Vector4(10f, 4f, 10f, 4f);
        }
        StyleTitle(Require<TMP_Text>(root, "Header/title"), context.Font, 50f, Color.white);
        StyleInput(context.GoalUi.habitNameInput, context.CreamButtonSprite, context.Font);
        StyleInput(context.GoalUi.durationInput, context.CreamButtonSprite, context.Font);
        StyleDropdown(context.GoalUi.unitDropdown, context.CreamButtonSprite, context.Font);
        StyleReward(root, context.Font, context.CardSprite);

        Button[] choices = { context.GoalUi.physicalButton, context.GoalUi.rhythmButton,
            context.GoalUi.ecoButton, context.GoalUi.growthButton, context.GoalUi.completeButton,
            context.GoalUi.valueButton, context.GoalUi.dailyButton, context.GoalUi.weeklyButton,
            context.GoalUi.longTermButton };
        foreach (Button button in choices) StyleButton(button, context.CreamButtonSprite, Color.white, Brown, context.Font, 34f);
        StyleButton(Require<Button>(root, "HabitScroll View/Viewport/Content/Amount/InputGroup/MinusButton"),
            context.CreamButtonSprite, Color.white, Brown, context.Font, 42f);
        StyleButton(Require<Button>(root, "HabitScroll View/Viewport/Content/Amount/InputGroup/PlusButton"),
            context.CreamButtonSprite, Color.white, Brown, context.Font, 42f);
        StyleButton(Require<Button>(root, "Header/BackButton"), context.CreamButtonSprite,
            new Color32(0xEA, 0xD7, 0xB5, 0xFF), MutedBrown, context.Font, 30f);
        StyleButton(Require<Button>(root, "HabitScroll View/Viewport/Content/Save/SaveButton"),
            context.GoldButtonSprite, Color.white, Brown, context.Font, 42f);

        ConfigureTopRow(Require<RectTransform>(root, "HabitScroll View/Viewport/Content/Name/Title"), 18f, 48f, 40f);
        ConfigureTopRow(Require<RectTransform>(root, "HabitScroll View/Viewport/Content/Name/HabitInput"), 78f, 88f, 40f);
        ConfigureSectionTitle(root, "Category"); ConfigureSectionTitle(root, "RecordTypePanel");
        ConfigureSectionTitle(root, "Repeat"); ConfigureSectionTitle(root, "Amount");
        ConfigureGrid(root); ConfigureSegment(root, "RecordTypePanel", 20f); ConfigureSegment(root, "Repeat", 16f);
        ConfigureDuration(root, context.Font); ConfigureAmount(root); ConfigureSave(root); ConfigureHeader(root);
        ApplyResponsiveVerticalSpacing(context, root, viewport, vertical);
        Record(context.GoalUi.amountText); context.GoalUi.amountText.font = context.Font;
        context.GoalUi.amountText.fontStyle = FontStyles.Bold; context.GoalUi.amountText.fontSize = 46f;
        context.GoalUi.amountText.color = Brown; context.GoalUi.amountText.alignment = TextAlignmentOptions.Center;
        Canvas.ForceUpdateCanvases(); LayoutRebuilder.ForceRebuildLayoutImmediate(content);

        foreach (Component component in context.FormRoot.GetComponentsInChildren<Component>(true))
        {
            if (component == null) continue;
            EditorUtility.SetDirty(component);
            if (PrefabUtility.IsPartOfPrefabInstance(component))
                PrefabUtility.RecordPrefabInstancePropertyModifications(component);
        }
    }

    private static void ApplyResponsiveVerticalSpacing(
        Context context, Transform root, RectTransform viewport, VerticalLayoutGroup vertical)
    {
        Canvas.ForceUpdateCanvases();
        RectTransform content = Require<RectTransform>(root, "HabitScroll View/Viewport/Content");
        RectTransform scrollArea = viewport.parent as RectTransform;
        RectTransform form = root as RectTransform;
        float viewportHeightFromOffsets = form != null && scrollArea != null
            ? Mathf.Max(0f, form.rect.height - scrollArea.offsetMin.y + scrollArea.offsetMax.y)
            : 0f;
        float viewportHeight = Mathf.Max(
            viewport.rect.height,
            scrollArea != null ? scrollArea.rect.height : 0f,
            viewportHeightFromOffsets);
        float baseHeight = CalculateBaseDefaultContentHeight(vertical);
        float availableExtra = Mathf.Max(
            0f, viewportHeight - baseHeight - MinimumBottomBreathingRoom);
        float expansion = Mathf.Min(availableExtra, MaximumResponsiveExpansion);
        float factor = MaximumResponsiveExpansion > 0f
            ? expansion / MaximumResponsiveExpansion
            : 0f;

        Record(vertical);
        vertical.spacing = BaseSectionSpacing + 3f * factor;
        for (int index = 0; index < SectionPaths.Length; index++)
        {
            RectTransform section = Require<RectTransform>(root, SectionPaths[index]);
            SetHeight(section.gameObject,
                BaseSectionHeights[index] + SectionHeightExpansion[index] * factor);
        }

        SetRectHeight(context.GoalUi.habitNameInput.GetComponent<RectTransform>(), 88f + 8f * factor);
        SetRectHeight(context.GoalUi.durationInput.GetComponent<RectTransform>(), 88f + 8f * factor);
        SetHeight(context.GoalUi.unitDropdown.gameObject, 88f + 8f * factor);

        GridLayoutGroup categoryGrid = Require<GridLayoutGroup>(
            root, "HabitScroll View/Viewport/Content/Category/CategoryGrid");
        Record(categoryGrid);
        categoryGrid.cellSize = new Vector2(430f, 84f + 12f * factor);
        ResizeCenteredRow(categoryGrid.GetComponent<RectTransform>(), 180f, 24f, -172f, -12f, factor);

        ResizeCenteredRow(Require<RectTransform>(
            root, "HabitScroll View/Viewport/Content/RecordTypePanel/ButtonGroup"),
            108f, 12f, -136f, -6f, factor);
        ResizeCenteredRow(Require<RectTransform>(
            root, "HabitScroll View/Viewport/Content/Repeat/ButtonGroup"),
            108f, 12f, -136f, -6f, factor);
        ResizeCenteredRow(Require<RectTransform>(
            root, "HabitScroll View/Viewport/Content/Amount/InputGroup"),
            112f, 12f, -138f, -6f, factor);

        Button[] enlargedButtons =
        {
            context.GoalUi.physicalButton, context.GoalUi.rhythmButton,
            context.GoalUi.ecoButton, context.GoalUi.growthButton,
            context.GoalUi.completeButton, context.GoalUi.valueButton,
            context.GoalUi.dailyButton, context.GoalUi.weeklyButton,
            context.GoalUi.longTermButton,
            Require<Button>(root, "HabitScroll View/Viewport/Content/Amount/InputGroup/MinusButton"),
            Require<Button>(root, "HabitScroll View/Viewport/Content/Amount/InputGroup/PlusButton")
        };
        foreach (Button button in enlargedButtons)
        {
            SetHeight(button.gameObject, 84f + 12f * factor);
        }

        foreach (TMP_Text label in RequireTransform(
                     root, "HabitScroll View/Viewport/Content/Reward")
                 .GetComponentsInChildren<TMP_Text>(true))
        {
            Record(label);
            label.margin = new Vector4(
                label.margin.x, 4f + 4f * factor, label.margin.z, 4f + 4f * factor);
        }

        LayoutRebuilder.MarkLayoutForRebuild(content);
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        Debug.Log(
            $"[HabitPVP MainScene] Habit Add 반응형 세로 배분: Viewport={viewportHeight:F1}, " +
            $"기본 Content={baseHeight:F1}, 추가 배분={expansion:F1}, factor={factor:F3}",
            context.FormRoot);
    }

    private static float CalculateBaseDefaultContentHeight(VerticalLayoutGroup vertical)
    {
        float height = vertical.padding.top + vertical.padding.bottom;
        int sectionCount = 0;
        for (int index = 0; index < BaseSectionHeights.Length; index++)
        {
            if (SectionPaths[index].EndsWith("/DurationPanel", StringComparison.Ordinal))
            {
                continue;
            }

            height += BaseSectionHeights[index];
            sectionCount++;
        }

        return height + BaseSectionSpacing * Mathf.Max(0, sectionCount - 1);
    }

    private static void SetRectHeight(RectTransform target, float height)
    {
        Record(target);
        target.sizeDelta = new Vector2(target.sizeDelta.x, height);
        SetHeight(target.gameObject, height);
    }

    private static void ResizeCenteredRow(
        RectTransform target, float baseHeight, float heightExpansion,
        float baseY, float yExpansion, float factor)
    {
        Record(target);
        target.anchoredPosition = new Vector2(target.anchoredPosition.x, baseY + yExpansion * factor);
        target.sizeDelta = new Vector2(target.sizeDelta.x, baseHeight + heightExpansion * factor);
    }

    private static void StyleSection(Transform root, string path, float height, Sprite sprite)
    {
        GameObject target = RequireTransform(root, path).gameObject;
        StyleImage(GetOrAdd<Image>(target), sprite, Color.white, false);
        LayoutElement layout = GetOrAdd<LayoutElement>(target); Record(layout);
        layout.minHeight = height; layout.preferredHeight = height; layout.flexibleHeight = 0f;
    }

    private static void StyleInput(TMP_InputField input, Sprite sprite, TMP_FontAsset font)
    {
        StyleImage(GetOrAdd<Image>(input.gameObject), sprite, Color.white, true); Record(input);
        input.selectionColor = new Color32(0xE7, 0xA8, 0x35, 0x66); SetHeight(input.gameObject, 88f);
        StyleInputText(input.textComponent, font, false);
        if (input.placeholder is TMP_Text placeholder) StyleInputText(placeholder, font, true);
    }

    private static void StyleInputText(TMP_Text text, TMP_FontAsset font, bool placeholder)
    {
        if (text == null) return; Record(text); text.font = font; text.enableAutoSizing = true;
        text.fontSizeMin = placeholder ? 27f : 28f; text.fontSizeMax = placeholder ? 33f : 36f;
        text.color = placeholder ? new Color32(0x78, 0x5A, 0x50, 0xE0) : Brown;
        if (!placeholder) text.fontStyle = FontStyles.Bold;
    }

    private static void StyleDropdown(TMP_Dropdown dropdown, Sprite sprite, TMP_FontAsset font)
    {
        StyleImage(GetOrAdd<Image>(dropdown.gameObject), sprite, Color.white, true); Record(dropdown);
        SetHeight(dropdown.gameObject, 88f); StyleInputText(dropdown.captionText, font, false);
        StyleInputText(dropdown.itemText, font, false);
    }

    private static void StyleReward(Transform root, TMP_FontAsset font, Sprite sprite)
    {
        Transform reward = RequireTransform(root, "HabitScroll View/Viewport/Content/Reward");
        StyleImage(GetOrAdd<Image>(reward.gameObject), sprite, new Color32(0xFF, 0xF2, 0xCE, 0xFF), false);
        TMP_Text[] labels = reward.GetComponentsInChildren<TMP_Text>(true);
        for (int i = 0; i < labels.Length; i++)
        {
            Record(labels[i]); labels[i].font = font; labels[i].fontStyle = FontStyles.Bold;
            labels[i].color = i == 0 ? Brown : new Color32(0x9A, 0x61, 0x16, 0xFF);
        }
    }

    private static void StyleButton(Button button, Sprite sprite, Color imageColor, Color textColor,
        TMP_FontAsset font, float maxSize)
    {
        StyleImage(button.image != null ? button.image : GetOrAdd<Image>(button.gameObject), sprite, imageColor, true);
        Record(button); SetHeight(button.gameObject, 84f);
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text == null) return; Record(text); text.font = font; text.fontStyle = FontStyles.Bold;
        text.color = textColor; text.alignment = TextAlignmentOptions.Center; text.enableAutoSizing = true;
        text.fontSizeMin = 22f; text.fontSizeMax = maxSize; text.margin = new Vector4(14f, 6f, 14f, 8f);
    }

    private static void StyleImage(Image image, Sprite sprite, Color color, bool raycast)
    { Record(image); image.sprite = sprite; image.type = Image.Type.Sliced; image.color = color; image.raycastTarget = raycast; }

    private static void StyleTitle(TMP_Text text, TMP_FontAsset font, float size, Color color)
    { Record(text); text.font = font; text.fontStyle = FontStyles.Bold; text.fontSize = size;
        text.fontSizeMin = 30f; text.fontSizeMax = size; text.color = color; text.alignment = TextAlignmentOptions.Center; }

    private static void ConfigureScrollArea(Context context, RectTransform header, RectTransform scroll)
    {
        RectTransform form = context.FormRoot.GetComponent<RectTransform>(); Canvas.ForceUpdateCanvases();
        Rect headerBounds = RelativeRect(form, header);
        float top = Mathf.Max(0f, form.rect.yMax - headerBounds.yMin) + ScrollEdgeGap;
        Record(scroll); scroll.anchorMin = Vector2.zero; scroll.anchorMax = Vector2.one;
        scroll.pivot = new Vector2(0.5f, 0.5f);
        scroll.offsetMin = new Vector2(ScrollHorizontalInset, ScrollEdgeGap);
        scroll.offsetMax = new Vector2(-ScrollHorizontalInset, -top);
    }

    private static void ConfigureFormRoot(RectTransform form)
    {
        if (form == null)
        {
            throw new InvalidOperationException("LifeAdd_1 RectTransform이 없습니다.");
        }

        Record(form);
        form.anchorMin = Vector2.zero;
        form.anchorMax = Vector2.one;
        form.pivot = new Vector2(0.5f, 0.5f);
        form.anchoredPosition = Vector2.zero;
        form.sizeDelta = Vector2.zero;
    }

    private static void ConfigureViewport(RectTransform viewport)
    {
        Record(viewport);
        viewport.anchorMin = Vector2.zero;
        viewport.anchorMax = Vector2.one;
        viewport.pivot = new Vector2(0.5f, 0.5f);
        viewport.anchoredPosition = Vector2.zero;
        viewport.sizeDelta = Vector2.zero;
    }

    private static void ConfigureHeader(Transform root)
    { RectTransform rect = Require<RectTransform>(root, "Header/BackButton"); Record(rect);
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f); rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = new Vector2(48f, 0f); }

    private static void ConfigureGrid(Transform root)
    {
        RectTransform rect = Require<RectTransform>(root, "HabitScroll View/Viewport/Content/Category/CategoryGrid");
        ConfigureBox(rect, -172f, 180f, -40f); GridLayoutGroup grid = GetOrAdd<GridLayoutGroup>(rect.gameObject);
        Record(grid); grid.cellSize = new Vector2(430f, 84f); grid.spacing = new Vector2(18f, 12f);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount; grid.constraintCount = 2;
        grid.childAlignment = TextAnchor.MiddleCenter;
    }

    private static void ConfigureSectionTitle(Transform root, string section)
    {
        RectTransform rect = Require<RectTransform>(root,
            "HabitScroll View/Viewport/Content/" + section + "/Title");
        ConfigureTopRow(rect, 18f, 48f, 40f);
    }

    private static void ConfigureSegment(Transform root, string section, float spacing)
    {
        RectTransform rect = Require<RectTransform>(root,
            "HabitScroll View/Viewport/Content/" + section + "/ButtonGroup");
        ConfigureBox(rect, -136f, 108f, -40f); HorizontalLayoutGroup group = GetOrAdd<HorizontalLayoutGroup>(rect.gameObject);
        ConfigureHorizontal(group, spacing);
    }

    private static void ConfigureAmount(Transform root)
    {
        RectTransform rect = Require<RectTransform>(root, "HabitScroll View/Viewport/Content/Amount/InputGroup");
        ConfigureBox(rect, -138f, 112f, -40f); ConfigureHorizontal(GetOrAdd<HorizontalLayoutGroup>(rect.gameObject), 12f);
    }

    private static void ConfigureHorizontal(HorizontalLayoutGroup group, float spacing)
    { Record(group); group.padding = new RectOffset(12, 12, 12, 12); group.spacing = spacing;
        group.childAlignment = TextAnchor.MiddleCenter; group.childControlWidth = true;
        group.childControlHeight = true; group.childForceExpandWidth = true; group.childForceExpandHeight = false; }

    private static void ConfigureDuration(Transform root, TMP_FontAsset font)
    {
        const string path = "HabitScroll View/Viewport/Content/DurationPanel/";
        ConfigureTopRow(Require<RectTransform>(root, path + "DurationLabel"), 18f, 48f, 40f);
        TMP_Text label = Require<TMP_Text>(root, path + "DurationLabel"); Record(label); label.font = font;
        label.fontStyle = FontStyles.Bold; label.color = Brown; label.alignment = TextAlignmentOptions.Left;
        RectTransform input = Require<RectTransform>(root, path + "DurationInput"); Record(input);
        input.anchorMin = input.anchorMax = new Vector2(0.5f, 1f); input.pivot = new Vector2(0.5f, 0.5f);
        input.anchoredPosition = new Vector2(-98f, -126f); input.sizeDelta = new Vector2(360f, 88f);
        RectTransform days = Require<RectTransform>(root, path + "DaysText"); Record(days);
        days.anchorMin = days.anchorMax = new Vector2(0.5f, 1f); days.pivot = new Vector2(0.5f, 0.5f);
        days.anchoredPosition = new Vector2(188f, -126f); days.sizeDelta = new Vector2(180f, 60f);
        TMP_Text daysText = Require<TMP_Text>(root, path + "DaysText"); Record(daysText);
        daysText.alignment = TextAlignmentOptions.MidlineLeft;
    }

    private static void ConfigureSave(Transform root)
    { RectTransform rect = Require<RectTransform>(root, "HabitScroll View/Viewport/Content/Save/SaveButton");
        Record(rect); rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one; rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero; rect.sizeDelta = new Vector2(-40f, -32f); }

    private static void ConfigureTopRow(RectTransform rect, float top, float height, float inset)
    { Record(rect); rect.anchorMin = new Vector2(0f, 1f); rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f); rect.anchoredPosition = new Vector2(0f, -top);
        rect.sizeDelta = new Vector2(-inset, height); }

    private static void ConfigureBox(RectTransform rect, float y, float height, float widthDelta)
    { Record(rect); rect.anchorMin = new Vector2(0f, 1f); rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 0.5f); rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(widthDelta, height); }

    private static void ValidateLayout(Context context)
    {
        Transform root = context.FormRoot.transform;
        RectTransform scrollArea = Require<RectTransform>(root, "HabitScroll View");
        RectTransform viewport = Require<RectTransform>(root, "HabitScroll View/Viewport");
        RectTransform content = Require<RectTransform>(root, "HabitScroll View/Viewport/Content");
        RectTransform save = Require<RectTransform>(root, "HabitScroll View/Viewport/Content/Save");
        RectTransform saveButton = Require<RectTransform>(
            root, "HabitScroll View/Viewport/Content/Save/SaveButton");
        ScrollRect scroll = Require<ScrollRect>(root, "HabitScroll View");
        VerticalLayoutGroup vertical = content.GetComponent<VerticalLayoutGroup>();
        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();

        LayoutRebuilder.MarkLayoutForRebuild(content);
        LayoutRebuilder.MarkLayoutForRebuild(scrollArea);
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(content);
        LayoutRebuilder.ForceRebuildLayoutImmediate(scrollArea);
        Canvas.ForceUpdateCanvases();

        float inactivePreferredHeight = CalculateExpectedContentHeight(root, content, false);
        float longTermPreferredHeight = CalculateExpectedContentHeight(root, content, true);
        string diagnostics = BuildLayoutDiagnostics(
            context, scroll, viewport, content, save, inactivePreferredHeight, longTermPreferredHeight);

        if (vertical == null || fitter == null ||
            fitter.verticalFit != ContentSizeFitter.FitMode.PreferredSize)
            FailLayout(context, "Content의 VerticalLayoutGroup/ContentSizeFitter 구성이 올바르지 않습니다.", diagnostics);
        if (scroll.content != content || scroll.viewport != viewport || !scroll.enabled || !scroll.vertical)
            FailLayout(context, "ScrollRect 연결 또는 세로 스크롤 설정이 올바르지 않습니다.", diagnostics);
        if (inactivePreferredHeight <= 0f || longTermPreferredHeight <= inactivePreferredHeight)
            FailLayout(context, "기본/LONG-TERM 상태의 예상 Content 높이를 계산할 수 없습니다.", diagnostics);
        RectTransform duration = Require<RectTransform>(
            root, "HabitScroll View/Viewport/Content/DurationPanel");
        float currentExpectedHeight = duration.gameObject.activeSelf
            ? longTermPreferredHeight
            : inactivePreferredHeight;
        if (context.FormRoot.activeInHierarchy &&
            (viewport.rect.height <= 0f || content.rect.height + 1f < currentExpectedHeight))
            FailLayout(context, "활성 Form의 실제 Content/Viewport 높이가 예상 레이아웃보다 작습니다.", diagnostics);
        if (save.parent != content || !IsLastLayoutParticipant(content, save) ||
            GetPreferredHeightForValidation(save) <= 0f)
            FailLayout(context, "마지막 Save 섹션이 Content 레이아웃 끝에 배치되지 않았습니다.", diagnostics);
        if (!Approximately(saveButton.anchorMin, Vector2.zero) ||
            !Approximately(saveButton.anchorMax, Vector2.one) ||
            saveButton.sizeDelta.y >= 0f ||
            -saveButton.sizeDelta.y >= GetPreferredHeightForValidation(save))
            FailLayout(context, "SaveButton이 Save 섹션 내부에 완전히 표시될 수 없는 설정입니다.", diagnostics);
        if (!Mathf.Approximately(ScrollAreaOffsetMin(root).y, ScrollEdgeGap))
            FailLayout(context, "Habit Add Form 하단 여백이 화면 기준 18px로 적용되지 않았습니다.", diagnostics);
        RectTransform form = context.FormRoot.GetComponent<RectTransform>();
        if (!Approximately(form.anchorMin, Vector2.zero) || !Approximately(form.anchorMax, Vector2.one) ||
            !Approximately(form.anchoredPosition, Vector2.zero) || !Approximately(form.sizeDelta, Vector2.zero))
            FailLayout(context, "LifeAdd_1의 기존 TapBar 예약 180px가 제거되지 않았습니다.", diagnostics);
        if (!Approximately(viewport.anchorMin, Vector2.zero) || !Approximately(viewport.anchorMax, Vector2.one) ||
            !Approximately(viewport.anchoredPosition, Vector2.zero) || !Approximately(viewport.sizeDelta, Vector2.zero))
            FailLayout(context, "Habit Add Form Viewport가 ScrollRect 전체 영역을 사용하지 않습니다.", diagnostics);

        Debug.Log("[HabitPVP MainScene] Habit Add Form 레이아웃 검증 성공.\n" + diagnostics,
            context.FormRoot);
    }

    private static float CalculateExpectedContentHeight(
        Transform root, RectTransform content, bool includeLongTerm)
    {
        VerticalLayoutGroup layout = content.GetComponent<VerticalLayoutGroup>();
        if (layout == null)
        {
            return 0f;
        }

        float height = layout.padding.top + layout.padding.bottom;
        int sectionCount = 0;
        foreach (string path in SectionPaths)
        {
            RectTransform section = Require<RectTransform>(root, path);
            if (section.name == "DurationPanel" && !includeLongTerm)
            {
                continue;
            }

            height += GetPreferredHeightForValidation(section);
            sectionCount++;
        }

        if (sectionCount > 1)
        {
            height += layout.spacing * (sectionCount - 1);
        }

        return height;
    }

    private static string BuildLayoutDiagnostics(
        Context context, ScrollRect scroll, RectTransform viewport, RectTransform content,
        RectTransform save, float inactivePreferredHeight, float longTermPreferredHeight)
    {
        return "레이아웃 진단: " +
               $"Form activeSelf={context.FormRoot.activeSelf}, activeInHierarchy={context.FormRoot.activeInHierarchy}, " +
               $"Content actual={content.rect.height:F1}, LayoutUtility preferred=" +
               $"{LayoutUtility.GetPreferredHeight(content):F1}, " +
               $"expected(default)={inactivePreferredHeight:F1}, " +
               $"expected(long-term)={longTermPreferredHeight:F1}, " +
               $"Duration active={context.GoalUi.durationPanel.activeSelf}, " +
               $"Viewport={viewport.rect.height:F1}, " +
               $"Save sibling={save.GetSiblingIndex()}/{content.childCount - 1}, " +
               $"Save actual={save.rect.height:F1}, " +
               $"Save LayoutUtility={LayoutUtility.GetPreferredHeight(save):F1}, " +
               $"Save configured={GetPreferredHeightForValidation(save):F1}, " +
               $"Save pivot={save.pivot}, anchoredPosition={save.anchoredPosition}, " +
               $"Content children=[{BuildChildOrder(content)}], " +
               $"Scroll enabled={scroll.enabled}, vertical={scroll.vertical}, " +
               $"movement={scroll.movementType}, contentLinked={scroll.content == content}, " +
               $"viewportLinked={scroll.viewport == viewport}, bottomOffset={ScrollAreaOffsetMin(context.FormRoot.transform).y:F1}";
    }

    private static void FailLayout(Context context, string message, string diagnostics)
    {
        Debug.LogError("[HabitPVP MainScene] " + message + "\n" + diagnostics, context.FormRoot);
        throw new InvalidOperationException(message);
    }

    private static float GetPreferredHeightForValidation(RectTransform target)
    {
        float preferred = LayoutUtility.GetPreferredHeight(target);
        LayoutElement element = target.GetComponent<LayoutElement>();
        if (element != null && element.enabled && !element.ignoreLayout)
        {
            preferred = Mathf.Max(preferred, element.minHeight, element.preferredHeight);
        }

        return Mathf.Max(preferred, target.rect.height);
    }

    private static bool IsLastLayoutParticipant(RectTransform content, RectTransform target)
    {
        for (int index = target.GetSiblingIndex() + 1; index < content.childCount; index++)
        {
            RectTransform sibling = content.GetChild(index) as RectTransform;
            if (sibling == null || !sibling.gameObject.activeSelf)
            {
                continue;
            }

            bool ignoresLayout = sibling.GetComponents<Behaviour>()
                .OfType<ILayoutIgnorer>()
                .Any(item => item.ignoreLayout);
            if (!ignoresLayout)
            {
                return false;
            }
        }

        return true;
    }

    private static string BuildChildOrder(RectTransform content)
    {
        return string.Join(", ", Enumerable.Range(0, content.childCount)
            .Select(index =>
            {
                Transform child = content.GetChild(index);
                return $"{index}:{child.name}(active={child.gameObject.activeSelf})";
            }));
    }

    private static bool Approximately(Vector2 left, Vector2 right)
    {
        return Mathf.Approximately(left.x, right.x) && Mathf.Approximately(left.y, right.y);
    }

    private static Vector2 ScrollAreaOffsetMin(Transform root)
    {
        return Require<RectTransform>(root, "HabitScroll View").offsetMin;
    }

    private static Rect RelativeRect(RectTransform parent, RectTransform child)
    {
        Vector3[] corners = new Vector3[4]; child.GetWorldCorners(corners);
        for (int i = 0; i < corners.Length; i++) corners[i] = parent.InverseTransformPoint(corners[i]);
        return Rect.MinMaxRect(corners.Min(point => point.x), corners.Min(point => point.y),
            corners.Max(point => point.x), corners.Max(point => point.y));
    }

    private static void SetHeight(GameObject target, float height)
    { LayoutElement layout = GetOrAdd<LayoutElement>(target); Record(layout); layout.minHeight = height; layout.preferredHeight = height; }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    { T component = target.GetComponent<T>(); return component != null ? component : Undo.AddComponent<T>(target); }

    private static T Find<T>(Transform root, string path) where T : Component
    { Transform target = root.Find(path); return target != null ? target.GetComponent<T>() : null; }

    private static T Require<T>(Transform root, string path) where T : Component
    { T component = Find<T>(root, path); if (component == null) throw new InvalidOperationException(typeof(T).Name + " 없음: " + path); return component; }

    private static Transform RequireTransform(Transform root, string path)
    { Transform target = root.Find(path); if (target == null) throw new InvalidOperationException("Transform 없음: " + path); return target; }

    private static void Record(UnityEngine.Object target)
    { Undo.RecordObject(target, "Style MainScene Habit Add Form"); EditorUtility.SetDirty(target); }

    private sealed class Context
    {
        public Scene Scene; public GameObject Canvas; public GameObject FormRoot; public HabitGoalUI GoalUi;
        public HabitUIManager HabitManager; public MainTabManager TabManager; public RectTransform TapBar;
        public TMP_FontAsset Font; public Sprite CardSprite; public Sprite CreamButtonSprite; public Sprite GoldButtonSprite;
        public readonly List<string> Errors = new List<string>(); public bool IsValid => Errors.Count == 0;
    }

    private sealed class ProtectionSnapshot
    {
        private string goalJson; private string habitManagerJson; private string tabManagerJson;
        private string[] eventSignatures; private string[] dropdownOptions; private int dropdownValue;

        public static ProtectionSnapshot Capture(Context context)
        {
            return new ProtectionSnapshot
            {
                goalJson = EditorJsonUtility.ToJson(context.GoalUi),
                habitManagerJson = EditorJsonUtility.ToJson(context.HabitManager),
                tabManagerJson = EditorJsonUtility.ToJson(context.TabManager),
                eventSignatures = CaptureEvents(context.Canvas),
                dropdownOptions = context.GoalUi.unitDropdown.options.Select(item => item.text).ToArray(),
                dropdownValue = context.GoalUi.unitDropdown.value
            };
        }

        public void AssertUnchanged(Context context)
        {
            if (goalJson != EditorJsonUtility.ToJson(context.GoalUi) ||
                habitManagerJson != EditorJsonUtility.ToJson(context.HabitManager) ||
                tabManagerJson != EditorJsonUtility.ToJson(context.TabManager))
                throw new InvalidOperationException("HabitGoalUI 또는 Manager SerializeField가 변경되었습니다.");
            if (!eventSignatures.SequenceEqual(CaptureEvents(context.Canvas)))
                throw new InvalidOperationException("MainScene Canvas의 UnityEvent가 변경되었습니다.");
            if (dropdownValue != context.GoalUi.unitDropdown.value ||
                !dropdownOptions.SequenceEqual(context.GoalUi.unitDropdown.options.Select(item => item.text)))
                throw new InvalidOperationException("Unit Dropdown의 옵션/value가 변경되었습니다.");
        }

        private static string[] CaptureEvents(GameObject root)
        {
            List<string> values = new List<string>();
            foreach (Button button in root.GetComponentsInChildren<Button>(true))
                Add(values, button.transform, "Button", button.onClick);
            foreach (TMP_InputField input in root.GetComponentsInChildren<TMP_InputField>(true))
            { Add(values, input.transform, "InputChange", input.onValueChanged); Add(values, input.transform, "InputEnd", input.onEndEdit); }
            foreach (TMP_Dropdown dropdown in root.GetComponentsInChildren<TMP_Dropdown>(true))
                Add(values, dropdown.transform, "Dropdown", dropdown.onValueChanged);
            values.Sort(StringComparer.Ordinal); return values.ToArray();
        }

        private static void Add(ICollection<string> values, Transform owner, string label, UnityEventBase unityEvent)
        {
            string path = owner.name; Transform current = owner.parent;
            while (current != null) { path = current.name + "/" + path; current = current.parent; }
            values.Add(path + "|" + label + "|" + unityEvent.GetPersistentEventCount());
            for (int i = 0; i < unityEvent.GetPersistentEventCount(); i++)
                values.Add(path + "|" + label + "|" + i + "|" +
                    (unityEvent.GetPersistentTarget(i) != null ? unityEvent.GetPersistentTarget(i).GetInstanceID().ToString() : "null") +
                    "|" + unityEvent.GetPersistentMethodName(i));
        }
    }
}
