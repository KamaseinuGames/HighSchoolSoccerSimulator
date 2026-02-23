// 試合全体の状態を管理するクラス（ゲームサーバーの役割）
[System.Serializable]
public class MatchState
{
    public TeamSideCode attackingTeamSideCode;
    public TeamSideCode prevAttackingTeamSideCode;
    public int lastHolderIdInt;
    public int homeScoreInt;
    public int awayScoreInt;
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
        homeScoreInt = 0;
        awayScoreInt = 0;
        lastHolderIdInt = -1;
        UpdateAttackingTeam();
        prevAttackingTeamSideCode = attackingTeamSideCode;
    }

    public void UpdateAttackingTeam()
    {
        if (ball.holderId >= 0 && ball.holderTeamSideCode.HasValue)
        {
            attackingTeamSideCode = ball.holderTeamSideCode.Value;
        }
    }

    public bool IsAttackingTeam(Player _player)
    {
        return _player.teamSideCode == attackingTeamSideCode;
    }

    public bool IsDefendingTeam(Player _player)
    {
        return !IsAttackingTeam(_player);
    }

    public Team GetAttackingTeam()
    {
        if (attackingTeamSideCode == TeamSideCode.HOME)
        {
            return homeTeam;
        }
        return awayTeam;
    }

    public Team GetDefendingTeam()
    {
        if (attackingTeamSideCode == TeamSideCode.HOME)
        {
            return awayTeam;
        }
        return homeTeam;
    }

    public void ScoreGoal(TeamSideCode _scoringTeamSideCode)
    {
        if (_scoringTeamSideCode == TeamSideCode.HOME)
        {
            homeScoreInt++;
        }
        else
        {
            awayScoreInt++;
        }
    }

    // ネガティブトランジション: 守備行動不能の残り時間を1tick進める
    public void TickNegativeTransition()
    {
        for (int i = 0; i < allPlayerList.Length; i++)
        {
            Player player = allPlayerList[i];
            if (player.defenseFreezeRemainingPeriodCountInt <= 0)
            {
                continue;
            }
            player.defenseFreezeRemainingPeriodCountInt--;
        }
    }

    // ポジティブトランジション: 攻撃行動不能の残り時間を1tick進める
    public void TickPositiveTransition()
    {
        for (int i = 0; i < allPlayerList.Length; i++)
        {
            Player player = allPlayerList[i];
            if (player.offenseFreezeRemainingPeriodCountInt <= 0)
            {
                continue;
            }
            player.offenseFreezeRemainingPeriodCountInt--;
        }
    }

    // ネガティブトランジション適用: ボールロスト側チームに守備凍結を適用
    public void ApplyNegativeTransition(TeamSideCode _lostTeamSideCode)
    {
        for (int i = 0; i < allPlayerList.Length; i++)
        {
            Player player = allPlayerList[i];
            if (player.teamSideCode != _lostTeamSideCode)
            {
                continue;
            }

            int freezePeriodCountInt = player.playerVariable.negativeTransitionNonHolderFreezePeriodCountInt;
            if (player.matchId == lastHolderIdInt)
            {
                freezePeriodCountInt = player.playerVariable.negativeTransitionLostHolderFreezePeriodCountInt;
            }

            if (player.defenseFreezeRemainingPeriodCountInt < freezePeriodCountInt)
            {
                player.defenseFreezeRemainingPeriodCountInt = freezePeriodCountInt;
            }
        }
    }

    // ポジティブトランジション適用: ボール奪取側チームに攻撃凍結を適用
    public void ApplyPositiveTransition(TeamSideCode _gainedTeamSideCode)
    {
        for (int i = 0; i < allPlayerList.Length; i++)
        {
            Player player = allPlayerList[i];
            if (player.teamSideCode != _gainedTeamSideCode)
            {
                continue;
            }

            int freezePeriodCountInt = player.playerVariable.positiveTransitionNonHolderFreezePeriodCountInt;
            if (player.matchId == lastHolderIdInt)
            {
                freezePeriodCountInt = player.playerVariable.positiveTransitionHolderFreezePeriodCountInt;
            }

            if (player.offenseFreezeRemainingPeriodCountInt < freezePeriodCountInt)
            {
                player.offenseFreezeRemainingPeriodCountInt = freezePeriodCountInt;
            }
        }
    }
}
