using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class CharacterUIManager : Singleton<CharacterUIManager>
{
    [SerializeField]
    private TextMeshProUGUI nameText;
    [SerializeField]
    private TextMeshProUGUI atkStatText;
    [SerializeField]
    private TextMeshProUGUI defStatText;
    [SerializeField]
    private TextMeshProUGUI hpStatText;
    [SerializeField]
    private TextMeshProUGUI spdStatText;
    [SerializeField]
    private TextMeshProUGUI critStatText;

    [SerializeField]
    private Image fireExpFill;
    [SerializeField]
    private Image waterExpFill;
    [SerializeField]
    private Image grassExpFill;
    [SerializeField]
    private Image auroraExpFill;

    [SerializeField]
    private TextMeshProUGUI fireLevelText;
    [SerializeField]
    private TextMeshProUGUI waterLevelText;
    [SerializeField]
    private TextMeshProUGUI grassLevelText;
    [SerializeField]
    private TextMeshProUGUI auroraLevelText;

    protected override void Awake()
    {
        base.Awake();
        OpenCharacterTap();
    }

    public void OpenCharacterTap()
    {
        // CharacterResponse characterResponse = await CharacterManager.Instance.RefreshCharacterAsync();

        CharacterResponse characterResponse = new CharacterResponse    // 테스트용 임시 데이터
        {
            UserId = 1,
            Gold = 1000,
            Hp = 1000.0f,
            Atk = 100.0f,
            Def = 100.0f,
            Spd = 10.0f,
            Crit = 10f,
            FireLv = 7,
            WaterLv = 8,
            GrassLv = 5,
            AuroraLv = 9,
            FireExp = 70,
            WaterExp = 80,
            GrassExp = 50,
            AuroraExp = 90
        };
        ApplyName($"tmpName + {characterResponse.UserId}");    // 이름 정보 필요
        ApplyStatus(characterResponse);
        ApplyAttrLevel(characterResponse);
        ApplyAttrExp(characterResponse);
    }

    private void ApplyName(string name)
    {
        nameText.text = name;
    }

    private void ApplyStatus(CharacterResponse Response)
    {
        atkStatText.text = Response.Atk.ToString();
        defStatText.text = Response.Def.ToString();
        hpStatText.text = Response.Hp.ToString();
        spdStatText.text = Response.Spd.ToString();
        critStatText.text = Response.Crit.ToString();
    }

    private void ApplyAttrLevel(CharacterResponse Response)
    {
        fireLevelText.text = Response.FireLv.ToString();
        waterLevelText.text = Response.WaterLv.ToString();
        grassLevelText.text = Response.GrassLv.ToString();
        auroraLevelText.text = Response.AuroraLv.ToString();
    }

    
    private void ApplyAttrExp(CharacterResponse Response)
    {

    }
}
