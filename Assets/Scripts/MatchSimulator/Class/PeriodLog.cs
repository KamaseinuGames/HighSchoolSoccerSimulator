// 1ピリオドのログを記録するクラス
[System.Serializable]
public class PeriodLog
{
    public int minuteInt;
    public int holderId;           // ボール保持者のID
    public ActionCode holderAction;  // ボール保持者の行動
    public int involverId;          // 関与者のID（GKがブロック、DFがインターセプト、味方がパスを受けるなど）
    public ActionCode involverAction;  // 関与者の行動
    public Coordinate ballCoordinate;
    public Coordinate[] playerCoordinates;  // 全選手の位置
    public bool[] playerHasBall;            // 各選手がボールを持っているか
    
    // ゴール関連
    public bool hasGoalFlag;                    // ゴールが発生したか
    public Coordinate goalCoordinate;       // ゴール位置（ゴール時のみ）
    public int kickoffPlayerIndex;          // キックオフ選手のインデックス（ゴール時のみ）

    public PeriodLog(int _minuteInt, int _playerCount)
    {
        this.minuteInt = _minuteInt;
        holderId = -1;
        holderAction = ActionCode.NONE;
        involverId = -1;
        involverAction = ActionCode.NONE;
        playerCoordinates = new Coordinate[_playerCount];
        playerHasBall = new bool[_playerCount];
        hasGoalFlag = false;
        kickoffPlayerIndex = -1;
    }

    public override string ToString()
    {
        // Constsのtick定義に従って時間表示を作る（0.1秒 = 1ピリオド）
        int minute = minuteInt / Consts.PERIODS_PER_MINUTE;
        int second = (minuteInt % Consts.PERIODS_PER_MINUTE) / Consts.PERIODS_PER_SECOND;
        int tenth = minuteInt % Consts.PERIODS_PER_SECOND;
        return $"[{minute}分{second}.{tenth}秒]";
    }
}
