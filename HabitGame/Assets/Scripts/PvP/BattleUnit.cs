using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleUnit
{
    public string name;
    public int maxHp;
    public int hp;
    public float atk;
    public float def;
    public float spd;
    public float crtk;    // 0~1

    public AttributeType attribute;
    public int attributeLevel;
}
