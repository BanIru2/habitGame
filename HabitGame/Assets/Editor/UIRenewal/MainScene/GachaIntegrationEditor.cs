using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class GachaIntegrationEditor
{
    private const string ScenePath = "Assets/Scenes/MainScene.unity";
    private const string CanvasPrefabPath = "Assets/Resources/UIPrefabs/Canvas.prefab";
    private const string ResultItemPrefabPath = "Assets/Resources/Prefabs/GachaResultItem.prefab";
    private const string FontPath = "Assets/Login/Fonts/NotoSansKR Login SDF.asset";
    private const string CardSpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/HabitItem/panel_rpg_card.png";
    private const string CreamButtonSpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/HabitItem/Button_square_cream.png";
    private const string GoldButtonSpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/HabitItem/Button_square_rpg.png";
    private const string MenuRoot = "Tools/HabitPVP/UI Renewal/MainScene/";

    private static readonly Color32 CreamLight = new Color32(0xFF, 0xF9, 0xEA, 0xFF);
    private static readonly Color32 Brown = new Color32(0x5B, 0x36, 0x39, 0xFF);
    private static readonly Color32 ReadableBrown = new Color32(0x6E, 0x4B, 0x45, 0xFF);
    private static readonly Color32 Gold = new Color32(0xE7, 0xA8, 0x35, 0xFF);
    private static readonly Color32 DimColor = new Color32(45, 28, 24, 180);

    [MenuItem(MenuRoot + "Validate Gacha UI", false, 2260)]
    private static void ValidateMenu()
    {
        Context context = Inspect(true);
        if (!context.IsValid) return;

        List<string> styleErrors = ValidateAppliedStyle(context, false);
        if (styleErrors.Count == 0)
            Debug.Log("[HabitPVP MainScene] Gacha UI 구조/디자인 검증 성공. 변경하지 않았습니다.\n" +
                      BuildDiagnostics(context), context.GachaRoot);
        else
        {
            Debug.Log("[HabitPVP MainScene] Gacha UI 적용 전 구조 검증 성공. 변경하지 않았습니다.\n" +
                      BuildDiagnostics(context), context.GachaRoot);
            foreach (string error in styleErrors)
                Debug.LogWarning("[HabitPVP MainScene] Gacha UI 스타일 미적용/불일치: " + error,
                    context.GachaRoot);
        }
    }

    [MenuItem(MenuRoot + "Dry Run Gacha UI", false, 2261)]
    private static void DryRunMenu()
    {
        Context context = Inspect(true);
        if (!context.IsValid)
        {
            Debug.LogError("[HabitPVP MainScene] Gacha UI Dry Run 중단: 구조 오류를 해결해 주세요.");
            return;
        }

        Debug.Log(
            "[HabitPVP MainScene] Gacha UI Dry Run (변경 없음)\n" +
            "- GachaPanel: 15-D-6A 본문 안전 영역 유지, RPG 카드/버튼 스타일 적용\n" +
            "- ProbabilityPopup: 실제 tier 텍스트 유지, NotoSansKR/골드 카드/Dim/닫기 버튼 정리\n" +
            "- ResultPopup: 1회·10회 공용 4열 Grid, 제목/닫기 버튼, Dim 정리\n" +
            "- GachaResultItem.prefab: 실제 아이콘·이름·티어를 표시하는 카드 하위 UI 추가\n" +
            "- 보호: API/확률/골드/지급 로직, Manager SerializeField, 기존 Button UnityEvent, " +
            "activeSelf, 기존 sibling 순서, TapBar, Canvas.prefab\n" + BuildDiagnostics(context),
            context.GachaRoot);
    }

    [MenuItem(MenuRoot + "Apply Gacha UI", false, 2262)]
    private static void ApplyMenu()
    {
        Context context = Inspect(true);
        if (!context.IsValid) return;

        ProtectionSnapshot snapshot = ProtectionSnapshot.Capture(context);
        byte[] prefabBackup = File.ReadAllBytes(ResultItemPrefabPath);
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply Gacha UI Renewal");

        try
        {
            ApplySceneStyle(context);
            snapshot.AssertUnchanged(context);
            ApplyResultItemPrefab(context);
            snapshot.AssertUnchanged(context);

            List<string> errors = ValidateAppliedStyle(context, true);
            if (errors.Count > 0) throw new InvalidOperationException(string.Join("\n", errors));

            EditorSceneManager.MarkSceneDirty(context.Scene);
            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = context.GachaRoot;
            Debug.Log(
                "[HabitPVP MainScene] Gacha UI 적용 성공. MainScene은 dirty 상태이며 자동 저장하지 않았습니다. " +
                "GachaResultItem.prefab만 별도 Prefab asset으로 저장했습니다. Canvas.prefab은 변경하지 않았습니다.",
                context.GachaRoot);
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(undoGroup);
            File.WriteAllBytes(ResultItemPrefabPath, prefabBackup);
            AssetDatabase.ImportAsset(ResultItemPrefabPath, ImportAssetOptions.ForceUpdate);
            EditorSceneManager.MarkSceneDirty(context.Scene);
            Debug.LogException(exception);
            Debug.LogError(
                "[HabitPVP MainScene] Gacha UI Apply 실패. 이번 실행의 Scene 변경과 결과 Prefab을 복원했습니다.");
        }
    }

    [MenuItem(MenuRoot + "Apply Gacha UI", true)]
    private static bool CanApply()
    {
        Scene scene = SceneManager.GetActiveScene();
        return !EditorApplication.isPlayingOrWillChangePlaymode &&
               scene.IsValid() && scene.isLoaded && scene.path == ScenePath;
    }

    private static Context Inspect(bool log)
    {
        Context context = new Context { Scene = SceneManager.GetActiveScene() };
        if (!context.Scene.IsValid() || !context.Scene.isLoaded)
        {
            context.Errors.Add("활성 Scene이 유효하지 않습니다.");
            return Finish(context, log);
        }
        if (context.Scene.path != ScenePath)
            context.Errors.Add($"MainScene에서만 실행할 수 있습니다. 현재: '{context.Scene.path}'");
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            context.Errors.Add("Play Mode를 종료한 뒤 실행해 주세요.");

        context.Canvas = FindRoot(context.Scene, "Canvas");
        if (context.Canvas == null)
        {
            context.Errors.Add("Scene 루트 Canvas가 없습니다.");
            return Finish(context, log);
        }
        if (PrefabUtility.GetOutermostPrefabInstanceRoot(context.Canvas) != context.Canvas ||
            PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(context.Canvas) != CanvasPrefabPath)
            context.Errors.Add("Canvas가 예상한 Canvas.prefab의 최상위 Scene instance가 아닙니다.");

        context.ShopTap = Find(context.Canvas, "ShopTap")?.gameObject;
        context.GachaRoot = Find(context.ShopTap, "GachaRoot")?.gameObject;
        context.GachaPanel = Find(context.GachaRoot, "GachaPanel")?.gameObject;
        context.GachaImage = Find(context.GachaPanel, "GachaImage")?.GetComponent<Image>();
        context.OneButton = Find(context.GachaPanel, "OneGachaButton")?.GetComponent<Button>();
        context.TenButton = Find(context.GachaPanel, "TenGachaButton")?.GetComponent<Button>();
        context.ProbabilityButton = Find(context.GachaPanel, "ProbabilityButton")?.GetComponent<Button>();
        context.TapBar = Find(context.Canvas, "TapBar")?.gameObject;
        context.GachaManager = FindUnique<GachaManager>(context);
        context.ResultManager = FindUnique<GachaResultPopupManager>(context);
        context.Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        context.CardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CardSpritePath);
        context.CreamButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CreamButtonSpritePath);
        context.GoldButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GoldButtonSpritePath);
        context.ResultItemPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ResultItemPrefabPath);

        Require(context, context.ShopTap, "Canvas/ShopTap");
        Require(context, context.GachaRoot, "Canvas/ShopTap/GachaRoot");
        Require(context, context.GachaPanel, "GachaRoot/GachaPanel");
        Require(context, context.GachaImage, "GachaPanel/GachaImage");
        Require(context, context.OneButton, "GachaPanel/OneGachaButton");
        Require(context, context.TenButton, "GachaPanel/TenGachaButton");
        Require(context, context.ProbabilityButton, "GachaPanel/ProbabilityButton");
        Require(context, context.TapBar, "Canvas/TapBar");
        Require(context, context.GachaManager, "GachaManager");
        Require(context, context.ResultManager, "GachaResultPopupManager");
        Require(context, context.Font, FontPath);
        Require(context, context.CardSprite, CardSpritePath);
        Require(context, context.CreamButtonSprite, CreamButtonSpritePath);
        Require(context, context.GoldButtonSprite, GoldButtonSpritePath);
        Require(context, context.ResultItemPrefab, ResultItemPrefabPath);

        if (context.GachaManager != null) ReadGachaReferences(context);
        if (context.ResultManager != null) ReadResultReferences(context);
        ValidatePrefabScope(context);
        return Finish(context, log);
    }

    private static void ReadGachaReferences(Context context)
    {
        SerializedObject serialized = new SerializedObject(context.GachaManager);
        context.ProbabilityPopup = Reference<GameObject>(serialized, "probabilityPopup");
        context.ProbabilityDim = Reference<GameObject>(serialized, "dim");
        context.ProbabilityCloseButton = Reference<Button>(serialized, "probabilityCloseButton");
        context.TierTexts = new[]
        {
            Reference<TMP_Text>(serialized, "tier1Text"), Reference<TMP_Text>(serialized, "tier2Text"),
            Reference<TMP_Text>(serialized, "tier3Text"), Reference<TMP_Text>(serialized, "tier4Text")
        };

        CheckReference(context, serialized, "gachaPanel", context.GachaPanel);
        CheckReference(context, serialized, "gachaMainImage", context.GachaImage.gameObject);
        CheckReference(context, serialized, "oneGachaButton", context.OneButton);
        CheckReference(context, serialized, "tenGachaButton", context.TenButton);
        CheckReference(context, serialized, "probabilityButton", context.ProbabilityButton);
        CheckReference(context, serialized, "resultPopupManager", context.ResultManager);
        Require(context, context.ProbabilityPopup, "GachaManager/probabilityPopup");
        Require(context, context.ProbabilityDim, "GachaManager/dim");
        Require(context, context.ProbabilityCloseButton, "GachaManager/probabilityCloseButton");
        for (int i = 0; i < context.TierTexts.Length; i++)
            Require(context, context.TierTexts[i], $"GachaManager/tier{i + 1}Text");
    }

    private static void ReadResultReferences(Context context)
    {
        SerializedObject serialized = new SerializedObject(context.ResultManager);
        context.ResultPopup = Reference<GameObject>(serialized, "popupRoot");
        context.ResultDim = Reference<GameObject>(serialized, "dim");
        context.ResultRect = Reference<RectTransform>(serialized, "popupRect");
        context.ResultContainer = Reference<RectTransform>(serialized, "resultContainer");
        context.ResultGrid = Reference<GridLayoutGroup>(serialized, "resultGridLayout");
        GameObject prefab = Reference<GameObject>(serialized, "resultItemPrefab");
        Require(context, context.ResultPopup, "GachaResultPopupManager/popupRoot");
        Require(context, context.ResultDim, "GachaResultPopupManager/dim");
        Require(context, context.ResultRect, "GachaResultPopupManager/popupRect");
        Require(context, context.ResultContainer, "GachaResultPopupManager/resultContainer");
        Require(context, context.ResultGrid, "GachaResultPopupManager/resultGridLayout");
        if (prefab != context.ResultItemPrefab)
            context.Errors.Add("GachaResultPopupManager/resultItemPrefab이 예상 Prefab과 다릅니다.");
    }

    private static void ValidatePrefabScope(Context context)
    {
        if (context.GachaRoot != null &&
            PrefabUtility.GetOutermostPrefabInstanceRoot(context.GachaRoot) != context.Canvas)
            context.Errors.Add("GachaRoot가 Canvas Prefab instance 밖에 있습니다.");
        if (context.ResultItemPrefab != null &&
            AssetDatabase.GetAssetPath(context.ResultItemPrefab) != ResultItemPrefabPath)
            context.Errors.Add("결과 아이템 Prefab 경로가 예상과 다릅니다.");
    }

    private static void ApplySceneStyle(Context context)
    {
        StyleImage(GetOrAdd<Image>(context.GachaPanel), context.CardSprite, CreamLight, Image.Type.Sliced);
        SetInsetStretch(context.GachaPanel.GetComponent<RectTransform>(), 40f, 40f, 450f, 210f);

        Record(context.GachaImage);
        context.GachaImage.preserveAspect = true;
        context.GachaImage.enabled = context.GachaImage.sprite != null;
        SetCentered(context.GachaImage.rectTransform, new Vector2(620f, 570f), new Vector2(0f, 210f));

        StyleButton(context.OneButton, "1회 가챠\n500 G", context.GoldButtonSprite,
            context.Font, new Vector2(420f, 125f), new Vector2(-235f, -275f), 32f);
        StyleButton(context.TenButton, "10회 가챠\n5,000 G", context.GoldButtonSprite,
            context.Font, new Vector2(420f, 125f), new Vector2(235f, -275f), 32f);
        StyleButton(context.ProbabilityButton, "확률 보기", context.CreamButtonSprite,
            context.Font, new Vector2(420f, 100f), new Vector2(0f, -430f), 31f);

        StyleImage(GetOrAdd<Image>(context.ProbabilityPopup), context.CardSprite, CreamLight, Image.Type.Sliced);
        SetCentered(context.ProbabilityPopup.GetComponent<RectTransform>(), new Vector2(900f, 1120f), Vector2.zero);
        TMP_Text probabilityTitle = Find(context.ProbabilityPopup, "AchievementText")?.GetComponent<TMP_Text>();
        if (probabilityTitle != null)
        {
            StyleText(probabilityTitle, "장비 가챠 확률 안내", context.Font, 42f, Brown, FontStyles.Bold);
            SetCentered(probabilityTitle.rectTransform, new Vector2(760f, 100f), new Vector2(0f, 465f));
        }
        Transform probabilityContent = Find(context.ProbabilityPopup, "Content");
        if (probabilityContent != null)
        {
            SetCentered(probabilityContent.GetComponent<RectTransform>(), new Vector2(760f, 690f), Vector2.zero);
            VerticalLayoutGroup layout = GetOrAdd<VerticalLayoutGroup>(probabilityContent.gameObject);
            Record(layout);
            layout.padding = new RectOffset(24, 24, 24, 24);
            layout.spacing = 18f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = true;
        }
        foreach (TMP_Text text in context.TierTexts)
        {
            StyleText(text, null, context.Font, 34f, ReadableBrown, FontStyles.Bold);
            text.alignment = TextAlignmentOptions.Center;
            text.enableWordWrapping = false;
        }
        StyleButton(context.ProbabilityCloseButton, "닫기", context.GoldButtonSprite,
            context.Font, new Vector2(340f, 105f), new Vector2(0f, -470f), 31f);
        StyleDim(context.ProbabilityDim);

        StyleImage(GetOrAdd<Image>(context.ResultPopup), context.CardSprite, CreamLight, Image.Type.Sliced);
        SetCentered(context.ResultRect, new Vector2(960f, 1180f), Vector2.zero);
        Record(context.ResultGrid);
        context.ResultGrid.padding = new RectOffset(64, 64, 175, 175);
        context.ResultGrid.spacing = new Vector2(24f, 24f);
        context.ResultGrid.cellSize = new Vector2(190f, 240f);
        context.ResultGrid.childAlignment = TextAnchor.MiddleCenter;
        context.ResultGrid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        context.ResultGrid.constraintCount = 4;
        StyleDim(context.ResultDim);
        EnsureResultOverlay(context);
    }

    private static void EnsureResultOverlay(Context context)
    {
        TMP_Text title = EnsureText(context.ResultPopup.transform, "ResultTitle", context.Font);
        StyleText(title, "가챠 결과", context.Font, 44f, Brown, FontStyles.Bold);
        SetTopOverlay(title.gameObject, 86f, 55f, 90f);

        Button close = EnsureButton(context.ResultPopup.transform, "CloseButton");
        if (close.GetComponent<GachaResultCloseButton>() == null)
            Undo.AddComponent<GachaResultCloseButton>(close.gameObject);
        LayoutElement layout = GetOrAdd<LayoutElement>(close.gameObject);
        Record(layout);
        layout.ignoreLayout = true;
        LayoutElement titleLayout = GetOrAdd<LayoutElement>(title.gameObject);
        Record(titleLayout);
        titleLayout.ignoreLayout = true;

        StyleButton(close, "확인", context.GoldButtonSprite, context.Font,
            new Vector2(340f, 105f), new Vector2(0f, -485f), 31f);
        CenterButtonText(close);
        LayoutRebuilder.ForceRebuildLayoutImmediate(context.ResultContainer);
        SetCentered(close.GetComponent<RectTransform>(),
            new Vector2(340f, 105f), new Vector2(0f, -485f));
    }

    private static void ApplyResultItemPrefab(Context context)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(ResultItemPrefabPath);
        try
        {
            if (root.GetComponent<GachaResultItem>() == null)
                throw new InvalidOperationException("GachaResultItem.prefab에 GachaResultItem이 없습니다.");

            Image frame = root.GetComponent<Image>();
            if (frame == null) throw new InvalidOperationException("GachaResultItem.prefab 루트 Image가 없습니다.");
            frame.sprite = context.CardSprite;
            frame.type = Image.Type.Sliced;
            frame.color = Color.white;

            Image icon = EnsurePrefabImage(root.transform, "ItemIcon");
            SetPrefabRect(icon.rectTransform, new Vector2(132f, 132f), new Vector2(0f, 36f));
            icon.preserveAspect = true;
            icon.raycastTarget = false;

            TMP_Text nameText = EnsurePrefabText(root.transform, "ItemNameText", context.Font);
            nameText.font = context.Font;
            nameText.fontSize = 22f;
            nameText.fontStyle = FontStyles.Bold;
            nameText.color = Brown;
            nameText.alignment = TextAlignmentOptions.Center;
            nameText.enableWordWrapping = false;
            nameText.overflowMode = TextOverflowModes.Ellipsis;
            SetPrefabRect(nameText.rectTransform, new Vector2(166f, 34f), new Vector2(0f, -57f));

            TMP_Text tierText = EnsurePrefabText(root.transform, "ItemTierText", context.Font);
            tierText.font = context.Font;
            tierText.fontSize = 20f;
            tierText.fontStyle = FontStyles.Bold;
            tierText.color = Gold;
            tierText.alignment = TextAlignmentOptions.Center;
            tierText.enableWordWrapping = false;
            SetPrefabRect(tierText.rectTransform, new Vector2(166f, 30f), new Vector2(0f, -92f));

            GachaResultItemPresentation presentation =
                root.GetComponent<GachaResultItemPresentation>() ?? root.AddComponent<GachaResultItemPresentation>();
            SerializedObject serialized = new SerializedObject(presentation);
            serialized.FindProperty("frameSprite").objectReferenceValue = context.CardSprite;
            serialized.FindProperty("itemIcon").objectReferenceValue = icon;
            serialized.FindProperty("itemNameText").objectReferenceValue = nameText;
            serialized.FindProperty("itemTierText").objectReferenceValue = tierText;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, ResultItemPrefabPath, out bool saved);
            if (!saved) throw new InvalidOperationException("GachaResultItem.prefab 저장에 실패했습니다.");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static List<string> ValidateAppliedStyle(Context context, bool requirePrefabStyle)
    {
        List<string> errors = new List<string>();
        if (!context.IsValid) return new List<string>(context.Errors);
        if (!MatchesInsetStretch(context.GachaPanel.GetComponent<RectTransform>(), 40f, 40f, 450f, 210f))
            errors.Add("GachaPanel이 15-D-6A 본문 안전 영역을 유지하지 않습니다.");
        if (context.OneButton.GetComponentInChildren<TMP_Text>(true)?.font != context.Font ||
            context.TenButton.GetComponentInChildren<TMP_Text>(true)?.font != context.Font ||
            context.ProbabilityButton.GetComponentInChildren<TMP_Text>(true)?.font != context.Font)
            errors.Add("가챠 버튼에 NotoSansKR TMP Font가 적용되지 않았습니다.");
        if (context.ProbabilityPopup.GetComponent<Image>()?.sprite != context.CardSprite)
            errors.Add("확률 팝업에 RPG 카드 Sprite가 적용되지 않았습니다.");
        if (context.TierTexts.Any(text => text == null || text.font != context.Font || text.fontSize < 30f))
            errors.Add("확률 표 텍스트의 폰트 또는 크기가 올바르지 않습니다.");
        if (context.ResultPopup.GetComponent<Image>()?.sprite != context.CardSprite ||
            context.ResultGrid.constraint != GridLayoutGroup.Constraint.FixedColumnCount ||
            context.ResultGrid.constraintCount != 4)
            errors.Add("결과 팝업 또는 4열 결과 Grid 스타일이 올바르지 않습니다.");
        if (Find(context.ResultPopup, "ResultTitle") == null || Find(context.ResultPopup, "CloseButton") == null)
            errors.Add("결과 팝업 제목 또는 확인 버튼이 없습니다.");
        RectTransform resultCloseRect = Find(context.ResultPopup, "CloseButton")?.GetComponent<RectTransform>();
        LayoutElement resultCloseLayout = resultCloseRect != null
            ? resultCloseRect.GetComponent<LayoutElement>()
            : null;
        if (resultCloseRect == null || resultCloseLayout == null || !resultCloseLayout.ignoreLayout ||
            !Approximately(resultCloseRect.anchorMin, new Vector2(0.5f, 0.5f)) ||
            !Approximately(resultCloseRect.anchorMax, new Vector2(0.5f, 0.5f)) ||
            !Approximately(resultCloseRect.pivot, new Vector2(0.5f, 0.5f)) ||
            !Approximately(resultCloseRect.anchoredPosition, new Vector2(0f, -485f)) ||
            !Approximately(resultCloseRect.sizeDelta, new Vector2(340f, 105f)))
            errors.Add("결과 확인 버튼이 GridLayout에서 제외된 팝업 하단 중앙 배치가 아닙니다.");
        TMP_Text[] resultCloseTexts = resultCloseRect != null
            ? resultCloseRect.GetComponentsInChildren<TMP_Text>(true)
            : Array.Empty<TMP_Text>();
        if (resultCloseTexts.Length != 1)
            errors.Add($"결과 확인 버튼 내부 TMP_Text는 정확히 1개여야 합니다. 현재: {resultCloseTexts.Length}");
        else
        {
            TMP_Text closeText = resultCloseTexts[0];
            RectTransform closeTextRect = closeText.rectTransform;
            if (!Approximately(closeTextRect.anchorMin, Vector2.zero) ||
                !Approximately(closeTextRect.anchorMax, Vector2.one) ||
                !Approximately(closeTextRect.pivot, new Vector2(0.5f, 0.5f)) ||
                !Approximately(closeTextRect.anchoredPosition, Vector2.zero) ||
                !Approximately(closeTextRect.offsetMin, Vector2.zero) ||
                !Approximately(closeTextRect.offsetMax, Vector2.zero) ||
                closeText.alignment != TextAlignmentOptions.Center ||
                closeText.margin.sqrMagnitude > 0.001f)
                errors.Add("결과 확인 버튼 내부 텍스트가 버튼 전체 영역의 가로·세로 중앙 정렬이 아닙니다.");
        }
        if (context.ProbabilityDim.GetComponent<Image>()?.raycastTarget != true ||
            context.ResultDim.GetComponent<Image>()?.raycastTarget != true)
            errors.Add("Popup Dim이 입력을 차단하도록 설정되지 않았습니다.");
        if (context.TapBar == null || !context.TapBar.activeSelf)
            errors.Add("하단 TapBar가 없거나 초기 상태가 비활성입니다.");
        if (requirePrefabStyle)
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ResultItemPrefabPath);
            if (prefab == null || prefab.GetComponent<GachaResultItem>() == null ||
                prefab.GetComponent<GachaResultItemPresentation>() == null ||
                prefab.transform.Find("ItemIcon") == null || prefab.transform.Find("ItemNameText") == null ||
                prefab.transform.Find("ItemTierText") == null)
                errors.Add("GachaResultItem.prefab의 실제 아이콘/이름/티어 카드 구성이 없습니다.");
        }
        return errors;
    }

    private static string BuildDiagnostics(Context context)
    {
        return "Hierarchy: Canvas/ShopTap/GachaRoot/GachaPanel/{GachaImage, OneGachaButton, " +
               "TenGachaButton, ProbabilityButton, ProbabilityPopup, ResultPopup}\n" +
               "Result manager: " + GetPath(context.ResultManager.transform) +
               ", container=" + GetPath(context.ResultContainer) +
               ", prefab=" + AssetDatabase.GetAssetPath(context.ResultItemPrefab) + "\n" +
               "Initial state: GachaRoot=" + context.GachaRoot.activeSelf +
               ", ProbabilityPopup=" + context.ProbabilityPopup.activeSelf +
               ", ResultPopup=" + context.ResultPopup.activeSelf +
               ", TapBar=" + context.TapBar.activeSelf;
    }

    private static Context Finish(Context context, bool log)
    {
        if (log)
            foreach (string error in context.Errors)
                Debug.LogError("[HabitPVP MainScene] Gacha UI: " + error);
        return context;
    }

    private static void Require(Context context, UnityEngine.Object value, string label)
    {
        if (value == null) context.Errors.Add("필수 대상이 없습니다: " + label);
    }

    private static T FindUnique<T>(Context context) where T : Component
    {
        T[] values = context.Scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        if (values.Length != 1)
            context.Errors.Add(typeof(T).Name + $"가 Scene에 정확히 1개여야 합니다. 현재: {values.Length}");
        return values.Length == 1 ? values[0] : null;
    }

    private static GameObject FindRoot(Scene scene, string name)
    {
        return scene.GetRootGameObjects().FirstOrDefault(root => root.name == name);
    }

    private static Transform Find(GameObject root, string path)
    {
        return root == null ? null : root.transform.Find(path);
    }

    private static T Reference<T>(SerializedObject serialized, string name) where T : UnityEngine.Object
    {
        SerializedProperty property = serialized.FindProperty(name);
        return property == null ? null : property.objectReferenceValue as T;
    }

    private static void CheckReference(Context context, SerializedObject serialized, string name,
        UnityEngine.Object expected)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property == null) context.Errors.Add("SerializeField가 없습니다: " + name);
        else if (property.objectReferenceValue != expected)
            context.Errors.Add("SerializeField 참조가 Hierarchy와 다릅니다: " + name);
    }

    private static void StyleButton(Button button, string label, Sprite sprite, TMP_FontAsset font,
        Vector2 size, Vector2 position, float fontSize)
    {
        StyleImage(button.GetComponent<Image>(), sprite, Color.white, Image.Type.Sliced);
        SetCentered(button.GetComponent<RectTransform>(), size, position);
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        StyleText(text, label, font, fontSize, Brown, FontStyles.Bold);
        SetStretch(text.rectTransform, new Vector2(18f, 9f), new Vector2(-18f, -9f));
        Record(button);
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color32(0xFF, 0xF4, 0xC9, 0xFF);
        colors.pressedColor = new Color32(0xD8, 0xB5, 0x6C, 0xFF);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color32(0xD7, 0xCC, 0xBA, 0xFF);
        button.colors = colors;
        RecordPrefabModification(button);
    }

    private static void CenterButtonText(Button button)
    {
        TMP_Text[] texts = button.GetComponentsInChildren<TMP_Text>(true);
        if (texts.Length != 1)
            throw new InvalidOperationException(
                $"결과 확인 버튼 내부 TMP_Text는 정확히 1개여야 합니다. 현재: {texts.Length}");

        TMP_Text text = texts[0];
        RectTransform rect = text.rectTransform;
        Record(text);
        Record(rect);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.anchoredPosition = Vector2.zero;
        text.alignment = TextAlignmentOptions.Center;
        text.margin = Vector4.zero;
        RecordPrefabModification(rect);
        RecordPrefabModification(text);
    }

    private static void StyleDim(GameObject dim)
    {
        Image image = GetOrAdd<Image>(dim);
        Record(image);
        image.color = DimColor;
        image.raycastTarget = true;
        RecordPrefabModification(image);
        SetStretch(dim.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
    }

    private static Button EnsureButton(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        GameObject gameObject;
        if (existing == null)
        {
            gameObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            Undo.RegisterCreatedObjectUndo(gameObject, "Create Gacha result close button");
            gameObject.transform.SetParent(parent, false);
        }
        else gameObject = existing.gameObject;

        Button button = gameObject.GetComponent<Button>() ?? Undo.AddComponent<Button>(gameObject);
        TMP_Text text = gameObject.GetComponentInChildren<TMP_Text>(true);
        if (text == null) text = EnsureText(gameObject.transform, "Text", null);
        return button;
    }

    private static TMP_Text EnsureText(Transform parent, string name, TMP_FontAsset font)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
        {
            TMP_Text current = existing.GetComponent<TMP_Text>();
            if (current != null) return current;
        }
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        Undo.RegisterCreatedObjectUndo(gameObject, "Create Gacha popup text");
        gameObject.transform.SetParent(parent, false);
        TMP_Text text = gameObject.GetComponent<TMP_Text>();
        text.font = font;
        text.raycastTarget = false;
        return text;
    }

    private static Image EnsurePrefabImage(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null) return existing.GetComponent<Image>() ?? existing.gameObject.AddComponent<Image>();
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(Image));
        gameObject.transform.SetParent(parent, false);
        return gameObject.GetComponent<Image>();
    }

    private static TMP_Text EnsurePrefabText(Transform parent, string name, TMP_FontAsset font)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            return existing.GetComponent<TMP_Text>() ?? existing.gameObject.AddComponent<TextMeshProUGUI>();
        GameObject gameObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        gameObject.transform.SetParent(parent, false);
        TMP_Text text = gameObject.GetComponent<TMP_Text>();
        text.font = font;
        text.raycastTarget = false;
        return text;
    }

    private static void SetTopOverlay(GameObject target, float height, float top, float side)
    {
        RectTransform rect = target.GetComponent<RectTransform>();
        Record(rect);
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.offsetMin = new Vector2(side, -top - height);
        rect.offsetMax = new Vector2(-side, -top);
        RecordPrefabModification(rect);
    }

    private static void SetPrefabRect(RectTransform rect, Vector2 size, Vector2 position)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
    }

    private static void StyleImage(Image image, Sprite sprite, Color color, Image.Type type)
    {
        Record(image);
        image.sprite = sprite;
        image.color = color;
        image.type = type;
        RecordPrefabModification(image);
    }

    private static void StyleText(TMP_Text text, string value, TMP_FontAsset font, float size,
        Color color, FontStyles style)
    {
        if (text == null) return;
        Record(text);
        if (value != null) text.text = value;
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.fontStyle = style;
        text.overflowMode = TextOverflowModes.Ellipsis;
        RecordPrefabModification(text);
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(target);
    }

    private static void SetCentered(RectTransform rect, Vector2 size, Vector2 position)
    {
        Record(rect);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        RecordPrefabModification(rect);
    }

    private static void SetStretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        Record(rect);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = min;
        rect.offsetMax = max;
        RecordPrefabModification(rect);
    }

    private static void SetInsetStretch(RectTransform rect, float left, float right, float top, float bottom)
    {
        Record(rect);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = new Vector2(left, bottom);
        rect.offsetMax = new Vector2(-right, -top);
        RecordPrefabModification(rect);
    }

    private static bool MatchesInsetStretch(RectTransform rect, float left, float right, float top,
        float bottom)
    {
        return rect != null && Approximately(rect.anchorMin, Vector2.zero) &&
               Approximately(rect.anchorMax, Vector2.one) &&
               Approximately(rect.offsetMin, new Vector2(left, bottom)) &&
               Approximately(rect.offsetMax, new Vector2(-right, -top));
    }

    private static bool Approximately(Vector2 left, Vector2 right)
    {
        return Mathf.Approximately(left.x, right.x) && Mathf.Approximately(left.y, right.y);
    }

    private static void Record(UnityEngine.Object target)
    {
        if (target != null) Undo.RecordObject(target, "Apply Gacha UI Renewal");
    }

    private static void RecordPrefabModification(UnityEngine.Object target)
    {
        if (target != null && PrefabUtility.IsPartOfPrefabInstance(target))
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        if (target != null) EditorUtility.SetDirty(target);
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

    private sealed class ProtectionSnapshot
    {
        private readonly string gachaReferences;
        private readonly string resultReferences;
        private readonly Dictionary<int, string> buttonEvents;
        private readonly Dictionary<int, bool> activeStates;
        private readonly Dictionary<int, int> siblingIndices;

        private ProtectionSnapshot(Context context)
        {
            gachaReferences = CaptureObjectReferences(context.GachaManager);
            resultReferences = CaptureObjectReferences(context.ResultManager);
            buttonEvents = context.Canvas.GetComponentsInChildren<Button>(true)
                .ToDictionary(button => button.GetInstanceID(), button => CaptureEvent(button.onClick));
            Transform[] transforms = context.GachaRoot.GetComponentsInChildren<Transform>(true)
                .Concat(context.ResultManager.GetComponentsInChildren<Transform>(true)).Distinct().ToArray();
            activeStates = transforms.ToDictionary(item => item.GetInstanceID(), item => item.gameObject.activeSelf);
            siblingIndices = transforms.ToDictionary(item => item.GetInstanceID(), item => item.GetSiblingIndex());
        }

        public static ProtectionSnapshot Capture(Context context) => new ProtectionSnapshot(context);

        public void AssertUnchanged(Context context)
        {
            if (gachaReferences != CaptureObjectReferences(context.GachaManager) ||
                resultReferences != CaptureObjectReferences(context.ResultManager))
                throw new InvalidOperationException("가챠 Manager SerializeField 참조가 변경되었습니다.");
            foreach (Button button in context.Canvas.GetComponentsInChildren<Button>(true))
                if (buttonEvents.TryGetValue(button.GetInstanceID(), out string before) &&
                    before != CaptureEvent(button.onClick))
                    throw new InvalidOperationException("기존 Button UnityEvent가 변경되었습니다: " + GetPath(button.transform));
            foreach (Transform item in context.GachaRoot.GetComponentsInChildren<Transform>(true)
                         .Concat(context.ResultManager.GetComponentsInChildren<Transform>(true)).Distinct())
            {
                int id = item.GetInstanceID();
                if (activeStates.TryGetValue(id, out bool active) && active != item.gameObject.activeSelf)
                    throw new InvalidOperationException("기존 activeSelf가 변경되었습니다: " + GetPath(item));
                if (siblingIndices.TryGetValue(id, out int sibling) && sibling != item.GetSiblingIndex())
                    throw new InvalidOperationException("기존 sibling 순서가 변경되었습니다: " + GetPath(item));
            }
        }

        private static string CaptureObjectReferences(UnityEngine.Object target)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty iterator = serialized.GetIterator();
            List<string> values = new List<string>();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.propertyType == SerializedPropertyType.ObjectReference)
                    values.Add(iterator.propertyPath + "=" +
                               (iterator.objectReferenceValue == null ? "null" :
                                   GlobalObjectId.GetGlobalObjectIdSlow(iterator.objectReferenceValue).ToString()));
            }
            return string.Join("|", values);
        }

        private static string CaptureEvent(UnityEventBase value)
        {
            return string.Join(",", Enumerable.Range(0, value.GetPersistentEventCount())
                .Select(index =>
                {
                    UnityEngine.Object target = value.GetPersistentTarget(index);
                    return (target == null ? "null" : GlobalObjectId.GetGlobalObjectIdSlow(target).ToString()) +
                           ":" + value.GetPersistentMethodName(index);
                }));
        }
    }

    private sealed class Context
    {
        public Scene Scene;
        public readonly List<string> Errors = new List<string>();
        public bool IsValid => Errors.Count == 0;
        public GameObject Canvas, ShopTap, GachaRoot, GachaPanel, TapBar;
        public Image GachaImage;
        public Button OneButton, TenButton, ProbabilityButton, ProbabilityCloseButton;
        public GachaManager GachaManager;
        public GachaResultPopupManager ResultManager;
        public GameObject ProbabilityPopup, ProbabilityDim, ResultPopup, ResultDim, ResultItemPrefab;
        public TMP_Text[] TierTexts = Array.Empty<TMP_Text>();
        public RectTransform ResultRect, ResultContainer;
        public GridLayoutGroup ResultGrid;
        public TMP_FontAsset Font;
        public Sprite CardSprite, CreamButtonSprite, GoldButtonSprite;
    }
}
