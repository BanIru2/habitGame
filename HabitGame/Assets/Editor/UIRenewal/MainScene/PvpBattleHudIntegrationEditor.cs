using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class PvpBattleHudIntegrationEditor
{
    private const string ScenePath = "Assets/Scenes/MainScene.unity";
    private const string CanvasPrefabPath = "Assets/Resources/UIPrefabs/Canvas.prefab";
    private const string FontPath = "Assets/Login/Fonts/NotoSansKR Login SDF.asset";
    private const string CardSpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/HabitItem/panel_rpg_card.png";
    private const string CreamSpritePath =
        "Assets/UIRenewal/Generated/FreeCasualGUI/HabitItem/Button_square_cream.png";
    private const string MenuRoot =
        "Tools/HabitPVP/UI Renewal/MainScene/15-D-7C Battle HUD/";
    private const string NameFrame = "__15D7C_NameCardFrame";
    private const string CharacterFrame = "__15D7C_CharacterFrame";
    private const string AttributeIcon = "__15D7C_AttributeIcon";

    private static readonly Color32 Cream = new Color32(0xF8, 0xEF, 0xD9, 0xFF);
    private static readonly Color32 CreamLight = new Color32(0xFF, 0xF9, 0xEA, 0xFF);
    private static readonly Color32 Brown = new Color32(0x5B, 0x36, 0x39, 0xFF);
    private static readonly Color32 MutedBrown = new Color32(0x86, 0x68, 0x5D, 0xFF);
    private static readonly Color32 Gold = new Color32(0xE7, 0xA8, 0x35, 0xFF);
    private static readonly Color32 PlayerHp = new Color32(0x64, 0xB8, 0x67, 0xFF);
    private static readonly Color32 OpponentHp = new Color32(0xD8, 0x65, 0x5B, 0xFF);

    [MenuItem(MenuRoot + "Validate Structure", false, 2390)]
    private static void ValidateStructureMenu()
    {
        Context context = Inspect(true);
        if (!context.IsValid) return;

        Debug.Log(
            "[HabitPVP 15-D-7C] Battle HUD 구조 검증 성공. Scene/Prefab을 변경하지 않았습니다.\n" +
            BuildDiagnostics(context), context.Battle);
    }

    [MenuItem(MenuRoot + "Dry Run", false, 2391)]
    private static void DryRunMenu()
    {
        Context context = Inspect(true);
        if (!context.IsValid)
        {
            Debug.LogError("[HabitPVP 15-D-7C] Dry Run 중단: 구조 오류를 먼저 해결해 주세요.");
            return;
        }

        Debug.Log(
            "[HabitPVP 15-D-7C] Battle HUD Dry Run (변경/Dirty/저장 없음)\n" +
            "- 이름/칭호: MyNameCard, OppNameCard에 크림·골드 RPG 카드 프레임을 추가하고 " +
            "기존 닉네임/칭호를 보존하면서 Ready 특성 Sprite용 Image를 카드별 1개 생성\n" +
            "- 플레이어 정보: 특성 아이콘과 닉네임을 일정 간격의 가로 그룹으로 취급해 " +
            "카드 중앙에 배치하고 긴 닉네임은 한 줄 Auto Size/Ellipsis 처리\n" +
            "- 배경 정리: 기존 무스프라이트 노란 NameCard Image만 비활성화하고 " +
            "칭호 Image와 신규 RPG 프레임은 유지\n" +
            "- HP: 기존 My/OppHpBarBackground를 RPG 프레임으로 스타일링하고 기존 Fill Image와 " +
            "HP 텍스트를 프레임 안에 배치 (type/method/origin/fillAmount 참조 보호)\n" +
            "- 턴: 기존 TurnTexts/TurnCountText/TurnText를 화면 중앙 상단 카드 형태로 재배치\n" +
            "- 캐릭터: MyCharacter/OppCharacter 안에 raycastTarget=false 장식 프레임을 각각 1개만 생성하고 " +
            "기존 RawImage의 texture/material/uvRect는 유지\n" +
            "- 제외: TimerText 활성화, ResultPanel, ResultWaitingPanel, 결과 버튼/이미지, 전투 기능 추가\n" +
            "- 보호: BattleUIManager/Photon/Battle/MainTab 참조, Button Persistent UnityEvent, activeSelf, " +
            "기존 객체명과 기존 객체 간 상대 sibling 순서, Canvas.prefab 원본\n" +
            "- 예상 Scene Override: Battle 하위 RectTransform/TMP/Image 속성과 " +
            NameFrame + " 2개, " + CharacterFrame + " 2개\n" +
            BuildDiagnostics(context), context.Battle);
    }

    [MenuItem(MenuRoot + "Apply (No Auto Save)", false, 2392)]
    private static void ApplyMenu()
    {
        Context context = Inspect(true);
        if (!context.IsValid) return;

        ProtectionSnapshot snapshot = ProtectionSnapshot.Capture(context);
        Hash128 prefabHash = AssetDatabase.GetAssetDependencyHash(CanvasPrefabPath);
        Undo.IncrementCurrentGroup();
        int group = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Apply 15-D-7C Battle HUD UI Renewal");

        try
        {
            ApplyDesign(context);
            snapshot.AssertUnchanged(context);

            if (AssetDatabase.GetAssetDependencyHash(CanvasPrefabPath) != prefabHash)
                throw new InvalidOperationException("Canvas.prefab 원본의 dependency hash가 변경되었습니다.");

            List<string> styleErrors = ValidateAppliedDesign(context);
            if (styleErrors.Count > 0)
                throw new InvalidOperationException(string.Join("\n", styleErrors));

            EditorSceneManager.MarkSceneDirty(context.Scene);
            Undo.CollapseUndoOperations(group);
            Selection.activeGameObject = context.Battle;
            Debug.Log(
                "[HabitPVP 15-D-7C] Battle HUD 적용 성공. MainScene은 dirty 상태이며 자동 저장하지 않았습니다. " +
                "Canvas.prefab 원본과 전투 런타임 참조는 변경하지 않았습니다.", context.Battle);
        }
        catch (Exception exception)
        {
            Undo.RevertAllDownToGroup(group);
            Debug.LogException(exception);
            Debug.LogError(
                "[HabitPVP 15-D-7C] Apply 실패. 이번 Scene 변경을 Undo로 복원했습니다. " +
                "Scene 저장이나 Prefab Apply는 수행하지 않았습니다.");
        }
    }

    [MenuItem(MenuRoot + "Validate Applied Design", false, 2393)]
    private static void ValidateAppliedDesignMenu()
    {
        Context context = Inspect(true);
        if (!context.IsValid) return;

        List<string> errors = ValidateAppliedDesign(context);
        if (errors.Count == 0)
        {
            Debug.Log(
                "[HabitPVP 15-D-7C] Battle HUD 적용 디자인 검증 성공. 파일을 변경하지 않았습니다.\n" +
                "정적 검증 범위: 1080x1920 기준 배치, 이름/칭호/HP/턴/캐릭터 영역, 런타임 참조, " +
                "결과 패널 표시 우선순위. 화면 비율 변화와 전투 애니메이션은 Play Mode 확인이 필요합니다.\n" +
                BuildDiagnostics(context), context.Battle);
            return;
        }

        foreach (string error in errors)
            Debug.LogError("[HabitPVP 15-D-7C] 적용 디자인 오류: " + error, context.Battle);
    }

    [MenuItem(MenuRoot + "Validate Structure", true)]
    [MenuItem(MenuRoot + "Dry Run", true)]
    [MenuItem(MenuRoot + "Apply (No Auto Save)", true)]
    [MenuItem(MenuRoot + "Validate Applied Design", true)]
    private static bool CanUseMenus()
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

        context.PvpTap = Find(context.Canvas, "PvPTap")?.gameObject;
        context.CanvasScaler = context.Canvas.GetComponent<CanvasScaler>();
        context.Battle = Find(context.PvpTap, "Battle")?.gameObject;
        context.TurnTexts = FindRect(context.Battle, "TurnTexts");
        context.TurnCountText = FindComponent<TMP_Text>(context.Battle, "TurnTexts/TurnCountText");
        context.TurnText = FindComponent<TMP_Text>(context.Battle, "TurnTexts/TurnText");
        context.NameCards = FindRect(context.Battle, "NameCards");
        context.NameCardsLayout = context.NameCards != null
            ? context.NameCards.GetComponent<HorizontalLayoutGroup>()
            : null;
        context.MyNameCard = FindRect(context.Battle, "NameCards/MyNameCard");
        context.OppNameCard = FindRect(context.Battle, "NameCards/OppNameCard");
        context.MyTitleImage = FindComponent<Image>(context.Battle,
            "NameCards/MyNameCard/MyTitleImage");
        context.OppTitleImage = FindComponent<Image>(context.Battle,
            "NameCards/OppNameCard/OppTitleImage");
        context.MyNameText = FindComponent<TMP_Text>(context.Battle,
            "NameCards/MyNameCard/MyNameText");
        context.OppNameText = FindComponent<TMP_Text>(context.Battle,
            "NameCards/OppNameCard/OppNameText");
        context.MyAttributeIcon = FindComponent<Image>(context.Battle,
            "NameCards/MyNameCard/" + AttributeIcon);
        context.OppAttributeIcon = FindComponent<Image>(context.Battle,
            "NameCards/OppNameCard/" + AttributeIcon);
        context.Characters = FindRect(context.Battle, "Charaters");
        context.CharactersLayout = context.Characters != null
            ? context.Characters.GetComponent<HorizontalLayoutGroup>()
            : null;
        context.MyCharacter = FindRect(context.Battle, "Charaters/MyCharacter");
        context.OppCharacter = FindRect(context.Battle, "Charaters/OppCharacter");
        context.MyHpBackground = FindComponent<Image>(context.Battle,
            "Charaters/MyCharacter/MyHpBarBackground");
        context.OppHpBackground = FindComponent<Image>(context.Battle,
            "Charaters/OppCharacter/OppHpBarBackground");
        context.MyHpBar = FindComponent<Image>(context.Battle, "Charaters/MyCharacter/MyHpBar");
        context.OppHpBar = FindComponent<Image>(context.Battle, "Charaters/OppCharacter/OppHpBar");
        context.MyHpText = FindComponent<TMP_Text>(context.Battle,
            "Charaters/MyCharacter/MyHpBar/MyHpText");
        context.OppHpText = FindComponent<TMP_Text>(context.Battle,
            "Charaters/OppCharacter/OppHpBar/OppHpText");
        context.MyCharacterImage = FindComponent<RawImage>(context.Battle,
            "Charaters/MyCharacter/MyCharacterImage");
        context.OppCharacterImage = FindComponent<RawImage>(context.Battle,
            "Charaters/OppCharacter/OppCharacterImage");
        context.ResultPanel = Find(context.Battle, "ResultPanel")?.gameObject;
        context.ResultWaitingPanel = Find(context.Battle, "ResultWaitingPanel")?.gameObject;
        context.TimerText = Find(context.Battle, "TimerText")?.gameObject;
        context.BattleUiManager = FindUnique<BattleUIManager>(context);
        context.BattleManager = FindUnique<BattleManager>(context);
        context.BattleBackendManager = FindUnique<BattleBackendManager>(context);
        context.PhotonManager = FindUnique<PhotonManager>(context);
        context.MainTabManager = FindUnique<MainTabManager>(context);
        context.Font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);
        context.CardSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CardSpritePath);
        context.CreamSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CreamSpritePath);

        Require(context, context.PvpTap, "Canvas/PvPTap");
        Require(context, context.CanvasScaler, "Canvas/CanvasScaler");
        Require(context, context.Battle, "Canvas/PvPTap/Battle");
        Require(context, context.TurnTexts, "Battle/TurnTexts");
        Require(context, context.TurnCountText, "Battle/TurnTexts/TurnCountText");
        Require(context, context.TurnText, "Battle/TurnTexts/TurnText");
        Require(context, context.NameCards, "Battle/NameCards");
        Require(context, context.NameCardsLayout, "Battle/NameCards/HorizontalLayoutGroup");
        Require(context, context.MyNameCard, "Battle/NameCards/MyNameCard");
        Require(context, context.OppNameCard, "Battle/NameCards/OppNameCard");
        Require(context, context.MyTitleImage, "MyNameCard/MyTitleImage");
        Require(context, context.OppTitleImage, "OppNameCard/OppTitleImage");
        Require(context, context.MyNameText, "MyNameCard/MyNameText");
        Require(context, context.OppNameText, "OppNameCard/OppNameText");
        Require(context, context.Characters, "Battle/Charaters (기존 이름 유지)");
        Require(context, context.CharactersLayout, "Battle/Charaters/HorizontalLayoutGroup");
        Require(context, context.MyCharacter, "Charaters/MyCharacter");
        Require(context, context.OppCharacter, "Charaters/OppCharacter");
        Require(context, context.MyHpBackground, "MyCharacter/MyHpBarBackground");
        Require(context, context.OppHpBackground, "OppCharacter/OppHpBarBackground");
        Require(context, context.MyHpBar, "MyCharacter/MyHpBar");
        Require(context, context.OppHpBar, "OppCharacter/OppHpBar");
        Require(context, context.MyHpText, "MyHpBar/MyHpText");
        Require(context, context.OppHpText, "OppHpBar/OppHpText");
        Require(context, context.MyCharacterImage, "MyCharacter/MyCharacterImage");
        Require(context, context.OppCharacterImage, "OppCharacter/OppCharacterImage");
        Require(context, context.ResultPanel, "Battle/ResultPanel");
        Require(context, context.ResultWaitingPanel, "Battle/ResultWaitingPanel");
        Require(context, context.TimerText, "Battle/TimerText");
        Require(context, context.Font, FontPath);
        Require(context, context.CardSprite, CardSpritePath);
        Require(context, context.CreamSprite, CreamSpritePath);

        if (context.BattleUiManager != null) ValidateManagerReferences(context);
        ValidateFunctionalStructure(context);
        return Finish(context, log);
    }

    private static void ValidateManagerReferences(Context context)
    {
        SerializedObject serialized = new SerializedObject(context.BattleUiManager);
        context.AttributeButtons = ReferenceArray<Button>(serialized, "attrButtons");
        CheckReference(context, serialized, "battlePanel", context.Battle);
        CheckReference(context, serialized, "resultPanel", context.ResultPanel);
        CheckReference(context, serialized, "resultWaitingPanel", context.ResultWaitingPanel);
        CheckReference(context, serialized, "myName", context.MyNameText);
        CheckReference(context, serialized, "oppName", context.OppNameText);
        CheckReference(context, serialized, "myBattleTitleImage", context.MyTitleImage);
        CheckReference(context, serialized, "oppBattleTitleImage", context.OppTitleImage);
        CheckReference(context, serialized, "myHpBar", context.MyHpBar);
        CheckReference(context, serialized, "oppHpBar", context.OppHpBar);
        CheckReference(context, serialized, "myHpText", context.MyHpText);
        CheckReference(context, serialized, "oppHpText", context.OppHpText);
        CheckReference(context, serialized, "myCharacterImage", context.MyCharacterImage);
        CheckReference(context, serialized, "oppCharacterImage", context.OppCharacterImage);
        CheckReference(context, serialized, "turnCountText", context.TurnCountText);

        if (context.AttributeButtons.Length != 4)
            context.Errors.Add("BattleUIManager.attrButtons 배열이 4개가 아닙니다.");
        else
            ValidateAttributeSpriteSources(context);

        foreach (string propertyName in new[]
                 {
                     "photonManager", "lobbyPanel", "loadingPanel", "readyPanel", "battlePanel",
                     "resultPanel", "resultWaitingPanel", "rankingboardManager", "checkButton"
                 })
            RequireObjectReference(context, serialized, propertyName);
    }

    private static void ValidateFunctionalStructure(Context context)
    {
        if (context.Battle == null) return;

        if (context.Battle.activeSelf)
            context.Errors.Add("Battle Root의 초기 activeSelf는 false여야 합니다.");
        if (context.TimerText != null && context.TimerText.activeSelf)
            context.Errors.Add("비활성 TimerText가 활성화되어 있습니다.");
        if (context.CanvasScaler != null &&
            context.CanvasScaler.referenceResolution != new Vector2(1080f, 1920f))
            context.Errors.Add("CanvasScaler 기준 해상도가 1080x1920이 아닙니다: " +
                               context.CanvasScaler.referenceResolution);

        ValidateHpImage(context, context.MyHpBar, "MyHpBar");
        ValidateHpImage(context, context.OppHpBar, "OppHpBar");
        ValidateRawImage(context, context.MyCharacterImage, "MyCharacterImage");
        ValidateRawImage(context, context.OppCharacterImage, "OppCharacterImage");

        if (context.ResultPanel != null && context.ResultPanel.transform.parent != context.Battle.transform)
            context.Errors.Add("ResultPanel이 Battle의 직접 자식이 아닙니다.");
        if (context.ResultWaitingPanel != null &&
            context.ResultWaitingPanel.transform.parent != context.Battle.transform)
            context.Errors.Add("ResultWaitingPanel이 Battle의 직접 자식이 아닙니다.");

        foreach (Button button in context.Battle.GetComponentsInChildren<Button>(true))
        {
            for (int index = 0; index < button.onClick.GetPersistentEventCount(); index++)
            {
                if (button.onClick.GetPersistentTarget(index) == null ||
                    string.IsNullOrEmpty(button.onClick.GetPersistentMethodName(index)))
                    context.Errors.Add("Button Persistent UnityEvent가 손상되었습니다: " +
                                       GetPath(button.transform));
            }
        }
    }

    private static void ValidateAttributeSpriteSources(Context context)
    {
        Dictionary<AttributeType, Sprite> sprites = new Dictionary<AttributeType, Sprite>();
        foreach (Button button in context.AttributeButtons)
        {
            AttributeButtonInfo info = button != null ? button.GetComponent<AttributeButtonInfo>() : null;
            if (info == null || info.attrType == AttributeType.None)
            {
                context.Errors.Add("Ready 특성 버튼의 AttributeButtonInfo 매핑이 없습니다.");
                continue;
            }

            Sprite sprite = button.GetComponentsInChildren<Image>(true)
                .Where(image => image != button.image && image.sprite != null)
                .Select(image => image.sprite).FirstOrDefault();
            if (sprite == null)
            {
                context.Errors.Add("Ready 특성 버튼의 기존 Sprite가 없습니다: " +
                                   GetPath(button.transform));
                continue;
            }

            if (sprites.ContainsKey(info.attrType))
                context.Errors.Add("Ready 특성 enum 매핑이 중복되었습니다: " + info.attrType);
            else
                sprites.Add(info.attrType, sprite);
        }

        foreach (AttributeType type in new[]
                 {
                     AttributeType.Fire, AttributeType.Water,
                     AttributeType.Grass, AttributeType.Aurora
                 })
            if (!sprites.ContainsKey(type))
                context.Errors.Add("Battle 아이콘용 Ready Sprite 매핑이 없습니다: " + type);
    }

    private static void ValidateHpImage(Context context, Image image, string label)
    {
        if (image == null) return;
        if (image.type != Image.Type.Filled)
            context.Errors.Add(label + " Image Type이 Filled가 아닙니다.");
        if (image.fillMethod != Image.FillMethod.Horizontal)
            context.Errors.Add(label + " Fill Method가 Horizontal이 아닙니다.");
    }

    private static void ValidateRawImage(Context context, RawImage image, string label)
    {
        if (image != null && image.texture == null)
            context.Errors.Add(label + "의 RenderTexture 참조가 없습니다.");
    }

    private static void ApplyDesign(Context context)
    {
        StyleNameCards(context);
        StyleTurn(context);
        StyleCharacters(context);
        LayoutRebuilder.ForceRebuildLayoutImmediate(context.NameCards);
        LayoutRebuilder.ForceRebuildLayoutImmediate(context.Characters);
    }

    private static void StyleNameCards(Context context)
    {
        SetTop(context.NameCards, new Vector2(1000f, 166f), new Vector2(0f, -165f));
        ConfigureHorizontalLayout(context.NameCardsLayout, 20, 20, 20f);
        context.MyAttributeIcon = StyleNameCard(context.MyNameCard, context.MyNameText,
            context.MyTitleImage, -245f, context.CardSprite, context.Font);
        context.OppAttributeIcon = StyleNameCard(context.OppNameCard, context.OppNameText,
            context.OppTitleImage, 245f, context.CardSprite, context.Font);
    }

    private static Image StyleNameCard(RectTransform card, TMP_Text name, Image title, float x,
        Sprite sprite, TMP_FontAsset font)
    {
        SetCentered(card, new Vector2(470f, 148f), new Vector2(x, 0f));
        Image legacyBackground = card.GetComponent<Image>();
        if (legacyBackground != null)
        {
            Record(legacyBackground);
            legacyBackground.enabled = false;
            Dirty(legacyBackground);
        }

        Image frame = GetOrCreateDecoration(card, NameFrame, sprite);
        SetStretch(frame.rectTransform, new Vector2(-4f, -4f), new Vector2(4f, 4f));
        StyleImage(frame, sprite, Color.white, Image.Type.Sliced);

        Image attribute = GetOrCreateAttributeIcon(card);
        SetCentered(attribute.rectTransform, new Vector2(80f, 80f), new Vector2(-130f, 0f));

        SetCentered(title.rectTransform, new Vector2(62f, 62f), new Vector2(188f, 0f));
        Record(title);
        title.preserveAspect = true;
        title.raycastTarget = false;
        Dirty(title);

        SetCentered(name.rectTransform, new Vector2(214f, 88f), new Vector2(28f, 0f));
        StyleText(name, font, 36f, Brown, FontStyles.Bold);
        Record(name);
        name.enableAutoSizing = true;
        name.fontSizeMin = 24f;
        name.fontSizeMax = 36f;
        name.enableWordWrapping = false;
        name.overflowMode = TextOverflowModes.Ellipsis;
        Dirty(name);
        return attribute;
    }

    private static void StyleTurn(Context context)
    {
        SetTop(context.TurnTexts, new Vector2(400f, 130f), new Vector2(0f, -20f));
        Image panel = GetOrAdd<Image>(context.TurnTexts.gameObject);
        StyleImage(panel, context.CreamSprite, Color.white, Image.Type.Sliced);
        panel.raycastTarget = false;

        SetCentered(context.TurnCountText.rectTransform, new Vector2(160f, 72f),
            new Vector2(-105f, 0f));
        StyleText(context.TurnCountText, context.Font, 38f, Brown, FontStyles.Bold);
        SetCentered(context.TurnText.rectTransform, new Vector2(210f, 72f),
            new Vector2(90f, 0f));
        StyleText(context.TurnText, context.Font, 30f, MutedBrown, FontStyles.Bold);
    }

    private static void StyleCharacters(Context context)
    {
        SetCentered(context.Characters, new Vector2(1000f, 1120f), new Vector2(0f, -155f));
        ConfigureHorizontalLayout(context.CharactersLayout, 20, 20, 20f);
        StyleCharacter(context.MyCharacter, context.MyHpBackground, context.MyHpBar,
            context.MyHpText, context.MyCharacterImage, -245f, PlayerHp, context);
        StyleCharacter(context.OppCharacter, context.OppHpBackground, context.OppHpBar,
            context.OppHpText, context.OppCharacterImage, 245f, OpponentHp, context);
    }

    private static void StyleCharacter(RectTransform root, Image hpBackground, Image hpBar,
        TMP_Text hpText, RawImage characterImage, float x, Color hpColor, Context context)
    {
        SetCentered(root, new Vector2(470f, 1060f), new Vector2(x, 0f));
        Image frame = GetOrCreateDecoration(root, CharacterFrame, context.CardSprite);
        SetStretch(frame.rectTransform, new Vector2(18f, 70f), new Vector2(-18f, -150f));
        StyleImage(frame, context.CardSprite, new Color(1f, 1f, 1f, 0.92f), Image.Type.Sliced);

        SetTop(hpBackground.rectTransform, new Vector2(424f, 106f), new Vector2(0f, -22f));
        StyleImage(hpBackground, context.CreamSprite, CreamLight, Image.Type.Sliced);
        hpBackground.raycastTarget = false;

        SetTop(hpBar.rectTransform, new Vector2(382f, 56f), new Vector2(0f, -47f));
        Record(hpBar);
        hpBar.color = hpColor;
        hpBar.raycastTarget = false;
        Dirty(hpBar);

        SetStretch(hpText.rectTransform, Vector2.zero, Vector2.zero);
        StyleText(hpText, context.Font, 27f, CreamLight, FontStyles.Bold);
        Record(hpText);
        hpText.outlineColor = Brown;
        hpText.outlineWidth = 0.24f;
        Dirty(hpText);

        SetCentered(characterImage.rectTransform, new Vector2(410f, 820f), new Vector2(0f, -55f));
        Record(characterImage);
        characterImage.raycastTarget = false;
        Dirty(characterImage);
    }

    private static List<string> ValidateAppliedDesign(Context context)
    {
        List<string> errors = new List<string>();
        LayoutRebuilder.ForceRebuildLayoutImmediate(context.NameCards);
        LayoutRebuilder.ForceRebuildLayoutImmediate(context.Characters);
        ValidateDecoration(errors, context.MyNameCard, NameFrame, context.CardSprite);
        ValidateDecoration(errors, context.OppNameCard, NameFrame, context.CardSprite);
        ValidateDecoration(errors, context.MyCharacter, CharacterFrame, context.CardSprite);
        ValidateDecoration(errors, context.OppCharacter, CharacterFrame, context.CardSprite);
        ValidateAttributeIcon(errors, context.MyNameCard, context.MyAttributeIcon,
            context.MyNameText, context.MyTitleImage);
        ValidateAttributeIcon(errors, context.OppNameCard, context.OppAttributeIcon,
            context.OppNameText, context.OppTitleImage);

        foreach (TMP_Text text in new[]
                 {
                     context.MyNameText, context.OppNameText, context.MyHpText, context.OppHpText,
                     context.TurnCountText, context.TurnText
                 })
        {
            if (text.font != context.Font)
                errors.Add("NotoSansKR 폰트가 적용되지 않았습니다: " + GetPath(text.transform));
            if (text.color.a < 0.95f)
                errors.Add("텍스트 대비를 해치는 투명도가 설정되어 있습니다: " + GetPath(text.transform));
        }

        if (context.TurnTexts.GetComponent<Image>()?.sprite != context.CreamSprite)
            errors.Add("TurnTexts RPG 카드가 적용되지 않았습니다.");
        if (Mathf.Abs(context.TurnTexts.anchoredPosition.x) > 0.1f)
            errors.Add("TurnTexts가 화면 중앙에 정렬되지 않았습니다.");
        if (Overlaps(context.MyNameCard, context.OppNameCard))
            errors.Add("플레이어와 상대 이름 카드가 겹칩니다.");
        if (context.MyNameCard.rect.size != context.OppNameCard.rect.size)
            errors.Add("플레이어와 상대 이름 카드의 크기가 서로 다릅니다.");
        Image myLegacyBackground = context.MyNameCard.GetComponent<Image>();
        Image oppLegacyBackground = context.OppNameCard.GetComponent<Image>();
        if ((myLegacyBackground != null && myLegacyBackground.enabled) ||
            (oppLegacyBackground != null && oppLegacyBackground.enabled))
            errors.Add("기존 노란 NameCard 배경 Image가 비활성화되지 않았습니다.");
        if (Overlaps(context.TurnTexts, context.MyNameCard) ||
            Overlaps(context.TurnTexts, context.OppNameCard))
            errors.Add("턴 표시와 이름 카드가 겹칩니다.");
        if (Overlaps(context.NameCards, context.Characters))
            errors.Add("이름 카드와 캐릭터 표시 영역이 겹칩니다.");
        foreach (RectTransform rect in new[]
                 {
                     context.TurnTexts, context.NameCards, context.MyNameCard, context.OppNameCard,
                     context.Characters, context.MyCharacter, context.OppCharacter,
                     context.MyHpBackground.rectTransform, context.OppHpBackground.rectTransform,
                     context.MyCharacterImage.rectTransform, context.OppCharacterImage.rectTransform
                 })
            if (!Contains(context.Battle.GetComponent<RectTransform>(), rect))
                errors.Add("Battle 화면 밖으로 벗어난 HUD 요소가 있습니다: " + GetPath(rect));
        if (Overlaps(context.MyTitleImage.rectTransform, context.MyNameText.rectTransform) ||
            Overlaps(context.OppTitleImage.rectTransform, context.OppNameText.rectTransform))
            errors.Add("칭호 이미지와 닉네임 영역이 겹칩니다.");

        if (context.MyHpBackground.sprite != context.CreamSprite ||
            context.OppHpBackground.sprite != context.CreamSprite)
            errors.Add("HP RPG 프레임이 적용되지 않았습니다.");
        if (context.MyHpBar.rectTransform.rect.width >= context.MyHpBackground.rectTransform.rect.width ||
            context.OppHpBar.rectTransform.rect.width >= context.OppHpBackground.rectTransform.rect.width)
            errors.Add("HP Fill이 프레임 안쪽에 배치되지 않았습니다.");
        if (context.MyCharacterImage.rectTransform.rect.width > context.MyCharacter.rect.width ||
            context.OppCharacterImage.rectTransform.rect.width > context.OppCharacter.rect.width)
            errors.Add("캐릭터 RawImage가 캐릭터 표시 영역을 벗어납니다.");

        ValidateHpImageForApplied(errors, context.MyHpBar, "MyHpBar");
        ValidateHpImageForApplied(errors, context.OppHpBar, "OppHpBar");
        if (context.MyCharacterImage.texture == null || context.OppCharacterImage.texture == null)
            errors.Add("캐릭터 RenderTexture 참조가 없습니다.");
        if (context.TimerText.activeSelf)
            errors.Add("제외 대상 TimerText가 활성화되었습니다.");

        if (context.ResultPanel.transform.GetSiblingIndex() <= context.Characters.GetSiblingIndex())
            errors.Add("ResultPanel이 캐릭터 HUD보다 뒤에 표시될 위험이 있습니다.");
        if (context.ResultWaitingPanel.transform.GetSiblingIndex() <= context.Characters.GetSiblingIndex())
            errors.Add("ResultWaitingPanel이 캐릭터 HUD보다 뒤에 표시될 위험이 있습니다.");

        return errors;
    }

    private static void ConfigureHorizontalLayout(HorizontalLayoutGroup layout, int left, int right,
        float spacing)
    {
        Record(layout);
        layout.padding = new RectOffset(left, right, 0, 0);
        layout.spacing = spacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        Dirty(layout);
    }

    private static bool Contains(RectTransform outer, RectTransform inner)
    {
        Vector3[] outerCorners = new Vector3[4];
        Vector3[] innerCorners = new Vector3[4];
        outer.GetWorldCorners(outerCorners);
        inner.GetWorldCorners(innerCorners);
        const float tolerance = 0.5f;
        return innerCorners[0].x >= outerCorners[0].x - tolerance &&
               innerCorners[0].y >= outerCorners[0].y - tolerance &&
               innerCorners[2].x <= outerCorners[2].x + tolerance &&
               innerCorners[2].y <= outerCorners[2].y + tolerance;
    }

    private static void ValidateDecoration(List<string> errors, RectTransform parent, string name,
        Sprite sprite)
    {
        Transform[] matches = parent.Cast<Transform>().Where(child => child.name == name).ToArray();
        if (matches.Length != 1)
        {
            errors.Add(GetPath(parent) + "/" + name + $" 개수가 1이 아닙니다. 현재: {matches.Length}");
            return;
        }

        Image image = matches[0].GetComponent<Image>();
        if (image == null || image.sprite != sprite || image.raycastTarget)
            errors.Add("장식 Image 설정이 올바르지 않습니다: " + GetPath(matches[0]));
    }

    private static void ValidateAttributeIcon(List<string> errors, RectTransform card, Image icon,
        TMP_Text name, Image title)
    {
        int count = card.Cast<Transform>().Count(child => child.name == AttributeIcon);
        if (count != 1 || icon == null)
        {
            errors.Add(GetPath(card) + "/" + AttributeIcon + $" 개수가 1이 아닙니다. 현재: {count}");
            return;
        }

        if (!icon.preserveAspect || icon.raycastTarget)
            errors.Add("Battle 특성 아이콘 Image 설정이 올바르지 않습니다: " + GetPath(icon.transform));
        if (icon.enabled || icon.sprite != null)
            errors.Add("Battle 특성 아이콘은 실제 BattleUnit 수신 전 비어 있고 비활성이어야 합니다: " +
                       GetPath(icon.transform));
        if (!Contains(card, icon.rectTransform))
            errors.Add("Battle 특성 아이콘이 닉네임 카드 밖으로 벗어납니다: " +
                       GetPath(icon.transform));
        if (Overlaps(icon.rectTransform, name.rectTransform) ||
            Overlaps(icon.rectTransform, title.rectTransform) ||
            Overlaps(name.rectTransform, title.rectTransform))
            errors.Add("Battle 특성 아이콘/닉네임/칭호 영역이 겹칩니다: " + GetPath(card));
        float iconRight = icon.rectTransform.anchoredPosition.x + icon.rectTransform.rect.width * 0.5f;
        float nameLeft = name.rectTransform.anchoredPosition.x - name.rectTransform.rect.width * 0.5f;
        float contentLeft = icon.rectTransform.anchoredPosition.x - icon.rectTransform.rect.width * 0.5f;
        float contentRight = name.rectTransform.anchoredPosition.x + name.rectTransform.rect.width * 0.5f;
        float contentCenter = (contentLeft + contentRight) * 0.5f;
        float gap = nameLeft - iconRight;
        if (gap < 8f || gap > 24f)
            errors.Add("Battle 특성 아이콘과 닉네임 사이 간격이 올바르지 않습니다: " + gap);
        if (Mathf.Abs(contentCenter) > 20f)
            errors.Add("Battle 특성 아이콘+닉네임 그룹이 카드 중앙에서 벗어납니다: " +
                       contentCenter);
        if (Mathf.Abs(icon.rectTransform.anchoredPosition.y - name.rectTransform.anchoredPosition.y) >
            0.1f)
            errors.Add("Battle 특성 아이콘과 닉네임의 세로 중심이 일치하지 않습니다: " +
                       GetPath(card));
        if (!name.enableAutoSizing || name.fontSizeMin < 24f || name.fontSizeMax > 36f ||
            name.enableWordWrapping || name.overflowMode != TextOverflowModes.Ellipsis)
            errors.Add("Battle 긴 닉네임용 Auto Size/Ellipsis 설정이 올바르지 않습니다: " +
                       GetPath(name.transform));
    }

    private static void ValidateHpImageForApplied(List<string> errors, Image image, string label)
    {
        if (image.type != Image.Type.Filled || image.fillMethod != Image.FillMethod.Horizontal)
            errors.Add(label + " Fill 설정이 보호되지 않았습니다.");
    }

    private static bool Overlaps(RectTransform first, RectTransform second)
    {
        Vector3[] firstCorners = new Vector3[4];
        Vector3[] secondCorners = new Vector3[4];
        first.GetWorldCorners(firstCorners);
        second.GetWorldCorners(secondCorners);
        Rect a = Rect.MinMaxRect(firstCorners[0].x, firstCorners[0].y,
            firstCorners[2].x, firstCorners[2].y);
        Rect b = Rect.MinMaxRect(secondCorners[0].x, secondCorners[0].y,
            secondCorners[2].x, secondCorners[2].y);
        return a.Overlaps(b);
    }

    private static string BuildDiagnostics(Context context)
    {
        int persistentEvents = context.Battle.GetComponentsInChildren<Button>(true)
            .Sum(button => button.onClick.GetPersistentEventCount());
        return "대상: Canvas/PvPTap/Battle (activeSelf=" + context.Battle.activeSelf + ")\n" +
               "CanvasScaler: " + context.CanvasScaler.referenceResolution + "\n" +
               "Canvas source: " + CanvasPrefabPath + "\n" +
               "Layout: NameCards children=" + context.MyNameCard.rect.width + "+" +
               context.OppNameCard.rect.width + ", spacing=" + context.NameCardsLayout.spacing +
               "; Characters children=" + context.MyCharacter.rect.width + "+" +
               context.OppCharacter.rect.width + ", spacing=" + context.CharactersLayout.spacing + "\n" +
               "Attribute icons: My=" + (context.MyAttributeIcon == null ? "없음" : "있음") +
               ", Opp=" + (context.OppAttributeIcon == null ? "없음" : "있음") +
               ", Ready Sprite sources=" + context.AttributeButtons.Length + "\n" +
               "HP Fill: My=" + DescribeFill(context.MyHpBar) + ", Opp=" +
               DescribeFill(context.OppHpBar) + "\n" +
               "RawImage texture: My=" + context.MyCharacterImage.texture.name + ", Opp=" +
               context.OppCharacterImage.texture.name + "\n" +
               "Result: ResultPanel(active=" + context.ResultPanel.activeSelf + ", sibling=" +
               context.ResultPanel.transform.GetSiblingIndex() + "), ResultWaitingPanel(active=" +
               context.ResultWaitingPanel.activeSelf + ", sibling=" +
               context.ResultWaitingPanel.transform.GetSiblingIndex() + ")\n" +
               "Battle Button: " + context.Battle.GetComponentsInChildren<Button>(true).Length +
               "개, Persistent UnityEvent: " + persistentEvents +
               "개 (0개인 버튼은 런타임 등록 가능하므로 오류 아님)";
    }

    private static string DescribeFill(Image image)
    {
        return image.type + "/" + image.fillMethod + "/origin=" + image.fillOrigin +
               "/amount=" + image.fillAmount;
    }

    private static Image GetOrCreateAttributeIcon(RectTransform parent)
    {
        Transform existing = parent.Find(AttributeIcon);
        Image image;
        if (existing == null)
        {
            GameObject item = new GameObject(AttributeIcon, typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(item, "Create 15-D-7C Battle attribute icon");
            item.transform.SetParent(parent, false);
            image = item.GetComponent<Image>();
        }
        else
        {
            image = existing.GetComponent<Image>() ?? Undo.AddComponent<Image>(existing.gameObject);
        }

        Record(image);
        image.sprite = null;
        image.color = Color.white;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.enabled = false;
        Dirty(image);
        return image;
    }

    private static Image GetOrCreateDecoration(RectTransform parent, string name, Sprite sprite)
    {
        Transform existing = parent.Find(name);
        Image image;
        if (existing == null)
        {
            GameObject item = new GameObject(name, typeof(RectTransform), typeof(Image));
            Undo.RegisterCreatedObjectUndo(item, "Create 15-D-7C Battle HUD decoration");
            item.transform.SetParent(parent, false);
            item.transform.SetAsFirstSibling();
            image = item.GetComponent<Image>();
        }
        else
        {
            image = existing.GetComponent<Image>() ?? Undo.AddComponent<Image>(existing.gameObject);
        }

        StyleImage(image, sprite, Color.white, Image.Type.Sliced);
        Record(image);
        image.raycastTarget = false;
        Dirty(image);
        return image;
    }

    private static void StyleImage(Image image, Sprite sprite, Color color, Image.Type type)
    {
        Record(image);
        image.sprite = sprite;
        image.color = color;
        image.type = type;
        Dirty(image);
    }

    private static void StyleText(TMP_Text text, TMP_FontAsset font, float size, Color color,
        FontStyles style)
    {
        Record(text);
        text.font = font;
        text.fontSize = size;
        text.color = color;
        text.fontStyle = style;
        text.alignment = TextAlignmentOptions.Center;
        text.enableAutoSizing = false;
        text.enableWordWrapping = false;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        Dirty(text);
    }

    private static void SetStretch(RectTransform rect, Vector2 min, Vector2 max)
    {
        Record(rect);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = min;
        rect.offsetMax = max;
        Dirty(rect);
    }

    private static void SetCentered(RectTransform rect, Vector2 size, Vector2 position)
    {
        Record(rect);
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        Dirty(rect);
    }

    private static void SetTop(RectTransform rect, Vector2 size, Vector2 position)
    {
        Record(rect);
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.sizeDelta = size;
        rect.anchoredPosition = position;
        Dirty(rect);
    }

    private static Context Finish(Context context, bool log)
    {
        if (log)
            foreach (string error in context.Errors)
                Debug.LogError("[HabitPVP 15-D-7C] " + error);
        return context;
    }

    private static void Require(Context context, UnityEngine.Object value, string label)
    {
        if (value == null) context.Errors.Add("필수 대상이 없습니다: " + label);
    }

    private static void RequireObjectReference(Context context, SerializedObject serialized,
        string propertyName)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property == null)
            context.Errors.Add("BattleUIManager SerializeField가 없습니다: " + propertyName);
        else if (property.objectReferenceValue == null)
            context.Errors.Add("BattleUIManager SerializeField 연결이 없습니다: " + propertyName);
    }

    private static void CheckReference(Context context, SerializedObject serialized, string name,
        UnityEngine.Object expected)
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property == null)
            context.Errors.Add("BattleUIManager SerializeField가 없습니다: " + name);
        else if (property.objectReferenceValue != expected)
            context.Errors.Add("BattleUIManager 참조가 실제 Battle Hierarchy와 다릅니다: " + name);
    }

    private static T[] ReferenceArray<T>(SerializedObject serialized, string name)
        where T : UnityEngine.Object
    {
        SerializedProperty property = serialized.FindProperty(name);
        if (property == null || !property.isArray) return Array.Empty<T>();
        return Enumerable.Range(0, property.arraySize)
            .Select(index => property.GetArrayElementAtIndex(index).objectReferenceValue as T)
            .Where(value => value != null).ToArray();
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

    private static RectTransform FindRect(GameObject root, string path)
    {
        return Find(root, path) as RectTransform;
    }

    private static T FindComponent<T>(GameObject root, string path) where T : Component
    {
        return Find(root, path)?.GetComponent<T>();
    }

    private static T GetOrAdd<T>(GameObject target) where T : Component
    {
        T component = target.GetComponent<T>();
        return component != null ? component : Undo.AddComponent<T>(target);
    }

    private static void Record(UnityEngine.Object target)
    {
        if (target != null) Undo.RecordObject(target, "Apply 15-D-7C Battle HUD UI Renewal");
    }

    private static void Dirty(UnityEngine.Object target)
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
        private readonly Dictionary<int, string> managerReferences;
        private readonly Dictionary<int, string> buttonEvents;
        private readonly Dictionary<int, bool> activeStates;
        private readonly Dictionary<int, int> siblingOrder;
        private readonly HashSet<int> protectedTransformIds;
        private readonly RawImageState myRawImage;
        private readonly RawImageState opponentRawImage;
        private readonly HpImageState myHp;
        private readonly HpImageState opponentHp;
        private readonly int resultPanelRelativeOrder;
        private readonly int resultWaitingRelativeOrder;

        private ProtectionSnapshot(Context context)
        {
            Component[] managers =
            {
                context.BattleUiManager, context.BattleManager, context.BattleBackendManager,
                context.PhotonManager, context.MainTabManager
            };
            managerReferences = managers.ToDictionary(manager => manager.GetInstanceID(),
                CaptureObjectReferences);
            buttonEvents = context.Battle.GetComponentsInChildren<Button>(true)
                .ToDictionary(button => button.GetInstanceID(), button => CaptureEvent(button.onClick));
            Transform[] transforms = context.Battle.GetComponentsInChildren<Transform>(true);
            protectedTransformIds = transforms.Select(item => item.GetInstanceID()).ToHashSet();
            activeStates = transforms.ToDictionary(item => item.GetInstanceID(),
                item => item.gameObject.activeSelf);
            siblingOrder = transforms.ToDictionary(item => item.GetInstanceID(), RelativeSiblingOrder);
            myRawImage = new RawImageState(context.MyCharacterImage);
            opponentRawImage = new RawImageState(context.OppCharacterImage);
            myHp = new HpImageState(context.MyHpBar);
            opponentHp = new HpImageState(context.OppHpBar);
            resultPanelRelativeOrder = RelativeSiblingOrder(context.ResultPanel.transform);
            resultWaitingRelativeOrder = RelativeSiblingOrder(context.ResultWaitingPanel.transform);
        }

        public static ProtectionSnapshot Capture(Context context)
        {
            return new ProtectionSnapshot(context);
        }

        public void AssertUnchanged(Context context)
        {
            foreach (Component manager in new Component[]
                     {
                         context.BattleUiManager, context.BattleManager, context.BattleBackendManager,
                         context.PhotonManager, context.MainTabManager
                     })
            {
                if (managerReferences[manager.GetInstanceID()] != CaptureObjectReferences(manager))
                    throw new InvalidOperationException(
                        manager.GetType().Name + " ObjectReference가 변경되었습니다.");
            }

            foreach (Button button in context.Battle.GetComponentsInChildren<Button>(true))
            {
                if (buttonEvents.TryGetValue(button.GetInstanceID(), out string before) &&
                    before != CaptureEvent(button.onClick))
                    throw new InvalidOperationException("Button UnityEvent가 변경되었습니다: " +
                                                        GetPath(button.transform));
            }

            foreach (Transform item in context.Battle.GetComponentsInChildren<Transform>(true))
            {
                int id = item.GetInstanceID();
                if (!protectedTransformIds.Contains(id)) continue;
                if (activeStates[id] != item.gameObject.activeSelf)
                    throw new InvalidOperationException("기존 activeSelf가 변경되었습니다: " +
                                                        GetPath(item));
                if (siblingOrder[id] != RelativeSiblingOrder(item))
                    throw new InvalidOperationException("기존 객체 간 상대 sibling 순서가 변경되었습니다: " +
                                                        GetPath(item));
            }

            myRawImage.AssertUnchanged(context.MyCharacterImage, "MyCharacterImage");
            opponentRawImage.AssertUnchanged(context.OppCharacterImage, "OppCharacterImage");
            myHp.AssertUnchanged(context.MyHpBar, "MyHpBar");
            opponentHp.AssertUnchanged(context.OppHpBar, "OppHpBar");
            if (resultPanelRelativeOrder != RelativeSiblingOrder(context.ResultPanel.transform) ||
                resultWaitingRelativeOrder != RelativeSiblingOrder(context.ResultWaitingPanel.transform))
                throw new InvalidOperationException("ResultPanel/ResultWaitingPanel 표시 순서가 변경되었습니다.");
        }

        private int RelativeSiblingOrder(Transform item)
        {
            if (item.parent == null) return 0;
            int order = 0;
            foreach (Transform sibling in item.parent)
            {
                if (sibling == item) break;
                if (protectedTransformIds.Contains(sibling.GetInstanceID())) order++;
            }
            return order;
        }

        private static string CaptureObjectReferences(Component target)
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
                                   GlobalObjectId.GetGlobalObjectIdSlow(iterator.objectReferenceValue)
                                       .ToString()));
            }
            return string.Join("|", values);
        }

        private static string CaptureEvent(UnityEventBase value)
        {
            return string.Join(",", Enumerable.Range(0, value.GetPersistentEventCount())
                .Select(index =>
                {
                    UnityEngine.Object target = value.GetPersistentTarget(index);
                    return (target == null ? "null" :
                               GlobalObjectId.GetGlobalObjectIdSlow(target).ToString()) +
                           ":" + value.GetPersistentMethodName(index);
                }));
        }

        private readonly struct RawImageState
        {
            private readonly Texture texture;
            private readonly Material material;
            private readonly Rect uvRect;

            public RawImageState(RawImage image)
            {
                texture = image.texture;
                material = image.material;
                uvRect = image.uvRect;
            }

            public void AssertUnchanged(RawImage image, string label)
            {
                if (image.texture != texture || image.material != material || image.uvRect != uvRect)
                    throw new InvalidOperationException(label + " texture/material/uvRect가 변경되었습니다.");
            }
        }

        private readonly struct HpImageState
        {
            private readonly Image.Type type;
            private readonly Image.FillMethod method;
            private readonly int origin;
            private readonly bool clockwise;
            private readonly float fillAmount;

            public HpImageState(Image image)
            {
                type = image.type;
                method = image.fillMethod;
                origin = image.fillOrigin;
                clockwise = image.fillClockwise;
                fillAmount = image.fillAmount;
            }

            public void AssertUnchanged(Image image, string label)
            {
                if (image.type != type || image.fillMethod != method || image.fillOrigin != origin ||
                    image.fillClockwise != clockwise || !Mathf.Approximately(image.fillAmount, fillAmount))
                    throw new InvalidOperationException(label + " Fill 설정이 변경되었습니다.");
            }
        }
    }

    private sealed class Context
    {
        public Scene Scene;
        public readonly List<string> Errors = new List<string>();
        public bool IsValid => Errors.Count == 0;
        public GameObject Canvas, PvpTap, Battle, ResultPanel, ResultWaitingPanel, TimerText;
        public CanvasScaler CanvasScaler;
        public RectTransform TurnTexts, NameCards, MyNameCard, OppNameCard, Characters,
            MyCharacter, OppCharacter;
        public HorizontalLayoutGroup NameCardsLayout, CharactersLayout;
        public TMP_Text TurnCountText, TurnText, MyNameText, OppNameText, MyHpText, OppHpText;
        public Image MyTitleImage, OppTitleImage, MyAttributeIcon, OppAttributeIcon,
            MyHpBackground, OppHpBackground, MyHpBar, OppHpBar;
        public Button[] AttributeButtons = Array.Empty<Button>();
        public RawImage MyCharacterImage, OppCharacterImage;
        public BattleUIManager BattleUiManager;
        public BattleManager BattleManager;
        public BattleBackendManager BattleBackendManager;
        public PhotonManager PhotonManager;
        public MainTabManager MainTabManager;
        public TMP_FontAsset Font;
        public Sprite CardSprite, CreamSprite;
    }
}
