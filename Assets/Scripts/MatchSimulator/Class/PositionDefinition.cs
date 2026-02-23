using System.Collections.Generic;

// 各ポジションの追従係数を定義するクラス
// PositionDefinition_SO（ScriptableObject）からデータを読み込む
[System.Serializable]
public class PositionDefinition
{
    public string positionStr;
    public string positionGroupStr;
    public float offenseFollowXRate;
    public float offenseFollowYRate;
    public float defenseFollowXRate;
    public float defenseFollowYRate;

    public PositionDefinition(
        string _positionStr,
        string _positionGroupStr,
        float _offenseFollowXRate,
        float _offenseFollowYRate,
        float _defenseFollowXRate,
        float _defenseFollowYRate
    )
    {
        positionStr = _positionStr;
        positionGroupStr = _positionGroupStr;
        offenseFollowXRate = _offenseFollowXRate;
        offenseFollowYRate = _offenseFollowYRate;
        defenseFollowXRate = _defenseFollowXRate;
        defenseFollowYRate = _defenseFollowYRate;
    }

    // PositionDefinition_SOからポジション辞書を生成
    public static Dictionary<string, PositionDefinition> CreateFromSO(PositionDefinition_SO _positionDefinitionSO)
    {
        Dictionary<string, PositionDefinition> roleDictionary = new Dictionary<string, PositionDefinition>();

        foreach (PositionDefinitionSlot_SO slot in _positionDefinitionSO.data)
        {
            PositionDefinition roleData = new PositionDefinition(
                slot.positionStr,
                slot.positionGroupStr,
                slot.offenseFollowXRate,
                slot.offenseFollowYRate,
                slot.defenseFollowXRate,
                slot.defenseFollowYRate
            );
            roleDictionary[slot.positionStr] = roleData;
        }

        return roleDictionary;
    }
}
