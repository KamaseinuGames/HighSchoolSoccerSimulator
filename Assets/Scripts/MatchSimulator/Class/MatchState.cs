// 試合全体の状態を管理するクラス（PeriodLogには残さないものたち）
[System.Serializable]
public class MatchState
{
    public TeamSideCode attackingTeamSideCode;  // 攻撃側チーム（ボール保持側）
    public Ball ball;
    public Team homeTeam;
    public Team awayTeam;
    public Player[] allPlayerList;

    public MatchState(Ball _ball, Team _homeTeam, Team _awayTeam, Player[] _allPlayerList)
    {
        ball = _ball;
        homeTeam = _homeTeam;
        awayTeam = _awayTeam;
        allPlayerList = _allPlayerList;
        UpdateAttackingTeam();
    }

    // 攻撃側チームを更新（ボール保持者に基づく）
    public void UpdateAttackingTeam()
    {
        if (ball.holderId >= 0 && ball.holderTeamSideCode.HasValue)
        {
            attackingTeamSideCode = ball.holderTeamSideCode.Value;
        }
        // ルーズボールの場合は前回の攻撃側を維持（または判定が必要な場合は追加ロジック）
    }

    // 指定選手が攻撃側かどうかを判定
    public bool IsAttackingTeam(Player _player)
    {
        return _player.teamSideCode == attackingTeamSideCode;
    }

    // 指定選手が守備側かどうかを判定
    public bool IsDefendingTeam(Player _player)
    {
        return !IsAttackingTeam(_player);
    }

    // 攻撃側チームを取得
    public Team GetAttackingTeam()
    {
        if (attackingTeamSideCode == TeamSideCode.HOME)
        {
            return homeTeam;
        }
        else
        {
            return awayTeam;
        }
    }

    // 守備側チームを取得
    public Team GetDefendingTeam()
    {
        if (attackingTeamSideCode == TeamSideCode.HOME)
        {
            return awayTeam;
        }
        else
        {
            return homeTeam;
        }
    }
}
