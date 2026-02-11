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

    // プロフィールとステータス
    public PlayerProfile playerProfile;
    public PlayerStatus playerStatus;
    public PlayerVariable playerVariable;

    public Player(int _matchId, PlayerProfile _playerProfile, TeamSideCode _teamSideCode, PlayerStatus _playerStatus)
    {
        this.matchId = _matchId;
        this.teamSideCode = _teamSideCode;
        
        this.actionCode = ActionCode.NONE;
        this.offensiveTacticsCode = TacticsCode.NONE;
        this.defensiveTacticsCode = TacticsCode.NONE;
        this.hasBall = false;
        
        this.playerProfile = _playerProfile;
        this.playerStatus = _playerStatus;
        this.playerVariable = new PlayerVariable(_playerStatus, _matchId, _teamSideCode);
    }

    // 意図位置に向けて移動（Speedに基づく）
    public void MoveToIntent(Player[] _allPlayerList)
    {
        if (coordinate == intentCoordinate) return;

        // Speedに基づいて移動できるマス数を計算（5秒1ピリオド）
        int maxMove = playerVariable.maxMovableInt;

        int dx = intentCoordinate.x - coordinate.x;
        int dy = intentCoordinate.y - coordinate.y;

        Coordinate newCoord = coordinate;

        // x方向に移動
        if (dx != 0)
        {
            int moveX = System.Math.Sign(dx) * System.Math.Min(System.Math.Abs(dx), maxMove);
            newCoord = new Coordinate(coordinate.x + moveX, coordinate.y);
            maxMove -= System.Math.Abs(moveX);
        }

        // 残りでy方向に移動
        if (dy != 0 && maxMove > 0)
        {
            int moveY = System.Math.Sign(dy) * System.Math.Min(System.Math.Abs(dy), maxMove);
            newCoord = new Coordinate(newCoord.x, newCoord.y + moveY);
        }

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
