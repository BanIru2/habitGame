using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BattleUnit
{
    public string name;
    public float maxHp;
    public float hp;
    public int atk;
    public int def;
    public int spd;
    public float crtk;    // 0~1

    public AttributeType attribute;
    public int attributeLevel;
}
