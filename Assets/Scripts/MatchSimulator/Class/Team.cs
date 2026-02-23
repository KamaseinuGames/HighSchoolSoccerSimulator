using System.Collections.Generic;

// チームを表すクラス
[System.Serializable]
public class Team
{
    public string nameStr;
    public TeamSideCode teamSideCode;
    public List<Player> playerList;
    public FormationData formationData;
    public Coordinate[] formationCoordinates;

    // ポジション可動域辞書（全チーム共通、初回のみ生成）
    static Dictionary<string, PositionDefinition> positionRoleDictionary;

    public Team(TeamSideCode _teamSideCode, string _nameStr, PositionDefinition_SO _positionDefinitionSO)
    {
        teamSideCode = _teamSideCode;
        nameStr = _nameStr;
        playerList = new List<Player>();
        formationData = FormationData.CreateDefault442();
        formationCoordinates = new Coordinate[11];

        if (positionRoleDictionary == null)
        {
            positionRoleDictionary = PositionDefinition.CreateFromSO(_positionDefinitionSO);
        }
    }

    // 11人の選手をフォーメーションに基づいて生成
    // playerList[0] = GK（固定座標）、playerList[1〜10] = slotArray[0〜9]
    public void CreatePlayers()
    {
        playerList.Clear();

        // GK（固定座標、slotArray外）
        Coordinate gkCoord = GetGkCoordinate();
        formationCoordinates[0] = gkCoord;
        PositionDefinition gkRoleData = GetPositionDefinition("GK");
        FormationSlot gkSlot = new FormationSlot(
            "GK", "GK",
            FormationData.GK_BASE_COORDINATE,
            FormationData.GK_BASE_COORDINATE,
            FormationData.GK_BASE_COORDINATE,
            FormationData.GK_BASE_COORDINATE,
            FormationData.GK_BASE_COORDINATE
        );
        AddPlayer("GK", gkCoord, gkSlot, gkRoleData);

        // フィールドプレイヤー（slotArray: 10人）
        for (int i = 0; i < formationData.slotArray.Length; i++)
        {
            FormationSlot slot = formationData.slotArray[i];
            Coordinate coord = GetBaseCoordinate(i);
            formationCoordinates[i + 1] = coord;

            string roleStr = slot.defaultPositionStr;
            PositionDefinition roleData = GetPositionDefinition(roleStr);
            AddPlayer(roleStr, coord, slot, roleData);
        }
    }

    void AddPlayer(string _roleStr, Coordinate _coord, FormationSlot _slot, PositionDefinition _roleData)
    {
        int teamIndex;
        if (teamSideCode == TeamSideCode.HOME)
        {
            teamIndex = 0;
        }
        else
        {
            teamIndex = 1;
        }
        int matchId = teamIndex * 100 + playerList.Count;
        int uniformId = playerList.Count + 1;
        string playerNameStr = $"{nameStr}_{_roleStr}{uniformId}";
        PlayerProfile playerProfile = new PlayerProfile(uniformId, playerNameStr);
        Player player = new Player(matchId, playerProfile, teamSideCode, PlayerStatus.CreateRandom(), _slot, _roleData);
        player.coordinate = _coord;
        player.intentCoordinate = _coord;
        playerList.Add(player);
    }

    // GK座標を取得（AwayはY反転）
    Coordinate GetGkCoordinate()
    {
        Coordinate gkCoord = FormationData.GK_BASE_COORDINATE;
        if (teamSideCode == TeamSideCode.AWAY)
        {
            return new Coordinate(gkCoord.x, (GridEvaluator.HEIGHT - 1) - gkCoord.y);
        }
        return gkCoord;
    }

    // フィールドプレイヤーのベース座標を取得（AwayはY反転）
    // _slotIndex: slotArray上のインデックス（0〜9）
    Coordinate GetBaseCoordinate(int _slotIndex)
    {
        Coordinate baseCoord = formationData.slotArray[_slotIndex].baseCoordinate;
        if (teamSideCode == TeamSideCode.AWAY)
        {
            return new Coordinate(baseCoord.x, (GridEvaluator.HEIGHT - 1) - baseCoord.y);
        }
        return baseCoord;
    }

    // ポジション可動域データを取得（見つからない場合はCMFをデフォルトとする）
    PositionDefinition GetPositionDefinition(string _positionStr)
    {
        if (positionRoleDictionary.ContainsKey(_positionStr))
        {
            return positionRoleDictionary[_positionStr];
        }
        return positionRoleDictionary["CMF"];
    }

    // 指定インデックスの選手の初期配置を取得
    // _playerIndex: playerList上のインデックス（0=GK, 1〜10=フィールド）
    public Coordinate GetPlayerInitialCoordinate(int _playerIndex)
    {
        if (_playerIndex < 0 || _playerIndex >= 11)
        {
            int defaultY;
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
        return formationCoordinates[_playerIndex];
    }

    public Player GetBallHolder()
    {
        return playerList.Find(p => p.hasBall);
    }

    public override string ToString()
    {
        return nameStr;
    }
}
