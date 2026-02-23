using UnityEngine;

// MatchSimulatorController のうち、ドリブル/競り合い関連ロジックを分離
public partial class MatchSimulatorController
{
    class PartialDribble
    {
        readonly MatchSimulatorController controller;

        public PartialDribble(MatchSimulatorController _controller)
        {
            controller = _controller;
        }

        public bool TryDribble(Player _dribblePlayer, int _goalY, PeriodLog _log)
        {
            // 0.1秒tick用：
            // - ドリブルは「1マス運ぶ（carry）」が基本
            // - 近距離接触は「競り合い（duel）」として数tickかけて決着

            bool hasWonDuelThisTickFlag = false;
            Player duelOpponent = null;

            // 競り合い継続中なら、時間経過で決着をつける（このtickでは移動しない）
            if (_dribblePlayer.IsInDuel())
            {
                _dribblePlayer.duelRemainingPeriodCountInt--;
                if (_dribblePlayer.duelRemainingPeriodCountInt > 0)
                {
                    _log.holderAction = ActionCode.DRIBBLE_SUCCESS;
                    _dribblePlayer.actionCode = ActionCode.DRIBBLE_SUCCESS;
                    return true;
                }

                Player opponent = controller.helpers.GetPlayerByMatchId(_dribblePlayer.duelOpponentId);
                _dribblePlayer.ClearDuel();

                if (opponent == null)
                {
                    // 相手が見つからない場合は継続成功扱い
                    _log.holderAction = ActionCode.DRIBBLE_SUCCESS;
                    _dribblePlayer.actionCode = ActionCode.DRIBBLE_SUCCESS;
                    return true;
                }

                // 決着
                int dribbleScore = _dribblePlayer.playerStatus.dribbleInt + Random.Range(0, 101);
                int defenseScore = opponent.playerStatus.defenseInt + Random.Range(0, 101);

                if (dribbleScore <= defenseScore)
                {
                    _log.holderAction = ActionCode.DRIBBLE_FAIL;
                    _dribblePlayer.actionCode = ActionCode.DRIBBLE_FAIL;

                    float foulProb = CalcTackleFoulProb(opponent);
                    bool isFoul = controller.gridEvaluator.RollSuccess(foulProb);
                    if (isFoul)
                    {
                        _dribblePlayer.hasBall = false;
                        opponent.ClearDuel();
                        controller.setPlay.ProcessFoul(_dribblePlayer, opponent, _dribblePlayer.coordinate, false, _log);
                        return true;
                    }

                    int scoreGapInt = defenseScore - dribbleScore;
                    float spillProb = System.Math.Clamp(Consts.DRIBBLE_DUEL_SPILL_BASE_PROB + scoreGapInt / 300f, Consts.DRIBBLE_DUEL_SPILL_BASE_PROB, Consts.DRIBBLE_DUEL_SPILL_MAX_PROB);
                    bool isSpill = controller.gridEvaluator.RollSuccess(spillProb);
                    if (isSpill)
                    {
                        _dribblePlayer.hasBall = false;
                        opponent.ClearDuel();
                        _log.involverId = opponent.matchId;
                        _log.involverAction = ActionCode.DRIBBLE_SPILL;

                        Coordinate spillCoord = BuildSpillCoordinate(_dribblePlayer.coordinate, opponent.coordinate);
                        if (controller.setPlay.IsOutOfPitch(spillCoord))
                        {
                            controller.setPlay.ProcessBallOutOfPlay(spillCoord, opponent.teamSideCode, _log);
                        }
                        else
                        {
                            controller.ball.lastTouchTeamSideCode = opponent.teamSideCode;
                            controller.ball.SetLoose(spillCoord);
                        }
                        controller.matchState.UpdateAttackingTeam();
                        return true;
                    }

                    _dribblePlayer.hasBall = false;
                    opponent.coordinate = _dribblePlayer.coordinate;
                    opponent.ClearDuel();
                    controller.ball.SetHolder(opponent);
                    controller.ball.coordinate = opponent.coordinate;
                    _log.involverId = opponent.matchId;
                    _log.involverAction = ActionCode.TACKLE;
                    controller.matchState.UpdateAttackingTeam();
                    return true;
                }

                // 勝ち：このtickで1マス前進を試みる（突破）
                hasWonDuelThisTickFlag = true;
                duelOpponent = opponent;
            }

            // 視野内に空きスペースがあるか探す
            Coordinate targetCoord;
            bool hasSpaceInView = GridEvaluator.TryFindBestDribbleTargetInView(_dribblePlayer, controller.allPlayerList, out targetCoord);

            // 近距離の敵がいる場合は競り合い開始
            Player nearestEnemy = controller.helpers.FindNearestEnemyWithin2Grids(_dribblePlayer.coordinate, _dribblePlayer.teamSideCode);
            bool isContested = false;
            if (nearestEnemy != null)
            {
                int dist = nearestEnemy.coordinate.DistanceTo(_dribblePlayer.coordinate);
                if (dist <= 1)
                {
                    isContested = true;
                }
            }

            // NOTE:
            // - 競り合いに勝った直後のtickは、同じ場所で再度競り合い開始してしまうと「その場で奪い合いループ」になりやすい
            // - 勝ったtickは前進を優先し、次tickから再度競り合い判定する
            if (isContested && !_dribblePlayer.IsInDuel() && !hasWonDuelThisTickFlag)
            {
                // 競り合い開始（0.3〜0.8秒）
                int duelDuration = 3 + Random.Range(0, 6);
                _dribblePlayer.StartDuel(nearestEnemy.matchId, duelDuration);

                _log.holderAction = ActionCode.DRIBBLE_SUCCESS;
                _dribblePlayer.actionCode = ActionCode.DRIBBLE_SUCCESS;
                _log.involverId = -1;
                _log.involverAction = ActionCode.NONE;
                return true;
            }

            // 視野内に空きスペースがなければドリブル不可
            if (!hasSpaceInView)
            {
                return false;
            }

            // 成功：視野内の空きスペースへ1マス前進
            _log.holderAction = ActionCode.DRIBBLE_SUCCESS;
            _dribblePlayer.actionCode = ActionCode.DRIBBLE_SUCCESS;
            _dribblePlayer.coordinate = targetCoord;
            controller.ball.coordinate = _dribblePlayer.coordinate;

            if (hasWonDuelThisTickFlag && duelOpponent != null)
            {
                _log.involverId = duelOpponent.matchId;
                _log.involverAction = ActionCode.DRIBBLE_BREAKTHROUGH;
            }
            else if (_log.involverAction == ActionCode.NONE)
            {
                _log.involverId = -1;
                _log.involverAction = ActionCode.NONE;
            }
            return true;
        }

        float CalcTackleFoulProb(Player _opponent)
        {
            float controlRate = _opponent.playerStatus.defenseInt / 100f;
            float prob = Consts.TACKLE_FOUL_MAX_PROB - controlRate * (Consts.TACKLE_FOUL_MAX_PROB - Consts.TACKLE_FOUL_BASE_PROB);
            return System.Math.Clamp(prob, Consts.TACKLE_FOUL_BASE_PROB, Consts.TACKLE_FOUL_MAX_PROB);
        }

        Coordinate BuildSpillCoordinate(Coordinate _dribblerCoord, Coordinate _opponentCoord)
        {
            int awayX = _dribblerCoord.x - _opponentCoord.x;
            int awayY = _dribblerCoord.y - _opponentCoord.y;
            int signX = System.Math.Sign(awayX);
            int signY = System.Math.Sign(awayY);
            if (signX == 0)
            {
                signX = Random.Range(-1, 2);
            }
            if (signY == 0)
            {
                signY = Random.Range(-1, 2);
            }

            int spillDistanceInt = 2 + Random.Range(0, 4);  // 2〜5
            int targetX = _dribblerCoord.x + signX * spillDistanceInt + Random.Range(-1, 2);
            int targetY = _dribblerCoord.y + signY * spillDistanceInt + Random.Range(-1, 2);
            return new Coordinate(targetX, targetY);
        }
    }
}

