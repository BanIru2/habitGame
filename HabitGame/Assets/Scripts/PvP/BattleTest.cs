using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleTest : MonoBehaviour
{
    [SerializeField]
    BattleManager battleManger;

    BattleUnit my = new BattleUnit { name = "my", atk = 20, attribute = AttributeType.Water, attributeLevel = 2, crtk = 0.1f, def = 2, hp = 500, spd = 4 };
    BattleUnit opp = new BattleUnit { name = "opp", atk = 10, attribute = AttributeType.Fire, attributeLevel = 3, crtk = 0.05f, def = 4, hp = 400, spd = 3 };

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
    }
}
