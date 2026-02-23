using UnityEngine;

// MatchSimulatorController のうち、ボール飛行（FLYING）関連ロジックを分離
public partial class MatchSimulatorController
{
    class PartialBallFlight
    {
        readonly MatchSimulatorController controller;

        public PartialBallFlight(MatchSimulatorController _controller)
        {
            controller = _controller;
        }

        public void ApplyChaseIntent()
        {
            if (controller.ball.ballStateCode != BallStateCode.FLYING)
            {
                return;
            }

            // 受け手（予定）は落下点へ
            if (controller.ball.flightFinalHolderId >= 0)
            {
                Player receiver = controller.helpers.GetPlayerByMatchId(controller.ball.flightFinalHolderId);
                if (receiver != null)
                {
                    receiver.intentCoordinate = controller.ball.flightTargetCoordinate;
                }
            }

            // 最寄りの敵1人も落下点へ（簡易：カット/競争の雰囲気作り）
            Player nearestEnemy = FindNearestEnemy(controller.ball.flightTargetCoordinate, controller.ball.flightPasserTeamSideCode);
            if (nearestEnemy != null)
            {
                nearestEnemy.intentCoordinate = controller.ball.flightTargetCoordinate;
            }
        }

        public void ApplyLooseBallChaseIntent()
        {
            if (controller.ball.ballStateCode != BallStateCode.LOOSE)
            {
                return;
            }

            Coordinate ballCoord = controller.ball.coordinate;

            Player nearestHome = FindNearestPlayer(ballCoord, TeamSideCode.HOME);
            if (nearestHome != null)
            {
                nearestHome.intentCoordinate = ballCoord;
            }

            Player nearestAway = FindNearestPlayer(ballCoord, TeamSideCode.AWAY);
            if (nearestAway != null)
            {
                nearestAway.intentCoordinate = ballCoord;
            }
        }

        public void ProcessBallFlight(PeriodLog _log)
        {
            if (controller.ball.ballStateCode != BallStateCode.FLYING)
            {
                return;
            }

            bool isReachedEnd = false;

            // 飛行：1tickでcellsPerPeriod分だけ前進。途中で触れられたらその場で確定
            for (int step = 0; step < controller.ball.flightCellsPerPeriodInt; step++)
            {
                isReachedEnd = controller.ball.AdvanceFlightOneCell();

                if (controller.setPlay.IsOutOfPitch(controller.ball.coordinate))
                {
                    controller.setPlay.ProcessBallOutOfPlay(controller.ball.coordinate, controller.ball.lastTouchTeamSideCode, _log);
                    return;
                }

                Player touchPlayer = FindBestTouchPlayerNear(controller.ball.coordinate, Consts.FLIGHT_TOUCH_RADIUS);
                if (touchPlayer != null)
                {
                    ResolveBallTouch(touchPlayer, _log);
                    return;
                }

                if (isReachedEnd)
                {
                    break;
                }
            }

            controller.ball.flightRemainingPeriodCount--;

            if (!isReachedEnd)
            {
                return;
            }

            // 到着：半径内で競争して保持者を確定（いなければこぼれ球）
            Player winner = FindBestTouchPlayerNear(controller.ball.coordinate, Consts.FLIGHT_ARRIVAL_COMPETE_RADIUS);
            if (winner == null)
            {
                controller.ball.EndFlightAsLoose();
                return;
            }

            ResolveBallTouch(winner, _log);
        }

        public void ProcessLooseBall(PeriodLog _log)
        {
            if (controller.ball.ballStateCode != BallStateCode.LOOSE)
            {
                return;
            }

            if (controller.setPlay.IsOutOfPitch(controller.ball.coordinate))
            {
                controller.setPlay.ProcessBallOutOfPlay(controller.ball.coordinate, controller.ball.lastTouchTeamSideCode, _log);
                return;
            }

            Player pickupPlayer = FindBestTouchPlayerNear(controller.ball.coordinate, Consts.FLIGHT_TOUCH_RADIUS);
            if (pickupPlayer == null)
            {
                return;
            }

            // 拾った瞬間はボール座標に合わせる（簡易）
            pickupPlayer.coordinate = controller.ball.coordinate;
            pickupPlayer.ClearDuel();
            controller.ball.SetHolder(pickupPlayer);

            // ルーズ回収は既存ActionCodeで表現できないので、暫定でNONEのままにする
            if (_log.involverId < 0)
            {
                _log.involverId = pickupPlayer.matchId;
                _log.involverAction = ActionCode.NONE;
            }
        }

        Player FindNearestEnemy(Coordinate _coord, TeamSideCode _friendlyTeamSideCode)
        {
            Player nearest = null;
            int minDist = int.MaxValue;
            for (int i = 0; i < controller.allPlayerList.Length; i++)
            {
                Player player = controller.allPlayerList[i];
                if (player.teamSideCode == _friendlyTeamSideCode)
                {
                    continue;
                }

                int dist = player.coordinate.DistanceTo(_coord);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = player;
                }
            }
            return nearest;
        }

        Player FindNearestPlayer(Coordinate _coord, TeamSideCode _teamSideCode)
        {
            Player nearest = null;
            int minDist = int.MaxValue;
            for (int i = 0; i < controller.allPlayerList.Length; i++)
            {
                Player player = controller.allPlayerList[i];
                if (player.teamSideCode != _teamSideCode)
                {
                    continue;
                }

                int dist = player.coordinate.DistanceTo(_coord);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = player;
                }
            }
            return nearest;
        }

        Player FindBestTouchPlayerNear(Coordinate _coord, int _radius)
        {
            Player bestPlayer = null;
            int bestScore = int.MinValue;

            for (int i = 0; i < controller.allPlayerList.Length; i++)
            {
                Player player = controller.allPlayerList[i];
                int dist = player.coordinate.DistanceTo(_coord);
                if (dist > _radius)
                {
                    continue;
                }

                int score = 1000 - (dist * Consts.FLIGHT_SCORE_DIST_PENALTY);
                score += player.playerStatus.speedInt;
                score += player.playerStatus.defenseInt;
                score += Random.Range(0, Consts.FLIGHT_SCORE_RANDOM_RANGE + 1);

                if (player.matchId == controller.ball.flightFinalHolderId)
                {
                    score += Consts.FLIGHT_SCORE_FINAL_HOLDER_BONUS;
                }
                if (player.matchId == controller.ball.flightIntendedReceiverId)
                {
                    score += Consts.FLIGHT_SCORE_INTENDED_RECEIVER_BONUS;
                }

                if (score > bestScore)
                {
                    bestScore = score;
                    bestPlayer = player;
                }
            }

            return bestPlayer;
        }

        void ResolveBallTouch(Player _player, PeriodLog _log)
        {
            // トラップした瞬間はボール座標に合わせる（簡易）
            _player.coordinate = controller.ball.coordinate;
            _player.ClearDuel();

            ActionCode arrivalActionCode;
            if (_player.teamSideCode == controller.ball.flightPasserTeamSideCode)
            {
                arrivalActionCode = ActionCode.RECEIVE;
            }
            else
            {
                arrivalActionCode = ActionCode.INTERCEPT;
            }

            if (arrivalActionCode == ActionCode.RECEIVE)
            {
                Coordinate passerCoordinate = GetPasserCoordinateFromFlight();
                float trapMissProb = CalcTrapMissProb(_player, passerCoordinate);
                bool isTrapMiss = controller.gridEvaluator.RollSuccess(trapMissProb);
                if (isTrapMiss)
                {
                    Coordinate trapMissCoordinate = BuildTrapMissCoordinate(_player.coordinate);
                    _log.involverId = _player.matchId;
                    _log.involverAction = ActionCode.TRAP_MISS;

                    if (controller.setPlay.IsOutOfPitch(trapMissCoordinate))
                    {
                        controller.setPlay.ProcessBallOutOfPlay(trapMissCoordinate, _player.teamSideCode, _log);
                    }
                    else
                    {
                        controller.ball.lastTouchTeamSideCode = _player.teamSideCode;
                        controller.ball.SetLoose(trapMissCoordinate);
                    }
                    return;
                }
            }
            else if (arrivalActionCode == ActionCode.INTERCEPT)
            {
                if (!IsGoalkeeperLegalHandling(_player, controller.ball.coordinate))
                {
                    float handBallProb = CalcHandBallProb(_player);
                    bool isHandBall = controller.gridEvaluator.RollSuccess(handBallProb);
                    if (isHandBall)
                    {
                        TeamSideCode fouledTeamSideCode = controller.ball.flightPasserTeamSideCode;
                        Player fouledPlayer = FindNearestPlayer(controller.ball.coordinate, fouledTeamSideCode);
                        if (fouledPlayer == null)
                        {
                            fouledPlayer = _player;
                        }
                        controller.setPlay.ProcessFoul(fouledPlayer, _player, controller.ball.coordinate, true, _log);
                        return;
                    }
                }
            }

            controller.ball.SetHolder(_player);

            _log.involverId = _player.matchId;
            _log.involverAction = arrivalActionCode;

            if (arrivalActionCode == ActionCode.RECEIVE)
            {
                TryOneTouchPass(_player, _log);
            }
        }

        void TryOneTouchPass(Player _player, PeriodLog _log)
        {
            float oneTouchProb = CalcOneTouchPassProb(_player);
            bool isOneTouch = controller.gridEvaluator.RollSuccess(oneTouchProb);
            if (!isOneTouch)
            {
                return;
            }

            Player targetPlayer = controller.helpers.FindBestPassTargetPlayer(_player);
            if (targetPlayer == null || targetPlayer.matchId == _player.matchId)
            {
                return;
            }

            _player.hasBall = false;
            _player.actionCode = ActionCode.ONE_TOUCH_PASS;
            _log.involverId = _player.matchId;
            _log.involverAction = ActionCode.ONE_TOUCH_PASS;

            Coordinate targetCoordinate = targetPlayer.intentCoordinate;
            Coordinate[] pathCoordinateArray = controller.helpers.BuildLinePath(_player.coordinate, targetCoordinate);
            int cellsPerPeriod = 1 + (_player.playerStatus.passInt / 34);
            controller.ball.StartFlight(
                _player.coordinate,
                targetCoordinate,
                pathCoordinateArray,
                cellsPerPeriod,
                _player.teamSideCode,
                targetPlayer.matchId,
                targetPlayer.matchId,
                ActionCode.RECEIVE
            );
        }

        Coordinate GetPasserCoordinateFromFlight()
        {
            if (controller.ball.flightPathCoordinateArray != null && controller.ball.flightPathCoordinateArray.Length > 0)
            {
                return controller.ball.flightPathCoordinateArray[0];
            }
            return controller.ball.coordinate;
        }

        float CalcTrapMissProb(Player _player, Coordinate _passerCoordinate)
        {
            float controlInt = (_player.playerStatus.passInt + _player.playerStatus.dribbleInt) / 2f;
            float prob = Consts.TRAP_MISS_MAX_PROB - (controlInt / 100f * (Consts.TRAP_MISS_MAX_PROB - Consts.TRAP_MISS_MIN_PROB));

            bool isPassInView = GridEvaluator.IsPassApproachInReceiverView(_player, _passerCoordinate);
            if (!isPassInView)
            {
                prob += Consts.TRAP_MISS_OUT_OF_VIEW_BONUS;
            }

            return System.Math.Clamp(prob, Consts.TRAP_MISS_MIN_PROB, Consts.TRAP_MISS_MAX_PROB);
        }

        float CalcOneTouchPassProb(Player _player)
        {
            float passRate = _player.playerStatus.passInt / 100f;
            float prob = Consts.ONE_TOUCH_PASS_MIN_PROB + passRate * (Consts.ONE_TOUCH_PASS_MAX_PROB - Consts.ONE_TOUCH_PASS_MIN_PROB);
            return System.Math.Clamp(prob, Consts.ONE_TOUCH_PASS_MIN_PROB, Consts.ONE_TOUCH_PASS_MAX_PROB);
        }

        float CalcHandBallProb(Player _player)
        {
            float defenseRate = _player.playerStatus.defenseInt / 100f;
            float prob = Consts.HAND_BALL_MAX_PROB - defenseRate * (Consts.HAND_BALL_MAX_PROB - Consts.HAND_BALL_MIN_PROB);
            return System.Math.Clamp(prob, Consts.HAND_BALL_MIN_PROB, Consts.HAND_BALL_MAX_PROB);
        }

        bool IsGoalkeeperLegalHandling(Player _player, Coordinate _coord)
        {
            int role = _player.matchId % 100;
            if (role != 0)
            {
                return false;
            }

            if (_player.teamSideCode == TeamSideCode.HOME)
            {
                if (_coord.y >= 0 && _coord.y <= 15 && _coord.x >= 15 && _coord.x <= 55)
                {
                    return true;
                }
                return false;
            }

            if (_coord.y >= 85 && _coord.y <= GridEvaluator.HEIGHT - 1 && _coord.x >= 15 && _coord.x <= 55)
            {
                return true;
            }
            return false;
        }

        Coordinate BuildTrapMissCoordinate(Coordinate _baseCoordinate)
        {
            int offsetX = Random.Range(-4, 5);
            int offsetY = Random.Range(-3, 4);
            return new Coordinate(_baseCoordinate.x + offsetX, _baseCoordinate.y + offsetY);
        }


    }
}

