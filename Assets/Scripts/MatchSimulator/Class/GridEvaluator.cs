using System.Collections.Generic;

// グリッド評価を行うクラス
public class GridEvaluator
{
    public const int WIDTH = 71;    // 横: 0〜70 (1m = 1グリッド、中央=35)
    public const int HEIGHT = 101;  // 縦: 0〜100 (1m = 1グリッド、中央=50)

    System.Random random;

    // パス評価結果
    public struct PassEvaluationResult
    {
        public bool isSuccess;
        public Player interceptor;  // カットされた場合のインターセプター（null if success）
        public Coordinate interceptPoint;  // インターセプト位置（中点、カットされた場合のみ有効）
    }

    public GridEvaluator()
    {
        random = new System.Random();
    }

    // GK専用の意図座標を算出
    public Coordinate FindGkIntent(Player _gkPlayer, Ball _ball)
    {
        int goalY;
        if (_gkPlayer.teamSideCode == TeamSideCode.HOME)
        {
            goalY = 0;
        }
        else
        {
            goalY = HEIGHT - 1;
        }
        int newX = System.Math.Clamp(_ball.coordinate.x, 20, 50);
        return new Coordinate(newX, goalY);
    }

    // ボール保持者を取得
    Player GetBallHolder(Ball _ball, Player[] _allPlayers)
    {
        if (_ball.holderId < 0)
        {
            return null;
        }

        foreach (Player player in _allPlayers)
        {
            if (player.matchId == _ball.holderId)
            {
                return player;
            }
        }
        return null;
    }

    // 最も近い敵との距離を取得
    float GetNearestEnemyDistance(Coordinate _coord, TeamSideCode _teamSideCode, Player[] _allPlayers)
    {
        float minDist = float.MaxValue;
        foreach (Player p in _allPlayers)
        {
            if (p.teamSideCode != _teamSideCode)
            {
                float dist = _coord.DistanceTo(p.coordinate);
                if (dist < minDist) minDist = dist;
            }
        }
        return minDist;
    }

    // パスコースが通るか判定（簡易版）
    bool IsPassPathClear(Coordinate _from, Coordinate _to, TeamSideCode _teamSideCode, Player[] _allPlayers)
    {
        // 敵がパスライン上にいるかチェック（簡易版: 中間点に敵がいないか）
        int midX = (_from.x + _to.x) / 2;
        int midY = (_from.y + _to.y) / 2;
        Coordinate midCoord = new Coordinate(midX, midY);

        foreach (Player p in _allPlayers)
        {
            if (p.teamSideCode != _teamSideCode && p.coordinate.DistanceTo(midCoord) <= 10)
            {
                return false;
            }
        }
        return true;
    }

    // パスを評価（パスカット判定を含む）
    public PassEvaluationResult EvaluatePass(Player _passer, Player _receiver, Player[] _allPlayers)
    {
        PassEvaluationResult result = new PassEvaluationResult();

        // パサーとレシーバーの中点を計算
        int midX = (_passer.coordinate.x + _receiver.coordinate.x) / 2;
        int midY = (_passer.coordinate.y + _receiver.coordinate.y) / 2;
        Coordinate midPoint = new Coordinate(midX, midY);
        result.interceptPoint = midPoint;

        // passIntに応じてカット判定範囲を決定
        int cutRange;
        if (_passer.playerStatus.passInt > 75)
        {
            cutRange = 1;
        }
        else if (_passer.playerStatus.passInt >= 50)
        {
            cutRange = 2;
        }
        else if (_passer.playerStatus.passInt >= 25)
        {
            cutRange = 3;
        }
        else
        {
            cutRange = 4;
        }

        // 中点からcutRangeグリッド以内に敵選手がいるかチェック
        Player nearestEnemy = null;
        int minDist = int.MaxValue;
        foreach (Player player in _allPlayers)
        {
            if (player.teamSideCode == _passer.teamSideCode) continue;
            
            int dist = player.coordinate.DistanceTo(midPoint);
            if (dist <= cutRange && dist < minDist)
            {
                minDist = dist;
                nearestEnemy = player;
            }
        }

        // カット判定
        if (nearestEnemy != null)
        {
            // パスカット
            result.isSuccess = false;
            result.interceptor = nearestEnemy;
        }
        else
        {
            // パス成功
            result.isSuccess = true;
            result.interceptor = null;
        }

        return result;
    }

    // シュート成功率を計算
    public float CalcShootSuccessProb(Player _shooter, int _goalY)
    {
        float baseProb = 0.4f;
        float shootBonus = _shooter.playerStatus.shootInt / 100f * 0.5f;

        int distToGoal = System.Math.Abs(_shooter.coordinate.y - _goalY);
        float distPenalty = distToGoal * 0.01f;

        return System.Math.Clamp(baseProb + shootBonus - distPenalty, 0.05f, 0.8f);
    }

    // ドリブル成功率を計算
    public float CalcDribbleSuccessProb(Player _dribbler, Player[] _allPlayers)
    {
        float baseProb = 0.6f;
        float dribbleBonus = _dribbler.playerStatus.dribbleInt / 100f * 0.4f;

        // 近くに敵がいるとペナルティ
        float nearestEnemy = GetNearestEnemyDistance(_dribbler.coordinate, _dribbler.teamSideCode, _allPlayers);
        float enemyPenalty;
        if (nearestEnemy <= 10)
        {
            enemyPenalty = 0.2f;
        }
        else
        {
            enemyPenalty = 0;
        }

        return System.Math.Clamp(baseProb + dribbleBonus - enemyPenalty, 0.2f, 0.9f);
    }

    // 確率判定
    public bool RollSuccess(float _prob)
    {
        return random.NextDouble() < _prob;
    }

    // 二選手間の距離を返す（汎用関数）
    public static int GetPlayerDistance(Player _player1, Player _player2)
    {
        return _player1.coordinate.DistanceTo(_player2.coordinate);
    }

    // === 視野角 ===

    // ボール保持者の向き（攻撃ゴール方向）を取得。正規化された方向ベクトル (dx, dy)
    public static void GetFacingDirectionToGoal(Player _player, out float _dirX, out float _dirY)
    {
        int goalY;
        if (_player.teamSideCode == TeamSideCode.HOME)
        {
            goalY = HEIGHT - 1;
        }
        else
        {
            goalY = 0;
        }

        int dx = 35 - _player.coordinate.x;
        int dy = goalY - _player.coordinate.y;

        float len = UnityEngine.Mathf.Sqrt(dx * dx + dy * dy);
        if (len <= 0.001f)
        {
            _dirX = 0f;
            _dirY = (_player.teamSideCode == TeamSideCode.HOME) ? 1f : -1f;
            return;
        }

        _dirX = dx / len;
        _dirY = dy / len;
    }

    // 非保持者の向き（ボール方向）を取得。正規化された方向ベクトル (dx, dy)
    public static void GetFacingDirectionToBall(Player _player, Coordinate _ballCoordinate, out float _dirX, out float _dirY)
    {
        int dx = _ballCoordinate.x - _player.coordinate.x;
        int dy = _ballCoordinate.y - _player.coordinate.y;

        float len = UnityEngine.Mathf.Sqrt(dx * dx + dy * dy);
        if (len <= 0.001f)
        {
            GetFacingDirectionToGoal(_player, out _dirX, out _dirY);
            return;
        }

        _dirX = dx / len;
        _dirY = dy / len;
    }

    // 指定座標が視野内（扇形）にあるか判定
    // _from: 視点の座標
    // _dirX, _dirY: 向きベクトル（正規化済み）
    // _target: 判定対象の座標
    // _halfAngleDeg: 視野の半角（度）。60なら左右60度ずつ
    public static bool IsInFieldOfView(Coordinate _from, float _dirX, float _dirY, Coordinate _target, float _halfAngleDeg)
    {
        int toX = _target.x - _from.x;
        int toY = _target.y - _from.y;

        if (toX == 0 && toY == 0)
        {
            return true;
        }

        float toLen = UnityEngine.Mathf.Sqrt(toX * toX + toY * toY);
        float targetDirX = toX / toLen;
        float targetDirY = toY / toLen;

        float dot = _dirX * targetDirX + _dirY * targetDirY;
        float angleRad = UnityEngine.Mathf.Acos(UnityEngine.Mathf.Clamp(dot, -1f, 1f));
        float angleDeg = angleRad * 180f / UnityEngine.Mathf.PI;

        return angleDeg <= _halfAngleDeg;
    }

    // パサーがレシーバーを視野内に見ているか
    public static bool CanPasserSeeReceiver(Player _passer, Player _receiver, Ball _ball)
    {
        float dirX;
        float dirY;
        if (_passer.hasBall)
        {
            GetFacingDirectionToGoal(_passer, out dirX, out dirY);
        }
        else
        {
            GetFacingDirectionToBall(_passer, _ball.coordinate, out dirX, out dirY);
        }

        return IsInFieldOfView(_passer.coordinate, dirX, dirY, _receiver.coordinate, Consts.VISION_HALF_ANGLE_DEG);
    }

    // レシーバーがパス方向（パサーからの接近）を視野内で捉えているか
    // 視野外からのパス = 後ろから来たボール → トラップミスしやすい
    public static bool IsPassApproachInReceiverView(Player _receiver, Coordinate _passerCoordinate)
    {
        int approachX = _passerCoordinate.x - _receiver.coordinate.x;
        int approachY = _passerCoordinate.y - _receiver.coordinate.y;

        if (approachX == 0 && approachY == 0)
        {
            return true;
        }

        float approachLen = UnityEngine.Mathf.Sqrt(approachX * approachX + approachY * approachY);
        float approachDirX = approachX / approachLen;
        float approachDirY = approachY / approachLen;

        float receiverDirX;
        float receiverDirY;
        GetFacingDirectionToGoal(_receiver, out receiverDirX, out receiverDirY);

        float dot = receiverDirX * approachDirX + receiverDirY * approachDirY;
        float angleRad = UnityEngine.Mathf.Acos(UnityEngine.Mathf.Clamp(dot, -1f, 1f));
        float angleDeg = angleRad * 180f / UnityEngine.Mathf.PI;

        return angleDeg <= Consts.VISION_HALF_ANGLE_DEG;
    }

    // ドリブル: 視野内で空いている最良の1マスを取得。見つからなければfalse
    public static bool TryFindBestDribbleTargetInView(Player _dribbler, Player[] _allPlayers, out Coordinate _targetCoord)
    {
        _targetCoord = _dribbler.coordinate;

        float dirX;
        float dirY;
        GetFacingDirectionToGoal(_dribbler, out dirX, out dirY);

        int goalY;
        if (_dribbler.teamSideCode == TeamSideCode.HOME)
        {
            goalY = HEIGHT - 1;
        }
        else
        {
            goalY = 0;
        }

        Coordinate bestCoord = _dribbler.coordinate;
        int bestGoalDist = int.MaxValue;
        bool foundAny = false;

        for (int dx = -1; dx <= 1; dx++)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                if (dx == 0 && dy == 0)
                {
                    continue;
                }

                int nextX = _dribbler.coordinate.x + dx;
                int nextY = _dribbler.coordinate.y + dy;
                if (nextX < 0 || nextX >= WIDTH || nextY < 0 || nextY >= HEIGHT)
                {
                    continue;
                }

                Coordinate candidateCoord = new Coordinate(nextX, nextY);
                if (!IsInFieldOfView(_dribbler.coordinate, dirX, dirY, candidateCoord, Consts.VISION_HALF_ANGLE_DEG))
                {
                    continue;
                }

                if (IsOccupiedByAnyone(candidateCoord, _dribbler, _allPlayers))
                {
                    continue;
                }

                int goalDist = System.Math.Abs(nextY - goalY);
                if (goalDist < bestGoalDist)
                {
                    bestGoalDist = goalDist;
                    bestCoord = candidateCoord;
                    foundAny = true;
                }
            }
        }

        if (foundAny)
        {
            _targetCoord = bestCoord;
            return true;
        }

        return false;
    }

    static bool IsOccupiedByAnyone(Coordinate _coord, Player _self, Player[] _allPlayers)
    {
        foreach (Player p in _allPlayers)
        {
            if (p == _self)
            {
                continue;
            }
            if (p.coordinate == _coord)
            {
                return true;
            }
        }
        return false;
    }

    // シュート: 視野中央（ゴール方向の狭い範囲）に敵がいるか
    public static bool HasEnemyInCentralVision(Player _shooter, Player[] _allPlayers)
    {
        float dirX;
        float dirY;
        GetFacingDirectionToGoal(_shooter, out dirX, out dirY);

        foreach (Player p in _allPlayers)
        {
            if (p.teamSideCode == _shooter.teamSideCode)
            {
                continue;
            }

            if (p.coordinate == _shooter.coordinate)
            {
                continue;
            }

            if (!IsInFieldOfView(_shooter.coordinate, dirX, dirY, p.coordinate, Consts.VISION_CENTRAL_HALF_ANGLE_DEG))
            {
                continue;
            }

            int goalY;
            if (_shooter.teamSideCode == TeamSideCode.HOME)
            {
                goalY = HEIGHT - 1;
            }
            else
            {
                goalY = 0;
            }

            bool isBetweenGoal;
            if (_shooter.teamSideCode == TeamSideCode.HOME)
            {
                isBetweenGoal = p.coordinate.y >= _shooter.coordinate.y && p.coordinate.y <= goalY;
            }
            else
            {
                isBetweenGoal = p.coordinate.y <= _shooter.coordinate.y && p.coordinate.y >= goalY;
            }

            if (isBetweenGoal)
            {
                return true;
            }
        }

        return false;
    }
}
