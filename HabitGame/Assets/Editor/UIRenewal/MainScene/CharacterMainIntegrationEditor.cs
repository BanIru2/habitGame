using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Applies the approved Character Main preview styling to MainScene without saving the scene.
/// The shared Canvas prefab and every runtime reference/event are treated as protected data.
/// </summary>
public static class CharacterMainIntegrationEditor
{
    private const string ScenePath = "Assets/Scenes/MainScene.unity";
    private const string CanvasPrefabPath = "Assets/Resources/UIPrefabs/Canvas.prefab";
    private const string FontPath = "Assets/Login/Fonts/NotoSansKR Login SDF.asset";
    private const string CardSpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/HabitItem/panel_rpg_card.png";
    private const string CreamButtonSpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/HabitItem/Button_square_cream.png";
    private const string GoldButtonSpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/HabitItem/Button_square_rpg.png";
    private const string BarSpritePath =
        "Assets/Skyden_Games/Free_Casual_GUI/Demo/Sprites/Bars/Bar.png";
    private const string BarFillSpritePath =
        "Assets/Skyden_Games/Free_Casual_GUI/Demo/Sprites/Bars/Bar_Fill_02.png";
    private const string MenuRoot = "Tools/HabitPVP/UI Renewal/MainScene/15-D-5A Character Main/";

    private static readonly Color32 Cream = new Color32(0xF8, 0xEF, 0xD9, 0xFF);
    private static readonly Color32 CreamLight = new Color32(0xFF, 0xF9, 0xEA, 0xFF);
    private static readonly Color32 Brown = new Color32(0x5B, 0x36, 0x39, 0xFF);
    private static readonly Color32 MutedBrown = new Color32(0x86, 0x68, 0x5D, 0xFF);
    private static readonly Color32 Gold = new Color32(0xE7, 0xA8, 0x35, 0xFF);
    private static readonly Color32 Fire = new Color32(0xD9, 0x5B, 0x3D, 0xFF);
    private static readonly Color32 Water = new Color32(0x45, 0x91, 0xC9, 0xFF);
    private static readonly Color32 Grass = new Color32(0x65, 0xA8, 0x53, 0xFF);
    private static readonly Color32 Aurora = new Color32(0x9A, 0x68, 0xB5, 0xFF);

    private static readonly string[] CharacterReferenceNames =
    {
        "equippedTitleImage", "nameText", "atkStatText", "defStatText", "hpStatText",
        "spdStatText", "critStatText", "fireExpFill", "waterExpFill", "grassExpFill",
        "auroraExpFill", "fireLevelText", "fireEXPText", "waterLevelText", "waterEXPText",
        "grassLevelText", "grassEXPText", "auroraLevelText", "auroraEXPText", "titleManager",
        "characterPartsManager"
    };

    [MenuItem(MenuRoot + "Validate Structure", false, 2310)]
    private static void ValidateStructure()
    {
        Context context = BuildContext(true);
        if (!context.IsValid) return;

        Debug.Log("[HabitPVP 15-D-5A] 적용 전 구조 검증 성공.\n" + Diagnostics(context),
            context.CharacterTap);
    }

    [MenuItem(MenuRoot + "Dry Run", false, 2311)]
    private static void DryRun()
    {
        Context context = BuildContext(true);
        if (!context.IsValid) return;

        Debug.Log(
            "[HabitPVP 15-D-5A] Dry Run (파일 변경 없음)\n" + Diagnostics(context) + "\n" +
            "변경 예정: CharacterTap 배경, 이름/칭호 배치, 3개 기존 이동 버튼, " +
            "능력치 카드 5개, 속성 EXP 카드 4개, 캐릭터 표시 프레임, NotoSansKR 폰트.\n" +
            "레이아웃 제어: StatusBoxes=" + LayoutState(context.StatLayout) +
            ", AttributeLevelBars=" + LayoutState(context.AttributeLayout) +
            " (Apply 시 수동 3+2/4행 배치를 위해 비활성화).\n" +
            "속성 구성요소:\n" + AttributeTargetSummary(context) + "\n" +
            "보호: HabitTap/HabitUIManager, 공용 Canvas.prefab 원본, CharacterUIManager/" +
            "MainTabManager SerializeField, PartsManager, RawImage.texture, 모든 기존 Button UnityEvent, " +
            "activeSelf, CanvasGroup, TapBar 및 다른 탭.\n" +
            "충돌 가능성: Apply 후 저장하면 MainScene의 Canvas Prefab Instance에 CharacterTap 전용 " +
            "Override가 추가됩니다. 팀원의 일반 UI 작업이 같은 CharacterTap 또는 공통 Canvas 부모를 " +
            "수정한다면 병합 충돌 가능성이 있으므로 저장 전 Inspector Overrides를 확인하세요.",
            context.CharacterTap);
    }

    [MenuItem(MenuRoot + "Apply (No Auto Save)", false, 2312)]
    private static void Apply()
    {
        Context context = BuildContext(true);
        if (!context.IsValid) return;

        ProtectionSnapshot snapshot = ProtectionSnapshot.Capture(context);
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply 15-D-5A Character Main UI");

        try
        {
            ApplyCharacterStyle(context);
            snapshot.AssertUnchanged(context);
            Canvas.ForceUpdateCanvases();

            List<string> styleErrors = ValidateAppliedStyle(context);
            if (styleErrors.Count > 0)
                throw new InvalidOperationException(string.Join("\n", styleErrors));

            Undo.CollapseUndoOperations(undoGroup);
            Selection.activeGameObject = context.CharacterTap;
            Debug.Log(
                "[HabitPVP 15-D-5A] 적용 성공. Scene은 저장하지 않았습니다. " +
                "Inspector Overrides와 Game View를 확인한 뒤 사용자가 직접 저장하세요. " +
                "Canvas.prefab 원본, HabitTap, Manager/API/DTO 및 런타임 참조는 변경하지 않았습니다.",
                context.CharacterTap);
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(undoGroup);
            Debug.LogException(exception);
            Debug.LogError("[HabitPVP 15-D-5A] Apply 실패. 이번 실행의 변경을 Undo로 되돌렸습니다.");
        }
    }

    [MenuItem(MenuRoot + "Validate Applied Design", false, 2313)]
    private static void ValidateAppliedDesign()
    {
        Context context = BuildContext(true);
        if (!context.IsValid) return;

        List<string> errors = ValidateAppliedStyle(context);
        if (errors.Count == 0)
        {
            Debug.Log("[HabitPVP 15-D-5A] 적용 후 디자인 검증 성공. Scene은 자동 저장되지 않았습니다.",
                context.CharacterTap);
            return;
        }

        foreach (string error in errors)
            Debug.LogError("[HabitPVP 15-D-5A] " + error, context.CharacterTap);
    }

    [MenuItem(MenuRoot + "Apply (No Auto Save)", true)]
    [MenuItem(MenuRoot + "Validate Structure", true)]
    [MenuItem(MenuRoot + "Dry Run", true)]
    [MenuItem(MenuRoot + "Validate Applied Design", true)]
    private static bool CanRun() => !EditorApplication.isPlayingOrWillChangePlaymode &&
                                    SceneManager.GetActiveScene().path == ScenePath;

    private static Context BuildContext(bool log)
    {
        Context context = new Context { Scene = SceneManager.GetActiveScene() };
        if (!context.Scene.IsValid() || !context.Scene.isLoaded)
            context.Errors.Add("활성 Scene이 유효하지 않거나 로드되지 않았습니다.");
        else if (context.Scene.path != ScenePath)
            context.Errors.Add("MainScene에서만 실행할 수 있습니다. 현재: " + context.Scene.path);
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            context.Errors.Add("Edit Mode에서만 실행할 수 있습니다.");
        if (context.Errors.Count > 0) return Finish(context, log);

        Canvas[] canvases = context.Scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<Canvas>(true))
            .Where(canvas => canvas.transform.parent == null && canvas.name == "Canvas").ToArray();
        if (canvases.Length != 1)
        {
            context.Errors.Add("Scene 루트 Canvas가 정확히 하나여야 합니다. 현재: " + canvases.Length);
            return Finish(context, log);
        }

        context.Canvas = canvases[0].gameObject;
        context.CanvasPrefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(context.Canvas);
        if (context.CanvasPrefabPath != CanvasPrefabPath)
            context.Errors.Add("Canvas 원본 Prefab이 예상과 다릅니다: " + context.CanvasPrefabPath);
        if (!PrefabUtility.IsPartOfPrefabInstance(context.Canvas))
            context.Errors.Add("MainScene Canvas가 Prefab Instance가 아닙니다.");

        context.HabitTap = Find(context.Canvas, "HabitTap")?.gameObject;
        context.CharacterTap = Find(context.Canvas, "CharacterTap")?.gameObject;
        context.TapBar = Find(context.Canvas, "TapBar")?.gameObject;
        Require(context, context.HabitTap, "Canvas/HabitTap");
        Require(context, context.CharacterTap, "Canvas/CharacterTap");
        Require(context, context.TapBar, "Canvas/TapBar");
        if (!context.IsValid) return Finish(context, log);

        context.BackgroundImage = context.CharacterTap.GetComponent<Image>();

        context.CharacterManager = FindUnique<CharacterUIManager>(context);
        context.HabitManager = FindUnique<HabitUIManager>(context);
        context.TabManager = FindUnique<MainTabManager>(context);
        Require(context, context.CharacterManager, "CharacterUIManager");
        Require(context, context.HabitManager, "HabitUIManager");
        Require(context, context.TabManager, "MainTabManager");
        if (!context.IsValid) return Finish(context, log);

        if (context.CharacterTap.transform.parent != context.HabitTap.transform.parent ||
            context.CharacterTap.transform.parent != context.TapBar.transform.parent)
            context.Errors.Add("HabitTap, CharacterTap, TapBar가 공통 Canvas 부모의 형제가 아닙니다.");

        SerializedObject character = new SerializedObject(context.CharacterManager);
        foreach (string propertyName in CharacterReferenceNames)
        {
            SerializedProperty property = character.FindProperty(propertyName);
            if (property == null || property.objectReferenceValue == null)
                context.Errors.Add("CharacterUIManager 참조가 비어 있습니다: " + propertyName);
        }

        context.PartsManager = character.FindProperty("characterPartsManager")?.objectReferenceValue as Component;
        UnityEngine.Object boundName = character.FindProperty("nameText")?.objectReferenceValue;
        if (boundName is Component boundNameComponent &&
            !boundNameComponent.transform.IsChildOf(context.CharacterTap.transform))
            context.Errors.Add("CharacterUIManager 표시 참조가 CharacterTap 밖을 가리킵니다: nameText");
        context.NameText = Find<TMP_Text>(context, "NameText");
        context.TitleImage = Find<Image>(context, "TitleImage");
        context.TitleButton = Find<Button>(context, "TitleButton");
        context.AchievementButton = Find<Button>(context, "AchievementButton");
        context.StatusBackground = Find<Image>(context, "StatusBackgroundImage");
        context.StatusBoxes = Find<RectTransform>(context, "StatusBackgroundImage/Content/StatusBoxes");
        context.AttributeBars = Find<RectTransform>(context,
            "StatusBackgroundImage/Content/AttributeLevelBars");
        context.StatLayout = context.StatusBoxes != null
            ? context.StatusBoxes.GetComponent<LayoutGroup>()
            : null;
        context.AttributeLayout = context.AttributeBars != null
            ? context.AttributeBars.GetComponent<LayoutGroup>()
            : null;
        context.InventoryButton = Find<Button>(context,
            "StatusBackgroundImage/Content/InventoryButton");

        foreach (string name in new[] { "AtkBox", "DefBox", "HpBox", "SpdBox", "CritBox" })
            AddIfPresent(context.StatCards, Find<Image>(context,
                "StatusBackgroundImage/Content/StatusBoxes/" + name));
        foreach (Image card in context.StatCards)
        {
            StatCardParts parts = ResolveStatCardParts(card);
            context.StatRows[card.GetInstanceID()] = parts;
            foreach (string error in parts.Errors)
                context.Errors.Add(error);
        }
        foreach (string name in new[] { "FireLevelBar", "WaterLevelBar", "GrassLevelBar", "AuroraLevelBar" })
            AddIfPresent(context.AttributeCards, Find<Image>(context,
                "StatusBackgroundImage/Content/AttributeLevelBars/" + name));

        string[] fillProperties = { "fireExpFill", "waterExpFill", "grassExpFill", "auroraExpFill" };
        for (int index = 0; index < context.AttributeCards.Count; index++)
        {
            Image row = context.AttributeCards[index];
            AttributeRowParts parts = ResolveAttributeRowParts(row);
            context.AttributeRows[row.GetInstanceID()] = parts;
            foreach (string error in parts.Errors)
                context.Errors.Add(error);

            Image managerFill = character.FindProperty(fillProperties[index])?.objectReferenceValue as Image;
            if (parts.Fill != null && parts.Fill != managerFill)
                context.Errors.Add(row.name + "에서 찾은 EXP Fill이 CharacterUIManager." +
                                   fillProperties[index] + " 참조와 다릅니다. 검색=" +
                                   PathOf(parts.Fill.transform) + ", Manager=" +
                                   PathOf(managerFill?.transform));
        }
        Sprite[] iconSprites = context.AttributeRows.Values
            .Select(parts => parts.IconSprite).Where(sprite => sprite != null).ToArray();
        if (iconSprites.Length == 4 && iconSprites.Distinct().Count() != 4)
            context.Errors.Add("네 속성 아이콘이 서로 다른 Sprite를 사용하지 않습니다: " +
                               string.Join(", ", context.AttributeRows.Values.Select(parts =>
                                   parts.Row.name + "=" + (parts.IconSprite == null
                                       ? "null"
                                       : AssetDatabase.GetAssetPath(parts.IconSprite)))));

        RawImage[] characterImages = context.CharacterTap.GetComponentsInChildren<RawImage>(true)
            .Where(image => image.texture != null && image.texture.name == "CharView_RT").ToArray();
        if (characterImages.Length != 1)
            context.Errors.Add("CharView_RT를 사용하는 RawImage가 CharacterTap에 정확히 하나여야 합니다. 현재: " +
                               characterImages.Length);
        else
            context.CharacterDisplay = characterImages[0];

        context.Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        context.CardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CardSpritePath);
        context.CreamButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CreamButtonSpritePath);
        context.GoldButtonSprite = AssetDatabase.LoadAssetAtPath<Sprite>(GoldButtonSpritePath);
        context.BarSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BarSpritePath);
        context.BarFillSprite = AssetDatabase.LoadAssetAtPath<Sprite>(BarFillSpritePath);
        if (context.Font == null || context.CardSprite == null || context.CreamButtonSprite == null ||
            context.GoldButtonSprite == null || context.BarSprite == null || context.BarFillSprite == null)
            context.Errors.Add("필요한 NotoSansKR 또는 Free Casual GUI 에셋을 찾지 못했습니다.");

        if (context.StatCards.Count != 5) context.Errors.Add("능력치 카드가 5개가 아닙니다.");
        if (context.AttributeCards.Count != 4) context.Errors.Add("속성 EXP 카드가 4개가 아닙니다.");
        return Finish(context, log);
    }

    private static void ApplyCharacterStyle(Context context)
    {
        Image background = EnsureCharacterBackground(context);
        SetImage(background, null, Cream, Image.Type.Simple);
        SetStretch(context.CharacterTap.GetComponent<RectTransform>(), Vector2.zero, Vector2.zero);
        SetText(context.NameText, context.Font, 50f, Brown, TextAlignmentOptions.Center);
        SetCentered(context.NameText.rectTransform, new Vector2(920f, 68f), new Vector2(0f, 845f));
        SetCentered(context.TitleImage.rectTransform, new Vector2(520f, 70f), new Vector2(0f, 775f));
        Record(context.TitleImage);
        context.TitleImage.preserveAspect = true;
        Dirty(context.TitleImage);

        StyleButton(context.TitleButton, context.GoldButtonSprite, context.Font,
            new Vector2(-240f, 230f), "칭호", new Vector2(400f, 92f));
        StyleButton(context.AchievementButton, context.CreamButtonSprite, context.Font,
            new Vector2(240f, 230f), "업적", new Vector2(400f, 92f));

        SetImage(context.StatusBackground, context.CardSprite, CreamLight, Image.Type.Sliced);
        SetCentered(context.StatusBackground.rectTransform, new Vector2(1000f, 920f),
            new Vector2(0f, -265f));
        RectTransform content = context.StatusBackground.transform.Find("Content") as RectTransform;
        SetStretch(content, new Vector2(36f, 32f), new Vector2(-36f, -32f));

        TMP_Text statusHeading = content?.Find("StatusText")?.GetComponent<TMP_Text>();
        TMP_Text attributeHeading = content?.Find("AttributeLevelText")?.GetComponent<TMP_Text>();
        SetText(statusHeading, context.Font, 34f, Brown, TextAlignmentOptions.Left);
        SetCentered(statusHeading?.rectTransform, new Vector2(880f, 48f), new Vector2(0f, -105f));
        SetText(attributeHeading, context.Font, 34f, Brown, TextAlignmentOptions.Left);
        SetCentered(attributeHeading?.rectTransform, new Vector2(880f, 48f), new Vector2(0f, 385f));

        SetStretch(context.StatusBoxes, Vector2.zero, Vector2.zero);
        DisableAutomaticLayout(context.StatLayout);
        Vector2[] statPositions =
        {
            new Vector2(-290f, -195f), new Vector2(0f, -195f), new Vector2(290f, -195f),
            new Vector2(-145f, -315f), new Vector2(145f, -315f)
        };
        for (int index = 0; index < context.StatCards.Count; index++)
            StyleStatCard(context.StatCards[index], context, statPositions[index]);

        SetStretch(context.AttributeBars, Vector2.zero, Vector2.zero);
        DisableAutomaticLayout(context.AttributeLayout);
        float[] ys = { 310f, 200f, 90f, -20f };
        Color32[] colors = { Fire, Water, Grass, Aurora };
        for (int index = 0; index < context.AttributeCards.Count; index++)
            StyleAttributeCard(context.AttributeCards[index], context, ys[index], colors[index]);

        StyleButton(context.InventoryButton, context.GoldButtonSprite, context.Font,
            new Vector2(0f, -415f), "인벤토리", new Vector2(520f, 88f));

        RectTransform display = context.CharacterDisplay.rectTransform;
        SetCentered(display, new Vector2(420f, 420f), new Vector2(0f, 505f));
        Outline frame = context.CharacterDisplay.GetComponent<Outline>();
        if (frame == null) frame = Undo.AddComponent<Outline>(context.CharacterDisplay.gameObject);
        Record(frame);
        frame.effectColor = Gold;
        frame.effectDistance = new Vector2(5f, -5f);
        frame.useGraphicAlpha = true;
        Dirty(frame);

        foreach (TMP_Text text in context.CharacterTap.GetComponentsInChildren<TMP_Text>(true))
        {
            Record(text);
            text.font = context.Font;
            Dirty(text);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(context.StatusBackground.rectTransform);
    }

    private static void StyleStatCard(Image card, Context context, Vector2 position)
    {
        StatCardParts parts = context.StatRows[card.GetInstanceID()];
        SetImage(card, context.CardSprite, Color.white, Image.Type.Sliced);
        SetCentered(card.rectTransform, new Vector2(270f, 104f), position);
        SetText(parts.Label, context.Font, 22f, MutedBrown, TextAlignmentOptions.Center);
        SetCentered(parts.Label.rectTransform, new Vector2(230f, 34f), new Vector2(0f, 22f));
        SetText(parts.Value, context.Font, 28f, Brown, TextAlignmentOptions.Center);
        SetCentered(parts.Value.rectTransform, new Vector2(230f, 42f), new Vector2(0f, -22f));
    }

    private static void StyleAttributeCard(Image card, Context context, float y, Color32 accent)
    {
        AttributeRowParts parts = context.AttributeRows[card.GetInstanceID()];
        SetImage(card, context.CardSprite, Color.white, Image.Type.Sliced);
        SetCentered(card.rectTransform, new Vector2(880f, 104f), new Vector2(0f, y));
        Image fill = parts.Fill;
        Image bar = parts.Bar;
        Image icon = parts.Icon;
        if (bar == card)
            throw new InvalidOperationException(card.name +
                " 속성 행과 진행바 배경이 같은 Image를 참조합니다.");
        if (bar != null) SetImage(bar, context.BarSprite, Color.white, Image.Type.Sliced);
        if (fill != null) SetImage(fill, context.BarFillSprite, accent, fill.type);
        if (icon != null)
        {
            SetImage(icon, parts.IconSprite, Color.white, Image.Type.Simple);
            Record(icon);
            icon.preserveAspect = true;
            icon.raycastTarget = false;
            Dirty(icon);
        }
        SetCentered(icon?.rectTransform, new Vector2(64f, 64f), new Vector2(-400f, 0f));
        SetCentered(bar?.rectTransform, new Vector2(460f, 34f), new Vector2(-20f, 0f));
        SetHorizontalFill(fill?.rectTransform, new Vector2(460f, 30f), new Vector2(190f, 0f));

        TMP_Text[] texts = card.GetComponentsInChildren<TMP_Text>(true);
        TMP_Text level = parts.Level;
        TMP_Text exp = parts.Exp;
        TMP_Text label = parts.Label;
        if (label != null)
        {
            SetText(label, context.Font, 24f, Brown, TextAlignmentOptions.Center);
            SetCentered(label.rectTransform, new Vector2(90f, 50f), new Vector2(-315f, 0f));
        }
        if (level != null)
        {
            SetText(level, context.Font, 27f, Brown, TextAlignmentOptions.Center);
            SetCentered(level.rectTransform, new Vector2(190f, 42f), new Vector2(325f, 22f));
        }
        if (exp != null)
        {
            SetText(exp, context.Font, 23f, MutedBrown, TextAlignmentOptions.Center);
            SetCentered(exp.rectTransform, new Vector2(210f, 38f), new Vector2(325f, -23f));
        }
        foreach (TMP_Text text in texts.Where(text => text != label && text != level && text != exp))
            SetText(text, context.Font, 24f, Brown, TextAlignmentOptions.Center);

        context.AttributeApplyDiagnostics[card.GetInstanceID()] = RectState(card.rectTransform);
    }

    private static void StyleButton(Button button, Sprite sprite, TMP_FontAsset font,
        Vector2 position, string label, Vector2? size = null)
    {
        SetImage(button.GetComponent<Image>(), sprite, Color.white, Image.Type.Sliced);
        SetCentered(button.GetComponent<RectTransform>(), size ?? new Vector2(420f, 104f), position);
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            Record(text);
            text.text = label;
            SetText(text, font, 31f, Brown, TextAlignmentOptions.Center);
        }
        Record(button);
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color32(0xFF, 0xF4, 0xC9, 0xFF);
        colors.pressedColor = new Color32(0xD8, 0xB5, 0x6C, 0xFF);
        colors.selectedColor = colors.highlightedColor;
        button.colors = colors;
        Dirty(button);
    }

    private static List<string> ValidateAppliedStyle(Context context)
    {
        Canvas.ForceUpdateCanvases();
        List<string> errors = new List<string>();
        Image background = context.BackgroundImage != null
            ? context.BackgroundImage
            : context.CharacterTap.GetComponent<Image>();
        if (background == null)
        {
            errors.Add("CharacterTap 배경 Image가 없습니다. Apply가 배경 컴포넌트를 생성하지 못했습니다.");
        }
        else if (!Approximately(background.color, Cream))
        {
            errors.Add("CharacterTap 크림 배경색이 일치하지 않습니다. 대상=" +
                       PathOf(background.transform) + ", 기대=" + ColorText(Cream) +
                       ", 실제=" + ColorText(background.color) +
                       ", PrefabInstance=" + PrefabUtility.IsPartOfPrefabInstance(background));
        }
        else if (background.raycastTarget)
        {
            errors.Add("CharacterTap 배경 Image의 Raycast Target이 켜져 있습니다.");
        }
        if (context.StatusBackground.sprite != context.CardSprite)
            errors.Add("캐릭터 정보 RPG 카드가 적용되지 않았습니다.");
        if (context.TitleButton.GetComponent<Image>()?.sprite != context.GoldButtonSprite ||
            context.AchievementButton.GetComponent<Image>()?.sprite != context.CreamButtonSprite ||
            context.InventoryButton.GetComponent<Image>()?.sprite != context.GoldButtonSprite)
            errors.Add("기존 이동 버튼의 RPG 스타일이 완전하지 않습니다.");
        if (context.CharacterTap.GetComponentsInChildren<TMP_Text>(true).Any(text => text.font != context.Font))
            errors.Add("CharacterTap에 NotoSansKR이 아닌 텍스트가 있습니다.");
        if (context.CharacterDisplay.texture == null || context.CharacterDisplay.texture.name != "CharView_RT")
            errors.Add("캐릭터 RawImage의 CharView_RT 연결이 손상되었습니다.");
        Outline frame = context.CharacterDisplay.GetComponent<Outline>();
        if (frame == null || frame.effectColor != (Color)Gold)
            errors.Add("캐릭터 표시 영역의 골드 프레임이 적용되지 않았습니다.");
        ValidateLayout(context, errors);
        return errors;
    }

    private static void ValidateLayout(Context context, ICollection<string> errors)
    {
        RectTransform root = context.CharacterTap.GetComponent<RectTransform>();
        RectTransform display = context.CharacterDisplay.rectTransform;
        RectTransform titleButton = context.TitleButton.GetComponent<RectTransform>();
        RectTransform achievementButton = context.AchievementButton.GetComponent<RectTransform>();
        RectTransform status = context.StatusBackground.rectTransform;

        if (context.AttributeLayout != null && context.AttributeLayout.enabled)
            errors.Add("AttributeLevelBars LayoutGroup이 활성 상태라 수동 속성 행 배치를 덮어쓸 수 있습니다: " +
                       LayoutState(context.AttributeLayout));
        if (context.StatLayout != null && context.StatLayout.enabled)
            errors.Add("StatusBoxes LayoutGroup이 활성 상태라 3+2 스탯 배치를 덮어쓸 수 있습니다: " +
                       LayoutState(context.StatLayout));

        if (Overlaps(root, display, titleButton, 6f) ||
            Overlaps(root, display, achievementButton, 6f))
            errors.Add("캐릭터 이미지가 칭호/업적 버튼과 겹칩니다.");
        if (Overlaps(root, display, status, 6f))
            errors.Add("캐릭터 이미지가 캐릭터 정보 카드 영역과 겹칩니다.");
        if (!Contains(root, root, display, 1f))
            errors.Add("캐릭터 이미지가 CharacterTap 화면 밖으로 나갑니다.");

        for (int first = 0; first < context.AttributeCards.Count; first++)
        {
            RectTransform card = context.AttributeCards[first].rectTransform;
            if (!Approximately(card.rect.size, new Vector2(880f, 104f), 0.1f))
                errors.Add("속성 행 크기가 880x104가 아닙니다: " + BoundsDiagnostic(root, card));
            if (!Contains(root, status, card, 1f))
                errors.Add("속성 카드가 정보 카드 경계를 침범합니다: " + PathOf(card));
            for (int second = first + 1; second < context.AttributeCards.Count; second++)
                if (Overlaps(root, card, context.AttributeCards[second].rectTransform, 2f))
                    errors.Add("속성 카드 행이 서로 겹칩니다: " + BoundsDiagnostic(root, card) +
                               " / " + BoundsDiagnostic(root, context.AttributeCards[second].rectTransform) +
                               ", Apply직후={" + AppliedState(context, card) + " / " +
                               AppliedState(context, context.AttributeCards[second].rectTransform) + "}" +
                               ", 부모 Layout=" + LayoutState(context.AttributeLayout));

            AttributeRowParts parts = context.AttributeRows[context.AttributeCards[first].GetInstanceID()];
            if (parts.Icon == null || parts.Icon.sprite == null)
                errors.Add("속성 아이콘 Sprite가 없습니다: " + PathOf(card));
            else if (parts.Icon.sprite != parts.IconSprite)
                errors.Add("속성 아이콘 Sprite가 Apply 대상과 다릅니다: " +
                           PathOf(parts.Icon.transform) + ", 기대=" +
                           AssetDatabase.GetAssetPath(parts.IconSprite) + ", 실제=" +
                           AssetDatabase.GetAssetPath(parts.Icon.sprite));
            else
            {
                if (parts.Icon.type != Image.Type.Simple || !parts.Icon.preserveAspect)
                    errors.Add("속성 아이콘 표시 설정이 Simple/Preserve Aspect가 아닙니다: " +
                               PathOf(parts.Icon.transform));
                if (!Approximately(parts.Icon.color, new Color32(255, 255, 255, 255)))
                    errors.Add("속성 아이콘 Tint가 흰색이 아닙니다: " +
                               PathOf(parts.Icon.transform) + ", 실제=" + ColorText(parts.Icon.color));
                if (!Approximately(parts.Icon.rectTransform.rect.size, new Vector2(64f, 64f), 0.1f) ||
                    !Approximately(parts.Icon.rectTransform.anchoredPosition,
                        new Vector2(-400f, 0f), 0.1f))
                    errors.Add("속성 아이콘 RectTransform이 적용 좌표와 다릅니다: " +
                               RectState(parts.Icon.rectTransform));
            }
            RectTransform[] contentRects = AttributeContentRects(parts).ToArray();
            if (contentRects.Length != 5)
                errors.Add("속성 행의 아이콘/라벨/진행바/레벨/EXP 요소를 모두 찾지 못했습니다: " +
                           PathOf(card));
            foreach (RectTransform item in contentRects)
                if (!Contains(root, card, item, 1f))
                    errors.Add("속성 행 내부 요소가 카드 경계를 침범합니다: " + PathOf(item));

            Image fill = parts.Fill;
            Image progress = parts.Bar;
            if (progress == null || progress == context.AttributeCards[first] || fill == null ||
                fill.transform.parent != card.transform)
                errors.Add("속성 행과 진행바 역할이 분리되지 않았습니다: 행=" + PathOf(card) +
                           ", 진행바=" + PathOf(progress?.transform) +
                           ", Fill=" + PathOf(fill?.transform));
        }

        if (context.AttributeCards.Select(card =>
                Mathf.RoundToInt(card.rectTransform.anchoredPosition.y * 100f)).Distinct().Count() != 4)
            errors.Add("네 속성 행의 anchoredPosition.y가 서로 다르지 않습니다: " +
                       string.Join(", ", context.AttributeCards.Select(card =>
                           card.name + "=" + card.rectTransform.anchoredPosition.y.ToString("F2"))));
        Sprite[] appliedIcons = context.AttributeCards.Select(card =>
                context.AttributeRows[card.GetInstanceID()].Icon?.sprite)
            .Where(sprite => sprite != null).ToArray();
        if (appliedIcons.Length != 4 || appliedIcons.Distinct().Count() != 4)
            errors.Add("네 속성 아이콘이 모두 유효하고 서로 다른 Sprite여야 합니다: " +
                       string.Join(", ", context.AttributeCards.Select(card =>
                       {
                           Sprite sprite = context.AttributeRows[card.GetInstanceID()].Icon?.sprite;
                           return card.name + "=" + (sprite == null ? "null" :
                               AssetDatabase.GetAssetPath(sprite));
                       })));

        for (int first = 0; first < context.StatCards.Count; first++)
        {
            RectTransform card = context.StatCards[first].rectTransform;
            StatCardParts parts = context.StatRows[context.StatCards[first].GetInstanceID()];
            if (!Contains(root, root, card, 1f))
                errors.Add("스탯 카드가 CharacterTap 화면 밖으로 나갑니다: " + PathOf(card));
            if (!Contains(root, status, card, 1f))
                errors.Add("스탯 카드가 정보 카드 경계를 침범합니다: " + PathOf(card));
            for (int second = first + 1; second < context.StatCards.Count; second++)
                if (Overlaps(root, card, context.StatCards[second].rectTransform, 2f))
                    errors.Add("스탯 카드가 서로 겹칩니다: " + card.name + ", " +
                               context.StatCards[second].name);
            if (!Contains(root, card, parts.Label.rectTransform, 1f))
                errors.Add("스탯 이름이 카드 밖으로 나갑니다: " +
                           BoundsDiagnostic(root, parts.Label.rectTransform) + " / 카드=" +
                           BoundsDiagnostic(root, card));
            if (!Contains(root, card, parts.Value.rectTransform, 1f))
                errors.Add("스탯 수치가 카드 밖으로 나갑니다: " +
                           BoundsDiagnostic(root, parts.Value.rectTransform) + " / 카드=" +
                           BoundsDiagnostic(root, card));
            if (Overlaps(root, parts.Label.rectTransform, parts.Value.rectTransform, 2f))
                errors.Add("스탯 이름과 수치가 겹칩니다: " + card.name + " / " +
                           BoundsDiagnostic(root, parts.Label.rectTransform) + " / " +
                           BoundsDiagnostic(root, parts.Value.rectTransform));
            if (parts.Label.alignment != TextAlignmentOptions.Center ||
                parts.Value.alignment != TextAlignmentOptions.Center)
                errors.Add("스탯 이름/수치가 중앙 정렬이 아닙니다: " + card.name);
        }

        if (!HasThreePlusTwoStatGrid(context.StatCards))
            errors.Add("스탯 카드가 1행 3개 + 2행 2개의 중앙 정렬 그리드가 아닙니다.");

        RectTransform canvas = context.Canvas.GetComponent<RectTransform>();
        RectTransform inventory = context.InventoryButton.GetComponent<RectTransform>();
        RectTransform tapBar = context.TapBar.GetComponent<RectTransform>();
        if (context.StatCards.Any(card => Overlaps(root, card.rectTransform, inventory, 2f)))
            errors.Add("인벤토리 버튼이 하단 스탯 카드와 겹칩니다.");
        if (Overlaps(canvas, inventory, tapBar, 4f))
            errors.Add("인벤토리 버튼이 하단 TapBar와 겹칩니다.");
    }

    private static IEnumerable<RectTransform> AttributeContentRects(AttributeRowParts parts)
    {
        return new RectTransform[]
            {
                parts.Icon?.rectTransform, parts.Label?.rectTransform, parts.Bar?.rectTransform,
                parts.Level?.rectTransform, parts.Exp?.rectTransform
            }
            .Where(value => value != null);
    }

    private static AttributeRowParts ResolveAttributeRowParts(Image row)
    {
        AttributeRowParts parts = new AttributeRowParts { Row = row };
        if (row == null)
        {
            parts.Errors.Add("속성 행 Image가 null입니다.");
            return parts;
        }

        Image[] directImages = row.GetComponentsInChildren<Image>(true)
            .Where(image => image != row && image.transform.parent == row.transform).ToArray();
        TMP_Text[] directTexts = row.GetComponentsInChildren<TMP_Text>(true)
            .Where(text => text.transform.parent == row.transform).ToArray();

        parts.Bar = Unique(directImages.Where(image =>
            image.name.Equals("EXPBarBase", StringComparison.OrdinalIgnoreCase)),
            parts, "진행바 배경", "EXPBarBase", "Image");
        parts.Fill = Unique(directImages.Where(image =>
            image.name.IndexOf("EXPBarFill", StringComparison.OrdinalIgnoreCase) >= 0),
            parts, "EXP Fill", "<Attribute>EXPBarFill", "Image");

        Image[] iconCandidates = directImages
            .Where(image => image != parts.Bar && image != parts.Fill).ToArray();
        parts.Icon = Unique(iconCandidates, parts, "속성 아이콘",
            "<Attribute>Icon 또는 행의 유일한 나머지 Image", "Image");
        parts.CurrentIconSprite = parts.Icon != null ? parts.Icon.sprite : null;
        parts.SourceIcon = parts.Icon != null
            ? PrefabUtility.GetCorrespondingObjectFromSource(parts.Icon)
            : null;
        parts.IconSprite = parts.SourceIcon != null ? parts.SourceIcon.sprite : null;
        if (parts.Icon != null && parts.SourceIcon == null)
            parts.Errors.Add(PathOf(parts.Icon.transform) +
                             " 속성 아이콘의 Canvas.prefab 원본 Image를 찾지 못했습니다.");
        else if (parts.SourceIcon != null && parts.IconSprite == null)
            parts.Errors.Add(PathOf(parts.SourceIcon.transform) +
                             " Canvas.prefab 속성 아이콘의 Source Image(Sprite)가 비어 있습니다.");

        parts.Level = Unique(directTexts.Where(text =>
            text.name.IndexOf("LevelText", StringComparison.OrdinalIgnoreCase) >= 0),
            parts, "레벨", "<Attribute>LevelText", "TMP_Text");
        parts.Exp = Unique(directTexts.Where(text =>
            text.name.IndexOf("EXPText", StringComparison.OrdinalIgnoreCase) >= 0),
            parts, "EXP 수치", "<Attribute>EXPText", "TMP_Text");
        parts.Label = Unique(directTexts.Where(text => text != parts.Level && text != parts.Exp),
            parts, "속성 이름", "<Attribute>Text", "TMP_Text");

        if (parts.Errors.Count > 0)
            parts.Errors.Add("실제 직계 자식: " + string.Join(", ", row.transform.Cast<Transform>()
                .Select(child => PathOf(child) + " [" + ComponentTypes(child.gameObject) + "]")));
        return parts;
    }

    private static StatCardParts ResolveStatCardParts(Image card)
    {
        StatCardParts parts = new StatCardParts { Card = card };
        if (card == null)
        {
            parts.Errors.Add("스탯 카드 Image가 null입니다.");
            return parts;
        }

        TMP_Text[] texts = card.GetComponentsInChildren<TMP_Text>(true)
            .Where(text => text.transform.parent == card.transform).ToArray();
        parts.Value = UniqueStat(texts.Where(text =>
            text.name.IndexOf("StatusText", StringComparison.OrdinalIgnoreCase) >= 0),
            parts, "수치", "<Stat>StatusText");
        parts.Label = UniqueStat(texts.Where(text => text != parts.Value),
            parts, "이름", "ATKText/DEFText/HPText/SPDText/CRITText");
        if (parts.Errors.Count > 0)
            parts.Errors.Add("실제 직계 자식: " + string.Join(", ", card.transform.Cast<Transform>()
                .Select(child => PathOf(child) + " [" + ComponentTypes(child.gameObject) + "]")));
        return parts;
    }

    private static TMP_Text UniqueStat(IEnumerable<TMP_Text> candidates, StatCardParts parts,
        string role, string expectedName)
    {
        TMP_Text[] values = candidates.Where(value => value != null).ToArray();
        if (values.Length == 1) return values[0];
        parts.Errors.Add(PathOf(parts.Card?.transform) + "의 스탯 " + role +
                         " 텍스트 검색 실패. 기대=" + expectedName + " (TMP_Text), 발견=" +
                         (values.Length == 0 ? "없음" : string.Join(", ", values.Select(value =>
                             PathOf(value.transform)))));
        return null;
    }

    private static T Unique<T>(IEnumerable<T> candidates, AttributeRowParts parts, string role,
        string expectedName, string expectedType) where T : Component
    {
        T[] values = candidates.Where(value => value != null).ToArray();
        if (values.Length == 1) return values[0];

        string rowPath = PathOf(parts.Row?.transform);
        string found = values.Length == 0
            ? "없음"
            : string.Join(", ", values.Select(value =>
                PathOf(value.transform) + " [" + ComponentTypes(value.gameObject) + "]"));
        parts.Errors.Add(rowPath + "의 " + role + " 검색 실패. 기대 이름/규칙='" +
                         expectedName + "', 기대 타입=" + expectedType +
                         ", 발견=" + found + ", 원인=" +
                         (values.Length == 0 ? "해당 행의 직계 자식 범위에 일치 요소가 없음" :
                             "동일 역할 후보가 여러 개라 안전하게 선택할 수 없음"));
        return null;
    }

    private static string ComponentTypes(GameObject target)
    {
        return target == null
            ? "null"
            : string.Join(", ", target.GetComponents<Component>()
                .Where(component => component != null)
                .Select(component => component.GetType().Name));
    }

    private static string AttributeTargetSummary(Context context)
    {
        return string.Join("\n", context.AttributeCards.Select(row =>
        {
            if (!context.AttributeRows.TryGetValue(row.GetInstanceID(), out AttributeRowParts parts))
                return "- " + PathOf(row.transform) + ": 매핑 없음";
            return "- " + PathOf(row.transform) +
                   " | Icon=" + PathOf(parts.Icon?.transform) +
                   " (현재=" + (parts.CurrentIconSprite == null ? "null" :
                       AssetDatabase.GetAssetPath(parts.CurrentIconSprite)) +
                   ", 적용=" + (parts.IconSprite == null ? "null" :
                       AssetDatabase.GetAssetPath(parts.IconSprite)) + ")" +
                   " | Label=" + PathOf(parts.Label?.transform) +
                   " | Bar=" + PathOf(parts.Bar?.transform) +
                   " | Fill=" + PathOf(parts.Fill?.transform) +
                   " | Level=" + PathOf(parts.Level?.transform) +
                   " | EXP=" + PathOf(parts.Exp?.transform);
        }));
    }

    private static bool HasThreePlusTwoStatGrid(IReadOnlyList<Image> cards)
    {
        if (cards.Count != 5) return false;
        const float tolerance = 1f;
        float firstRowY = cards[0].rectTransform.anchoredPosition.y;
        float secondRowY = cards[3].rectTransform.anchoredPosition.y;
        bool rows = Mathf.Abs(cards[1].rectTransform.anchoredPosition.y - firstRowY) <= tolerance &&
                    Mathf.Abs(cards[2].rectTransform.anchoredPosition.y - firstRowY) <= tolerance &&
                    Mathf.Abs(cards[4].rectTransform.anchoredPosition.y - secondRowY) <= tolerance &&
                    firstRowY > secondRowY + cards[0].rectTransform.rect.height;
        bool centered = Mathf.Abs(cards[1].rectTransform.anchoredPosition.x) <= tolerance &&
                        Mathf.Abs(cards[0].rectTransform.anchoredPosition.x +
                                  cards[2].rectTransform.anchoredPosition.x) <= tolerance &&
                        Mathf.Abs(cards[3].rectTransform.anchoredPosition.x +
                                  cards[4].rectTransform.anchoredPosition.x) <= tolerance;
        return rows && centered;
    }

    private static bool Contains(RectTransform reference, RectTransform outer, RectTransform inner,
        float tolerance)
    {
        Bounds outerBounds = BoundsIn(reference, outer);
        Bounds innerBounds = BoundsIn(reference, inner);
        return innerBounds.min.x >= outerBounds.min.x - tolerance &&
               innerBounds.max.x <= outerBounds.max.x + tolerance &&
               innerBounds.min.y >= outerBounds.min.y - tolerance &&
               innerBounds.max.y <= outerBounds.max.y + tolerance;
    }

    private static bool Overlaps(RectTransform reference, RectTransform first, RectTransform second,
        float requiredGap)
    {
        Bounds a = BoundsIn(reference, first);
        Bounds b = BoundsIn(reference, second);
        return a.min.x < b.max.x + requiredGap && a.max.x > b.min.x - requiredGap &&
               a.min.y < b.max.y + requiredGap && a.max.y > b.min.y - requiredGap;
    }

    private static Bounds BoundsIn(RectTransform reference, RectTransform target)
    {
        Vector3[] corners = new Vector3[4];
        target.GetWorldCorners(corners);
        Vector3 first = reference.InverseTransformPoint(corners[0]);
        Bounds bounds = new Bounds(first, Vector3.zero);
        for (int index = 1; index < corners.Length; index++)
            bounds.Encapsulate(reference.InverseTransformPoint(corners[index]));
        return bounds;
    }

    private static string BoundsDiagnostic(RectTransform reference, RectTransform target)
    {
        Bounds bounds = BoundsIn(reference, target);
        return target.name + "[min=(" + bounds.min.x.ToString("F2") + ", " +
               bounds.min.y.ToString("F2") + "), max=(" + bounds.max.x.ToString("F2") + ", " +
               bounds.max.y.ToString("F2") + "), size=(" + bounds.size.x.ToString("F2") + ", " +
               bounds.size.y.ToString("F2") + "), anchored=" + target.anchoredPosition +
               ", activeSelf=" + target.gameObject.activeSelf +
               ", activeInHierarchy=" + target.gameObject.activeInHierarchy + "]";
    }

    private static string RectState(RectTransform target)
    {
        return target.name + "[size=" + target.rect.size + ", sizeDelta=" + target.sizeDelta +
               ", anchorMin=" + target.anchorMin + ", anchorMax=" + target.anchorMax +
               ", pivot=" + target.pivot + ", anchored=" + target.anchoredPosition + "]";
    }

    private static string AppliedState(Context context, RectTransform target)
    {
        return context.AttributeApplyDiagnostics.TryGetValue(target.GetInstanceID(), out string state)
            ? state
            : "기록 없음";
    }

    private static string LayoutState(LayoutGroup layout)
    {
        return layout == null
            ? "none"
            : layout.GetType().Name + "(enabled=" + layout.enabled +
              ", active=" + layout.gameObject.activeInHierarchy + ")";
    }

    private static Image EnsureCharacterBackground(Context context)
    {
        Image background = context.CharacterTap.GetComponent<Image>();
        if (background == null)
            background = Undo.AddComponent<Image>(context.CharacterTap);

        context.BackgroundImage = background;
        Record(background);
        background.raycastTarget = false;
        background.maskable = true;
        Dirty(background);
        return background;
    }

    private static bool Approximately(Color actual, Color32 expected)
    {
        Color expectedColor = expected;
        const float tolerance = 0.0001f;
        return Mathf.Abs(actual.r - expectedColor.r) <= tolerance &&
               Mathf.Abs(actual.g - expectedColor.g) <= tolerance &&
               Mathf.Abs(actual.b - expectedColor.b) <= tolerance &&
               Mathf.Abs(actual.a - expectedColor.a) <= tolerance;
    }

    private static bool Approximately(Vector2 actual, Vector2 expected, float tolerance)
    {
        return Mathf.Abs(actual.x - expected.x) <= tolerance &&
               Mathf.Abs(actual.y - expected.y) <= tolerance;
    }

    private static string ColorText(Color color)
    {
        return $"RGBA({color.r:F6}, {color.g:F6}, {color.b:F6}, {color.a:F6})";
    }

    private static string Diagnostics(Context context)
    {
        PropertyModification[] modifications = PrefabUtility.GetPropertyModifications(context.Canvas);
        int overrideCount = modifications?.Length ?? 0;
        return "Scene: " + ScenePath + "\n" +
               "Canvas: Prefab Instance -> " + context.CanvasPrefabPath +
               " (현재 Property Override " + overrideCount + "개)\n" +
               "Hierarchy: Canvas/{HabitTap, CharacterTap, InventoryTap, ShopTap, PvPTap, TapBar}\n" +
               "캐릭터 UI: " + PathOf(context.CharacterTap.transform) +
               "/{NameText, TitleImage, TitleButton, AchievementButton, StatusBackgroundImage/...}\n" +
               "배경 Image: " + (context.BackgroundImage == null
                   ? "현재 없음 (Apply 시 CharacterTap에 1회 추가, Raycast Target 비활성)"
                   : PathOf(context.BackgroundImage.transform) + " / " + ColorText(context.BackgroundImage.color)) +
               "\n" +
               "CharacterUIManager: " + PathOf(context.CharacterManager.transform) +
               " (표시 참조는 CharacterTap에 연결)\n" +
               "PartsManager: " + PathOf(context.PartsManager?.transform) + "\n" +
               "RenderTexture: " + PathOf(context.CharacterDisplay.transform) + " -> " +
               context.CharacterDisplay.texture.name + "\n" +
               "속성 아이콘 Prefab 원본: 4개 고유 Sprite, 현재 Scene 복구 대상=" +
               context.AttributeRows.Values.Count(parts =>
                   parts.CurrentIconSprite != parts.IconSprite) + "개\n" +
               "HabitUIManager: " + PathOf(context.HabitManager.transform) +
               ", 보호 UI 영역: " + PathOf(context.HabitTap.transform) +
               " (CharacterTap과 형제 탭, 비변경)\n" +
               "실제 변경 소유: MainScene의 Canvas Prefab Instance/CharacterTap 전용 Override; " +
               "Canvas.prefab 원본은 비변경.";
    }

    private static Context Finish(Context context, bool log)
    {
        if (log)
            foreach (string error in context.Errors)
                Debug.LogError("[HabitPVP 15-D-5A] " + error,
                    context.CharacterTap != null ? context.CharacterTap : context.Canvas);
        return context;
    }

    private static T FindUnique<T>(Context context) where T : Component
    {
        T[] values = context.Scene.GetRootGameObjects()
            .SelectMany(root => root.GetComponentsInChildren<T>(true)).ToArray();
        if (values.Length != 1)
            context.Errors.Add(typeof(T).Name + "가 Scene에 정확히 1개여야 합니다. 현재: " + values.Length);
        return values.Length == 1 ? values[0] : null;
    }

    private static Transform Find(GameObject root, string path) => root?.transform.Find(path);

    private static T Find<T>(Context context, string path) where T : Component
    {
        T value = Find(context.CharacterTap, path)?.GetComponent<T>();
        if (value == null) context.Errors.Add("필수 UI를 찾을 수 없습니다: CharacterTap/" + path);
        return value;
    }

    private static void Require(Context context, UnityEngine.Object value, string label)
    {
        if (value == null) context.Errors.Add("필수 대상이 없습니다: " + label);
    }

    private static void AddIfPresent<T>(ICollection<T> values, T value) where T : class
    {
        if (value != null) values.Add(value);
    }

    private static void SetImage(Image image, Sprite sprite, Color color, Image.Type type)
    {
        if (image == null) return;
        Record(image);
        image.sprite = sprite;
        image.color = color;
        image.type = type;
        Dirty(image);
    }

    private static void SetText(TMP_Text text, TMP_FontAsset font, float size, Color color,
        TextAlignmentOptions alignment)
    {
        if (text == null) return;
        Record(text);
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.fontStyle = FontStyles.Bold;
        text.alignment = alignment;
        text.enableAutoSizing = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        Dirty(text);
    }

    private static void SetCentered(RectTransform rect, Vector2 size, Vector2 position)
    {
        if (rect == null) return;
        Record(rect);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        Dirty(rect);
    }

    private static void SetStretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        if (rect == null) return;
        Record(rect);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = min;
        rect.offsetMax = max;
        Dirty(rect);
    }

    private static void SetHorizontalFill(RectTransform rect, Vector2 size, Vector2 position)
    {
        if (rect == null) return;
        Record(rect);
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        Dirty(rect);
    }

    private static void DisableAutomaticLayout(LayoutGroup layout)
    {
        if (layout == null || !layout.enabled) return;
        Record(layout);
        layout.enabled = false;
        Dirty(layout);
    }

    private static void Record(UnityEngine.Object target)
    {
        if (target != null) Undo.RecordObject(target, "Apply 15-D-5A Character Main UI");
    }

    private static void Dirty(UnityEngine.Object target)
    {
        if (target != null && PrefabUtility.IsPartOfPrefabInstance(target))
            PrefabUtility.RecordPrefabInstancePropertyModifications(target);
        if (target != null) EditorUtility.SetDirty(target);
    }

    private static string PathOf(Transform transform)
    {
        if (transform == null) return "(none)";
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
        private readonly Dictionary<int, string> componentReferences;
        private readonly Dictionary<int, string> buttonEvents;
        private readonly Dictionary<int, bool> activeStates;
        private readonly Dictionary<int, string> canvasGroups;
        private readonly Texture renderTexture;
        private readonly string protectedHabitDigest;

        private ProtectionSnapshot(Context context)
        {
            Component[] protectedComponents = { context.CharacterManager, context.HabitManager, context.TabManager,
                context.PartsManager };
            componentReferences = protectedComponents.Where(value => value != null)
                .ToDictionary(value => value.GetInstanceID(), ObjectReferenceDigest);
            buttonEvents = context.Canvas.GetComponentsInChildren<Button>(true)
                .ToDictionary(value => value.GetInstanceID(), value => EventDigest(value.onClick));
            Transform[] transforms = context.Canvas.GetComponentsInChildren<Transform>(true);
            activeStates = transforms.ToDictionary(value => value.GetInstanceID(), value => value.gameObject.activeSelf);
            canvasGroups = context.Canvas.GetComponentsInChildren<CanvasGroup>(true)
                .ToDictionary(value => value.GetInstanceID(), value =>
                    value.alpha + "|" + value.interactable + "|" + value.blocksRaycasts + "|" + value.ignoreParentGroups);
            renderTexture = context.CharacterDisplay.texture;
            protectedHabitDigest = HierarchyDigest(context.HabitTap.transform);
        }

        public static ProtectionSnapshot Capture(Context context) => new ProtectionSnapshot(context);

        public void AssertUnchanged(Context context)
        {
            foreach (Component component in new Component[]
                     { context.CharacterManager, context.HabitManager, context.TabManager, context.PartsManager })
                if (component != null && componentReferences[component.GetInstanceID()] != ObjectReferenceDigest(component))
                    throw new InvalidOperationException(component.GetType().Name + " SerializeField 참조가 변경되었습니다.");
            foreach (Button button in context.Canvas.GetComponentsInChildren<Button>(true))
                if (buttonEvents.TryGetValue(button.GetInstanceID(), out string before) &&
                    before != EventDigest(button.onClick))
                    throw new InvalidOperationException("기존 Button UnityEvent가 변경되었습니다: " + PathOf(button.transform));
            foreach (Transform transform in context.Canvas.GetComponentsInChildren<Transform>(true))
                if (activeStates.TryGetValue(transform.GetInstanceID(), out bool before) &&
                    before != transform.gameObject.activeSelf)
                    throw new InvalidOperationException("초기 activeSelf가 변경되었습니다: " + PathOf(transform));
            foreach (CanvasGroup group in context.Canvas.GetComponentsInChildren<CanvasGroup>(true))
            {
                string current = group.alpha + "|" + group.interactable + "|" + group.blocksRaycasts + "|" +
                                 group.ignoreParentGroups;
                if (canvasGroups.TryGetValue(group.GetInstanceID(), out string before) && before != current)
                    throw new InvalidOperationException("CanvasGroup이 변경되었습니다: " + PathOf(group.transform));
            }
            if (context.CharacterDisplay.texture != renderTexture)
                throw new InvalidOperationException("CharacterDisplay RawImage.texture가 변경되었습니다.");
            if (protectedHabitDigest != HierarchyDigest(context.HabitTap.transform))
                throw new InvalidOperationException("HabitUIManager 관리 영역(HabitTap)이 변경되었습니다.");
        }

        private static string ObjectReferenceDigest(Component component)
        {
            SerializedObject serialized = new SerializedObject(component);
            SerializedProperty iterator = serialized.GetIterator();
            List<string> values = new List<string>();
            bool enterChildren = true;
            while (iterator.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (iterator.propertyType != SerializedPropertyType.ObjectReference) continue;
                UnityEngine.Object value = iterator.objectReferenceValue;
                values.Add(iterator.propertyPath + "=" + (value == null ? "null" :
                    GlobalObjectId.GetGlobalObjectIdSlow(value).ToString()));
            }
            return string.Join("|", values);
        }

        private static string EventDigest(UnityEventBase value)
        {
            StringBuilder digest = new StringBuilder();
            for (int index = 0; index < value.GetPersistentEventCount(); index++)
            {
                UnityEngine.Object target = value.GetPersistentTarget(index);
                digest.Append(target == null ? "null" : GlobalObjectId.GetGlobalObjectIdSlow(target).ToString())
                    .Append(':').Append(value.GetPersistentMethodName(index)).Append('|');
            }
            return digest.ToString();
        }

        private static string HierarchyDigest(Transform root)
        {
            return string.Join("|", root.GetComponentsInChildren<Transform>(true)
                .Select(value => PathOf(value) + ":" + value.gameObject.activeSelf));
        }
    }

    private sealed class AttributeRowParts
    {
        public Image Row;
        public Image Icon;
        public Image Bar;
        public Image Fill;
        public TMP_Text Label;
        public TMP_Text Level;
        public TMP_Text Exp;
        public Image SourceIcon;
        public Sprite CurrentIconSprite;
        public Sprite IconSprite;
        public readonly List<string> Errors = new List<string>();
    }

    private sealed class StatCardParts
    {
        public Image Card;
        public TMP_Text Label;
        public TMP_Text Value;
        public readonly List<string> Errors = new List<string>();
    }

    private sealed class Context
    {
        public Scene Scene;
        public GameObject Canvas, HabitTap, CharacterTap, TapBar;
        public string CanvasPrefabPath;
        public CharacterUIManager CharacterManager;
        public HabitUIManager HabitManager;
        public MainTabManager TabManager;
        public Component PartsManager;
        public TMP_FontAsset Font;
        public Sprite CardSprite, CreamButtonSprite, GoldButtonSprite, BarSprite, BarFillSprite;
        public TMP_Text NameText;
        public Image BackgroundImage, TitleImage, StatusBackground;
        public Button TitleButton, AchievementButton, InventoryButton;
        public RectTransform StatusBoxes, AttributeBars;
        public LayoutGroup StatLayout, AttributeLayout;
        public RawImage CharacterDisplay;
        public readonly List<Image> StatCards = new List<Image>();
        public readonly Dictionary<int, StatCardParts> StatRows =
            new Dictionary<int, StatCardParts>();
        public readonly List<Image> AttributeCards = new List<Image>();
        public readonly Dictionary<int, AttributeRowParts> AttributeRows =
            new Dictionary<int, AttributeRowParts>();
        public readonly Dictionary<int, string> AttributeApplyDiagnostics =
            new Dictionary<int, string>();
        public readonly List<string> Errors = new List<string>();
        public bool IsValid => Errors.Count == 0;
    }
}
