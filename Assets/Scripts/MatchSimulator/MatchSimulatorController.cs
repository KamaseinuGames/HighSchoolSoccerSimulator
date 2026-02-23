using System.Collections.Generic;
using UnityEngine;

// 試合シミュレーションのメインコントローラー（MonoBehaviour）
public partial class MatchSimulatorController : MonoBehaviour
{
    [Header("試合設定")]
    public int matchMinutes = 90;

    [Header("データ")]
    [SerializeField] PositionDefinition_SO positionDefinitionSO;

    [Header("可視化")]
    public MatchSimulatorDigest matchSimulatorDigest;

    // 内部データ
    Team homeTeam;
    Team awayTeam;
    Ball ball;
    GridEvaluator gridEvaluator;
    MatchState matchState;
    List<PeriodLog> periodLogList;
    Player[] allPlayerList;

    // 機能（Feature）
    PartialBallFlight ballFlight;
    PartialKickoff kickoff;
    PartialPass pass;
    PartialShoot shoot;
    PartialClear clear;
    PartialDribble dribble;
    PartialHelpers helpers;
    PartialSetPlay setPlay;

    void Start()
    {
        RunSimulation();
    }

    public void RunSimulation()
    {
        Initialize();

        int totalPeriods = matchMinutes * Consts.PERIODS_PER_MINUTE;
        for (int period = 0; period < totalPeriods; period++)
        {
            PeriodLog log = ProcessPeriod(period);
            periodLogList.Add(log);
        }

        if (matchSimulatorDigest != null)
        {
            matchSimulatorDigest.Initialize(periodLogList, allPlayerList, matchState);
        }
    }

    void Initialize()
    {
        gridEvaluator = new GridEvaluator();
        periodLogList = new List<PeriodLog>();

        homeTeam = new Team(TeamSideCode.HOME, "Home Arsenal", positionDefinitionSO);
        awayTeam = new Team(TeamSideCode.AWAY, "Away Chelsea", positionDefinitionSO);
        homeTeam.CreatePlayers();
        awayTeam.CreatePlayers();

        List<Player> tempPlayerList = new List<Player>();
        tempPlayerList.AddRange(homeTeam.playerList);
        tempPlayerList.AddRange(awayTeam.playerList);
        allPlayerList = tempPlayerList.ToArray();

        DecideIntent();

        ball = new Ball();

        matchState = new MatchState(ball, homeTeam, awayTeam, allPlayerList);

        ballFlight = new PartialBallFlight(this);
        kickoff = new PartialKickoff(this);
        pass = new PartialPass(this);
        shoot = new PartialShoot(this);
        clear = new PartialClear(this);
        dribble = new PartialDribble(this);
        helpers = new PartialHelpers(this);
        setPlay = new PartialSetPlay(this);

        kickoff.SetupKickoff(TeamSideCode.HOME);
    }

    PeriodLog ProcessPeriod(int _period)
    {
        PeriodLog log = new PeriodLog(_period, allPlayerList.Length);

        // 0. トランジション残り時間を進める
        matchState.TickNegativeTransition();
        matchState.TickPositiveTransition();

        // 1. 攻撃側チームを更新・トランジション判定
        matchState.UpdateAttackingTeam();
        TeamSideCode currentAttackingTeamSideCode = matchState.attackingTeamSideCode;
        if (currentAttackingTeamSideCode != matchState.prevAttackingTeamSideCode)
        {
            matchState.ApplyNegativeTransition(matchState.prevAttackingTeamSideCode);
            matchState.ApplyPositiveTransition(currentAttackingTeamSideCode);
            matchState.prevAttackingTeamSideCode = currentAttackingTeamSideCode;
        }

        // 行動コードを毎tickリセット
        for (int i = 0; i < allPlayerList.Length; i++)
        {
            allPlayerList[i].actionCode = ActionCode.NONE;
        }

        // 2. 意図座標を更新（毎tick）
        ProcessNonBallHolderAction(log);

        // 2.1 パス中は受け手と最寄りの敵を落下点へ寄せる
        ballFlight.ApplyChaseIntent();

        // 2.2 ルーズボール時は両チームの最寄りをボールへ寄せる
        ballFlight.ApplyLooseBallChaseIntent();

        // 3. ボール保持者の行動決定
        Player holder = helpers.GetBallHolder();
        if (holder != null)
        {
            ProcessBallHolderAction(holder, log);
        }

        // 4. 全選手が移動
        foreach (Player player in allPlayerList)
        {
            if (!player.hasBall)
            {
                player.MoveToIntent(allPlayerList);
            }
        }

        // 5. 飛行中のボールを進める
        ballFlight.ProcessBallFlight(log);

        // 5.1 ルーズボールの回収
        ballFlight.ProcessLooseBall(log);

        // 6. ピリオド終了時の状態を記録
        if (!log.hasGoalFlag)
        {
            for (int i = 0; i < allPlayerList.Length; i++)
            {
                log.playerCoordinates[i] = allPlayerList[i].coordinate;
                log.playerHasBall[i] = allPlayerList[i].hasBall;
            }
            log.ballCoordinate = ball.coordinate;
        }
        return log;
    }

    void ProcessBallHolderAction(Player _holdPlayer, PeriodLog _log)
    {
        _log.holderId = _holdPlayer.matchId;
        matchState.lastHolderIdInt = _holdPlayer.matchId;
        _holdPlayer.actionCode = ActionCode.NONE;

        if (_holdPlayer.offenseFreezeRemainingPeriodCountInt > 0)
        {
            return;
        }

        int goalY;
        if (_holdPlayer.teamSideCode == TeamSideCode.HOME)
        {
            goalY = GridEvaluator.HEIGHT - 1;
        }
        else
        {
            goalY = 0;
        }
        int distToGoal = System.Math.Abs(_holdPlayer.coordinate.y - goalY);

        // 競り合い中はドリブル継続を優先
        if (_holdPlayer.IsInDuel())
        {
            dribble.TryDribble(_holdPlayer, goalY, _log);
            return;
        }

        bool hasProcessedPenaltyKick = setPlay.TryProcessPenaltyKickAction(_holdPlayer, _log);
        if (hasProcessedPenaltyKick)
        {
            return;
        }

        bool hasProcessedKickoff = kickoff.TryProcessKickoffAction(_holdPlayer, _log);
        if (hasProcessedKickoff)
        {
            return;
        }

        if (ShouldTryClear(_holdPlayer))
        {
            float clearSelectProb = CalcClearSelectProb(_holdPlayer);
            bool isSelectClear = gridEvaluator.RollSuccess(clearSelectProb);
            if (isSelectClear)
            {
                clear.TryClear(_holdPlayer, _log);
                return;
            }
        }

        // シュート選択（ゴールに近い場合）
        if (distToGoal <= 16)
        {
            bool isSelectShoot = gridEvaluator.RollSuccess(Consts.SHOOT_CLOSE_RANGE_SELECT_PROB);
            if (isSelectShoot)
            {
                bool hasEnemyInCentralView = GridEvaluator.HasEnemyInCentralVision(_holdPlayer, allPlayerList);
                if (!hasEnemyInCentralView)
                {
                    shoot.TryShoot(_holdPlayer, goalY, _log);
                    return;
                }
            }
        }

        // GKは必ずパス
        int roleInt = _holdPlayer.matchId % 100;
        if (roleInt == 0)
        {
            Player bestReceiver = helpers.FindBestPassTargetPlayer(_holdPlayer);
            if (bestReceiver != null)
            {
                pass.TryPass(_holdPlayer, bestReceiver, _log);
            }
            return;
        }

        // デフォルト: ドリブル → パス
        bool canDribble = dribble.TryDribble(_holdPlayer, goalY, _log);
        if (!canDribble)
        {
            Player bestReceiver = helpers.FindBestPassTargetPlayer(_holdPlayer);
            if (bestReceiver != null)
            {
                pass.TryPass(_holdPlayer, bestReceiver, _log);
            }
        }
    }

    bool ShouldTryClear(Player _holdPlayer)
    {
        bool isOwnDefensiveZone;
        if (_holdPlayer.teamSideCode == TeamSideCode.HOME)
        {
            isOwnDefensiveZone = _holdPlayer.coordinate.y <= 30;
        }
        else
        {
            isOwnDefensiveZone = _holdPlayer.coordinate.y >= GridEvaluator.HEIGHT - 31;
        }
        if (!isOwnDefensiveZone)
        {
            return false;
        }

        Player nearestEnemy = helpers.FindNearestEnemyWithin2Grids(_holdPlayer.coordinate, _holdPlayer.teamSideCode);
        return nearestEnemy != null;
    }

    float CalcClearSelectProb(Player _holdPlayer)
    {
        float passPenalty = (100f - _holdPlayer.playerStatus.passInt) / 100f * 0.25f;
        return System.Math.Clamp(Consts.CLEAR_UNDER_PRESSURE_BASE_PROB + passPenalty, Consts.CLEAR_UNDER_PRESSURE_BASE_PROB, Consts.CLEAR_UNDER_PRESSURE_MAX_PROB);
    }

    void DecideIntent()
    {
        // 現在は全選手同条件（Intent未使用）
    }

    void ProcessNonBallHolderAction(PeriodLog _log)
    {
        foreach (Player player in allPlayerList)
        {
            if (player.actionCode != ActionCode.NONE)
            {
                continue;
            }

            bool isAttacking = matchState.IsAttackingTeam(player);

            if (isAttacking)
            {
                if (player.offenseFreezeRemainingPeriodCountInt > 0)
                {
                    continue;
                }
            }
            else
            {
                if (player.defenseFreezeRemainingPeriodCountInt > 0)
                {
                    continue;
                }
            }

            // GKは専用処理
            int roleInt = player.matchId % 100;
            if (roleInt == 0)
            {
                player.intentCoordinate = gridEvaluator.FindGkIntent(player, ball);
                continue;
            }

            // ダイナミックアンカー = 基本の理想位置
            Coordinate anchor = player.playerVariable.CalcDynamicAnchor(ball.coordinate, isAttacking);

            // 味方との反発処理
            Team ownTeam;
            if (player.teamSideCode == TeamSideCode.HOME)
            {
                ownTeam = homeTeam;
            }
            else
            {
                ownTeam = awayTeam;
            }
            anchor = ApplyTeammateRepulsion(anchor, player, ownTeam);

            player.intentCoordinate = anchor;
        }
    }

    Coordinate ApplyTeammateRepulsion(Coordinate _anchor, Player _self, Team _ownTeam)
    {
        const int MIN_DISTANCE = 6;
        int adjustedX = _anchor.x;
        int adjustedY = _anchor.y;

        foreach (Player teammate in _ownTeam.playerList)
        {
            if (teammate == _self)
            {
                continue;
            }

            Coordinate teammateTarget = teammate.intentCoordinate;
            int distInt = _anchor.DistanceTo(teammateTarget);

            if (distInt >= MIN_DISTANCE)
            {
                continue;
            }
            if (distInt == 0)
            {
                distInt = 1;
            }

            int pushX = _anchor.x - teammateTarget.x;
            int pushY = _anchor.y - teammateTarget.y;

            int pushStrengthInt = (MIN_DISTANCE - distInt) / 2 + 1;

            if (pushX != 0)
            {
                adjustedX += System.Math.Sign(pushX) * pushStrengthInt;
            }
            if (pushY != 0)
            {
                adjustedY += System.Math.Sign(pushY) * pushStrengthInt;
            }
        }

        adjustedX = System.Math.Clamp(adjustedX, 0, GridEvaluator.WIDTH - 1);
        adjustedY = System.Math.Clamp(adjustedY, 0, GridEvaluator.HEIGHT - 1);
        return new Coordinate(adjustedX, adjustedY);
    }

    void ResetAfterGoal(TeamSideCode _kickoffTeamSideCode, PeriodLog _log)
    {
        for (int i = 0; i < allPlayerList.Length; i++)
        {
            Player player = allPlayerList[i];
            player.hasBall = false;
            player.defenseFreezeRemainingPeriodCountInt = 0;
            player.offenseFreezeRemainingPeriodCountInt = 0;
            
            Coordinate initialCoord;
            if (player.teamSideCode == TeamSideCode.HOME)
            {
                initialCoord = homeTeam.formationCoordinates[i];
            }
            else
            {
                int playerIndex = i - 11;
                initialCoord = awayTeam.formationCoordinates[playerIndex];
            }
            
            player.coordinate = initialCoord;
            player.intentCoordinate = initialCoord;
        }

        kickoff.SetupKickoff(_kickoffTeamSideCode);
        
        if (_kickoffTeamSideCode == TeamSideCode.HOME)
        {
            _log.kickoffPlayerIndex = 9;
        }
        else
        {
            _log.kickoffPlayerIndex = 20;
        }
    }
}
