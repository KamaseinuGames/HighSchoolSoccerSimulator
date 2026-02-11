using System.Collections.Generic;

// チームを表すクラス
[System.Serializable]
public class Team
{
    public string nameStr;
    public TeamSideCode teamSideCode;  // Home（Y+方向に攻める） or Away（Y-方向に攻める）
    public int scoreInt;
    public List<Player> playerList;
    public Coordinate[] formationCoordinates;  // フォーメーション座標リスト（初期配置）

    public Team(TeamSideCode _teamSideCode, string _nameStr)
    {
        this.teamSideCode = _teamSideCode;
        this.nameStr = _nameStr;
        scoreInt = 0;
        playerList = new List<Player>();
        formationCoordinates = new Coordinate[11];
    }

    // 11人の選手を初期配置で生成
    public void CreatePlayers()
    {
        playerList.Clear();
        
        // GK
        Coordinate coord0 = GetInitialCoordinate(0);
        formationCoordinates[0] = coord0;
        AddPlayer("GK", coord0);
        
        // DF (4人)
        Coordinate coord1 = GetInitialCoordinate(1);
        formationCoordinates[1] = coord1;
        AddPlayer("DF1", coord1);
        Coordinate coord2 = GetInitialCoordinate(2);
        formationCoordinates[2] = coord2;
        AddPlayer("DF2", coord2);
        Coordinate coord3 = GetInitialCoordinate(3);
        formationCoordinates[3] = coord3;
        AddPlayer("DF3", coord3);
        Coordinate coord4 = GetInitialCoordinate(4);
        formationCoordinates[4] = coord4;
        AddPlayer("DF4", coord4);
        
        // MF (4人)
        Coordinate coord5 = GetInitialCoordinate(5);
        formationCoordinates[5] = coord5;
        AddPlayer("MF1", coord5);
        Coordinate coord6 = GetInitialCoordinate(6);
        formationCoordinates[6] = coord6;
        AddPlayer("MF2", coord6);
        Coordinate coord7 = GetInitialCoordinate(7);
        formationCoordinates[7] = coord7;
        AddPlayer("MF3", coord7);
        Coordinate coord8 = GetInitialCoordinate(8);
        formationCoordinates[8] = coord8;
        AddPlayer("MF4", coord8);
        
        // FW (2人)
        Coordinate coord9 = GetInitialCoordinate(9);
        formationCoordinates[9] = coord9;
        AddPlayer("FW1", coord9);
        Coordinate coord10 = GetInitialCoordinate(10);
        formationCoordinates[10] = coord10;
        AddPlayer("FW2", coord10);
    }

    void AddPlayer(string _role, Coordinate _coord)
    {
        int teamIndex;  // Home: 0, Away: 1（プレイヤーID用）
        if (teamSideCode == TeamSideCode.HOME)
        {
            teamIndex = 0;
        }
        else
        {
            teamIndex = 1;
        }
        int matchId = teamIndex * 100 + playerList.Count;  // チームIDと連番でユニークID
        int uniformId = playerList.Count + 1;  // ユニフォーム番号（1〜11）
        string playerNameStr = $"{nameStr}_{_role}";
        PlayerProfile playerProfile = new PlayerProfile(uniformId, playerNameStr);
        Player player = new Player(matchId, playerProfile, teamSideCode, PlayerStatus.CreateRandom());
        player.coordinate = _coord;
        player.intentCoordinate = _coord;
        playerList.Add(player);
    }

    // 初期配置を取得（インデックスに基づく）
    Coordinate GetInitialCoordinate(int _index)
    {
        // 自陣に配置（Home: Y=0-40, Away: Y=60-99）
        int[] baseY = { 0, 10, 10, 20, 20, 30, 30, 40, 40, 55, 55 };  // 自陣内に配置（FWはY=55で相手陣側）
        int[] baseX = { 35, 7, 25, 45, 60, 8, 25, 42, 60, 17, 50 };  // 各ポジションの行動可能範囲内に配置

        int y, x;
        if (teamSideCode == TeamSideCode.HOME)
        {
            // Home: Y=0が自陣ゴール、Y=0-40が自陣、Y+方向に攻める
            y = baseY[_index];
            x = baseX[_index];
        }
        else
        {
            // Away: Y=HEIGHT-1が自陣ゴール、Y=60-100が自陣、Y-方向に攻める
            y = (GridEvaluator.HEIGHT - 1) - baseY[_index];  // 反転して自陣に配置
            x = baseX[_index];
        }

        return new Coordinate(x, y);
    }

    // 指定インデックスの選手の初期配置を取得（外部から呼び出し可能）
    public Coordinate GetPlayerInitialCoordinate(int _playerIndex)
    {
        if (_playerIndex < 0 || _playerIndex >= 11)
        {
            int defaultY;  // デフォルトはGK位置
            if (teamSideCode == TeamSideCode.HOME)
            {
                defaultY = 0;
            }
            else
            {
                defaultY = GridEvaluator.HEIGHT - 1;
            }
            return new Coordinate(35, defaultY);
        }
        
        return GetInitialCoordinate(_playerIndex);
    }

    // ボールを持っている選手を取得
    public Player GetBallHolder()
    {
        return playerList.Find(p => p.hasBall);
    }

    public override string ToString()
    {
        return $"{nameStr} (Score: {scoreInt})";
    }
}
