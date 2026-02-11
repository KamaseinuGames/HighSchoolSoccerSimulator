// ボールを表すクラス
[System.Serializable]
public class Ball
{
    public int holderId;       // 保持している選手のID（-1なら誰も持っていない）
    public TeamSideCode? holderTeamSideCode;  // 保持している選手のチームサイド（nullなら不明）
    public Coordinate coordinate;
    public BallStateCode ballStateCode;

    public Ball()
    {
        holderId = -1;
        holderTeamSideCode = null;
        coordinate = new Coordinate(35, 50);  // センターサークル付近（1m = 1グリッド）
        ballStateCode = BallStateCode.LOOSE;
    }

    // 選手がボールを保持する
    public void SetHolder(Player _player)
    {
        holderId = _player.matchId;
        holderTeamSideCode = _player.teamSideCode;
        coordinate = _player.coordinate;
        ballStateCode = BallStateCode.HOLD;
        _player.hasBall = true;
    }

    // ボールをルーズにする
    public void SetLoose(Coordinate _coord)
    {
        holderId = -1;
        holderTeamSideCode = null;
        coordinate = _coord;
        ballStateCode = BallStateCode.LOOSE;
    }

    // ボールを移動する（パスやシュート時）
    public void MoveTo(Coordinate _newCoordinate)
    {
        coordinate = _newCoordinate;
    }

    public override string ToString()
    {
        return $"Ball {coordinate} [{ballStateCode}] Holder:{holderId}";
    }
}
