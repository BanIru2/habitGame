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

public static class HabitPvpUiStyler
{
    private const string ScenePath = "Assets/Scenes/UITestScene.unity";
    private const string CanvasPrefabPath = "Assets/Resources/UIPrefabs/Canvas.prefab";
    private const string KoreanFontPath = "Assets/Login/Fonts/NotoSansKR Login SDF.asset";
    private const string SpriteSheetPath =
        "Assets/Skyden_Games/Free_Casual_GUI/Demo/Sprites/Buttons/Buttons.png";
    private const string ValidateMenu = "Tools/HabitPVP/UI Renewal/Validate";
    private const string ApplyMenu = "Tools/HabitPVP/UI Renewal/Apply UITestScene";
    private const string RestoreMenu = "Tools/HabitPVP/UI Renewal/Restore UITestScene";
    private const string ValidatePanelsMenu = "Tools/HabitPVP/UI Renewal/Validate Panels";
    private const string ApplyPanelsMenu = "Tools/HabitPVP/UI Renewal/Apply Panels";
    private const string GeneratedSpriteRoot = "Assets/UIRenewal/Generated/FreeCasualGUI/";
    private const string SummaryPanelSpritePath = GeneratedSpriteRoot + "panel_type4.png";
    private const string ListPanelSpritePath = GeneratedSpriteRoot + "panel_type1.png";
    private const string StreakPanelSpritePath = GeneratedSpriteRoot + "Pannel_time_orange.png";
    private const string ProgressBackgroundSpritePath =
        "Assets/Skyden_Games/Free_Casual_GUI/Demo/Sprites/Bars/Bar.png";
    private const string ProgressFillSpritePath =
        "Assets/Skyden_Games/Free_Casual_GUI/Demo/Sprites/Bars/Bar_Fill_02.png";
    private const string SummaryCardPath = "HabitTap/Life/HabitSummaryCard";
    private const string ListCardPath = "HabitTap/Life/HabitListCard";
    private const string ListTitlePath = ListCardPath + "/HabitListHeader/ListTitle";
    private const string ProgressBarPath = SummaryCardPath + "/HabitProgressBar";
    private const string StreakBackgroundName = "UIRenewal_StreakBackground";
    private const int SnapshotVersion = 2;

    private static readonly Color AiTextColor = new Color32(0x5B, 0x36, 0x39, 0xFF);
    private static readonly Color SummaryTextColor = new Color32(0x5B, 0x36, 0x39, 0xFF);
    private static readonly Color ProgressTextColor = new Color32(0x2F, 0x73, 0xD9, 0xFF);
    private static readonly RectOffset ContentPadding = new RectOffset(18, 18, 14, 24);
    private const float ContentSpacing = 15f;
    private const float ListTitleFontSize = 44f;
    private const float SummaryCardHeight = 300f;
    private static readonly Vector4 PanelSpriteBorder = new Vector4(96f, 96f, 96f, 96f);
    private static readonly Vector4 StreakSpriteBorder = new Vector4(80f, 70f, 80f, 70f);
    private static readonly Vector2 SummaryTitleAnchor = new Vector2(0f, 1f);
    private static readonly Vector2 SummaryTitlePosition = new Vector2(40f, -28f);
    private static readonly Vector2 SummaryTitleSize = new Vector2(620f, 52f);
    private static readonly Vector2 SummaryTitlePivot = new Vector2(0f, 1f);
    private static readonly Vector2 SummaryCountAnchor = new Vector2(1f, 1f);
    private static readonly Vector2 SummaryCountPosition = new Vector2(-40f, -28f);
    private static readonly Vector2 SummaryCountSize = new Vector2(260f, 52f);
    private static readonly Vector2 SummaryCountPivot = new Vector2(1f, 1f);
    private static readonly Vector2 ProgressAnchorMin = new Vector2(0.1f, 0.5f);
    private static readonly Vector2 ProgressAnchorMax = new Vector2(0.9f, 0.5f);
    private static readonly Vector2 ProgressPosition = new Vector2(0f, 5f);
    private static readonly Vector2 ProgressSize = new Vector2(0f, 64f);
    private static readonly Vector2 TrackAnchorMin = new Vector2(0f, 0.25f);
    private static readonly Vector2 TrackAnchorMax = new Vector2(1f, 0.75f);
    private static readonly Vector2 StreakBackgroundAnchor = new Vector2(0.5f, 0f);
    private static readonly Vector2 StreakBackgroundPosition = new Vector2(0f, 5f);
    private static readonly Vector2 StreakBackgroundSize = new Vector2(240f, 240f * 184f / 402f);
    private static readonly Vector2 StreakBackgroundPivot = new Vector2(0.5f, 0f);
    private static readonly Vector2 StreakTextAnchor = new Vector2(0.5f, 0f);
    private static readonly Vector2 StreakTextPosition = new Vector2(-24f, 60f);
    private static readonly Vector2 StreakTextSize = new Vector2(158f, 44f);
    private static readonly Vector2 StreakTextPivot = new Vector2(0.5f, 0.5f);
    private const float StreakFontSize = 26f;
    private const float StreakMinimumFontSize = 20f;
    private static readonly Definition[] Definitions =
    {
        new Definition("생활 습관 버튼", "HabitTap/TopTab/lifebutton", "Buttons_015", Style.TopTab),
        new Definition("소비 습관 버튼", "HabitTap/TopTab/spendbutton", "Buttons_019", Style.TopTab),
        new Definition(
            "습관 추가 버튼",
            "HabitTap/Life/HabitListCard/HabitListHeader/AddButton",
            "Buttons_02",
            Style.AddHabit),
        new Definition("AI 리포트 버튼", "HabitTap/Life/WeeklyReport", "Buttons_013", Style.AiReport)
    };

    private static string SnapshotPath => Path.GetFullPath(
        Path.Combine(Application.dataPath, "../Library/HabitPVP/UITestSceneUiRenewalSnapshot.json"));

    [MenuItem(ValidateMenu, false, 2000)]
    private static void Validate()
    {
        Result result = ValidateProject(true);
        if (result.IsValid)
        {
            Debug.Log(
                "[HabitPVP UI Renewal] Validate 성공. Apply는 실행 직전에 같은 검증을 다시 수행합니다.");

            if (!TryLoadSnapshot(result, out _, out string reason))
            {
                Debug.LogWarning(
                    $"[HabitPVP UI Renewal] Restore 메뉴 비활성: {reason} " +
                    "Apply 성공 시 직전 상태를 저장한 뒤 활성화됩니다.");
            }
            else if (!result.Targets.All(IsApplied))
            {
                Debug.LogWarning(
                    "[HabitPVP UI Renewal] Restore 메뉴 비활성: 대상이 Apply 결과와 달라 " +
                    "사용자 변경을 보호해야 합니다.");
            }
        }
    }

    [MenuItem(ApplyMenu, false, 2001)]
    private static void Apply()
    {
        Result result = ValidateProject(true);
        if (!result.IsValid)
        {
            Debug.LogError("[HabitPVP UI Renewal] Apply 중단: 사전 검증 오류를 먼저 해결해 주세요.");
            return;
        }

        if (result.Targets.All(IsApplied))
        {
            Debug.Log("[HabitPVP UI Renewal] 대상이 이미 요청된 디자인과 일치하여 변경하지 않았습니다.");
            return;
        }

        RunApply(result);
    }

    [MenuItem(ApplyMenu, true)]
    private static bool CanApply()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode && ValidateProject(false).IsValid;
    }

    [MenuItem(RestoreMenu, false, 2002)]
    private static void Restore()
    {
        Result result = ValidateProject(true);
        if (!result.IsValid)
        {
            Debug.LogError("[HabitPVP UI Renewal] Restore 중단: Scene 검증 오류를 먼저 해결해 주세요.");
            return;
        }

        if (!TryLoadSnapshot(result, out RenewalSnapshot snapshot, out string reason))
        {
            Debug.LogError($"[HabitPVP UI Renewal] Restore를 실행할 수 없습니다: {reason}");
            return;
        }

        if (!result.Targets.All(IsApplied))
        {
            Debug.LogError(
                "[HabitPVP UI Renewal] Restore 중단: Apply 이후 대상 디자인 속성이 변경되었습니다. " +
                "사용자 변경을 보호하기 위해 자동 복원하지 않습니다.");
            return;
        }

        RunRestore(result, snapshot);
    }

    [MenuItem(RestoreMenu, true)]
    private static bool CanRestore()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return false;
        }

        Result result = ValidateProject(false);
        return result.IsValid && result.Targets.All(IsApplied) &&
               TryLoadSnapshot(result, out _, out _);
    }

    [MenuItem(ValidatePanelsMenu, false, 2010)]
    private static void ValidatePanels()
    {
        PanelResult result = ValidatePanelsProject(true);
        if (result.IsValid)
        {
            if (ArePanelsApplied(result))
            {
                Debug.Log(
                    "[HabitPVP UI Renewal] Validate Panels 성공. 요약 카드 영역이 서로 겹치지 않고 " +
                    "패널, 진행률 바, Manager 및 ScrollRect/Layout 참조가 유효합니다.");
            }
            else
            {
                Debug.LogWarning(
                    "[HabitPVP UI Renewal] 구조 검증은 성공했지만 현재 Scene Override가 최종 요약 카드 " +
                    "레이아웃과 다릅니다. Apply Panels를 실행해 주세요.");
            }
        }
    }

    [MenuItem(ApplyPanelsMenu, false, 2011)]
    private static void ApplyPanels()
    {
        PanelResult result = ValidatePanelsProject(true);
        if (!result.IsValid)
        {
            Debug.LogError("[HabitPVP UI Renewal] Apply Panels 중단: 사전 검증 오류를 먼저 해결해 주세요.");
            return;
        }

        if (ArePanelsApplied(result))
        {
            Debug.Log("[HabitPVP UI Renewal] 패널과 진행률 바가 이미 요청된 디자인과 일치합니다.");
            return;
        }

        RunPanelApply(result);
    }

    [MenuItem(ApplyPanelsMenu, true)]
    private static bool CanApplyPanels()
    {
        return !EditorApplication.isPlayingOrWillChangePlaymode &&
               ValidatePanelsProject(false).IsValid;
    }

    private static PanelResult ValidatePanelsProject(bool log)
    {
        PanelResult result = new PanelResult();
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            result.Errors.Add("활성 Scene이 유효하거나 로드된 상태가 아닙니다.");
            return FinishPanelValidation(result, log);
        }

        if (!string.Equals(scene.path, ScenePath, StringComparison.Ordinal))
        {
            result.Errors.Add($"활성 Scene이 UITestScene이 아닙니다. 현재 경로: '{scene.path}'");
            return FinishPanelValidation(result, log);
        }

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            result.Errors.Add("Play Mode 또는 Play Mode 전환 중에는 실행할 수 없습니다.");
        }

        if (scene.isDirty)
        {
            result.Errors.Add("UITestScene에 저장되지 않은 변경이 있습니다. 먼저 저장해 주세요.");
        }

        GameObject[] canvases = scene.GetRootGameObjects()
            .Where(item => string.Equals(item.name, "Canvas", StringComparison.Ordinal)).ToArray();
        if (canvases.Length != 1)
        {
            result.Errors.Add($"Scene 루트 Canvas가 정확히 하나여야 합니다. 발견 수: {canvases.Length}");
            return FinishPanelValidation(result, log);
        }

        result.Canvas = canvases[0];
        if (!PrefabUtility.IsPartOfPrefabInstance(result.Canvas) ||
            PrefabUtility.GetOutermostPrefabInstanceRoot(result.Canvas) != result.Canvas)
        {
            result.Errors.Add("Canvas가 Prefab 인스턴스의 최상위 루트가 아닙니다.");
        }

        string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(result.Canvas);
        if (!string.Equals(prefabPath, CanvasPrefabPath, StringComparison.Ordinal))
        {
            result.Errors.Add($"Canvas 원본 Prefab 경로가 다릅니다: '{prefabPath}'");
        }

        result.MainTabManager = ValidateManager(scene, result.Errors);
        result.KoreanFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
        if (result.KoreanFont == null)
        {
            result.Errors.Add($"NotoSansKR TMP Font Asset을 불러올 수 없습니다: {KoreanFontPath}");
        }
        result.SummaryPanelSprite = LoadGeneratedSprite(
            SummaryPanelSpritePath, PanelSpriteBorder, "panel_type4", result.Errors);
        result.ListPanelSprite = LoadGeneratedSprite(
            ListPanelSpritePath, PanelSpriteBorder, "panel_type1", result.Errors);
        result.StreakPanelSprite = LoadGeneratedSprite(
            StreakPanelSpritePath, StreakSpriteBorder, "Pannel_time_orange", result.Errors);
        if (result.StreakPanelSprite != null &&
            !Mathf.Approximately(
                result.StreakPanelSprite.rect.width / result.StreakPanelSprite.rect.height,
                402f / 184f))
        {
            result.Errors.Add("Pannel_time_orange Sprite의 원본 비율이 402:184와 다릅니다.");
        }
        result.ProgressBackgroundSprite = LoadSprite(
            ProgressBackgroundSpritePath, "Bar", result.Errors);
        result.ProgressFillSprite = LoadSprite(
            ProgressFillSpritePath, "Bar_Fill_02", result.Errors);

        if (result.ProgressBackgroundSprite != null &&
            !Vec4Equal(result.ProgressBackgroundSprite.border, Vector4.zero))
        {
            result.Errors.Add("Bar Sprite는 border가 없어야 Simple Image로 안전하게 적용할 수 있습니다.");
        }
        if (result.ProgressFillSprite != null &&
            !(result.ProgressFillSprite.border.x > 0f && result.ProgressFillSprite.border.z > 0f))
        {
            result.Errors.Add("Bar_Fill_02 Sprite의 좌우 border가 없어 Sliced Image와 호환되지 않습니다.");
        }

        Transform summary = FindTransform(result.Canvas.transform, SummaryCardPath, result.Errors);
        Transform list = FindTransform(result.Canvas.transform, ListCardPath, result.Errors);
        Transform progress = FindTransform(result.Canvas.transform, ProgressBarPath, result.Errors);
        if (summary == null || list == null || progress == null)
        {
            return FinishPanelValidation(result, log);
        }

        result.SummaryCard = summary.gameObject;
        result.ListCard = list.gameObject;
        result.SummaryRect = summary as RectTransform;
        if (result.SummaryRect == null ||
            !Mathf.Approximately(result.SummaryRect.rect.height, SummaryCardHeight))
        {
            result.Errors.Add($"HabitSummaryCard의 계산 높이가 {SummaryCardHeight}px이어야 합니다.");
        }
        result.SummaryImage = GetUniqueComponent<Image>(summary, SummaryCardPath, result.Errors);
        result.ListImage = GetUniqueComponent<Image>(list, ListCardPath, result.Errors);
        result.ListTitle = GetText(
            FindTransform(result.Canvas.transform, ListTitlePath, result.Errors),
            ListTitlePath, result.Errors);
        result.SummaryTitle = GetText(summary.Find("SummaryTitle"), SummaryCardPath + "/SummaryTitle", result.Errors);
        result.SummaryCount = GetText(summary.Find("SummaryCount"), SummaryCardPath + "/SummaryCount", result.Errors);
        result.SummaryStreak = GetText(summary.Find("SummaryStreak"), SummaryCardPath + "/SummaryStreak", result.Errors);
        if (summary.GetComponent<LayoutGroup>() != null)
        {
            result.Errors.Add("HabitSummaryCard의 LayoutGroup이 진행률 바와 Streak 수동 배치를 덮어쓸 수 있습니다.");
        }

        Transform background = progress.Find("Background");
        Transform fillArea = progress.Find("Fill Area");
        Transform fill = progress.Find("Fill Area/Fill");
        result.ProgressRect = progress as RectTransform;
        result.ProgressBackgroundRect = background as RectTransform;
        result.ProgressFillAreaRect = fillArea as RectTransform;
        result.ProgressBackground = GetUniqueComponent<Image>(
            background, ProgressBarPath + "/Background", result.Errors);
        result.ProgressFill = GetUniqueComponent<Image>(
            fill, ProgressBarPath + "/Fill Area/Fill", result.Errors);
        result.ProgressSlider = GetUniqueComponent<Slider>(progress, ProgressBarPath, result.Errors);
        if (result.ProgressSlider != null && result.ProgressBackground != null && result.ProgressFill != null)
        {
            if (result.ProgressSlider.fillRect != result.ProgressFill.rectTransform)
            {
                result.Errors.Add("HabitProgressBar Slider.fillRect가 Fill RectTransform이 아닙니다.");
            }
            if (result.ProgressSlider.handleRect == null)
            {
                result.Errors.Add("HabitProgressBar Slider.handleRect 참조가 비어 있습니다.");
            }
        }
        if (result.ProgressRect == null || result.ProgressBackgroundRect == null ||
            result.ProgressFillAreaRect == null)
        {
            result.Errors.Add("HabitProgressBar의 시각적 RectTransform 구성이 올바르지 않습니다.");
        }

        ValidateSummaryManager(result, summary);
        ValidateListStructure(result, list);
        ValidatePanelPrefabScope(result, summary, list, progress);

        Transform[] streakBackgrounds = summary.GetComponentsInChildren<Transform>(true)
            .Where(child => child != summary && child.name == StreakBackgroundName).ToArray();
        if (streakBackgrounds.Length > 1)
        {
            result.Errors.Add($"{StreakBackgroundName}이 중복 생성되어 있습니다.");
        }
        else if (streakBackgrounds.Length == 1)
        {
            if (streakBackgrounds[0].parent != summary)
            {
                result.Errors.Add($"{StreakBackgroundName}이 HabitSummaryCard의 직속 자식이 아닙니다.");
            }
            else
            {
                result.StreakBackground = GetUniqueComponent<Image>(
                    streakBackgrounds[0], SummaryCardPath + "/" + StreakBackgroundName,
                    result.Errors);
            }
        }

        return FinishPanelValidation(result, log);
    }

    private static MonoBehaviour ValidateManager(Scene scene, List<string> errors)
    {
        Result result = new Result();
        MonoBehaviour manager = ValidateManager(scene, result);
        errors.AddRange(result.Errors);
        return manager;
    }

    private static Sprite LoadGeneratedSprite(
        string path, Vector4 expectedBorder, string label, List<string> errors)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            errors.Add($"생성 PNG의 TextureImporter를 찾을 수 없습니다: {path}");
            return null;
        }

        if (importer.textureType != TextureImporterType.Sprite ||
            importer.spriteImportMode != SpriteImportMode.Single || importer.mipmapEnabled ||
            !importer.alphaIsTransparency || !Vec4Equal(importer.spriteBorder, expectedBorder))
        {
            errors.Add(
                $"{label} Sprite Import 설정이 올바르지 않습니다. " +
                $"Sprite/Single, Alpha Transparency, Mipmap Off, Border {expectedBorder}가 필요합니다.");
        }

        Sprite sprite = LoadSprite(path, label, errors);
        if (sprite != null && !Vec4Equal(sprite.border, expectedBorder))
        {
            errors.Add($"{label} Sprite border가 예상 값과 다릅니다: {sprite.border}");
        }
        return sprite;
    }

    private static Sprite LoadSprite(string path, string label, List<string> errors)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
        {
            errors.Add($"{label} Sprite를 불러올 수 없습니다: {path}");
        }
        return sprite;
    }

    private static Transform FindTransform(Transform canvas, string path, List<string> errors)
    {
        Transform value = canvas.Find(path);
        if (value == null)
        {
            errors.Add($"대상 경로를 찾을 수 없습니다: Canvas/{path}");
        }
        return value;
    }

    private static T GetUniqueComponent<T>(Transform transform, string path, List<string> errors)
        where T : Component
    {
        if (transform == null)
        {
            errors.Add($"대상 Transform을 찾을 수 없습니다: Canvas/{path}");
            return null;
        }

        T[] components = transform.GetComponents<T>();
        if (components.Length != 1)
        {
            errors.Add($"{typeof(T).Name} 컴포넌트가 정확히 하나여야 합니다: Canvas/{path}");
            return null;
        }
        if (transform.GetComponents<Component>().Any(component => component == null))
        {
            errors.Add($"Missing Script가 있습니다: Canvas/{path}");
            return null;
        }
        return components[0];
    }

    private static TMP_Text GetText(Transform transform, string path, List<string> errors)
    {
        return GetUniqueComponent<TMP_Text>(transform, path, errors);
    }

    private static void ValidateSummaryManager(PanelResult result, Transform summary)
    {
        MonoBehaviour[] managers = summary.GetComponents<MonoBehaviour>().Where(item =>
            item != null && string.Equals(
                item.GetType().Name, "HabitSummaryManager", StringComparison.Ordinal)).ToArray();
        if (managers.Length != 1)
        {
            result.Errors.Add($"HabitSummaryManager가 정확히 하나여야 합니다. 발견 수: {managers.Length}");
            return;
        }

        result.SummaryManager = managers[0];
        SerializedObject serialized = new SerializedObject(result.SummaryManager);
        ValidateReference(serialized, "summaryCountText", result.SummaryCount, result.Errors);
        ValidateReference(serialized, "progressBar", result.ProgressSlider, result.Errors);

        Transform content = result.Canvas.transform.Find(
            ListCardPath + "/HabitScrollView/Viewport/Content");
        result.HabitContent = content;
        ValidateReference(serialized, "content", content, result.Errors);
    }

    private static void ValidateReference(
        SerializedObject serialized, string name, UnityEngine.Object expected, List<string> errors)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property == null || property.objectReferenceValue == null)
        {
            errors.Add($"{serialized.targetObject.GetType().Name} 참조가 비어 있거나 없습니다: {name}");
        }
        else if (expected != null && property.objectReferenceValue != expected)
        {
            errors.Add($"{serialized.targetObject.GetType().Name}.{name} 참조가 예상 대상과 다릅니다.");
        }
    }

    private static void ValidateListStructure(PanelResult result, Transform list)
    {
        result.ListLayout = GetUniqueComponent<VerticalLayoutGroup>(list, ListCardPath, result.Errors);
        Transform scrollTransform = list.Find("HabitScrollView");
        result.HabitScrollRect = GetUniqueComponent<ScrollRect>(
            scrollTransform, ListCardPath + "/HabitScrollView", result.Errors);
        Transform viewport = scrollTransform == null ? null : scrollTransform.Find("Viewport");
        result.ViewportMask = GetUniqueComponent<Mask>(
            viewport, ListCardPath + "/HabitScrollView/Viewport", result.Errors);
        Transform content = viewport == null ? null : viewport.Find("Content");
        result.ContentLayout = GetUniqueComponent<VerticalLayoutGroup>(
            content, ListCardPath + "/HabitScrollView/Viewport/Content", result.Errors);
        result.ContentSizeFitter = GetUniqueComponent<ContentSizeFitter>(
            content, ListCardPath + "/HabitScrollView/Viewport/Content", result.Errors);

        if (result.HabitScrollRect != null)
        {
            if (viewport == null || result.HabitScrollRect.viewport != viewport)
            {
                result.Errors.Add("HabitScrollView의 viewport 참조가 예상 대상과 다릅니다.");
            }
            if (content == null || result.HabitScrollRect.content != content)
            {
                result.Errors.Add("HabitScrollView의 content 참조가 예상 대상과 다릅니다.");
            }
        }
        if (result.HabitContent != null && content != result.HabitContent)
        {
            result.Errors.Add("HabitSummaryManager와 ScrollRect가 서로 다른 Content를 참조합니다.");
        }
        if (result.ContentSizeFitter != null &&
            result.ContentSizeFitter.verticalFit != ContentSizeFitter.FitMode.PreferredSize)
        {
            result.Errors.Add("Habit Content의 ContentSizeFitter.verticalFit이 PreferredSize가 아닙니다.");
        }
    }

    private static void ValidatePanelPrefabScope(
        PanelResult result, params Transform[] transforms)
    {
        foreach (Transform transform in transforms)
        {
            if (PrefabUtility.GetOutermostPrefabInstanceRoot(transform.gameObject) != result.Canvas)
            {
                result.Errors.Add($"Canvas Prefab 인스턴스 밖의 대상입니다: {transform.name}");
            }
        }
    }

    private static PanelResult FinishPanelValidation(PanelResult result, bool log)
    {
        if (!log)
        {
            return result;
        }

        if (result.IsValid)
        {
            Debug.Log(
                "[HabitPVP UI Renewal] 패널 검증 대상\n" +
                $"  Summary: {result.SummaryPanelSprite.name}, border {result.SummaryPanelSprite.border}\n" +
                $"  List: {result.ListPanelSprite.name}, border {result.ListPanelSprite.border}\n" +
                $"  Streak background: {result.StreakPanelSprite.name} (Simple, 402:184), " +
                $"text '{result.SummaryStreak.text}'\n" +
                $"  Progress: {result.ProgressBackgroundSprite.name} (Simple), " +
                $"{result.ProgressFillSprite.name} (Sliced)\n" +
                $"  Slider: min {result.ProgressSlider.minValue}, max {result.ProgressSlider.maxValue}, " +
                $"value {result.ProgressSlider.value}, direction {result.ProgressSlider.direction}",
                result.SummaryCard);
        }

        foreach (string error in result.Errors)
        {
            Debug.LogError($"[HabitPVP UI Renewal] {error}");
        }
        return result;
    }

    private static void RunPanelApply(PanelResult result)
    {
        Scene scene = SceneManager.GetActiveScene();
        PanelSafetySnapshot safety = PanelSafetySnapshot.Capture(result);
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply HabitPVP UITestScene Panels");
        try
        {
            UnityEngine.Object[] targets =
            {
                result.SummaryImage, result.ListImage, result.ProgressBackground, result.ProgressFill,
                result.SummaryTitle, result.SummaryCount, result.SummaryStreak, result.ListTitle,
                result.ContentLayout,
                result.ProgressRect, result.ProgressBackgroundRect, result.ProgressFillAreaRect,
                result.SummaryTitle.rectTransform, result.SummaryCount.rectTransform,
                result.SummaryStreak.rectTransform
            };
            Undo.RecordObjects(targets, "Style HabitPVP panels");

            ApplySlicedPanel(result.SummaryImage, result.SummaryPanelSprite);
            ApplySlicedPanel(result.ListImage, result.ListPanelSprite);
            result.SummaryTitle.font = result.KoreanFont;
            result.SummaryTitle.color = SummaryTextColor;
            result.SummaryTitle.fontStyle |= FontStyles.Bold;
            result.SummaryCount.font = result.KoreanFont;
            result.SummaryCount.color = ProgressTextColor;
            result.SummaryCount.fontStyle |= FontStyles.Bold;
            result.SummaryStreak.font = result.KoreanFont;
            result.SummaryStreak.color = Color.white;
            result.SummaryStreak.fontStyle |= FontStyles.Bold;
            result.SummaryStreak.fontSize = StreakFontSize;
            result.SummaryStreak.enableAutoSizing = true;
            result.SummaryStreak.fontSizeMin = StreakMinimumFontSize;
            result.SummaryStreak.fontSizeMax = StreakFontSize;
            result.SummaryStreak.enableWordWrapping = false;
            result.SummaryStreak.overflowMode = TextOverflowModes.Ellipsis;
            result.SummaryStreak.maxVisibleLines = 1;
            result.SummaryStreak.alignment = TextAlignmentOptions.Center;
            result.ListTitle.font = result.KoreanFont;
            result.ListTitle.color = SummaryTextColor;
            result.ListTitle.fontStyle |= FontStyles.Bold;
            result.ListTitle.fontSize = ListTitleFontSize;
            result.ListTitle.alignment = TextAlignmentOptions.MidlineLeft;

            result.ContentLayout.padding = new RectOffset(
                ContentPadding.left, ContentPadding.right, ContentPadding.top, ContentPadding.bottom);
            result.ContentLayout.spacing = ContentSpacing;

            ApplySummaryHeaderLayout(result);
            ApplyProgressLayout(result);
            ApplyStreakTextRect(result.SummaryStreak.rectTransform);

            result.ProgressBackground.sprite = result.ProgressBackgroundSprite;
            result.ProgressBackground.color = Color.white;
            result.ProgressBackground.type = Image.Type.Simple;
            result.ProgressBackground.preserveAspect = false;
            result.ProgressFill.sprite = result.ProgressFillSprite;
            result.ProgressFill.color = ProgressTextColor;
            result.ProgressFill.type = Image.Type.Sliced;
            result.ProgressFill.preserveAspect = false;

            result.StreakBackground = GetOrCreateStreakBackground(result);
            RecordPanelOverrides(result);
            safety.AssertUnchanged(result);
            if (!ArePanelsApplied(result))
            {
                throw new InvalidOperationException("적용 후 패널 스타일 검증에 실패했습니다.");
            }

            Undo.CollapseUndoOperations(group);
            SaveScene(scene);
            Debug.Log(
                "[HabitPVP UI Renewal] Apply Panels 성공. UITestScene의 패널/진행률 Scene Override만 " +
                "저장했습니다. Slider 값·범위·방향·fillRect, ScrollRect/Mask/LayoutGroup, Manager 참조와 " +
                "Streak 텍스트는 변경하지 않았습니다. Content Layout은 카드 여백과 기존 15px 간격만 " +
                "명시적으로 스타일링했습니다. Edit > Undo를 지원합니다.");
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(group);
            SaveScene(scene);
            Debug.LogException(exception);
            Debug.LogError("[HabitPVP UI Renewal] Apply Panels 실패. 이번 실행의 변경을 Undo로 되돌렸습니다.");
        }
    }

    private static void ApplySlicedPanel(Image image, Sprite sprite)
    {
        image.sprite = sprite;
        image.color = Color.white;
        image.type = Image.Type.Sliced;
        image.preserveAspect = false;
        image.fillCenter = true;
    }

    private static void ApplyProgressLayout(PanelResult result)
    {
        RectTransform progress = result.ProgressRect;
        progress.anchorMin = ProgressAnchorMin;
        progress.anchorMax = ProgressAnchorMax;
        progress.anchoredPosition = ProgressPosition;
        progress.sizeDelta = ProgressSize;
        progress.pivot = new Vector2(0.5f, 0.5f);

        RectTransform background = result.ProgressBackgroundRect;
        background.anchorMin = TrackAnchorMin;
        background.anchorMax = TrackAnchorMax;
        background.anchoredPosition = Vector2.zero;
        background.sizeDelta = Vector2.zero;

        RectTransform fillArea = result.ProgressFillAreaRect;
        fillArea.anchorMin = TrackAnchorMin;
        fillArea.anchorMax = TrackAnchorMax;
        fillArea.anchoredPosition = new Vector2(-5f, 0f);
        fillArea.sizeDelta = new Vector2(-20f, 0f);
    }

    private static void ApplySummaryHeaderLayout(PanelResult result)
    {
        RectTransform title = result.SummaryTitle.rectTransform;
        title.anchorMin = SummaryTitleAnchor;
        title.anchorMax = SummaryTitleAnchor;
        title.anchoredPosition = SummaryTitlePosition;
        title.sizeDelta = SummaryTitleSize;
        title.pivot = SummaryTitlePivot;

        RectTransform count = result.SummaryCount.rectTransform;
        count.anchorMin = SummaryCountAnchor;
        count.anchorMax = SummaryCountAnchor;
        count.anchoredPosition = SummaryCountPosition;
        count.sizeDelta = SummaryCountSize;
        count.pivot = SummaryCountPivot;
    }

    private static void ApplyStreakTextRect(RectTransform rect)
    {
        rect.anchorMin = StreakTextAnchor;
        rect.anchorMax = StreakTextAnchor;
        rect.anchoredPosition = StreakTextPosition;
        rect.sizeDelta = StreakTextSize;
        rect.pivot = StreakTextPivot;
    }

    private static void ApplyStreakBackgroundRect(RectTransform rect)
    {
        rect.anchorMin = StreakBackgroundAnchor;
        rect.anchorMax = StreakBackgroundAnchor;
        rect.anchoredPosition = StreakBackgroundPosition;
        rect.sizeDelta = StreakBackgroundSize;
        rect.pivot = StreakBackgroundPivot;
    }

    private static Image GetOrCreateStreakBackground(PanelResult result)
    {
        Image image = result.StreakBackground;
        if (image == null)
        {
            GameObject background = new GameObject(
                StreakBackgroundName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            background.layer = result.SummaryStreak.gameObject.layer;
            Undo.RegisterCreatedObjectUndo(background, "Create streak background");
            RectTransform rect = background.GetComponent<RectTransform>();
            Undo.SetTransformParent(rect, result.SummaryStreak.transform.parent, "Parent streak background");
            rect.SetSiblingIndex(result.SummaryStreak.transform.GetSiblingIndex());
            image = background.GetComponent<Image>();
        }
        else
        {
            Undo.RecordObjects(
                new UnityEngine.Object[] { image, image.rectTransform }, "Style streak background");
        }

        RectTransform source = result.SummaryStreak.rectTransform;
        RectTransform target = image.rectTransform;
        int sourceIndex = source.GetSiblingIndex();
        int targetIndex = target.GetSiblingIndex();
        int backgroundIndex = sourceIndex - (targetIndex < sourceIndex ? 1 : 0);
        target.SetSiblingIndex(backgroundIndex);
        target.localRotation = source.localRotation;
        target.localScale = source.localScale;
        ApplyStreakBackgroundRect(target);
        image.sprite = result.StreakPanelSprite;
        image.color = Color.white;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.raycastTarget = false;
        EditorUtility.SetDirty(image);
        EditorUtility.SetDirty(target);
        return image;
    }

    private static void RecordPanelOverrides(PanelResult result)
    {
        UnityEngine.Object[] values =
        {
            result.SummaryImage, result.ListImage, result.ProgressBackground, result.ProgressFill,
            result.SummaryTitle, result.SummaryCount, result.SummaryStreak,
            result.ListTitle, result.ContentLayout,
            result.ProgressRect, result.ProgressBackgroundRect, result.ProgressFillAreaRect,
            result.SummaryTitle.rectTransform, result.SummaryCount.rectTransform,
            result.SummaryStreak.rectTransform, result.StreakBackground,
            result.StreakBackground.rectTransform
        };
        foreach (UnityEngine.Object value in values)
        {
            EditorUtility.SetDirty(value);
            PrefabUtility.RecordPrefabInstancePropertyModifications(value);
        }
    }

    private static bool ArePanelsApplied(PanelResult result)
    {
        return result.StreakBackground != null &&
               IsSlicedPanel(result.SummaryImage, result.SummaryPanelSprite) &&
               IsSlicedPanel(result.ListImage, result.ListPanelSprite) &&
               result.ProgressBackground.sprite == result.ProgressBackgroundSprite &&
               result.ProgressBackground.type == Image.Type.Simple &&
               ColorEqual(result.ProgressBackground.color, Color.white) &&
               !result.ProgressBackground.preserveAspect &&
               result.ProgressFill.sprite == result.ProgressFillSprite &&
               result.ProgressFill.type == Image.Type.Sliced &&
               ColorEqual(result.ProgressFill.color, ProgressTextColor) &&
               !result.ProgressFill.preserveAspect &&
               ColorEqual(result.SummaryTitle.color, SummaryTextColor) &&
               result.SummaryTitle.font == result.KoreanFont &&
               (result.SummaryTitle.fontStyle & FontStyles.Bold) != 0 &&
               ColorEqual(result.SummaryCount.color, ProgressTextColor) &&
               result.SummaryCount.font == result.KoreanFont &&
               (result.SummaryCount.fontStyle & FontStyles.Bold) != 0 &&
               ColorEqual(result.SummaryStreak.color, Color.white) &&
               result.SummaryStreak.font == result.KoreanFont &&
               (result.SummaryStreak.fontStyle & FontStyles.Bold) != 0 &&
               result.SummaryStreak.enableAutoSizing &&
               Mathf.Approximately(result.SummaryStreak.fontSizeMin, StreakMinimumFontSize) &&
               Mathf.Approximately(result.SummaryStreak.fontSizeMax, StreakFontSize) &&
               !result.SummaryStreak.enableWordWrapping &&
               result.SummaryStreak.overflowMode == TextOverflowModes.Ellipsis &&
               result.SummaryStreak.maxVisibleLines == 1 &&
               result.SummaryStreak.alignment == TextAlignmentOptions.Center &&
               result.ListTitle.font == result.KoreanFont &&
               ColorEqual(result.ListTitle.color, SummaryTextColor) &&
               (result.ListTitle.fontStyle & FontStyles.Bold) != 0 &&
               Mathf.Approximately(result.ListTitle.fontSize, ListTitleFontSize) &&
               result.ListTitle.alignment == TextAlignmentOptions.MidlineLeft &&
               RectOffsetEqual(result.ContentLayout.padding, ContentPadding) &&
               Mathf.Approximately(result.ContentLayout.spacing, ContentSpacing) &&
               IsSummaryHeaderLayoutApplied(result) &&
               IsProgressLayoutApplied(result) &&
               IsStreakBackgroundApplied(result) &&
               HasSeparatedSummaryRegions(result);
    }

    private static bool IsSummaryHeaderLayoutApplied(PanelResult result)
    {
        RectTransform title = result.SummaryTitle.rectTransform;
        RectTransform count = result.SummaryCount.rectTransform;
        return Vec2Equal(title.anchorMin, SummaryTitleAnchor) &&
               Vec2Equal(title.anchorMax, SummaryTitleAnchor) &&
               Vec2Equal(title.anchoredPosition, SummaryTitlePosition) &&
               Vec2Equal(title.sizeDelta, SummaryTitleSize) &&
               Vec2Equal(title.pivot, SummaryTitlePivot) &&
               Vec2Equal(count.anchorMin, SummaryCountAnchor) &&
               Vec2Equal(count.anchorMax, SummaryCountAnchor) &&
               Vec2Equal(count.anchoredPosition, SummaryCountPosition) &&
               Vec2Equal(count.sizeDelta, SummaryCountSize) &&
               Vec2Equal(count.pivot, SummaryCountPivot);
    }

    private static bool IsProgressLayoutApplied(PanelResult result)
    {
        RectTransform progress = result.ProgressRect;
        RectTransform background = result.ProgressBackgroundRect;
        RectTransform fillArea = result.ProgressFillAreaRect;
        return Vec2Equal(progress.anchorMin, ProgressAnchorMin) &&
               Vec2Equal(progress.anchorMax, ProgressAnchorMax) &&
               Vec2Equal(progress.anchoredPosition, ProgressPosition) &&
               Vec2Equal(progress.sizeDelta, ProgressSize) &&
               Vec2Equal(progress.pivot, new Vector2(0.5f, 0.5f)) &&
               Vec2Equal(background.anchorMin, TrackAnchorMin) &&
               Vec2Equal(background.anchorMax, TrackAnchorMax) &&
               Vec2Equal(background.anchoredPosition, Vector2.zero) &&
               Vec2Equal(background.sizeDelta, Vector2.zero) &&
               Vec2Equal(fillArea.anchorMin, TrackAnchorMin) &&
               Vec2Equal(fillArea.anchorMax, TrackAnchorMax) &&
               Vec2Equal(fillArea.anchoredPosition, new Vector2(-5f, 0f)) &&
               Vec2Equal(fillArea.sizeDelta, new Vector2(-20f, 0f));
    }

    private static bool IsSlicedPanel(Image image, Sprite sprite)
    {
        return image.sprite == sprite && image.type == Image.Type.Sliced && image.fillCenter &&
               ColorEqual(image.color, Color.white) && !image.preserveAspect;
    }

    private static bool IsStreakBackgroundApplied(PanelResult result)
    {
        Image image = result.StreakBackground;
        if (image == null || image.sprite != result.StreakPanelSprite ||
            image.type != Image.Type.Simple || image.preserveAspect || image.raycastTarget ||
            !ColorEqual(image.color, Color.white))
        {
            return false;
        }

        RectTransform source = result.SummaryStreak.rectTransform;
        RectTransform target = image.rectTransform;
        return target.parent == source.parent &&
               target.GetSiblingIndex() + 1 == source.GetSiblingIndex() &&
               Vec2Equal(source.anchorMin, StreakTextAnchor) &&
               Vec2Equal(source.anchorMax, StreakTextAnchor) &&
               Vec2Equal(source.anchoredPosition, StreakTextPosition) &&
               Vec2Equal(source.sizeDelta, StreakTextSize) &&
               Vec2Equal(source.pivot, StreakTextPivot) &&
               Vec2Equal(target.anchorMin, StreakBackgroundAnchor) &&
               Vec2Equal(target.anchorMax, StreakBackgroundAnchor) &&
               Vec2Equal(target.anchoredPosition, StreakBackgroundPosition) &&
               Vec2Equal(target.sizeDelta, StreakBackgroundSize) &&
               Vec2Equal(target.pivot, StreakBackgroundPivot);
    }

    private static bool HasSeparatedSummaryRegions(PanelResult result)
    {
        Transform card = result.SummaryCard.transform;
        Bounds title = RectTransformUtility.CalculateRelativeRectTransformBounds(
            card, result.SummaryTitle.rectTransform);
        Bounds count = RectTransformUtility.CalculateRelativeRectTransformBounds(
            card, result.SummaryCount.rectTransform);
        Bounds progress = RectTransformUtility.CalculateRelativeRectTransformBounds(
            card, result.ProgressRect);
        Bounds streak = RectTransformUtility.CalculateRelativeRectTransformBounds(
            card, result.StreakBackground.rectTransform);
        Bounds streakText = RectTransformUtility.CalculateRelativeRectTransformBounds(
            card, result.SummaryStreak.rectTransform);

        const float minimumGap = 8f;
        return HasVerticalGap(title, progress, minimumGap) &&
               HasVerticalGap(count, progress, minimumGap) &&
               HasVerticalGap(progress, streak, minimumGap) &&
               ContainsBounds(streak, streakText);
    }

    private static bool HasVerticalGap(Bounds upper, Bounds lower, float gap) =>
        lower.max.y + gap <= upper.min.y;

    private static bool ContainsBounds(Bounds outer, Bounds inner) =>
        inner.min.x >= outer.min.x && inner.max.x <= outer.max.x &&
        inner.min.y >= outer.min.y && inner.max.y <= outer.max.y;

    private static Result ValidateProject(bool log)
    {
        Result result = new Result();
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || !scene.isLoaded)
        {
            result.Errors.Add("활성 Scene이 유효하거나 로드된 상태가 아닙니다.");
            return FinishValidation(result, log);
        }

        if (!string.Equals(scene.path, ScenePath, StringComparison.Ordinal))
        {
            result.Errors.Add($"활성 Scene이 UITestScene이 아닙니다. 현재 경로: '{scene.path}'");
            return FinishValidation(result, log);
        }

        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            result.Errors.Add("Play Mode 또는 Play Mode 전환 중에는 실행할 수 없습니다.");
        }

        if (scene.isDirty)
        {
            result.Errors.Add("UITestScene에 저장되지 않은 변경이 있습니다. 먼저 저장해 주세요.");
        }

        GameObject[] canvases = scene.GetRootGameObjects()
            .Where(item => string.Equals(item.name, "Canvas", StringComparison.Ordinal)).ToArray();
        if (canvases.Length != 1)
        {
            result.Errors.Add($"Scene 루트 Canvas가 정확히 하나여야 합니다. 발견 수: {canvases.Length}");
            return FinishValidation(result, log);
        }

        GameObject canvas = canvases[0];
        if (!PrefabUtility.IsPartOfPrefabInstance(canvas) ||
            PrefabUtility.GetOutermostPrefabInstanceRoot(canvas) != canvas)
        {
            result.Errors.Add("Canvas가 Prefab 인스턴스의 최상위 루트가 아닙니다.");
        }

        string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(canvas);
        if (!string.Equals(prefabPath, CanvasPrefabPath, StringComparison.Ordinal))
        {
            result.Errors.Add($"Canvas 원본 Prefab 경로가 다릅니다: '{prefabPath}'");
        }

        result.Manager = ValidateManager(scene, result);
        result.KoreanFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(KoreanFontPath);
        if (result.KoreanFont == null)
        {
            result.Errors.Add($"NotoSansKR TMP Font Asset을 불러올 수 없습니다: {KoreanFontPath}");
        }
        Dictionary<string, Sprite> sprites = LoadSprites(result);
        foreach (Definition definition in Definitions)
        {
            Transform transform = canvas.transform.Find(definition.Path);
            if (transform == null)
            {
                result.Errors.Add($"버튼 경로를 찾을 수 없습니다: Canvas/{definition.Path}");
                continue;
            }

            GameObject gameObject = transform.gameObject;
            Button[] buttons = gameObject.GetComponents<Button>();
            Image[] images = gameObject.GetComponents<Image>();
            Transform textTransform = transform.Find("Text (TMP)");
            TMP_Text text = textTransform == null ? null : textTransform.GetComponent<TMP_Text>();
            LayoutElement layout = gameObject.GetComponent<LayoutElement>();
            if (gameObject.GetComponents<Component>().Any(component => component == null))
            {
                result.Errors.Add($"Missing Script가 있습니다: Canvas/{definition.Path}");
                continue;
            }

            if (buttons.Length != 1 || images.Length != 1 || text == null)
            {
                result.Errors.Add(
                    $"Button, Image, 직속 Text (TMP)가 각각 하나여야 합니다: Canvas/{definition.Path}");
                continue;
            }

            if (buttons[0].targetGraphic != images[0] || images[0].type == Image.Type.Filled)
            {
                result.Errors.Add($"안전하게 변경할 수 없는 Image 구성입니다: Canvas/{definition.Path}");
                continue;
            }

            if (PrefabUtility.GetOutermostPrefabInstanceRoot(gameObject) != canvas ||
                PrefabUtility.GetOutermostPrefabInstanceRoot(text.gameObject) != canvas)
            {
                result.Errors.Add($"Canvas Prefab 인스턴스 밖의 대상입니다: Canvas/{definition.Path}");
                continue;
            }

            if (definition.TargetStyle == Style.AddHabit && layout == null)
            {
                result.Errors.Add($"기존 LayoutElement를 찾을 수 없습니다: Canvas/{definition.Path}");
                continue;
            }

            if (!sprites.TryGetValue(definition.SpriteName, out Sprite sprite))
            {
                result.Errors.Add($"Sprite를 찾을 수 없습니다: {definition.SpriteName}");
                continue;
            }

            result.Targets.Add(new Target(
                definition, gameObject, gameObject.GetComponent<RectTransform>(), buttons[0],
                images[0], text, layout, sprite, result.KoreanFont));
        }

        if (result.Targets.Count != Definitions.Length)
        {
            result.Errors.Add(
                $"검증된 대상 수가 다릅니다. 예상 {Definitions.Length}, 실제 {result.Targets.Count}");
        }

        return FinishValidation(result, log, sprites.Values);
    }

    private static MonoBehaviour ValidateManager(Scene scene, Result result)
    {
        GameObject[] objects = scene.GetRootGameObjects()
            .Where(item => string.Equals(item.name, "MainTapManager", StringComparison.Ordinal)).ToArray();
        if (objects.Length != 1)
        {
            result.Errors.Add($"MainTapManager GameObject가 정확히 하나여야 합니다. 발견 수: {objects.Length}");
            return null;
        }

        MonoBehaviour[] managers = objects[0].GetComponents<MonoBehaviour>().Where(item =>
            item != null && string.Equals(item.GetType().Name, "MainTabManager", StringComparison.Ordinal)).ToArray();
        if (managers.Length != 1)
        {
            result.Errors.Add($"MainTabManager 컴포넌트가 정확히 하나여야 합니다. 발견 수: {managers.Length}");
            return null;
        }

        SerializedObject serialized = new SerializedObject(managers[0]);
        string[] references =
        {
            "habitTap", "characterTap", "inventoryTap", "shopTap", "battleTap",
            "habitTapButton", "characterTapButton", "inventoryTapButton", "shopTapButton",
            "battleTapButton", "rankingboardManager"
        };
        foreach (string name in references)
        {
            SerializedProperty property = serialized.FindProperty(name);
            if (property == null || property.objectReferenceValue == null)
            {
                result.Errors.Add($"MainTabManager 참조가 비어 있거나 없습니다: {name}");
            }
        }

        return managers[0];
    }

    private static Dictionary<string, Sprite> LoadSprites(Result result)
    {
        Dictionary<string, Sprite> sprites = new Dictionary<string, Sprite>(StringComparer.Ordinal);
        TextureImporter importer = AssetImporter.GetAtPath(SpriteSheetPath) as TextureImporter;
        if (importer == null || importer.textureType != TextureImporterType.Sprite ||
            importer.spriteImportMode != SpriteImportMode.Multiple)
        {
            result.Errors.Add($"Multiple Sprite TextureImporter가 아닙니다: {SpriteSheetPath}");
            return sprites;
        }

        foreach (Sprite sprite in AssetDatabase.LoadAllAssetsAtPath(SpriteSheetPath).OfType<Sprite>())
        {
            if (!sprites.TryAdd(sprite.name, sprite))
            {
                result.Errors.Add($"중복 Sprite 이름이 있습니다: {sprite.name}");
            }
        }

        return sprites;
    }

    private static Result FinishValidation(Result result, bool log, IEnumerable<Sprite> sprites = null)
    {
        if (!log)
        {
            return result;
        }

        if (sprites != null)
        {
            Debug.Log($"[HabitPVP UI Renewal] Sprite 목록: {string.Join(", ", sprites.Select(x => x.name).OrderBy(x => x))}");
        }

        foreach (Target target in result.Targets)
        {
            Debug.Log(
                $"[HabitPVP UI Renewal] 대상: Canvas/{target.Definition.Path}\n" +
                $"  TMP: '{target.Text.text}'\n" +
                $"  OnClick: {DescribeEvents(target.Button)}\n" +
                $"  Sprite: {target.Sprite.name}, Rect: {target.Sprite.rect.size}", target.GameObject);
        }

        foreach (string error in result.Errors)
        {
            Debug.LogError($"[HabitPVP UI Renewal] {error}");
        }

        return result;
    }

    private static void RunApply(Result result)
    {
        Scene scene = SceneManager.GetActiveScene();
        List<SafetySnapshot> safety = result.Targets.Select(SafetySnapshot.Capture).ToList();
        string managerBefore = EditorJsonUtility.ToJson(result.Manager);
        string oldSnapshot = File.Exists(SnapshotPath) ? File.ReadAllText(SnapshotPath) : null;
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply HabitPVP UITestScene UI Renewal");
        try
        {
            WriteSnapshot(RenewalSnapshot.Capture(result));
            foreach (Target target in result.Targets)
            {
                Record(target, $"Style {target.Definition.Name}");
                ApplyStyle(target);
                RecordOverrides(target);
            }

            AssertProtectedState(result, safety, managerBefore);
            if (!result.Targets.All(IsApplied))
            {
                throw new InvalidOperationException("적용 후 스타일 검증에 실패했습니다.");
            }

            Undo.CollapseUndoOperations(group);
            SaveScene(scene);
            Debug.Log(
                "[HabitPVP UI Renewal] Apply 성공. 네 버튼의 Scene 인스턴스 Override만 저장했습니다. " +
                "Canvas.prefab에는 Apply하지 않았습니다. Edit > Undo 또는 Restore를 사용할 수 있습니다.");
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(group);
            RestoreSnapshotFile(oldSnapshot);
            SaveScene(scene);
            Debug.LogException(exception);
            Debug.LogError("[HabitPVP UI Renewal] Apply 실패. 이번 실행의 변경을 Undo로 되돌렸습니다.");
        }
    }

    private static void RunRestore(Result result, RenewalSnapshot snapshot)
    {
        Scene scene = SceneManager.GetActiveScene();
        List<SafetySnapshot> safety = result.Targets.Select(SafetySnapshot.Capture).ToList();
        string managerBefore = EditorJsonUtility.ToJson(result.Manager);
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Restore HabitPVP UITestScene UI Renewal");
        try
        {
            foreach (Target target in result.Targets)
            {
                TargetSnapshot saved = snapshot.Targets.Single(item => item.Path == target.Definition.Path);
                Record(target, $"Restore {target.Definition.Name}");
                saved.Restore(target);
                RecordOverrides(target);
            }

            AssertProtectedState(result, safety, managerBefore);
            Undo.CollapseUndoOperations(group);
            SaveScene(scene);
            File.Delete(SnapshotPath);
            Debug.Log(
                "[HabitPVP UI Renewal] Restore 성공. Apply 직전 스냅샷의 대상 속성만 복원했습니다.");
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(group);
            SaveScene(scene);
            Debug.LogException(exception);
            Debug.LogError("[HabitPVP UI Renewal] Restore 실패. 이번 실행의 변경을 Undo로 되돌렸습니다.");
        }
    }

    private static void ApplyStyle(Target target)
    {
        target.Image.sprite = target.Sprite;
        target.Image.color = Color.white;
        target.Image.type = Image.Type.Simple;
        target.Image.preserveAspect = target.Definition.TargetStyle == Style.AiReport;
        target.Text.font = target.Font;
        target.Text.fontStyle |= FontStyles.Bold;
        target.Text.alignment = TextAlignmentOptions.Center;
        target.TextRect.anchorMin = Vector2.zero;
        target.TextRect.anchorMax = Vector2.one;

        switch (target.Definition.TargetStyle)
        {
            case Style.TopTab:
                target.Rect.sizeDelta = new Vector2(300f, 130f);
                SetText(target, Color.white, 50f, new Vector4(24f, 10f, 24f, 10f), Vector2.zero, Vector2.zero);
                break;
            case Style.AddHabit:
                target.Layout.preferredWidth = 320f;
                target.Layout.preferredHeight = 100f;
                SetText(
                    target, Color.white, 44f, new Vector4(18f, 6f, 18f, 6f),
                    new Vector2(0f, 4f), new Vector2(-28f, -12f));
                break;
            case Style.AiReport:
                target.Rect.sizeDelta = new Vector2(target.Rect.sizeDelta.x, 113f);
                SetText(target, AiTextColor, 35f, new Vector4(24f, 8f, 24f, 8f), Vector2.zero, Vector2.zero);
                break;
        }

        Dirty(target);
    }

    private static void SetText(
        Target target, Color color, float size, Vector4 margin, Vector2 position, Vector2 sizeDelta)
    {
        target.Text.color = color;
        target.Text.fontSize = size;
        target.Text.margin = margin;
        target.TextRect.anchoredPosition = position;
        target.TextRect.sizeDelta = sizeDelta;
    }

    private static bool IsApplied(Target target)
    {
        bool common = target.Image.sprite == target.Sprite && ColorEqual(target.Image.color, Color.white) &&
                      target.Image.type == Image.Type.Simple &&
                      target.Image.preserveAspect == (target.Definition.TargetStyle == Style.AiReport) &&
                      target.Text.font == target.Font &&
                      (target.Text.fontStyle & FontStyles.Bold) != 0 &&
                      target.Text.alignment == TextAlignmentOptions.Center &&
                      Vec2Equal(target.TextRect.anchorMin, Vector2.zero) &&
                      Vec2Equal(target.TextRect.anchorMax, Vector2.one);
        if (!common)
        {
            return false;
        }

        switch (target.Definition.TargetStyle)
        {
            case Style.TopTab:
                return Vec2Equal(target.Rect.sizeDelta, new Vector2(300f, 130f)) &&
                       TextEqual(target, Color.white, 50f, new Vector4(24f, 10f, 24f, 10f),
                           Vector2.zero, Vector2.zero);
            case Style.AddHabit:
                return Mathf.Approximately(target.Layout.preferredWidth, 320f) &&
                       Mathf.Approximately(target.Layout.preferredHeight, 100f) &&
                       TextEqual(target, Color.white, 44f, new Vector4(18f, 6f, 18f, 6f),
                           new Vector2(0f, 4f), new Vector2(-28f, -12f));
            case Style.AiReport:
                return Mathf.Approximately(target.Rect.sizeDelta.y, 113f) &&
                       TextEqual(target, AiTextColor, 35f, new Vector4(24f, 8f, 24f, 8f),
                           Vector2.zero, Vector2.zero);
            default:
                return false;
        }
    }

    private static bool TextEqual(
        Target target, Color color, float size, Vector4 margin, Vector2 position, Vector2 sizeDelta)
    {
        return ColorEqual(target.Text.color, color) && Mathf.Approximately(target.Text.fontSize, size) &&
               Vec4Equal(target.Text.margin, margin) &&
               Vec2Equal(target.TextRect.anchoredPosition, position) &&
               Vec2Equal(target.TextRect.sizeDelta, sizeDelta);
    }

    private static void Record(Target target, string name)
    {
        List<UnityEngine.Object> objects = new List<UnityEngine.Object>
        {
            target.Rect, target.Image, target.Text, target.TextRect
        };
        if (target.Layout != null)
        {
            objects.Add(target.Layout);
        }
        Undo.RecordObjects(objects.ToArray(), name);
    }

    private static void Dirty(Target target)
    {
        EditorUtility.SetDirty(target.Rect);
        EditorUtility.SetDirty(target.Image);
        EditorUtility.SetDirty(target.Text);
        EditorUtility.SetDirty(target.TextRect);
        if (target.Layout != null)
        {
            EditorUtility.SetDirty(target.Layout);
        }
    }

    private static void RecordOverrides(Target target)
    {
        PrefabUtility.RecordPrefabInstancePropertyModifications(target.Rect);
        PrefabUtility.RecordPrefabInstancePropertyModifications(target.Image);
        PrefabUtility.RecordPrefabInstancePropertyModifications(target.Text);
        PrefabUtility.RecordPrefabInstancePropertyModifications(target.TextRect);
        if (target.Layout != null)
        {
            PrefabUtility.RecordPrefabInstancePropertyModifications(target.Layout);
        }
    }

    private static void AssertProtectedState(
        Result result, IEnumerable<SafetySnapshot> safety, string managerBefore)
    {
        foreach (SafetySnapshot item in safety)
        {
            item.AssertUnchanged();
        }
        if (!string.Equals(managerBefore, EditorJsonUtility.ToJson(result.Manager), StringComparison.Ordinal))
        {
            throw new InvalidOperationException("MainTabManager 참조가 변경되었습니다.");
        }
    }

    private static void SaveScene(Scene scene)
    {
        EditorSceneManager.MarkSceneDirty(scene);
        if (!EditorSceneManager.SaveScene(scene))
        {
            throw new InvalidOperationException("UITestScene 저장에 실패했습니다.");
        }
    }

    private static void WriteSnapshot(RenewalSnapshot snapshot)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SnapshotPath));
        File.WriteAllText(SnapshotPath, JsonUtility.ToJson(snapshot, true));
    }

    private static void RestoreSnapshotFile(string json)
    {
        if (json == null)
        {
            if (File.Exists(SnapshotPath))
            {
                File.Delete(SnapshotPath);
            }
            return;
        }
        Directory.CreateDirectory(Path.GetDirectoryName(SnapshotPath));
        File.WriteAllText(SnapshotPath, json);
    }

    private static bool TryLoadSnapshot(Result result, out RenewalSnapshot snapshot, out string reason)
    {
        snapshot = null;
        reason = null;
        if (!File.Exists(SnapshotPath))
        {
            reason = "Apply 직전 스냅샷이 없습니다.";
            return false;
        }

        try
        {
            snapshot = JsonUtility.FromJson<RenewalSnapshot>(File.ReadAllText(SnapshotPath));
        }
        catch (Exception exception)
        {
            reason = $"스냅샷을 읽을 수 없습니다: {exception.Message}";
            return false;
        }

        string guid = AssetDatabase.AssetPathToGUID(ScenePath);
        if (snapshot == null || snapshot.Version != SnapshotVersion || snapshot.SceneGuid != guid ||
            snapshot.Targets == null || snapshot.Targets.Count != result.Targets.Count)
        {
            reason = "스냅샷의 버전, Scene 또는 대상 목록이 현재 상태와 다릅니다.";
            return false;
        }

        foreach (Target target in result.Targets)
        {
            TargetSnapshot saved = snapshot.Targets.SingleOrDefault(item => item.Path == target.Definition.Path);
            if (saved == null || saved.Sprite == null || !saved.Sprite.CanResolve() ||
                (!string.IsNullOrEmpty(saved.FontAssetPath) &&
                 AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(saved.FontAssetPath) == null))
            {
                reason = $"복원 기준을 검증할 수 없습니다: Canvas/{target.Definition.Path}";
                return false;
            }
        }
        return true;
    }

    private static string DescribeEvents(Button button)
    {
        int count = button.onClick.GetPersistentEventCount();
        if (count == 0)
        {
            return "없음";
        }
        return string.Join(", ", Enumerable.Range(0, count).Select(index =>
        {
            UnityEngine.Object target = button.onClick.GetPersistentTarget(index);
            return $"{(target == null ? "null" : target.name)}.{button.onClick.GetPersistentMethodName(index)}";
        }));
    }

    private static bool ColorEqual(Color a, Color b) =>
        Mathf.Approximately(a.r, b.r) && Mathf.Approximately(a.g, b.g) &&
        Mathf.Approximately(a.b, b.b) && Mathf.Approximately(a.a, b.a);
    private static bool Vec2Equal(Vector2 a, Vector2 b) =>
        Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y);
    private static bool Vec4Equal(Vector4 a, Vector4 b) =>
        Mathf.Approximately(a.x, b.x) && Mathf.Approximately(a.y, b.y) &&
        Mathf.Approximately(a.z, b.z) && Mathf.Approximately(a.w, b.w);

    private static bool RectOffsetEqual(RectOffset a, RectOffset b) =>
        a != null && b != null && a.left == b.left && a.right == b.right &&
        a.top == b.top && a.bottom == b.bottom;

    private sealed class PanelResult
    {
        public readonly List<string> Errors = new List<string>();
        public GameObject Canvas;
        public GameObject SummaryCard;
        public GameObject ListCard;
        public RectTransform SummaryRect;
        public Image SummaryImage;
        public Image ListImage;
        public TMP_Text SummaryTitle;
        public TMP_Text SummaryCount;
        public TMP_Text SummaryStreak;
        public TMP_Text ListTitle;
        public Slider ProgressSlider;
        public RectTransform ProgressRect;
        public RectTransform ProgressBackgroundRect;
        public RectTransform ProgressFillAreaRect;
        public Image ProgressBackground;
        public Image ProgressFill;
        public Image StreakBackground;
        public ScrollRect HabitScrollRect;
        public Mask ViewportMask;
        public VerticalLayoutGroup ListLayout;
        public VerticalLayoutGroup ContentLayout;
        public ContentSizeFitter ContentSizeFitter;
        public Transform HabitContent;
        public MonoBehaviour MainTabManager;
        public MonoBehaviour SummaryManager;
        public Sprite SummaryPanelSprite;
        public Sprite ListPanelSprite;
        public Sprite StreakPanelSprite;
        public Sprite ProgressBackgroundSprite;
        public Sprite ProgressFillSprite;
        public TMP_FontAsset KoreanFont;
        public bool IsValid => Errors.Count == 0;
    }

    private sealed class PanelSafetySnapshot
    {
        private readonly string mainTabManagerJson;
        private readonly string summaryManagerJson;
        private readonly string sliderJson;
        private readonly string scrollRectJson;
        private readonly string maskJson;
        private readonly string listLayoutJson;
        private readonly bool contentChildControlWidth;
        private readonly bool contentChildControlHeight;
        private readonly bool contentChildForceExpandWidth;
        private readonly bool contentChildForceExpandHeight;
        private readonly TextAnchor contentChildAlignment;
        private readonly bool contentReverseArrangement;
        private readonly ContentSizeFitter.FitMode contentHorizontalFit;
        private readonly ContentSizeFitter.FitMode contentVerticalFit;
        private readonly string summaryTitleText;
        private readonly string summaryCountText;
        private readonly string summaryStreakText;
        private readonly string listTitleText;
        private readonly Transform content;
        private readonly TransformState[] hierarchy;

        private PanelSafetySnapshot(PanelResult result)
        {
            mainTabManagerJson = EditorJsonUtility.ToJson(result.MainTabManager);
            summaryManagerJson = EditorJsonUtility.ToJson(result.SummaryManager);
            sliderJson = EditorJsonUtility.ToJson(result.ProgressSlider);
            scrollRectJson = EditorJsonUtility.ToJson(result.HabitScrollRect);
            maskJson = EditorJsonUtility.ToJson(result.ViewportMask);
            listLayoutJson = EditorJsonUtility.ToJson(result.ListLayout);
            contentChildControlWidth = result.ContentLayout.childControlWidth;
            contentChildControlHeight = result.ContentLayout.childControlHeight;
            contentChildForceExpandWidth = result.ContentLayout.childForceExpandWidth;
            contentChildForceExpandHeight = result.ContentLayout.childForceExpandHeight;
            contentChildAlignment = result.ContentLayout.childAlignment;
            contentReverseArrangement = result.ContentLayout.reverseArrangement;
            contentHorizontalFit = result.ContentSizeFitter.horizontalFit;
            contentVerticalFit = result.ContentSizeFitter.verticalFit;
            summaryTitleText = result.SummaryTitle.text;
            summaryCountText = result.SummaryCount.text;
            summaryStreakText = result.SummaryStreak.text;
            listTitleText = result.ListTitle.text;
            content = result.HabitContent;
            hierarchy = result.Canvas.GetComponentsInChildren<Transform>(true)
                .Select(TransformState.Capture).ToArray();
        }

        public static PanelSafetySnapshot Capture(PanelResult result) =>
            new PanelSafetySnapshot(result);

        public void AssertUnchanged(PanelResult result)
        {
            if (!string.Equals(mainTabManagerJson, EditorJsonUtility.ToJson(result.MainTabManager),
                    StringComparison.Ordinal) ||
                !string.Equals(summaryManagerJson, EditorJsonUtility.ToJson(result.SummaryManager),
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException("Manager SerializeField 참조가 변경되었습니다.");
            }
            if (!string.Equals(sliderJson, EditorJsonUtility.ToJson(result.ProgressSlider),
                    StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    "Slider의 targetGraphic, fillRect, handleRect, minValue, maxValue, value, direction, interactable 또는 기타 설정이 변경되었습니다.");
            }
            if (!string.Equals(scrollRectJson, EditorJsonUtility.ToJson(result.HabitScrollRect),
                    StringComparison.Ordinal) ||
                !string.Equals(maskJson, EditorJsonUtility.ToJson(result.ViewportMask),
                    StringComparison.Ordinal) ||
                !string.Equals(listLayoutJson, EditorJsonUtility.ToJson(result.ListLayout),
                    StringComparison.Ordinal) || content != result.HabitContent)
            {
                throw new InvalidOperationException("ScrollRect, Mask, Content 또는 LayoutGroup이 변경되었습니다.");
            }
            if (contentChildControlWidth != result.ContentLayout.childControlWidth ||
                contentChildControlHeight != result.ContentLayout.childControlHeight ||
                contentChildForceExpandWidth != result.ContentLayout.childForceExpandWidth ||
                contentChildForceExpandHeight != result.ContentLayout.childForceExpandHeight ||
                contentChildAlignment != result.ContentLayout.childAlignment ||
                contentReverseArrangement != result.ContentLayout.reverseArrangement ||
                contentHorizontalFit != result.ContentSizeFitter.horizontalFit ||
                contentVerticalFit != result.ContentSizeFitter.verticalFit)
            {
                throw new InvalidOperationException(
                    "Habit Content의 Layout 동작 또는 ContentSizeFitter 설정이 변경되었습니다.");
            }
            if (!string.Equals(summaryTitleText, result.SummaryTitle.text, StringComparison.Ordinal) ||
                !string.Equals(summaryCountText, result.SummaryCount.text, StringComparison.Ordinal) ||
                !string.Equals(summaryStreakText, result.SummaryStreak.text, StringComparison.Ordinal) ||
                !string.Equals(listTitleText, result.ListTitle.text, StringComparison.Ordinal))
            {
                throw new InvalidOperationException("요약/Streak/목록 헤더 표시 데이터가 변경되었습니다.");
            }
            foreach (TransformState state in hierarchy)
            {
                state.AssertUnchanged();
            }
        }
    }

    private sealed class TransformState
    {
        private readonly Transform transform;
        private readonly Transform parent;
        private readonly string name;
        private readonly bool active;
        private readonly Type[] componentTypes;

        private TransformState(Transform value)
        {
            transform = value;
            parent = value.parent;
            name = value.name;
            active = value.gameObject.activeSelf;
            componentTypes = value.GetComponents<Component>()
                .Select(component => component == null ? null : component.GetType()).ToArray();
        }

        public static TransformState Capture(Transform value) => new TransformState(value);

        public void AssertUnchanged()
        {
            Type[] currentTypes = transform.GetComponents<Component>()
                .Select(component => component == null ? null : component.GetType()).ToArray();
            if (transform == null || transform.parent != parent ||
                !string.Equals(transform.name, name, StringComparison.Ordinal) ||
                transform.gameObject.activeSelf != active || !componentTypes.SequenceEqual(currentTypes))
            {
                throw new InvalidOperationException(
                    $"기존 Hierarchy 또는 컴포넌트 구성이 변경되었습니다: {name}");
            }
        }
    }

    private enum Style { TopTab, AddHabit, AiReport }

    private sealed class Definition
    {
        public readonly string Name;
        public readonly string Path;
        public readonly string SpriteName;
        public readonly Style TargetStyle;
        public Definition(string name, string path, string spriteName, Style style)
        {
            Name = name;
            Path = path;
            SpriteName = spriteName;
            TargetStyle = style;
        }
    }

    private sealed class Target
    {
        public readonly Definition Definition;
        public readonly GameObject GameObject;
        public readonly RectTransform Rect;
        public readonly Button Button;
        public readonly Image Image;
        public readonly TMP_Text Text;
        public readonly RectTransform TextRect;
        public readonly LayoutElement Layout;
        public readonly Sprite Sprite;
        public readonly TMP_FontAsset Font;
        public Target(
            Definition definition, GameObject gameObject, RectTransform rect, Button button,
            Image image, TMP_Text text, LayoutElement layout, Sprite sprite, TMP_FontAsset font)
        {
            Definition = definition;
            GameObject = gameObject;
            Rect = rect;
            Button = button;
            Image = image;
            Text = text;
            TextRect = text.rectTransform;
            Layout = layout;
            Sprite = sprite;
            Font = font;
        }
    }

    private sealed class Result
    {
        public readonly List<string> Errors = new List<string>();
        public readonly List<Target> Targets = new List<Target>();
        public MonoBehaviour Manager;
        public TMP_FontAsset KoreanFont;
        public bool IsValid => Errors.Count == 0;
    }

    private sealed class SafetySnapshot
    {
        private readonly Target target;
        private readonly Component[] components;
        private readonly bool active;
        private readonly string buttonJson;
        private readonly UnityEngine.Object[] eventTargets;
        private readonly string[] eventMethods;
        private SafetySnapshot(Target target)
        {
            this.target = target;
            components = target.GameObject.GetComponents<Component>();
            active = target.GameObject.activeSelf;
            buttonJson = EditorJsonUtility.ToJson(target.Button);
            int count = target.Button.onClick.GetPersistentEventCount();
            eventTargets = Enumerable.Range(0, count)
                .Select(target.Button.onClick.GetPersistentTarget).ToArray();
            eventMethods = Enumerable.Range(0, count)
                .Select(target.Button.onClick.GetPersistentMethodName).ToArray();
        }
        public static SafetySnapshot Capture(Target target) => new SafetySnapshot(target);
        public void AssertUnchanged()
        {
            if (!components.SequenceEqual(target.GameObject.GetComponents<Component>()) ||
                active != target.GameObject.activeSelf ||
                buttonJson != EditorJsonUtility.ToJson(target.Button) ||
                eventTargets.Length != target.Button.onClick.GetPersistentEventCount())
            {
                throw new InvalidOperationException(
                    $"보호 대상 구성, 활성 상태 또는 Button 데이터가 변경되었습니다: {target.Definition.Path}");
            }
            for (int index = 0; index < eventTargets.Length; index++)
            {
                if (eventTargets[index] != target.Button.onClick.GetPersistentTarget(index) ||
                    eventMethods[index] != target.Button.onClick.GetPersistentMethodName(index))
                {
                    throw new InvalidOperationException($"OnClick이 변경되었습니다: {target.Definition.Path}");
                }
            }
        }
    }

    [Serializable]
    private sealed class RenewalSnapshot
    {
        public int Version;
        public string SceneGuid;
        public string CreatedUtc;
        public List<TargetSnapshot> Targets;
        public static RenewalSnapshot Capture(Result result) => new RenewalSnapshot
        {
            Version = SnapshotVersion,
            SceneGuid = AssetDatabase.AssetPathToGUID(ScenePath),
            CreatedUtc = DateTime.UtcNow.ToString("O"),
            Targets = result.Targets.Select(TargetSnapshot.Capture).ToList()
        };
    }

    [Serializable]
    private sealed class TargetSnapshot
    {
        public string Path;
        public SpriteReference Sprite;
        public Color ImageColor;
        public int ImageType;
        public bool PreserveAspect;
        public Vector2 ButtonSize;
        public string FontAssetPath;
        public Color TextColor;
        public int FontStyle;
        public int Alignment;
        public float FontSize;
        public Vector4 Margin;
        public Vector2 TextAnchorMin;
        public Vector2 TextAnchorMax;
        public Vector2 TextPosition;
        public Vector2 TextSize;
        public bool HasLayout;
        public float PreferredWidth;
        public float PreferredHeight;

        public static TargetSnapshot Capture(Target target) => new TargetSnapshot
        {
            Path = target.Definition.Path,
            Sprite = SpriteReference.Capture(target.Image.sprite),
            ImageColor = target.Image.color,
            ImageType = (int)target.Image.type,
            PreserveAspect = target.Image.preserveAspect,
            ButtonSize = target.Rect.sizeDelta,
            FontAssetPath = AssetDatabase.GetAssetPath(target.Text.font),
            TextColor = target.Text.color,
            FontStyle = (int)target.Text.fontStyle,
            Alignment = (int)target.Text.alignment,
            FontSize = target.Text.fontSize,
            Margin = target.Text.margin,
            TextAnchorMin = target.TextRect.anchorMin,
            TextAnchorMax = target.TextRect.anchorMax,
            TextPosition = target.TextRect.anchoredPosition,
            TextSize = target.TextRect.sizeDelta,
            HasLayout = target.Layout != null,
            PreferredWidth = target.Layout == null ? 0f : target.Layout.preferredWidth,
            PreferredHeight = target.Layout == null ? 0f : target.Layout.preferredHeight
        };

        public void Restore(Target target)
        {
            target.Image.sprite = Sprite.Resolve();
            target.Image.color = ImageColor;
            target.Image.type = (Image.Type)ImageType;
            target.Image.preserveAspect = PreserveAspect;
            if (target.Definition.TargetStyle == Style.TopTab)
            {
                target.Rect.sizeDelta = ButtonSize;
            }
            else if (target.Definition.TargetStyle == Style.AiReport)
            {
                target.Rect.sizeDelta = new Vector2(target.Rect.sizeDelta.x, ButtonSize.y);
            }
            target.Text.color = TextColor;
            target.Text.font = string.IsNullOrEmpty(FontAssetPath)
                ? null
                : AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
            target.Text.fontStyle = (FontStyles)FontStyle;
            target.Text.alignment = (TextAlignmentOptions)Alignment;
            target.Text.fontSize = FontSize;
            target.Text.margin = Margin;
            target.TextRect.anchorMin = TextAnchorMin;
            target.TextRect.anchorMax = TextAnchorMax;
            target.TextRect.anchoredPosition = TextPosition;
            target.TextRect.sizeDelta = TextSize;
            if (HasLayout)
            {
                if (target.Layout == null)
                {
                    throw new InvalidOperationException($"LayoutElement가 없습니다: {Path}");
                }
                target.Layout.preferredWidth = PreferredWidth;
                target.Layout.preferredHeight = PreferredHeight;
            }
            Dirty(target);
        }
    }

    [Serializable]
    private sealed class SpriteReference
    {
        public bool HasValue;
        public string GlobalId;
        public string AssetPath;
        public string Name;
        public static SpriteReference Capture(Sprite sprite) => sprite == null
            ? new SpriteReference { HasValue = false }
            : new SpriteReference
            {
                HasValue = true,
                GlobalId = GlobalObjectId.GetGlobalObjectIdSlow(sprite).ToString(),
                AssetPath = AssetDatabase.GetAssetPath(sprite),
                Name = sprite.name
            };
        public bool CanResolve() => !HasValue || Resolve() != null;
        public Sprite Resolve()
        {
            if (!HasValue)
            {
                return null;
            }
            if (!string.IsNullOrEmpty(GlobalId) && GlobalObjectId.TryParse(GlobalId, out GlobalObjectId id))
            {
                Sprite value = GlobalObjectId.GlobalObjectIdentifierToObjectSlow(id) as Sprite;
                if (value != null)
                {
                    return value;
                }
            }
            if (!string.IsNullOrEmpty(AssetPath))
            {
                Sprite value = AssetDatabase.LoadAllAssetsAtPath(AssetPath).OfType<Sprite>()
                    .FirstOrDefault(item => item.name == Name);
                if (value != null)
                {
                    return value;
                }
            }
            return Name == "UISprite"
                ? AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd")
                : null;
        }
    }
}

public sealed class HabitPvpUiRenewalSpriteImporter : AssetPostprocessor
{
    private const string GeneratedSpriteRoot = "Assets/UIRenewal/Generated/FreeCasualGUI/";

    [InitializeOnLoadMethod]
    private static void QueueImportSettingsValidation()
    {
        EditorApplication.delayCall += EnsureGeneratedSpriteImportSettings;
    }

    private static void EnsureGeneratedSpriteImportSettings()
    {
        string[] paths =
        {
            GeneratedSpriteRoot + "panel_type4.png",
            GeneratedSpriteRoot + "panel_type1.png",
            GeneratedSpriteRoot + "Pannel_time_orange.png"
        };
        foreach (string path in paths)
        {
            if (!TryGetBorder(path, out Vector4 border))
            {
                continue;
            }

            TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null || HasExpectedSettings(importer, border))
            {
                continue;
            }

            ApplySettings(importer, border);
            importer.SaveAndReimport();
            Debug.Log($"[HabitPVP UI Renewal] Sprite Import 설정 적용: {path}");
        }
    }

    private void OnPreprocessTexture()
    {
        if (!TryGetBorder(assetPath, out Vector4 border))
        {
            return;
        }

        TextureImporter importer = (TextureImporter)assetImporter;
        ApplySettings(importer, border);
    }

    private static void ApplySettings(TextureImporter importer, Vector4 border)
    {
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 200f;
        importer.spriteBorder = border;
        importer.mipmapEnabled = false;
        importer.alphaSource = TextureImporterAlphaSource.FromInput;
        importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.maxTextureSize = 4096;
    }

    private static bool HasExpectedSettings(TextureImporter importer, Vector4 border)
    {
        return importer.textureType == TextureImporterType.Sprite &&
               importer.spriteImportMode == SpriteImportMode.Single &&
               Mathf.Approximately(importer.spritePixelsPerUnit, 200f) &&
               importer.spriteBorder == border && !importer.mipmapEnabled &&
               importer.alphaSource == TextureImporterAlphaSource.FromInput &&
               importer.alphaIsTransparency && importer.npotScale == TextureImporterNPOTScale.None &&
               importer.wrapMode == TextureWrapMode.Clamp && importer.filterMode == FilterMode.Bilinear &&
               importer.textureCompression == TextureImporterCompression.Uncompressed &&
               importer.maxTextureSize == 4096;
    }

    private static bool TryGetBorder(string path, out Vector4 border)
    {
        switch (path)
        {
            case GeneratedSpriteRoot + "panel_type4.png":
            case GeneratedSpriteRoot + "panel_type1.png":
                border = new Vector4(96f, 96f, 96f, 96f);
                return true;
            case GeneratedSpriteRoot + "Pannel_time_orange.png":
                border = new Vector4(80f, 70f, 80f, 70f);
                return true;
            default:
                border = Vector4.zero;
                return false;
        }
    }
}
