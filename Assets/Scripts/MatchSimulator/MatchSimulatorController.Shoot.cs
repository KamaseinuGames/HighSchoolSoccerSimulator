using UnityEngine;

// MatchSimulatorController のうち、シュート関連ロジックを分離
public partial class MatchSimulatorController
{
    class PartialShoot
    {
        readonly MatchSimulatorController controller;

        public PartialShoot(MatchSimulatorController _controller)
        {
            controller = _controller;
        }

        public void TryShoot(Player _shootPlayer, int _goalY, PeriodLog _log)
        {
            _shootPlayer.ClearDuel();

            Player blockPlayer = FindShootBlockPlayer(_shootPlayer);
            if (blockPlayer != null)
            {
                float blockProb = System.Math.Clamp(Consts.SHOOT_BLOCK_BASE_PROB + blockPlayer.playerStatus.defenseInt / 100f * 0.25f, Consts.SHOOT_BLOCK_BASE_PROB, Consts.SHOOT_BLOCK_MAX_PROB);
                bool isBlocked = controller.gridEvaluator.RollSuccess(blockProb);
                if (isBlocked)
                {
                    float handBallProb = CalcShootHandBallProb(blockPlayer);
                    bool isHandBall = controller.gridEvaluator.RollSuccess(handBallProb);
                    if (isHandBall)
                    {
                        _shootPlayer.hasBall = false;
                        controller.setPlay.ProcessFoul(_shootPlayer, blockPlayer, _shootPlayer.coordinate, true, _log);
                        return;
                    }

                    _log.holderAction = ActionCode.SHOOT_FAIL;
                    _shootPlayer.actionCode = ActionCode.SHOOT_FAIL;
                    _log.involverId = blockPlayer.matchId;
                    _log.involverAction = ActionCode.SHOOT_BLOCK;
                    _shootPlayer.hasBall = false;

                    Coordinate deflectCoordinate = BuildDeflectCoordinate(_shootPlayer.coordinate);
                    if (controller.setPlay.IsOutOfPitch(deflectCoordinate))
                    {
                        controller.setPlay.ProcessBallOutOfPlay(deflectCoordinate, blockPlayer.teamSideCode, _log);
                    }
                    else
                    {
                        controller.ball.SetLoose(deflectCoordinate);
                        controller.matchState.UpdateAttackingTeam();
                    }
                    return;
                }
            }

            float prob = controller.gridEvaluator.CalcShootSuccessProb(_shootPlayer, _goalY);
            bool isSuccess = controller.gridEvaluator.RollSuccess(prob);

            if (isSuccess)
            {
                _log.holderAction = ActionCode.SHOOT_SUCCESS;
                _shootPlayer.actionCode = ActionCode.SHOOT_SUCCESS;

                // スコア加算
                controller.matchState.ScoreGoal(_shootPlayer.teamSideCode);

                // ゴール時は関与者なし
                _log.involverId = -1;
                _log.involverAction = ActionCode.NONE;

                // ゴール位置を記録
                _log.hasGoalFlag = true;
                _log.goalCoordinate = new Coordinate(35, _goalY);

                // ゴール時の選手位置を記録（初期配置に戻す前に保存）
                for (int i = 0; i < controller.allPlayerList.Length; i++)
                {
                    _log.playerCoordinates[i] = controller.allPlayerList[i].coordinate;
                    _log.playerHasBall[i] = controller.allPlayerList[i].hasBall;
                }
                // ゴール時のボール位置も記録（シュート位置）
                _log.ballCoordinate = controller.ball.coordinate;

                // キックオフへ（相手ボール）
                TeamSideCode kickoffTeamSideCode;
                if (_shootPlayer.teamSideCode == TeamSideCode.HOME)
                {
                    kickoffTeamSideCode = TeamSideCode.AWAY;
                }
                else
                {
                    kickoffTeamSideCode = TeamSideCode.HOME;
                }
                controller.ResetAfterGoal(kickoffTeamSideCode, _log);
            }
            else
            {
                _log.holderAction = ActionCode.SHOOT_FAIL;
                _shootPlayer.actionCode = ActionCode.SHOOT_FAIL;

                // GKがシュートをキャッチ
                Team opponent;
                if (_shootPlayer.teamSideCode == TeamSideCode.HOME)
                {
                    opponent = controller.awayTeam;
                }
                else
                {
                    opponent = controller.homeTeam;
                }
                Player gk = opponent.playerList[0];  // GK

                _shootPlayer.hasBall = false;
                float gkCatchProb = System.Math.Clamp(Consts.SHOOT_GK_CATCH_BASE_PROB + gk.playerStatus.defenseInt / 100f * 0.35f, Consts.SHOOT_GK_CATCH_BASE_PROB, Consts.SHOOT_GK_CATCH_MAX_PROB);
                bool isCatch = controller.gridEvaluator.RollSuccess(gkCatchProb);
                if (isCatch)
                {
                    _log.involverId = gk.matchId;
                    _log.involverAction = ActionCode.SHOOT_CATCH;
                    controller.ball.SetHolder(gk);
                }
                else
                {
                    _log.involverId = gk.matchId;
                    _log.involverAction = ActionCode.SHOOT_PARRY;
                    Coordinate parryCoordinate = BuildParryCoordinate(gk.coordinate);
                    if (controller.setPlay.IsOutOfPitch(parryCoordinate))
                    {
                        controller.setPlay.ProcessBallOutOfPlay(parryCoordinate, gk.teamSideCode, _log);
                    }
                    else
                    {
                        controller.ball.SetLoose(parryCoordinate);
                    }
                }

                // 攻撃側チームを更新
                controller.matchState.UpdateAttackingTeam();
            }
        }

        Player FindShootBlockPlayer(Player _shootPlayer)
        {
            Player bestBlockPlayer = null;
            int bestDistanceInt = int.MaxValue;
            int goalY;
            if (_shootPlayer.teamSideCode == TeamSideCode.HOME)
            {
                goalY = GridEvaluator.HEIGHT - 1;
            }
            else
            {
                goalY = 0;
            }

            for (int i = 0; i < controller.allPlayerList.Length; i++)
            {
                Player player = controller.allPlayerList[i];
                if (player.teamSideCode == _shootPlayer.teamSideCode)
                {
                    continue;
                }

                int distYFromShooter = System.Math.Abs(player.coordinate.y - _shootPlayer.coordinate.y);
                if (distYFromShooter > 10)
                {
                    continue;
                }

                bool isBetweenGoal;
                if (_shootPlayer.teamSideCode == TeamSideCode.HOME)
                {
                    isBetweenGoal = player.coordinate.y >= _shootPlayer.coordinate.y && player.coordinate.y <= goalY;
                }
                else
                {
                    isBetweenGoal = player.coordinate.y <= _shootPlayer.coordinate.y && player.coordinate.y >= goalY;
                }
                if (!isBetweenGoal)
                {
                    continue;
                }

                int distX = System.Math.Abs(player.coordinate.x - _shootPlayer.coordinate.x);
                if (distX > 6)
                {
                    continue;
                }

                int distanceInt = player.coordinate.DistanceTo(_shootPlayer.coordinate);
                if (distanceInt < bestDistanceInt)
                {
                    bestDistanceInt = distanceInt;
                    bestBlockPlayer = player;
                }
            }
            return bestBlockPlayer;
        }

        Coordinate BuildDeflectCoordinate(Coordinate _baseCoordinate)
        {
            int offsetX = Random.Range(-8, 9);
            int offsetY = Random.Range(-8, 9);
            return new Coordinate(_baseCoordinate.x + offsetX, _baseCoordinate.y + offsetY);
        }

        Coordinate BuildParryCoordinate(Coordinate _gkCoordinate)
        {
            int offsetX = Random.Range(-10, 11);
            int offsetY = Random.Range(-6, 7);
            return new Coordinate(_gkCoordinate.x + offsetX, _gkCoordinate.y + offsetY);
        }

        float CalcShootHandBallProb(Player _blockPlayer)
        {
            int role = _blockPlayer.matchId % 100;
            if (role == 0)
            {
                return 0f;
            }

            float defenseRate = _blockPlayer.playerStatus.defenseInt / 100f;
            float prob = Consts.HAND_BALL_MAX_PROB - defenseRate * (Consts.HAND_BALL_MAX_PROB - Consts.HAND_BALL_MIN_PROB);
            return System.Math.Clamp(prob, Consts.HAND_BALL_MIN_PROB, Consts.HAND_BALL_MAX_PROB);
        }
    }
}

