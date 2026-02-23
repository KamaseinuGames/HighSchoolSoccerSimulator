using UnityEngine;

// MatchSimulatorController のうち、クリア関連ロジックを分離
public partial class MatchSimulatorController
{
    class PartialClear
    {
        readonly MatchSimulatorController controller;

        public PartialClear(MatchSimulatorController _controller)
        {
            controller = _controller;
        }

        public void TryClear(Player _clearPlayer, PeriodLog _log)
        {
            _clearPlayer.ClearDuel();
            _log.holderAction = ActionCode.CLEAR;
            _clearPlayer.actionCode = ActionCode.CLEAR;
            _clearPlayer.hasBall = false;

            Coordinate targetCoordinate = BuildClearTargetCoordinate(_clearPlayer);
            Coordinate[] pathCoordinateArray = controller.helpers.BuildLinePath(_clearPlayer.coordinate, targetCoordinate);
            int cellsPerPeriod = 2 + (_clearPlayer.playerStatus.speedInt / 40);  // 2〜4
            controller.ball.StartFlight(
                _clearPlayer.coordinate,
                targetCoordinate,
                pathCoordinateArray,
                cellsPerPeriod,
                _clearPlayer.teamSideCode,
                -1,
                -1,
                ActionCode.NONE
            );
        }

        Coordinate BuildClearTargetCoordinate(Player _clearPlayer)
        {
            int targetX = _clearPlayer.coordinate.x + Random.Range(-20, 21);
            int targetY;
            if (_clearPlayer.teamSideCode == TeamSideCode.HOME)
            {
                targetY = GridEvaluator.HEIGHT + Random.Range(4, 21);
            }
            else
            {
                targetY = -1 - Random.Range(4, 21);
            }
            return new Coordinate(targetX, targetY);
        }


    }
}

