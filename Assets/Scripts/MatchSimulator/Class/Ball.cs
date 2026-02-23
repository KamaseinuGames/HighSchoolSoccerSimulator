// ボールを表すクラス
[System.Serializable]
public class Ball
{
    public int holderId;       // 保持している選手のID（-1なら誰も持っていない）
    public TeamSideCode? holderTeamSideCode;  // 保持している選手のチームサイド（nullなら不明）
    public Coordinate coordinate;
    public BallStateCode ballStateCode;
    public TeamSideCode lastTouchTeamSideCode;  // 最後に触れたチーム（ラインアウト判定用）

    // === 飛行（中間処理） ===
    public Coordinate flightTargetCoordinate;
    public Coordinate[] flightPathCoordinateArray;
    public int flightPathIndexInt;
    public int flightCellsPerPeriodInt;
    public int flightRemainingPeriodCount;
    public int flightFinalHolderId;
    public TeamSideCode flightPasserTeamSideCode;
    public int flightIntendedReceiverId;
    public ActionCode flightArrivalActionCode;

    public Ball()
    {
        holderId = -1;
        holderTeamSideCode = null;
        coordinate = new Coordinate(35, 50);  // センターサークル付近（1m = 1グリッド）
        ballStateCode = BallStateCode.LOOSE;
        lastTouchTeamSideCode = TeamSideCode.HOME;
        ClearFlight();
    }

    void ClearFlight()
    {
        flightTargetCoordinate = coordinate;
        flightPathCoordinateArray = null;
        flightPathIndexInt = 0;
        flightCellsPerPeriodInt = 0;
        flightRemainingPeriodCount = 0;
        flightFinalHolderId = -1;
        flightPasserTeamSideCode = TeamSideCode.HOME;
        flightIntendedReceiverId = -1;
        flightArrivalActionCode = ActionCode.NONE;
    }

    // 選手がボールを保持する
    public void SetHolder(Player _player)
    {
        holderId = _player.matchId;
        holderTeamSideCode = _player.teamSideCode;
        coordinate = _player.coordinate;
        ballStateCode = BallStateCode.HOLD;
        lastTouchTeamSideCode = _player.teamSideCode;
        _player.hasBall = true;
        ClearFlight();
    }

    // ボールをルーズにする
    public void SetLoose(Coordinate _coord)
    {
        holderId = -1;
        holderTeamSideCode = null;
        coordinate = _coord;
        ballStateCode = BallStateCode.LOOSE;
        ClearFlight();
    }

    // ボールを飛行状態にする（保持者なしで移動）
    public void StartFlight(
        Coordinate _startCoordinate,
        Coordinate _targetCoordinate,
        Coordinate[] _pathCoordinateArray,
        int _cellsPerPeriodInt,
        TeamSideCode _passerTeamSideCode,
        int _intendedReceiverId,
        int _finalHolderId,
        ActionCode _arrivalActionCode
    )
    {
        holderId = -1;
        holderTeamSideCode = null;
        coordinate = _startCoordinate;
        ballStateCode = BallStateCode.FLYING;

        flightTargetCoordinate = _targetCoordinate;
        flightPathCoordinateArray = _pathCoordinateArray;
        flightPathIndexInt = 0;
        flightCellsPerPeriodInt = System.Math.Max(1, _cellsPerPeriodInt);
        flightPasserTeamSideCode = _passerTeamSideCode;
        lastTouchTeamSideCode = _passerTeamSideCode;
        flightIntendedReceiverId = _intendedReceiverId;
        flightFinalHolderId = _finalHolderId;
        flightArrivalActionCode = _arrivalActionCode;

        int pathStepCount = 0;
        if (flightPathCoordinateArray != null)
        {
            pathStepCount = flightPathCoordinateArray.Length - 1;
        }

        // 例: pathが10ステップ、2マス/ピリオドなら ceil(10/2)=5
        flightRemainingPeriodCount = (pathStepCount + flightCellsPerPeriodInt - 1) / flightCellsPerPeriodInt;
        if (flightRemainingPeriodCount <= 0)
        {
            flightRemainingPeriodCount = 1;
        }
    }

    // 飛行を1マスだけ進める。終点に到達したらtrue
    public bool AdvanceFlightOneCell()
    {
        if (ballStateCode != BallStateCode.FLYING)
        {
            return false;
        }

        if (flightPathCoordinateArray == null || flightPathCoordinateArray.Length == 0)
        {
            coordinate = flightTargetCoordinate;
            return true;
        }

        int maxIndex = flightPathCoordinateArray.Length - 1;
        flightPathIndexInt = System.Math.Min(flightPathIndexInt + 1, maxIndex);
        coordinate = flightPathCoordinateArray[flightPathIndexInt];

        if (flightPathIndexInt >= maxIndex)
        {
            coordinate = flightTargetCoordinate;
            return true;
        }

        return false;
    }

    // 飛行を終了してルーズにする（到着・こぼれ球用）
    public void EndFlightAsLoose()
    {
        if (ballStateCode != BallStateCode.FLYING)
        {
            return;
        }

        holderId = -1;
        holderTeamSideCode = null;
        ballStateCode = BallStateCode.LOOSE;
        ClearFlight();
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
