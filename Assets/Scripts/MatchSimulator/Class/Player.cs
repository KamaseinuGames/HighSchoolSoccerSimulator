// 選手（プレイヤー）を表すクラス
[System.Serializable]
public class Player
{
    // 試合内パラメータ
    public int matchId;  // 試合内での一意ID
    public TeamSideCode teamSideCode;  // Home or Away
    public Coordinate coordinate;
    public Coordinate intentCoordinate;  // 次に行きたい位置（意図）
    public ActionCode actionCode;  // 選手の行動（パス、ドリブル、シュートなど）
    public IntentHolderCode intentHolderCode;    // ボール保持時の意図
    public IntentOffenseCode intentOffenseCode;  // 非保持時（攻撃）の意図
    public IntentDefenseCode intentDefenseCode;  // 非保持時（守備）の意図
    public TacticsCode offensiveTacticsCode;  // 攻撃戦術コード
    public TacticsCode defensiveTacticsCode;  // 守備戦術コード
    public bool hasBall;

    // tick（0.1秒）移動用：移動ポイント蓄積
    int movePointInt;

    // 競り合い（ドリブル/奪取）簡易状態
    public int duelOpponentId;
    public int duelRemainingPeriodCountInt;

    // ネガティブトランジション：守備行動不能（残りperiod数）
    public int defenseFreezeRemainingPeriodCountInt;
    public int offenseFreezeRemainingPeriodCountInt;

    // プロフィールとステータス
    public PlayerProfile playerProfile;
    public PlayerStatus playerStatus;
    public PlayerVariable playerVariable;

    public Player(int _matchId, PlayerProfile _playerProfile, TeamSideCode _teamSideCode, PlayerStatus _playerStatus, FormationSlot _slot, PositionDefinition _roleData)
    {
        this.matchId = _matchId;
        this.teamSideCode = _teamSideCode;
        
        this.actionCode = ActionCode.NONE;
        this.offensiveTacticsCode = TacticsCode.NONE;
        this.defensiveTacticsCode = TacticsCode.NONE;
        this.hasBall = false;
        
        this.playerProfile = _playerProfile;
        this.playerStatus = _playerStatus;
        this.playerVariable = new PlayerVariable(_playerStatus, _teamSideCode, _slot, _roleData);
        this.movePointInt = 0;

        this.duelOpponentId = -1;
        this.duelRemainingPeriodCountInt = 0;
        this.defenseFreezeRemainingPeriodCountInt = 0;
        this.offenseFreezeRemainingPeriodCountInt = 0;
    }

    public bool IsInDuel()
    {
        return duelRemainingPeriodCountInt > 0;
    }

    public void ClearDuel()
    {
        duelOpponentId = -1;
        duelRemainingPeriodCountInt = 0;
    }

    public void StartDuel(int _opponentId, int _durationPeriodCountInt)
    {
        duelOpponentId = _opponentId;
        duelRemainingPeriodCountInt = System.Math.Max(1, _durationPeriodCountInt);
    }

    // 意図位置に向けて移動（Speedに基づく）
    public void MoveToIntent(Player[] _allPlayerList)
    {
        if (coordinate == intentCoordinate) return;

        // 0.1秒tick用：速度に応じて移動ポイントを蓄積し、50到達ごとに1マス移動
        movePointInt += playerStatus.speedInt;
        int maxMove = movePointInt / 50;
        if (maxMove <= 0)
        {
            return;
        }
        movePointInt -= maxMove * 50;

        int dx = intentCoordinate.x - coordinate.x;
        int dy = intentCoordinate.y - coordinate.y;

        // チェビシェフ距離: X,Yそれぞれ独立にmaxMoveまで移動（斜め移動コスト1）
        int moveX = System.Math.Sign(dx) * System.Math.Min(System.Math.Abs(dx), maxMove);
        int moveY = System.Math.Sign(dy) * System.Math.Min(System.Math.Abs(dy), maxMove);
        Coordinate newCoord = new Coordinate(coordinate.x + moveX, coordinate.y + moveY);

        // 移動先に味方がいる場合は移動をキャンセル（現在位置に留まる）
        if (IsOccupiedByTeammate(newCoord, _allPlayerList))
        {
            return;
        }

        coordinate = newCoord;
    }

    // 指定座標に味方がいるかチェック（自分自身は除く）
    bool IsOccupiedByTeammate(Coordinate _coord, Player[] _allPlayerList)
    {
        foreach (Player p in _allPlayerList)
        {
            if (p == this) continue;
            if (p.teamSideCode == teamSideCode && p.coordinate == _coord)
            {
                return true;
            }
        }
        return false;
    }

    public override string ToString()
    {
        string ballStr;
        if (hasBall)
        {
            ballStr = "●";
        }
        else
        {
            ballStr = "";
        }
        return $"{playerProfile.nameStr}{ballStr} {coordinate}";
    }
}
