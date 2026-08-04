using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
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
    private TextMeshProUGUI fireEXPText;
    [SerializeField]
    private TextMeshProUGUI waterLevelText;
    [SerializeField]
    private TextMeshProUGUI waterEXPText;
    [SerializeField]
    private TextMeshProUGUI grassLevelText;
    [SerializeField]
    private TextMeshProUGUI grassEXPText;
    [SerializeField]
    private TextMeshProUGUI auroraLevelText;
    [SerializeField]
    private TextMeshProUGUI auroraEXPText;

    private bool isLoadingCharacter;

    protected override void Awake()
    {
        base.Awake();
        OpenCharacterTap();
    }

    // 테스트용 임시 데이터 생성 함수
    private CharacterResponse CreateTMPCharacterData()
    {
        return new CharacterResponse    
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
            FireExp = 700,
            WaterExp = 80,
            GrassExp = 50,
            AuroraExp = 1,
            FireExpPercentage = 70,
            WaterExpPercentage = 60,
            GrassExpPercentage = 9,
            AuroraExpPercentage = 26,
            MaxFireExp = 1000,
            MaxWaterExp = 2000,
            MaxGrassExp = 500,
            MaxAuroraExp = 30
        };
    }

    public void OpenCharacterTap()
    {
        _ = OpenCharacterTapAsync();
    }

    private async Task OpenCharacterTapAsync()
    {
        if (isLoadingCharacter)
            return;

        isLoadingCharacter = true;

        try
        {
            CharacterResponse characterResponse = await CharacterManager.Instance.RefreshCharacterAsync();

            // 요청 도중 오브젝트가 제거된 경우 UI 접근 방지
            if (this == null)
                return;

            ApplyName($"tmpName {characterResponse.UserId}");     // 이름 정보 필요
            ApplyStatus(characterResponse);
            ApplyAttrLevel(characterResponse);
            ApplyAttrExp(characterResponse);
        }
        catch (ApiException exception)
        {
            // 서버 오류, 연결 실패, 4xx/5xx 응답
            Debug.LogError($"캐릭터 정보 요청 실패 ({exception.StatusCode}): {exception.Message}", this);
        }
        catch (InvalidOperationException exception)
        {
            // 로그인 사용자 ID 없음 또는 CharacterResponse가 null인 경우
            Debug.LogWarning($"캐릭터 정보를 불러올 수 없습니다: {exception.Message}", this);
        }
        catch (Exception exception)
        {
            // 예상하지 못한 역직렬화 오류 등을 마지막으로 처리
            Debug.LogException(exception, this);
        }
        finally
        {
            isLoadingCharacter = false;
        }
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
        fireEXPText.text = $"{Response.FireExp} / {Response.MaxFireExp}";
        waterEXPText.text = $"{Response.WaterExp} / {Response.MaxWaterExp}";
        grassEXPText.text = $"{Response.GrassExp} / {Response.MaxGrassExp}";
        auroraEXPText.text = $"{Response.AuroraExp} / {Response.MaxAuroraExp}";

        ApplyExpFill(fireExpFill, Response.FireExpPercentage);
        ApplyExpFill(waterExpFill, Response.WaterExpPercentage);
        ApplyExpFill(grassExpFill, Response.GrassExpPercentage);
        ApplyExpFill(auroraExpFill, Response.AuroraExpPercentage);
    }

    private void ApplyExpFill(Image fillImage, int percentage)
    {
        Vector3 scale = fillImage.rectTransform.localScale;
        scale.x = Mathf.Clamp01(percentage / 100f);
        fillImage.rectTransform.localScale = scale;
    }
}
