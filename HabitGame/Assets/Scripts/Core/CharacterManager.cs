using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CharacterManager : Singleton<CharacterManager>
{
    // 캐릭터 최신 정보
    // 외부에서 직접적으로 참조할 일이 없다면 private으로 잠구는게 낫긴 함
    public CharacterResponse characterStatusData { get; private set; }

    // 착용한 아이템 정보
    private List<EquipmentDataSO> equippedItems = new List<EquipmentDataSO>();


    protected override void Awake()
    {
        base.Awake();
        // 임시 데이터
        UpdateCharacterStatus(new CharacterResponse { UserId = 123, Atk = 10, Def = 50, Hp = 1000, Spd = 10, FireLv = 5, WaterLv = 3, GrassLv = 1, AuroraLv = 0 }); 
    }

    // 캐릭터 최신 정보 업데이트
    // 캐릭터 정보에 대한 수정 발생 시(생활 습관 목표 보상 수령) 받아오는 응답데이터에서 CharacterResponse를 추 출해서 호출
    // 항상 최신 상태를 유지하도록
    public void UpdateCharacterStatus(CharacterResponse data)
    {
        characterStatusData = data;
    }

    private void ApplyBonus(StatBonus bonus, ref float atk, ref float def, ref float hp, ref float spd, ref float crtk)
    {
        switch (bonus.statType)
        {
            case StatType.ATK: atk += bonus.value; break;
            case StatType.DEF: def += bonus.value; break;
            case StatType.HP: hp += bonus.value; break;
            case StatType.SPD: spd += bonus.value; break;
            case StatType.CRIT: crtk += bonus.value; break;
        }
    }

    // 전투 시 필요한  my BattleUnit 생성
    // 전투에 사용할 특성 선택이 반영되므로 선택 이후에 호출하도록
    public BattleUnit CreateBattleUnit(BattleSetupData setup)
    {
        float hp = characterStatusData.Hp;
        float atk = characterStatusData.Atk;
        float def = characterStatusData.Def;
        float spd = characterStatusData.Spd;
        float crtk = characterStatusData.Crit;


        int attrLevel = setup.selectedAttr switch
        {
            AttributeType.Fire => characterStatusData.FireLv,
            AttributeType.Water => characterStatusData.WaterLv,
            AttributeType.Grass => characterStatusData.GrassLv,
            AttributeType.Aurora => characterStatusData.AuroraLv,
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

        if(setup.selectedEquips != null)
        {
            foreach(var equip in setup.selectedEquips)
            {
                if (equip == null) continue;
                foreach(var bonus in equip.statBonuses)
                {
                    ApplyBonus(bonus, ref atk, ref def, ref hp, ref spd, ref crtk);
                }
            }
        }

        return new BattleUnit
        {
            name = "Name",    // 이름 어디서 끌어오지 << 어디 저장하지? 이름?????
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


    // -------------------------- 장착 장비 처리 -------------------------------
    public void SetEquippedItems(List<EquipmentDataSO> items)
    {
        equippedItems = items;
    }
}