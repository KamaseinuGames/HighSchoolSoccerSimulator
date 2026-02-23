using UnityEngine;

// MatchSimulatorController のうち、パス関連ロジックを分離
public partial class MatchSimulatorController
{
    class PartialPass
    {
        readonly MatchSimulatorController controller;

        public PartialPass(MatchSimulatorController _controller)
        {
            controller = _controller;
        }

        public void TryPass(Player _passPlayer, Player _receivePlayer, PeriodLog _log)
        {
            _passPlayer.ClearDuel();
            GridEvaluator.PassEvaluationResult result = controller.gridEvaluator.EvaluatePass(_passPlayer, _receivePlayer, controller.allPlayerList);

            _passPlayer.hasBall = false;
            Coordinate baseTargetCoordinate = _receivePlayer.intentCoordinate;
            int cellsPerPeriod = 1 + (_passPlayer.playerStatus.passInt / 34);  // 1〜3

            float overHitProb = System.Math.Clamp(Consts.PASS_OVERHIT_MAX_PROB - (_passPlayer.playerStatus.passInt / 100f * 0.15f), Consts.PASS_OVERHIT_MIN_PROB, Consts.PASS_OVERHIT_MAX_PROB);
            float strayProb = System.Math.Clamp(Consts.PASS_STRAY_MAX_PROB - (_passPlayer.playerStatus.passInt / 100f * 0.17f), Consts.PASS_STRAY_MIN_PROB, Consts.PASS_STRAY_MAX_PROB);

            bool isOverHit = controller.gridEvaluator.RollSuccess(overHitProb);
            if (isOverHit)
            {
                _log.holderAction = ActionCode.PASS_FAIL;
                _passPlayer.actionCode = ActionCode.PASS_FAIL;

                Coordinate targetCoordinate = BuildOverHitTargetCoordinate(_passPlayer.coordinate, baseTargetCoordinate);
                Coordinate[] pathCoordinateArray = controller.helpers.BuildLinePath(_passPlayer.coordinate, targetCoordinate);
                controller.ball.StartFlight(
                    _passPlayer.coordinate,
                    targetCoordinate,
                    pathCoordinateArray,
                    cellsPerPeriod,
                    _passPlayer.teamSideCode,
                    _receivePlayer.matchId,
                    -1,
                    ActionCode.NONE
                );
                return;
            }

            if (result.isSuccess)
            {
                bool isStray = controller.gridEvaluator.RollSuccess(strayProb);
                if (isStray)
                {
                    _log.holderAction = ActionCode.PASS_FAIL;
                    _passPlayer.actionCode = ActionCode.PASS_FAIL;

                    Coordinate strayTargetCoordinate = BuildStrayTargetCoordinate(baseTargetCoordinate, _passPlayer.playerStatus.passInt);
                    Coordinate[] strayPathCoordinateArray = controller.helpers.BuildLinePath(_passPlayer.coordinate, strayTargetCoordinate);
                    controller.ball.StartFlight(
                        _passPlayer.coordinate,
                        strayTargetCoordinate,
                        strayPathCoordinateArray,
                        cellsPerPeriod,
                        _passPlayer.teamSideCode,
                        _receivePlayer.matchId,
                        -1,
                        ActionCode.NONE
                    );
                    return;
                }

                _log.holderAction = ActionCode.PASS_SUCCESS;
                _passPlayer.actionCode = ActionCode.PASS_SUCCESS;

                // スルーパスの基本形：受け手の「意図座標」に出す（パス開始時にターゲットを固定）
                Coordinate targetCoordinate = baseTargetCoordinate;
                Coordinate[] pathCoordinateArray = controller.helpers.BuildLinePath(_passPlayer.coordinate, targetCoordinate);
                controller.ball.StartFlight(
                    _passPlayer.coordinate,
                    targetCoordinate,
                    pathCoordinateArray,
                    cellsPerPeriod,
                    _passPlayer.teamSideCode,
                    _receivePlayer.matchId,
                    _receivePlayer.matchId,
                    ActionCode.RECEIVE
                );
            }
            else
            {
                _log.holderAction = ActionCode.PASS_FAIL;
                _passPlayer.actionCode = ActionCode.PASS_FAIL;

                // パスカット
                if (result.interceptor != null)
                {
                    bool isDeflect = controller.gridEvaluator.RollSuccess(Consts.PASS_DEFLECT_WHEN_INTERCEPTOR_PROB);
                    if (isDeflect)
                    {
                        _log.involverId = result.interceptor.matchId;
                        _log.involverAction = ActionCode.PASS_DEFLECT;

                        Coordinate deflectTargetCoordinate = BuildDeflectTargetCoordinate(result.interceptor.coordinate);
                        Coordinate[] deflectPathCoordinateArray = controller.helpers.BuildLinePath(_passPlayer.coordinate, deflectTargetCoordinate);
                        TeamSideCode lastTouchTeamSideCode = result.interceptor.teamSideCode;
                        controller.ball.StartFlight(
                            _passPlayer.coordinate,
                            deflectTargetCoordinate,
                            deflectPathCoordinateArray,
                            cellsPerPeriod,
                            lastTouchTeamSideCode,
                            _receivePlayer.matchId,
                            -1,
                            ActionCode.NONE
                        );
                        return;
                    }

                    Coordinate targetCoordinate = result.interceptPoint;
                    Coordinate[] pathCoordinateArray = controller.helpers.BuildLinePath(_passPlayer.coordinate, targetCoordinate);
                    controller.ball.StartFlight(
                        _passPlayer.coordinate,
                        targetCoordinate,
                        pathCoordinateArray,
                        cellsPerPeriod,
                        _passPlayer.teamSideCode,
                        _receivePlayer.matchId,
                        result.interceptor.matchId,
                        ActionCode.INTERCEPT
                    );
                }
                else
                {
                    // カット者がいない失敗は「狙いがズレてルーズボール」扱いにする
                    // NOTE: ここでボール状態を更新しないと、ballはHOLDのままなのに保持者がいない（hasBall=false）状態になり硬直しやすい
                    Coordinate targetCoordinate = baseTargetCoordinate;
                    Coordinate[] pathCoordinateArray = controller.helpers.BuildLinePath(_passPlayer.coordinate, targetCoordinate);
                    controller.ball.StartFlight(
                        _passPlayer.coordinate,
                        targetCoordinate,
                        pathCoordinateArray,
                        cellsPerPeriod,
                        _passPlayer.teamSideCode,
                        _receivePlayer.matchId,
                        -1,
                        ActionCode.NONE
                    );
                }
            }
        }

        public void TryKickoffPass(Player _passPlayer, Player _receivePlayer, PeriodLog _log)
        {
            _passPlayer.ClearDuel();
            _passPlayer.hasBall = false;

            _log.holderAction = ActionCode.PASS_SUCCESS;
            _passPlayer.actionCode = ActionCode.PASS_SUCCESS;

            Coordinate targetCoordinate = _receivePlayer.coordinate;
            Coordinate[] pathCoordinateArray = controller.helpers.BuildLinePath(_passPlayer.coordinate, targetCoordinate);
            controller.ball.StartFlight(
                _passPlayer.coordinate,
                targetCoordinate,
                pathCoordinateArray,
                2,
                _passPlayer.teamSideCode,
                _receivePlayer.matchId,
                _receivePlayer.matchId,
                ActionCode.RECEIVE
            );
        }

        Coordinate BuildOverHitTargetCoordinate(Coordinate _from, Coordinate _baseTarget)
        {
            int dx = _baseTarget.x - _from.x;
            int dy = _baseTarget.y - _from.y;
            int absDx = System.Math.Abs(dx);
            int absDy = System.Math.Abs(dy);
            int dominant = System.Math.Max(absDx, absDy);
            if (dominant <= 0)
            {
                dominant = 1;
            }

            int overHitDistanceInt = 8 + Random.Range(0, 11);  // 8〜18
            float scale = (dominant + overHitDistanceInt) / (float)dominant;
            int targetX = Mathf.RoundToInt(_from.x + dx * scale);
            int targetY = Mathf.RoundToInt(_from.y + dy * scale);
            return new Coordinate(targetX, targetY);
        }

        Coordinate BuildStrayTargetCoordinate(Coordinate _baseTarget, int _passInt)
        {
            int maxErrorInt = System.Math.Clamp(8 - (_passInt / 20), 2, 8);
            int errorX = Random.Range(-maxErrorInt, maxErrorInt + 1);
            int errorY = Random.Range(-maxErrorInt, maxErrorInt + 1);
            return new Coordinate(_baseTarget.x + errorX, _baseTarget.y + errorY);
        }

        Coordinate BuildDeflectTargetCoordinate(Coordinate _interceptCoordinate)
        {
            int deflectX = _interceptCoordinate.x + Random.Range(-10, 11);
            int deflectY = _interceptCoordinate.y + Random.Range(-10, 11);
            return new Coordinate(deflectX, deflectY);
        }


    }
}

