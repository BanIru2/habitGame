using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleTest : MonoBehaviour
{
    [SerializeField]
    BattleManager battleManger;

    BattleUnit my = new BattleUnit { name = "my", atk = 20, attribute = AttributeType.Water, attributeLevel = 3, crtk = 0.3f, def = 4, hp = 500, spd = 4, maxHp = 500 };
    BattleUnit opp = new BattleUnit { name = "opp", atk = 10, attribute = AttributeType.Fire, attributeLevel = 3, crtk = 0.1f, def = 3, hp = 400, spd = 3, maxHp = 400 };

    private void Awake()
    {
        if(battleManger == null)
        {
            battleManger = FindObjectOfType<BattleManager>();
        }
    }

    public void onClick()
    {
        battleManger.StartBattle(my, opp);
        BattleUIManager.Instance.GetName(my.name, opp.name);
        BattleUIManager.Instance.InitBattleUI();
        BattleUIManager.Instance.ReadyComplete();
    }

    public void LoadingComplete()
    {
        BattleUIManager.Instance.LoadingComplete();
    }
}
