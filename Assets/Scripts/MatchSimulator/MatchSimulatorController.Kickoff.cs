// MatchSimulatorController のうち、キックオフ関連ロジックを分離
public partial class MatchSimulatorController
{
    class PartialKickoff
    {
        readonly MatchSimulatorController controller;

        bool isKickoffPendingFlag;
        int kickoffPasserIdInt;
        int kickoffReceiverIdInt;

        public PartialKickoff(MatchSimulatorController _controller)
        {
            controller = _controller;
            isKickoffPendingFlag = false;
            kickoffPasserIdInt = -1;
            kickoffReceiverIdInt = -1;
        }

        public void SetupKickoff(TeamSideCode _kickoffTeamSideCode)
        {
            Team kickoffTeam;
            if (_kickoffTeamSideCode == TeamSideCode.HOME)
            {
                kickoffTeam = controller.homeTeam;
            }
            else
            {
                kickoffTeam = controller.awayTeam;
            }

            Player kickoffPasser = kickoffTeam.playerList[10];   // 11番目がキッカー
            Player kickoffReceiver = kickoffTeam.playerList[9];  // 10番目が受け手

            // 11番目だけセンターサークルに配置、10番目は初期座標のまま
            Coordinate kickoffCoord = new Coordinate(35, 50);
            kickoffPasser.coordinate = kickoffCoord;
            kickoffPasser.intentCoordinate = kickoffCoord;

            controller.ball.SetHolder(kickoffPasser);
            controller.ball.coordinate = kickoffCoord;
            controller.matchState.UpdateAttackingTeam();
            controller.matchState.prevAttackingTeamSideCode = controller.matchState.attackingTeamSideCode;
            controller.matchState.lastHolderIdInt = kickoffPasser.matchId;

            isKickoffPendingFlag = true;
            kickoffPasserIdInt = kickoffPasser.matchId;
            kickoffReceiverIdInt = kickoffReceiver.matchId;
        }

        public bool TryProcessKickoffAction(Player _holdPlayer, PeriodLog _log)
        {
            if (!isKickoffPendingFlag)
            {
                return false;
            }

            if (_holdPlayer.matchId != kickoffPasserIdInt)
            {
                isKickoffPendingFlag = false;
                return false;
            }

            Player kickoffReceiver = controller.helpers.GetPlayerByMatchId(kickoffReceiverIdInt);
            if (kickoffReceiver == null)
            {
                isKickoffPendingFlag = false;
                return false;
            }

            controller.pass.TryKickoffPass(_holdPlayer, kickoffReceiver, _log);
            isKickoffPendingFlag = false;
            return true;
        }
    }
}

