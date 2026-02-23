// ステータスから算出されるパラメータを管理するクラス
// ダイナミックアンカー方式: ボール位置に応じて動的にアンカーと可動域を算出
[System.Serializable]
public class PlayerVariable
{
    public int maxMovableInt;

    // ダイナミックアンカー算出に必要な参照を保持
    FormationSlot formationSlot;
    PositionDefinition positionRoleData;
    TeamSideCode teamSideCode;

    // ネガティブトランジション（攻→守の切り替え遅れ）
    public int negativeTransitionLostHolderFreezePeriodCountInt;
    public int negativeTransitionNonHolderFreezePeriodCountInt;
    public int positiveTransitionHolderFreezePeriodCountInt;
    public int positiveTransitionNonHolderFreezePeriodCountInt;

    public PlayerVariable(PlayerStatus _playerStatus, TeamSideCode _teamSideCode, FormationSlot _slot, PositionDefinition _roleData)
    {
        maxMovableInt = GetMaxMovableInt(_playerStatus.speedInt);

        formationSlot = _slot;
        positionRoleData = _roleData;
        teamSideCode = _teamSideCode;

        // ネガティブトランジション
        negativeTransitionLostHolderFreezePeriodCountInt = GetNegativeTransitionLostHolderFreezePeriodCountInt(_playerStatus.defenseInt);
        negativeTransitionNonHolderFreezePeriodCountInt = GetNegativeTransitionNonHolderFreezePeriodCountInt(_playerStatus.defenseInt);

        int offenseSwitchInt = (_playerStatus.passInt + _playerStatus.dribbleInt + _playerStatus.shootInt) / 3;
        positiveTransitionHolderFreezePeriodCountInt = GetPositiveTransitionHolderFreezePeriodCountInt(offenseSwitchInt);
        positiveTransitionNonHolderFreezePeriodCountInt = GetPositiveTransitionNonHolderFreezePeriodCountInt(offenseSwitchInt);
    }

    // ダイナミックアンカーを算出
    // 動的アンカー = ベース座標（攻守別） + 追従係数 × (ボール位置 - ピッチ中央)
    public Coordinate CalcDynamicAnchor(Coordinate _ballCoordinate, bool _isAttacking)
    {
        Coordinate baseCoord;
        float followXRate;
        float followYRate;

        if (_isAttacking)
        {
            baseCoord = formationSlot.goalKickOffenseCoordinate;
            followXRate = positionRoleData.offenseFollowXRate;
            followYRate = positionRoleData.offenseFollowYRate;
        }
        else
        {
            baseCoord = formationSlot.goalKickDefenseCoordinate;
            followXRate = positionRoleData.defenseFollowXRate;
            followYRate = positionRoleData.defenseFollowYRate;
        }

        int centerX = GridEvaluator.WIDTH / 2;   // 35
        int centerY = GridEvaluator.HEIGHT / 2;   // 50

        float anchorXFloat = baseCoord.x + followXRate * (_ballCoordinate.x - centerX);
        float anchorYFloat = baseCoord.y + followYRate * (_ballCoordinate.y - centerY);

        if (teamSideCode == TeamSideCode.AWAY)
        {
            int maxY = GridEvaluator.HEIGHT - 1;
            // AwayはベースY反転のみ。追従方向は両チーム共通（ボールに寄る）
            anchorXFloat = baseCoord.x + followXRate * (_ballCoordinate.x - centerX);
            anchorYFloat = (maxY - baseCoord.y) + followYRate * (_ballCoordinate.y - centerY);
        }

        int anchorX = System.Math.Clamp((int)System.Math.Round(anchorXFloat), 0, GridEvaluator.WIDTH - 1);
        int anchorY = System.Math.Clamp((int)System.Math.Round(anchorYFloat), 0, GridEvaluator.HEIGHT - 1);

        return new Coordinate(anchorX, anchorY);
    }

    static int GetMaxMovableInt(int _speedInt)
    {
        if (_speedInt >= 90) return 9;
        if (_speedInt >= 80) return 8;
        if (_speedInt >= 70) return 7;
        if (_speedInt >= 60) return 6;
        if (_speedInt >= 50) return 5;
        if (_speedInt >= 40) return 4;
        if (_speedInt >= 30) return 3;
        if (_speedInt >= 20) return 2;
        return 1;
    }

    static int GetNegativeTransitionLostHolderFreezePeriodCountInt(int _defenseInt)
    {
        if (_defenseInt >= 90) return 3;
        if (_defenseInt >= 80) return 4;
        if (_defenseInt >= 70) return 5;
        if (_defenseInt >= 60) return 6;
        if (_defenseInt >= 50) return 7;
        return 8;
    }

    static int GetNegativeTransitionNonHolderFreezePeriodCountInt(int _defenseInt)
    {
        if (_defenseInt >= 90) return 1;
        if (_defenseInt >= 70) return 2;
        if (_defenseInt >= 50) return 3;
        return 4;
    }

    static int GetPositiveTransitionHolderFreezePeriodCountInt(int _offenseSwitchInt)
    {
        if (_offenseSwitchInt >= 90) return 2;
        if (_offenseSwitchInt >= 80) return 3;
        if (_offenseSwitchInt >= 70) return 4;
        if (_offenseSwitchInt >= 60) return 5;
        if (_offenseSwitchInt >= 50) return 6;
        return 7;
    }

    static int GetPositiveTransitionNonHolderFreezePeriodCountInt(int _offenseSwitchInt)
    {
        if (_offenseSwitchInt >= 90) return 1;
        if (_offenseSwitchInt >= 70) return 2;
        if (_offenseSwitchInt >= 50) return 3;
        return 4;
    }
}
