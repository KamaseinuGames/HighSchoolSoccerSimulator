using System.Collections;
using System.Collections.Generic;
using System;

// ポジション定義の1スロット分のデータ
// x, y座標を分けているのはNumbers仕様のため
[Serializable]
public class PositionDefinitionSlot
{
    public string positionStr;
    public string positionGroupStr;
    public float offenseFollowXRate;
    public float offenseFollowYRate;
    public float defenseFollowXRate;
    public float defenseFollowYRate;
}
