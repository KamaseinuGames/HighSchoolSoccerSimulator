// 1ピリオドのログを記録するクラス（ダイジェスト再現用）
[System.Serializable]
public class PeriodLog
{
    public int periodIndex;
    public int holderId;
    public ActionCode holderAction;
    public int involverId;
    public ActionCode involverAction;
    public Coordinate ballCoordinate;
    public Coordinate[] playerCoordinates;
    public bool[] playerHasBall;

    // ゴール関連
    public bool hasGoalFlag;
    public Coordinate goalCoordinate;
    public int kickoffPlayerIndex;

    public PeriodLog(int _periodIndex, int _playerCount)
    {
        periodIndex = _periodIndex;
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
        int minute = periodIndex / Consts.PERIODS_PER_MINUTE;
        int second = (periodIndex % Consts.PERIODS_PER_MINUTE) / Consts.PERIODS_PER_SECOND;
        int tenth = periodIndex % Consts.PERIODS_PER_SECOND;
        return $"[{minute}分{second}.{tenth}秒]";
    }
}
