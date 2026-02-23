// フォーメーションの1スロット分の座標定義
// SO設計.md準拠: ベース座標 + セットプレー座標
// goalKickOffense/DefenseCoordinateはダイナミックアンカーの攻守ベースとしても使用
[System.Serializable]
public class FormationSlot
{
    public string defaultPositionGroupStr;
    public string defaultPositionStr;
    public Coordinate baseCoordinate;
    public Coordinate goalKickOffenseCoordinate;
    public Coordinate goalKickDefenseCoordinate;
    public Coordinate cornerKickOffenseCoordinate;
    public Coordinate cornerKickDefenseCoordinate;

    public FormationSlot(
        string _defaultPositionGroupStr,
        string _defaultPositionStr,
        Coordinate _baseCoordinate,
        Coordinate _goalKickOffenseCoordinate,
        Coordinate _goalKickDefenseCoordinate,
        Coordinate _cornerKickOffenseCoordinate,
        Coordinate _cornerKickDefenseCoordinate
    )
    {
        defaultPositionGroupStr = _defaultPositionGroupStr;
        defaultPositionStr = _defaultPositionStr;
        baseCoordinate = _baseCoordinate;
        goalKickOffenseCoordinate = _goalKickOffenseCoordinate;
        goalKickDefenseCoordinate = _goalKickDefenseCoordinate;
        cornerKickOffenseCoordinate = _cornerKickOffenseCoordinate;
        cornerKickDefenseCoordinate = _cornerKickDefenseCoordinate;
    }
}

