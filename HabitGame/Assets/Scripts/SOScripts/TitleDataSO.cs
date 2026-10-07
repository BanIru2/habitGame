using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[CreateAssetMenu(menuName = "Character/TitleData", fileName = "Title")]
public class TitleDataSO : ScriptableObject
{
    public string titleId;
    public Sprite displaySprite;
    public string acquisitionRoute;    // È¹µæ °æ·Î : ¾÷Àû ³»¿ë
}
