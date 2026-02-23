using System.Collections.Generic;

// MatchSimulatorController のうち、検索/占有などの汎用処理を分離
public partial class MatchSimulatorController
{
    class PartialHelpers
    {
        readonly MatchSimulatorController controller;

        public PartialHelpers(MatchSimulatorController _controller)
        {
            controller = _controller;
        }

        public Player GetPlayerByMatchId(int _matchId)
        {
            for (int i = 0; i < controller.allPlayerList.Length; i++)
            {
                if (controller.allPlayerList[i].matchId == _matchId)
                {
                    return controller.allPlayerList[i];
                }
            }
            return null;
        }

        public Player GetBallHolder()
        {
            foreach (Player player in controller.allPlayerList)
            {
                if (player.hasBall) return player;
            }
            return null;
        }

        public Player FindBestPassTargetPlayer(Player _passPlayer)
        {
            Team team;
            if (_passPlayer.teamSideCode == TeamSideCode.HOME)
            {
                team = controller.homeTeam;
            }
            else
            {
                team = controller.awayTeam;
            }

            List<Player> tempPlayerList = new List<Player>(team.playerList);
            tempPlayerList.Remove(_passPlayer);

            List<Player> visibleList = new List<Player>();
            foreach (Player player in tempPlayerList)
            {
                if (GridEvaluator.CanPasserSeeReceiver(_passPlayer, player, controller.ball))
                {
                    visibleList.Add(player);
                }
            }

            if (visibleList.Count <= 0)
            {
                return null;
            }

            List<Player> selectedList = new List<Player>();
            System.Random random = new System.Random();
            int selectCount = System.Math.Min(3, visibleList.Count);
            for (int i = 0; i < selectCount; i++)
            {
                int randomIndex = random.Next(visibleList.Count);
                selectedList.Add(visibleList[randomIndex]);
                visibleList.RemoveAt(randomIndex);
            }

            Player bestPlayer = null;
            int minDistance = int.MaxValue;

            foreach (Player player in selectedList)
            {
                int distance = GridEvaluator.GetPlayerDistance(_passPlayer, player);
                if (distance < minDistance)
                {
                    minDistance = distance;
                    bestPlayer = player;
                }
            }

            return bestPlayer;
        }

        // ドリブル用：指定座標から2グリッド以内の最も近い敵を探す
        public Player FindNearestEnemyWithin2Grids(Coordinate _coord, TeamSideCode _teamSideCode)
        {
            Player nearest = null;
            int minDist = int.MaxValue;

            foreach (Player player in controller.allPlayerList)
            {
                if (player.teamSideCode == _teamSideCode) continue;
                int dist = player.coordinate.DistanceTo(_coord);
                if (dist <= 2 && dist < minDist)
                {
                    minDist = dist;
                    nearest = player;
                }
            }

            return nearest;
        }

        // 指定座標に味方がいるかチェック
        public bool IsOccupiedByTeammate(Coordinate _coord, Player _self, Player[] _allPlayers)
        {
            foreach (Player p in _allPlayers)
            {
                if (p == _self) continue;
                if (p.teamSideCode == _self.teamSideCode && p.coordinate == _coord)
                {
                    return true;
                }
            }
            return false;
        }

        // 指定座標に誰か（味方・敵問わず）がいるかチェック
        public bool IsOccupiedByAnyone(Coordinate _coord, Player _self, Player[] _allPlayers)
        {
            foreach (Player p in _allPlayers)
            {
                if (p == _self) continue;
                if (p.coordinate == _coord)
                {
                    return true;
                }
            }
            return false;
        }

        // 2点間を結ぶ直線パスを生成
        public Coordinate[] BuildLinePath(Coordinate _from, Coordinate _to)
        {
            int dx = _to.x - _from.x;
            int dy = _to.y - _from.y;

            int stepCount = System.Math.Max(System.Math.Abs(dx), System.Math.Abs(dy));
            if (stepCount <= 0)
            {
                return new Coordinate[] { _from };
            }

            Coordinate[] pathCoordinateArray = new Coordinate[stepCount + 1];
            for (int i = 0; i <= stepCount; i++)
            {
                float t = (float)i / (float)stepCount;
                int x = UnityEngine.Mathf.RoundToInt(_from.x + dx * t);
                int y = UnityEngine.Mathf.RoundToInt(_from.y + dy * t);
                pathCoordinateArray[i] = new Coordinate(x, y);
            }
            return pathCoordinateArray;
        }
    }
}

