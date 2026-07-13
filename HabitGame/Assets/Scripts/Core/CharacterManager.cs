using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class CharacterManager : Singleton<CharacterManager>
{
    // 캐릭터 최신 정보
    // 외부에서 직접적으로 참조할 일이 없다면 private으로 잠구는게 낫긴 함
    public CharacterResponse characterStatusData { get; private set; }

    // 캐릭터 닉네임
    private string nickname = "Unknown";

    protected override void Awake()
    {
        base.Awake();
        // 임시 데이터
        ApplyCharacterResponse(new CharacterResponse { UserId = 123, Atk = 10, Def = 50, Hp = 1000, Spd = 10, FireLv = 5, WaterLv = 3, GrassLv = 1, AuroraLv = 0 });
    }

    // CharacterREsponse를 요청하는 비동기함수
    public async Task<CharacterResponse> RefreshCharacterAsync()
    {
        CharacterResponse response = await ServiceRegistry.Instance.Character.GetMyCharacterAsync();

        if (response == null)
        {
            throw new System.InvalidOperationException("캐릭터 정보 응답이 null입니다.");
        }

        ApplyCharacterResponse(response);
        return response;
    }

    // 캐릭터 최신 정보 업데이트
    // 캐릭터 정보에 대한 수정 발생 시(생활 습관 목표 보상 수령) 받아오는 응답데이터에서 CharacterResponse를 추출해서 호출
    // 단순 조회 시에는 RefreshCharacterAsync()에서 호출되어 DB와 동기화시키는 목적
    public void ApplyCharacterResponse(CharacterResponse data)
    { 
        if (data == null)
        {
            Debug.LogWarning("CharacterResponse가 null이라 캐릭터 캐시를 갱신하지 않습니다.");
            return;
        }

        characterStatusData = data;
    }

    public void SetNickname(string nickname)
    {
        this.nickname = string.IsNullOrEmpty(nickname) ? "Unknown" : nickname;
    }

    // 전투 시 필요한  my BattleUnit 생성
    // 전투에 사용할 특성 선택이 반영되므로 선택 이후에 호출하도록
    private BattleUnit CreateBattleUnit(BattleSetupData setup, CharacterResponse characterData)
    {
        if (setup == null)
        {
            throw new System.ArgumentNullException(nameof(setup));
        }

        if (characterData == null)
        {
            throw new System.InvalidOperationException("전투 유닛 생성 실패: 캐릭터 정보가 없습니다.");
        }

        float hp = characterData.Hp;
        float atk = characterData.Atk;
        float def = characterData.Def;
        float spd = characterData.Spd;
        float crtk = characterData.Crit;


        int attrLevel = setup.selectedAttr switch
        {
            AttributeType.Fire => characterData.FireLv,
            AttributeType.Water => characterData.WaterLv,
            AttributeType.Grass => characterData.GrassLv,
            AttributeType.Aurora => characterData.AuroraLv,
            _ => 0
        };

        if(setup.selectedItem != null)
        {
            switch (setup.selectedItem.targetStat)
            {
                case StatType.HP: hp += setup.selectedItem.value; break;
                case StatType.ATK: atk += setup.selectedItem.value; break;
                case StatType.DEF: def += setup.selectedItem.value; break;
                case StatType.SPD: spd += setup.selectedItem.value; break;
                case StatType.CRIT: crtk += setup.selectedItem.value; break;
            }
        }

        return new BattleUnit
        {
            name = nickname,    // 이름 어디서 끌어오지 << 어디 저장하지? 이름????? (로그인 관리 스크립트에서 LoginResponse를 받아 저장한 후 받아오도록 해야할 것)
            maxHp = (int)hp,
            hp = (int)hp,
            atk = atk,
            def = def,
            spd = spd,
            crtk = crtk,
            attribute = setup.selectedAttr,
            attributeLevel = attrLevel
        };
    }

    public async Task<BattleUnit> CreateBattleUnitAsync(BattleSetupData setup)
    {
        CharacterResponse characterData = await RefreshCharacterAsync();

        return CreateBattleUnit(setup, characterData);
    }

    // BattleUIManager 컴파일 보호를 위한 임시 함수(로직 조정 후 삭제)
    public BattleUnit CreateBattleUnit(BattleSetupData setup)
    {
        return CreateBattleUnit(setup, characterStatusData);
    }
}