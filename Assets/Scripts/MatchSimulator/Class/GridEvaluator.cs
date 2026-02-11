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

    // 攻撃側: 最適な座標を探す
    public Coordinate FindBestOffensiveCoordinate(Player _player, Ball _ball, Player[] _allPlayers)
    {
        // 意図に応じて処理を分岐
        if (_player.intentOffenseCode == IntentOffenseCode.HOLD_POSITION)
        {
            return _player.coordinate;
        }
        else if (_player.intentOffenseCode == IntentOffenseCode.SUPPORT)
        {
            return FindBestOffensiveCoordinateSupport(_player, _ball, _allPlayers);
        }
        else if (_player.intentOffenseCode == IntentOffenseCode.RUN_INTO_SPACE)
        {
            return FindBestOffensiveCoordinateRunIntoSpace(_player, _ball, _allPlayers);
        }
        else if (_player.intentOffenseCode == IntentOffenseCode.MAKE_WIDTH)
        {
            return FindBestOffensiveCoordinateMakeWidth(_player, _ball, _allPlayers);
        }
        else
        {
            // NONE またはその他の場合は全範囲探索
            return FindBestOffensiveCoordinateDefault(_player, _ball, _allPlayers);
        }
    }

    // SUPPORT: ボール保持者に近づく
    Coordinate FindBestOffensiveCoordinateSupport(Player _player, Ball _ball, Player[] _allPlayers)
    {
        Player ballHolder = GetBallHolder(_ball, _allPlayers);
        if (ballHolder == null)
        {
            return _player.coordinate;
        }

        Coordinate bestCoord = _player.coordinate;
        float bestScore = float.MinValue;
        int maxMovableInt = _player.playerVariable.maxMovableInt;

        // ボール保持者の方向に向かって探索範囲を絞る
        int dxToHolder = ballHolder.coordinate.x - _player.coordinate.x;
        int dyToHolder = ballHolder.coordinate.y - _player.coordinate.y;

        // 方向に応じて探索範囲を制限
        int dxMin, dxMax, dyMin, dyMax;
        if (dxToHolder > 0)
        {
            dxMin = 0;
            dxMax = System.Math.Min(maxMovableInt, dxToHolder + maxMovableInt);
        }
        else if (dxToHolder < 0)
        {
            dxMin = System.Math.Max(-maxMovableInt, dxToHolder - maxMovableInt);
            dxMax = 0;
        }
        else
        {
            dxMin = -maxMovableInt;
            dxMax = maxMovableInt;
        }

        if (dyToHolder > 0)
        {
            dyMin = 0;
            dyMax = System.Math.Min(maxMovableInt, dyToHolder + maxMovableInt);
        }
        else if (dyToHolder < 0)
        {
            dyMin = System.Math.Max(-maxMovableInt, dyToHolder - maxMovableInt);
            dyMax = 0;
        }
        else
        {
            dyMin = -maxMovableInt;
            dyMax = maxMovableInt;
        }

        for (int dx = dxMin; dx <= dxMax; dx++)
        {
            for (int dy = dyMin; dy <= dyMax; dy++)
            {
                int newX = _player.coordinate.x + dx;
                int newY = _player.coordinate.y + dy;

                if (newX < 0 || newX >= WIDTH || newY < 0 || newY >= HEIGHT) continue;

                Coordinate coord = new Coordinate(newX, newY);

                if (!_player.playerVariable.IsInMovableAreaOffensive(coord))
                    continue;

                float score = EvaluateOffensiveCoordinate(coord, _player, _ball, _allPlayers);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCoord = coord;
                }
            }
        }

        return bestCoord;
    }

    // RUN_INTO_SPACE: できるだけ前進する
    Coordinate FindBestOffensiveCoordinateRunIntoSpace(Player _player, Ball _ball, Player[] _allPlayers)
    {
        Coordinate bestCoord = _player.coordinate;
        float bestScore = float.MinValue;
        int maxMovableInt = _player.playerVariable.maxMovableInt;

        // ゴール方向を決定
        int goalY;
        if (_player.teamSideCode == TeamSideCode.HOME)
        {
            goalY = HEIGHT - 1;
        }
        else
        {
            goalY = 0;
        }

        // 前進方向（ゴール方向）に向かって探索範囲を絞る
        int dyToGoal = goalY - _player.coordinate.y;
        int dyMin, dyMax;

        if (dyToGoal > 0)
        {
            // ゴールが前方（HOMEの場合）
            dyMin = 0;
            dyMax = System.Math.Min(maxMovableInt, dyToGoal + maxMovableInt);
        }
        else if (dyToGoal < 0)
        {
            // ゴールが後方（AWAYの場合）
            dyMin = System.Math.Max(-maxMovableInt, dyToGoal - maxMovableInt);
            dyMax = 0;
        }
        else
        {
            dyMin = -maxMovableInt;
            dyMax = maxMovableInt;
        }

        // x方向は全範囲探索（横方向の動きも考慮）
        for (int dx = -maxMovableInt; dx <= maxMovableInt; dx++)
        {
            for (int dy = dyMin; dy <= dyMax; dy++)
            {
                int newX = _player.coordinate.x + dx;
                int newY = _player.coordinate.y + dy;

                if (newX < 0 || newX >= WIDTH || newY < 0 || newY >= HEIGHT) continue;

                Coordinate coord = new Coordinate(newX, newY);

                if (!_player.playerVariable.IsInMovableAreaOffensive(coord))
                    continue;

                float score = EvaluateOffensiveCoordinate(coord, _player, _ball, _allPlayers);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCoord = coord;
                }
            }
        }

        return bestCoord;
    }

    // MAKE_WIDTH: できるだけ外側（近い方のサイドライン）に近づく
    Coordinate FindBestOffensiveCoordinateMakeWidth(Player _player, Ball _ball, Player[] _allPlayers)
    {
        Coordinate bestCoord = _player.coordinate;
        float bestScore = float.MinValue;
        int maxMovableInt = _player.playerVariable.maxMovableInt;

        // 近い方のサイドラインを決定
        int centerX = WIDTH / 2;  // 35
        int distToLeftSide = _player.coordinate.x;
        int distToRightSide = WIDTH - 1 - _player.coordinate.x;
        int targetSideX;

        if (distToLeftSide <= distToRightSide)
        {
            // 左サイドラインに近づく
            targetSideX = 0;
        }
        else
        {
            // 右サイドラインに近づく
            targetSideX = WIDTH - 1;
        }

        // サイドライン方向に向かって探索範囲を絞る
        int dxToSide = targetSideX - _player.coordinate.x;
        int dxMin, dxMax;

        if (dxToSide > 0)
        {
            // 右サイドライン方向
            dxMin = 0;
            dxMax = System.Math.Min(maxMovableInt, dxToSide + maxMovableInt);
        }
        else if (dxToSide < 0)
        {
            // 左サイドライン方向
            dxMin = System.Math.Max(-maxMovableInt, dxToSide - maxMovableInt);
            dxMax = 0;
        }
        else
        {
            dxMin = -maxMovableInt;
            dxMax = maxMovableInt;
        }

        // y方向は全範囲探索（縦方向の動きも考慮）
        for (int dx = dxMin; dx <= dxMax; dx++)
        {
            for (int dy = -maxMovableInt; dy <= maxMovableInt; dy++)
            {
                int newX = _player.coordinate.x + dx;
                int newY = _player.coordinate.y + dy;

                if (newX < 0 || newX >= WIDTH || newY < 0 || newY >= HEIGHT) continue;

                Coordinate coord = new Coordinate(newX, newY);

                if (!_player.playerVariable.IsInMovableAreaOffensive(coord))
                    continue;

                float score = EvaluateOffensiveCoordinate(coord, _player, _ball, _allPlayers);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCoord = coord;
                }
            }
        }

        return bestCoord;
    }

    // デフォルト: 全範囲探索
    Coordinate FindBestOffensiveCoordinateDefault(Player _player, Ball _ball, Player[] _allPlayers)
    {
        Coordinate bestCoord = _player.coordinate;
        float bestScore = float.MinValue;
        int maxMovableInt = _player.playerVariable.maxMovableInt;

        for (int dx = -maxMovableInt; dx <= maxMovableInt; dx++)
        {
            for (int dy = -maxMovableInt; dy <= maxMovableInt; dy++)
            {
                int newX = _player.coordinate.x + dx;
                int newY = _player.coordinate.y + dy;

                if (newX < 0 || newX >= WIDTH || newY < 0 || newY >= HEIGHT) continue;

                Coordinate coord = new Coordinate(newX, newY);

                if (!_player.playerVariable.IsInMovableAreaOffensive(coord))
                    continue;

                float score = EvaluateOffensiveCoordinate(coord, _player, _ball, _allPlayers);

                if (score > bestScore)
                {
                    bestScore = score;
                    bestCoord = coord;
                }
            }
        }

        return bestCoord;
    }

    // 守備側: 最適な座標を探す（役割に応じて動く）
    public Coordinate FindBestDefensiveCoordinate(Player _player, Ball _ball, Player[] _allPlayers)
    {
        Coordinate currentCoord = _player.coordinate;
        Coordinate ballCoord = _ball.coordinate;
        int maxMovable = _player.playerVariable.maxMovableInt;

        // 選手番号から役割を判定（0=GK, 1-4=DF, 5-8=MF, 9-10=FW）
        int role = _player.matchId % 100;  // チーム内インデックス

        int goalY;  // 守るゴールのY座標
        if (_player.teamSideCode == TeamSideCode.HOME)
        {
            goalY = 0;
        }
        else
        {
            goalY = HEIGHT - 1;
        }

        int newX, newY;

        if (role == 0)
        {
            // GK: ゴール前に留まる（x方向だけボールを追う）
            newX = System.Math.Clamp(ballCoord.x, 20, 50);  // 中央付近に制限
            newY = goalY;
        }
        else if (role <= 4)
        {
            // DF: ゴール前のゾーンを守る（y方向はあまり離れない）
            int defenseLineY;
            if (_player.teamSideCode == TeamSideCode.HOME)
            {
                defenseLineY = 20;
            }
            else
            {
                defenseLineY = 80;
            }
            newX = System.Math.Clamp(currentCoord.x + System.Math.Sign(ballCoord.x - currentCoord.x), 0, WIDTH - 1);
            newY = System.Math.Clamp(defenseLineY + System.Math.Sign(ballCoord.y - defenseLineY), 0, HEIGHT - 1);
        }
        else if (role <= 8)
        {
            // MF: 中盤でボールを追う
            int dx = System.Math.Sign(ballCoord.x - currentCoord.x);
            int dy = System.Math.Sign(ballCoord.y - currentCoord.y);
            newX = System.Math.Clamp(currentCoord.x + dx, 0, WIDTH - 1);
            newY = System.Math.Clamp(currentCoord.y + dy, 0, HEIGHT - 1);
        }
        else
        {
            // FW: ボールを積極的に追う
            int dx = System.Math.Sign(ballCoord.x - currentCoord.x);
            int dy = System.Math.Sign(ballCoord.y - currentCoord.y);
            newX = System.Math.Clamp(currentCoord.x + dx * maxMovable, 0, WIDTH - 1);
            newY = System.Math.Clamp(currentCoord.y + dy * maxMovable, 0, HEIGHT - 1);
        }

        Coordinate targetCoord = new Coordinate(newX, newY);

        // 守備時の行動可能範囲外の場合は現在位置に留まる
        if (!_player.playerVariable.IsInMovableAreaDefensive(targetCoord))
        {
            return currentCoord;
        }

        // 味方が既にいる場合は現在位置に留まる
        if (IsOccupiedByTeammate(targetCoord, _player, _allPlayers))
        {
            return currentCoord;
        }

        return targetCoord;
    }

    // 指定座標に味方がいるかチェック
    bool IsOccupiedByTeammate(Coordinate _coord, Player _self, Player[] _allPlayers)
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

    // 攻撃側の座標評価
    float EvaluateOffensiveCoordinate(Coordinate _coord, Player _player, Ball _ball, Player[] _allPlayers)
    {
        float score = 0;

        // IntentOffenseCodeに応じた評価
        if (_player.intentOffenseCode == IntentOffenseCode.SUPPORT)
        {
            // ボール保持者に近づく
            Player ballHolder = GetBallHolder(_ball, _allPlayers);
            if (ballHolder != null)
            {
                float distToHolder = _coord.DistanceTo(ballHolder.coordinate);
                // 距離が近いほど良い（最大30グリッドで正規化）
                score += (30f - System.Math.Min(distToHolder, 30f)) / 30f * 5.0f;
            }
        }
        else if (_player.intentOffenseCode == IntentOffenseCode.RUN_INTO_SPACE)
        {
            // できるだけ前進する
            int goalY;
            if (_player.teamSideCode == TeamSideCode.HOME)
            {
                goalY = HEIGHT - 1;
            }
            else
            {
                goalY = 0;
            }
            float goalDist = System.Math.Abs(_coord.y - goalY);
            score += (HEIGHT - goalDist) / HEIGHT * 5.0f;
        }
        else if (_player.intentOffenseCode == IntentOffenseCode.MAKE_WIDTH)
        {
            // できるだけ外側（近い方のサイドライン）に近づく
            int centerX = WIDTH / 2;  // 35
            int distToCenter = System.Math.Abs(_coord.x - centerX);
            // 中央から離れているほど良い（最大35グリッドで正規化）
            score += distToCenter / 35f * 5.0f;
        }
        else
        {
            // NONE またはその他の場合は従来の評価
            // ゴールへの近さ（y=HEIGHT-1がゴールと仮定）
            int goalY;
            if (_player.teamSideCode == TeamSideCode.HOME)
            {
                goalY = HEIGHT - 1;
            }
            else
            {
                goalY = 0;
            }
            float goalDist = System.Math.Abs(_coord.y - goalY);
            score += (HEIGHT - goalDist) / HEIGHT * 1.5f;

            // 敵との距離（遠いほど良い）
            float nearestEnemyDist = GetNearestEnemyDistance(_coord, _player.teamSideCode, _allPlayers);
            score += System.Math.Min(nearestEnemyDist / 30f, 1f) * 2.0f;

            // パスコースが通るか（簡易判定）
            if (IsPassPathClear(_ball.coordinate, _coord, _player.teamSideCode, _allPlayers))
            {
                score += 1.2f;
            }
        }

        return score;
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
}
