// MatchSimulatorController のうち、ラインアウト時の再開（セットプレー）を分離
public partial class MatchSimulatorController
{
    class PartialSetPlay
    {
        readonly MatchSimulatorController controller;
        bool isPenaltyKickPendingFlag;
        int penaltyKickerIdInt;
        int penaltyGoalYInt;

        public PartialSetPlay(MatchSimulatorController _controller)
        {
            controller = _controller;
            isPenaltyKickPendingFlag = false;
            penaltyKickerIdInt = -1;
            penaltyGoalYInt = 0;
        }

        public bool IsOutOfPitch(Coordinate _coord)
        {
            if (_coord.x < 0 || _coord.x >= GridEvaluator.WIDTH)
            {
                return true;
            }
            if (_coord.y < 0 || _coord.y >= GridEvaluator.HEIGHT)
            {
                return true;
            }
            return false;
        }

        public void ProcessBallOutOfPlay(Coordinate _outCoord, TeamSideCode _lastTouchTeamSideCode, PeriodLog _log)
        {
            bool isSideOut = _outCoord.x < 0 || _outCoord.x >= GridEvaluator.WIDTH;
            if (isSideOut)
            {
                RestartThrowIn(_outCoord, _lastTouchTeamSideCode, _log);
                return;
            }

            RestartGoalLineOut(_outCoord, _lastTouchTeamSideCode, _log);
        }

        public void ProcessFoul(Player _fouledPlayer, Player _foulerPlayer, Coordinate _foulCoord, bool _isHandFlag, PeriodLog _log)
        {
            if (_fouledPlayer == null)
            {
                return;
            }

            // 違反をログに残す
            if (_isHandFlag)
            {
                _log.holderAction = ActionCode.HAND;
            }
            else
            {
                _log.holderAction = ActionCode.FOUL;
            }

            if (_foulerPlayer != null)
            {
                _log.involverId = _foulerPlayer.matchId;
            }
            else
            {
                _log.involverId = -1;
            }

            TeamSideCode attackingTeamSideCode = _fouledPlayer.teamSideCode;
            if (IsPenaltyAreaForAttackingTeam(_foulCoord, attackingTeamSideCode))
            {
                RestartPenaltyKick(attackingTeamSideCode, _log);
                return;
            }

            RestartFreeKick(_foulCoord, attackingTeamSideCode, _log);
        }

        public bool TryProcessPenaltyKickAction(Player _holdPlayer, PeriodLog _log)
        {
            if (!isPenaltyKickPendingFlag)
            {
                return false;
            }

            if (_holdPlayer == null || _holdPlayer.matchId != penaltyKickerIdInt)
            {
                isPenaltyKickPendingFlag = false;
                penaltyKickerIdInt = -1;
                return false;
            }

            isPenaltyKickPendingFlag = false;
            penaltyKickerIdInt = -1;
            controller.shoot.TryShoot(_holdPlayer, penaltyGoalYInt, _log);
            return true;
        }

        void RestartThrowIn(Coordinate _outCoord, TeamSideCode _lastTouchTeamSideCode, PeriodLog _log)
        {
            TeamSideCode restartTeamSideCode = GetOpponentTeamSideCode(_lastTouchTeamSideCode);
            int restartX;
            if (_outCoord.x < 0)
            {
                restartX = 0;
            }
            else
            {
                restartX = GridEvaluator.WIDTH - 1;
            }
            int restartY = System.Math.Clamp(_outCoord.y, 0, GridEvaluator.HEIGHT - 1);
            Coordinate restartCoord = new Coordinate(restartX, restartY);

            RestartByNearestPlayer(restartCoord, restartTeamSideCode, ActionCode.THROW_IN, _log);
        }

        void RestartCornerKick(Coordinate _outCoord, TeamSideCode _lastTouchTeamSideCode, PeriodLog _log)
        {
            TeamSideCode restartTeamSideCode = GetOpponentTeamSideCode(_lastTouchTeamSideCode);

            int restartY;
            if (_outCoord.y < 0)
            {
                restartY = 0;
            }
            else
            {
                restartY = GridEvaluator.HEIGHT - 1;
            }

            int restartX;
            if (_outCoord.x <= GridEvaluator.WIDTH / 2)
            {
                restartX = 0;
            }
            else
            {
                restartX = GridEvaluator.WIDTH - 1;
            }
            Coordinate restartCoord = new Coordinate(restartX, restartY);

            // 左右コーナー判定（座標定義は左コーナー基準）
            bool isLeftCorner = (restartX <= GridEvaluator.WIDTH / 2);

            // コーナーキック蹴る側のゴール方向を判定
            // HOMEの攻撃方向はy=100、AWAYの攻撃方向はy=0
            bool isAttackingTowardHighY;
            if (restartTeamSideCode == TeamSideCode.HOME)
            {
                isAttackingTowardHighY = true;
            }
            else
            {
                isAttackingTowardHighY = false;
            }

            Team attackingTeam;
            Team defendingTeam;
            if (restartTeamSideCode == TeamSideCode.HOME)
            {
                attackingTeam = controller.homeTeam;
                defendingTeam = controller.awayTeam;
            }
            else
            {
                attackingTeam = controller.awayTeam;
                defendingTeam = controller.homeTeam;
            }

            // キッカーを決定（最寄り選手）
            Player kicker = FindNearestPlayerOfTeam(restartCoord, restartTeamSideCode);

            // 攻撃チームの全選手をCK攻撃配置に移動（GKとキッカーを除く）
            for (int i = 0; i < attackingTeam.playerList.Count; i++)
            {
                Player player = attackingTeam.playerList[i];
                if (player == kicker)
                {
                    continue;
                }

                FormationSlot slot;
                if (i == 0)
                {
                    // GKは動かさない
                    continue;
                }
                else
                {
                    slot = attackingTeam.formationData.slotArray[i - 1];
                }

                Coordinate ckCoord = slot.cornerKickOffenseCoordinate;
                ckCoord = ApplyCornerKickTransform(ckCoord, isLeftCorner, isAttackingTowardHighY);
                player.coordinate = ckCoord;
                player.intentCoordinate = ckCoord;
            }

            // 守備チームの全選手をCK守備配置に移動
            for (int i = 0; i < defendingTeam.playerList.Count; i++)
            {
                Player player = defendingTeam.playerList[i];

                if (i == 0)
                {
                    // 守備側GKはニアポスト寄りに配置
                    Coordinate gkCoord = CalcCornerKickGkCoordinate(restartCoord, defendingTeam.teamSideCode);
                    player.coordinate = gkCoord;
                    player.intentCoordinate = gkCoord;
                    continue;
                }

                FormationSlot slot = defendingTeam.formationData.slotArray[i - 1];
                Coordinate ckCoord = slot.cornerKickDefenseCoordinate;
                // 守備側も攻撃側と同じゴール付近にいるため、同じ方向で変換
                ckCoord = ApplyCornerKickTransform(ckCoord, isLeftCorner, isAttackingTowardHighY);
                player.coordinate = ckCoord;
                player.intentCoordinate = ckCoord;
            }

            // キッカーにボールを渡す
            RestartBySpecificPlayer(restartCoord, kicker, ActionCode.CORNER_KICK, _log);
        }

        // CK座標変換: 左コーナー基準座標を、左右・チーム方向に応じて変換
        Coordinate ApplyCornerKickTransform(Coordinate _coord, bool _isLeftCorner, bool _isTowardHighY)
        {
            int x = _coord.x;
            int y = _coord.y;

            // 右コーナーの場合はX反転
            if (!_isLeftCorner)
            {
                x = GridEvaluator.WIDTH - 1 - x;
            }

            // Awayチーム（y=0方向に攻める）の場合はY反転
            if (!_isTowardHighY)
            {
                y = GridEvaluator.HEIGHT - 1 - y;
            }

            x = System.Math.Clamp(x, 0, GridEvaluator.WIDTH - 1);
            y = System.Math.Clamp(y, 0, GridEvaluator.HEIGHT - 1);

            return new Coordinate(x, y);
        }

        // 守備側GKのCK時座標を計算（ニアポスト寄り）
        Coordinate CalcCornerKickGkCoordinate(Coordinate _cornerCoord, TeamSideCode _gkTeamSideCode)
        {
            int gkY;
            if (_gkTeamSideCode == TeamSideCode.HOME)
            {
                gkY = 1;
            }
            else
            {
                gkY = GridEvaluator.HEIGHT - 2;
            }

            // ニアポスト側に寄せる（コーナー側に3マス寄せ）
            int gkX;
            if (_cornerCoord.x <= GridEvaluator.WIDTH / 2)
            {
                gkX = 32;
            }
            else
            {
                gkX = 38;
            }

            return new Coordinate(gkX, gkY);
        }

        void RestartGoalLineOut(Coordinate _outCoord, TeamSideCode _lastTouchTeamSideCode, PeriodLog _log)
        {
            TeamSideCode defendingTeamSideCode;
            if (_outCoord.y < 0)
            {
                defendingTeamSideCode = TeamSideCode.HOME;
            }
            else
            {
                defendingTeamSideCode = TeamSideCode.AWAY;
            }

            if (_lastTouchTeamSideCode == defendingTeamSideCode)
            {
                RestartCornerKick(_outCoord, _lastTouchTeamSideCode, _log);
                return;
            }

            RestartGoalKick(defendingTeamSideCode, _log);
        }

        void RestartGoalKick(TeamSideCode _defendingTeamSideCode, PeriodLog _log)
        {
            Team defendingTeam;
            int restartY;
            if (_defendingTeamSideCode == TeamSideCode.HOME)
            {
                defendingTeam = controller.homeTeam;
                restartY = 5;
            }
            else
            {
                defendingTeam = controller.awayTeam;
                restartY = GridEvaluator.HEIGHT - 6;
            }

            Player gk = defendingTeam.playerList[0];
            Coordinate restartCoord = new Coordinate(35, restartY);
            RestartBySpecificPlayer(restartCoord, gk, ActionCode.GOAL_KICK, _log);
        }

        void RestartFreeKick(Coordinate _foulCoord, TeamSideCode _attackingTeamSideCode, PeriodLog _log)
        {
            Coordinate restartCoord = ClampToPitch(_foulCoord);
            Player restartPlayer = FindNearestPlayerOfTeam(restartCoord, _attackingTeamSideCode);
            RestartBySpecificPlayer(restartCoord, restartPlayer, ActionCode.FREE_KICK, _log);
        }

        void RestartPenaltyKick(TeamSideCode _attackingTeamSideCode, PeriodLog _log)
        {
            Team attackingTeam;
            Team defendingTeam;
            Coordinate penaltySpotCoord;
            int goalY;
            if (_attackingTeamSideCode == TeamSideCode.HOME)
            {
                attackingTeam = controller.homeTeam;
                defendingTeam = controller.awayTeam;
                penaltySpotCoord = new Coordinate(35, 89);
                goalY = GridEvaluator.HEIGHT - 1;
            }
            else
            {
                attackingTeam = controller.awayTeam;
                defendingTeam = controller.homeTeam;
                penaltySpotCoord = new Coordinate(35, 11);
                goalY = 0;
            }

            Player kicker = attackingTeam.playerList[9];
            Player gk = defendingTeam.playerList[0];
            RestartBySpecificPlayer(penaltySpotCoord, kicker, ActionCode.PENALTY_KICK, _log);

            // 守備側GKはゴール中央に戻す
            Coordinate gkCoord = new Coordinate(35, goalY);
            gk.coordinate = gkCoord;
            gk.intentCoordinate = gkCoord;

            isPenaltyKickPendingFlag = true;
            penaltyKickerIdInt = kicker.matchId;
            penaltyGoalYInt = goalY;
        }

        void RestartByNearestPlayer(Coordinate _restartCoord, TeamSideCode _restartTeamSideCode, ActionCode _restartActionCode, PeriodLog _log)
        {
            Player restartPlayer = FindNearestPlayerOfTeam(_restartCoord, _restartTeamSideCode);
            if (restartPlayer == null)
            {
                return;
            }

            RestartBySpecificPlayer(_restartCoord, restartPlayer, _restartActionCode, _log);
        }

        void RestartBySpecificPlayer(Coordinate _restartCoord, Player _restartPlayer, ActionCode _restartActionCode, PeriodLog _log)
        {
            // 保持フラグを全員クリアして整合を取る
            for (int i = 0; i < controller.allPlayerList.Length; i++)
            {
                controller.allPlayerList[i].hasBall = false;
                controller.allPlayerList[i].ClearDuel();
            }

            if (_restartPlayer == null)
            {
                return;
            }

            _restartPlayer.coordinate = _restartCoord;
            _restartPlayer.intentCoordinate = _restartCoord;
            controller.ball.SetHolder(_restartPlayer);
            controller.matchState.UpdateAttackingTeam();
            controller.matchState.prevAttackingTeamSideCode = controller.matchState.attackingTeamSideCode;
            controller.matchState.lastHolderIdInt = _restartPlayer.matchId;

            _log.involverId = _restartPlayer.matchId;
            _log.involverAction = _restartActionCode;
        }

        Player FindNearestPlayerOfTeam(Coordinate _coord, TeamSideCode _teamSideCode)
        {
            Player nearestPlayer = null;
            int minDistanceInt = int.MaxValue;
            for (int i = 0; i < controller.allPlayerList.Length; i++)
            {
                Player player = controller.allPlayerList[i];
                if (player.teamSideCode != _teamSideCode)
                {
                    continue;
                }

                int distanceInt = player.coordinate.DistanceTo(_coord);
                if (distanceInt < minDistanceInt)
                {
                    minDistanceInt = distanceInt;
                    nearestPlayer = player;
                }
            }
            return nearestPlayer;
        }

        TeamSideCode GetOpponentTeamSideCode(TeamSideCode _teamSideCode)
        {
            if (_teamSideCode == TeamSideCode.HOME)
            {
                return TeamSideCode.AWAY;
            }
            return TeamSideCode.HOME;
        }

        bool IsPenaltyAreaForAttackingTeam(Coordinate _coord, TeamSideCode _attackingTeamSideCode)
        {
            if (_attackingTeamSideCode == TeamSideCode.HOME)
            {
                if (_coord.y >= 85 && _coord.y <= GridEvaluator.HEIGHT - 1 && _coord.x >= 15 && _coord.x <= 55)
                {
                    return true;
                }
                return false;
            }

            if (_coord.y >= 0 && _coord.y <= 15 && _coord.x >= 15 && _coord.x <= 55)
            {
                return true;
            }
            return false;
        }

        Coordinate ClampToPitch(Coordinate _coord)
        {
            int x = System.Math.Clamp(_coord.x, 0, GridEvaluator.WIDTH - 1);
            int y = System.Math.Clamp(_coord.y, 0, GridEvaluator.HEIGHT - 1);
            return new Coordinate(x, y);
        }
    }
}

