using System.Collections.Generic;
using UnityEngine;

// 試合シミュレーションのメインコントローラー（MonoBehaviour）
// 空のGameObjectにアタッチしてStart()で実行
public class MatchSimulatorController : MonoBehaviour
{
    [Header("試合設定")]
    public int matchMinutes = 90;

    [Header("可視化")]
    public MatchSimulatorDigest matchSimulatorDigest;  // 可視化コンポーネント（任意）

    // 内部データ
    Team homeTeam;
    Team awayTeam;
    Ball ball;
    GridEvaluator gridEvaluator;
    MatchState matchState;
    List<PeriodLog> periodLogList;
    Player[] allPlayerList;

    void Start()
    {
        RunSimulation();
    }

    public void RunSimulation()
    {
        Initialize();

        // 試合時間分のシミュレーション（Constsでtickを固定）
        // 例: 0.1秒 = 1ピリオドの場合、90分 = 90 * 600 = 54000ピリオド
        int totalPeriods = matchMinutes * Consts.PERIODS_PER_MINUTE;
        for (int period = 0; period < totalPeriods; period++)
        {
            PeriodLog log = ProcessPeriod(period);
            periodLogList.Add(log);
        }

        // 可視化開始
        if (matchSimulatorDigest != null)
        {
            matchSimulatorDigest.Initialize(periodLogList, allPlayerList, homeTeam, awayTeam);
        }
    }

    void Initialize()
    {
        gridEvaluator = new GridEvaluator();
        periodLogList = new List<PeriodLog>();

        // チーム作成
        homeTeam = new Team(TeamSideCode.HOME, "HomeInter");
        awayTeam = new Team(TeamSideCode.AWAY, "AwayMilan");
        homeTeam.CreatePlayers();
        awayTeam.CreatePlayers();

        // 全選手配列を作成
        List<Player> tempPlayerList = new List<Player>();
        tempPlayerList.AddRange(homeTeam.playerList);
        tempPlayerList.AddRange(awayTeam.playerList);
        allPlayerList = tempPlayerList.ToArray();

        // 各Intentを初期設定
        DecideIntent();

        // ボール作成 & キックオフ（ホームチームから）
        ball = new Ball();
        Player kickoffPlayer = homeTeam.playerList[9];  // FW1がキックオフ
        ball.SetHolder(kickoffPlayer);

        // MatchStateを初期化
        matchState = new MatchState(ball, homeTeam, awayTeam, allPlayerList);
    }

    // 1ピリオドの処理（5秒1ピリオド）
    PeriodLog ProcessPeriod(int _period)
    {
        PeriodLog log = new PeriodLog(_period, allPlayerList.Length);

        // 0. MatchStateを更新（攻撃側チームを更新）
        matchState.UpdateAttackingTeam();

        // 1. ボール保持者と関与者が行動決定（先に処理）
        Player holder = GetBallHolder();
        if (holder != null)
        {
            ProcessBallHolderAction(holder, log);
        }

        // 2. ボール非保持者の行動処理（actionCodeがNONEのもののみ）
        ProcessNonBallHolderAction(log);

        // 3. 全選手が移動（順次実行して、移動済みの選手の座標を考慮）
        foreach (Player player in allPlayerList)
        {
            if (!player.hasBall)  // ボール保持者は行動で移動済み
            {
                player.MoveToIntent(allPlayerList);
            }
        }

        // 4. ピリオド終了時の状態を記録（ゴールが発生した場合は既に記録済みなのでスキップ）
        if (!log.hasGoalFlag)
        {
            for (int i = 0; i < allPlayerList.Length; i++)
            {
                log.playerCoordinates[i] = allPlayerList[i].coordinate;
                log.playerHasBall[i] = allPlayerList[i].hasBall;
            }
            log.ballCoordinate = ball.coordinate;
        }
        return log;
    }

    // ボール保持者の行動処理
    void ProcessBallHolderAction(Player _holdPlayer, PeriodLog _log)
    {
        _log.holderId = _holdPlayer.matchId;
        _holdPlayer.actionCode = ActionCode.NONE;  // 行動をリセット

        int goalY;
        if (_holdPlayer.teamSideCode == TeamSideCode.HOME)
        {
            goalY = GridEvaluator.HEIGHT - 1;
        }
        else
        {
            goalY = 0;
        }
        int distToGoal = System.Math.Abs(_holdPlayer.coordinate.y - goalY);

        // IntentHolderCodeに基づいて積極的に行動を決定
        if (_holdPlayer.intentHolderCode == IntentHolderCode.ASSERTIVE_SHOOT || distToGoal <= 16)
        {
            // 距離が30以下ならシュートを試みる
            if (distToGoal <= 30)
            {
                TryShoot(_holdPlayer, goalY, _log);
                return;
            }
        }
        else if (_holdPlayer.intentHolderCode == IntentHolderCode.ASSERTIVE_PASS)
        {
            // 積極的にパス
            Player bestReceiver = FindBestPassTargetPlayer(_holdPlayer);
            if (bestReceiver != null)
            {
                TryPass(_holdPlayer, bestReceiver, _log);
                return;
            }
        }
        else if (_holdPlayer.intentHolderCode == IntentHolderCode.ASSERTIVE_DRIBBLE)
        {
            // 積極的にドリブル
            bool canDribble = TryDribble(_holdPlayer, goalY, _log);
            if (canDribble)
            {
                return;
            }
            // ドリブル不可能な場合はパスを試す
            Player bestReceiver = FindBestPassTargetPlayer(_holdPlayer);
            if (bestReceiver != null)
            {
                TryPass(_holdPlayer, bestReceiver, _log);
                return;
            }
        }

        // デフォルト処理...先にドリブルを試す
        bool canDribbleNormal = TryDribble(_holdPlayer, goalY, _log);
        if (!canDribbleNormal)
        {
            // ドリブル不可能な場合はパスを試す
            Player bestReceiverNormal = FindBestPassTargetPlayer(_holdPlayer);
            if (bestReceiverNormal != null)
            {
                TryPass(_holdPlayer, bestReceiverNormal, _log);
            }
        }
    }

    // 各Intentを設定（全選手に対して実行）
    void DecideIntent()
    {
        System.Random random = new System.Random();
        
        foreach (Player player in allPlayerList)
        {
            int role = player.matchId % 100;  // チーム内インデックス（0=GK, 1-4=DF, 5-8=MF, 9-10=FW）
            
            // ボール保持時の意図を設定
            if (role <= 4)
            {
                // GKとDFは必ずASSERTIVE_PASS
                player.intentHolderCode = IntentHolderCode.ASSERTIVE_PASS;
            }
            else
            {
                // MFとFWはランダムに設定
                IntentHolderCode[] holderArray = (IntentHolderCode[])System.Enum.GetValues(typeof(IntentHolderCode));
                player.intentHolderCode = holderArray[random.Next(holderArray.Length)];
            }
            
            // 非保持時（攻撃）の意図を設定
            IntentOffenseCode[] offenseArray = (IntentOffenseCode[])System.Enum.GetValues(typeof(IntentOffenseCode));
            player.intentOffenseCode = offenseArray[random.Next(offenseArray.Length)];
            
            // 非保持時（守備）の意図を設定
            IntentDefenseCode[] defenseArray = (IntentDefenseCode[])System.Enum.GetValues(typeof(IntentDefenseCode));
            player.intentDefenseCode = defenseArray[random.Next(defenseArray.Length)];
        }
    }

    // ボール非保持者の行動処理
    void ProcessNonBallHolderAction(PeriodLog _log)
    {
        foreach (Player player in allPlayerList)
        {
            // actionCodeがNONEでないものは処理しない
            if (player.actionCode != ActionCode.NONE)
            {
                continue;
            }

            // 攻撃側/守備側の判定（MatchStateから取得）
            bool isAttacking = matchState.IsAttackingTeam(player);

            if (isAttacking)
            {
                // 攻撃側: 最適な座標を探す
                player.intentCoordinate = gridEvaluator.FindBestOffensiveCoordinate(player, matchState.ball, matchState.allPlayerList);
            }
            else
            {
                // 守備側: マーク対象に近づく or スペースを埋める
                player.intentCoordinate = gridEvaluator.FindBestDefensiveCoordinate(player, matchState.ball, matchState.allPlayerList);
            }
        }
    }

    void TryShoot(Player _shootPlayer, int _goalY, PeriodLog _log)
    {
        float prob = gridEvaluator.CalcShootSuccessProb(_shootPlayer, _goalY);
        bool isSuccess = gridEvaluator.RollSuccess(prob);

        if (isSuccess)
        {
            _log.holderAction = ActionCode.SHOOT_SUCCESS;
            _shootPlayer.actionCode = ActionCode.SHOOT_SUCCESS;

            // ゴール！
            Team team;
            if (_shootPlayer.teamSideCode == TeamSideCode.HOME)
            {
                team = homeTeam;
            }
            else
            {
                team = awayTeam;
            }

            // スコア加算
            team.scoreInt++;
            
            // ゴール時は関与者なし
            _log.involverId = -1;
            _log.involverAction = ActionCode.NONE;

            // ゴール位置を記録
            _log.hasGoalFlag = true;
            _log.goalCoordinate = new Coordinate(35, _goalY);

            // ゴール時の選手位置を記録（初期配置に戻す前に保存）
            for (int i = 0; i < allPlayerList.Length; i++)
            {
                _log.playerCoordinates[i] = allPlayerList[i].coordinate;
                _log.playerHasBall[i] = allPlayerList[i].hasBall;
            }
            // ゴール時のボール位置も記録（シュート位置）
            _log.ballCoordinate = ball.coordinate;

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
            ResetAfterGoal(kickoffTeamSideCode, _log);
        }
        else
        {
            _log.holderAction = ActionCode.SHOOT_FAIL;
            _shootPlayer.actionCode = ActionCode.SHOOT_FAIL;

            // GKがシュートをキャッチ
            Team opponent;
            if (_shootPlayer.teamSideCode == TeamSideCode.HOME)
            {
                opponent = awayTeam;
            }
            else
            {
                opponent = homeTeam;
            }
            Player gk = opponent.playerList[0];  // GK

            // 関与者を記録
            _log.involverId = gk.matchId;
            _log.involverAction = ActionCode.SHOOT_CATCH;

            // ボール保持者を更新
            ball.SetHolder(gk);
            _shootPlayer.hasBall = false;

            // 攻撃側チームを更新
            matchState.UpdateAttackingTeam();
        }
    }

    void TryPass(Player _passPlayer, Player _receivePlayer, PeriodLog _log)
    {
        GridEvaluator.PassEvaluationResult result = gridEvaluator.EvaluatePass(_passPlayer, _receivePlayer, allPlayerList);

        _passPlayer.hasBall = false;

        if (result.isSuccess)
        {
            _log.holderAction = ActionCode.PASS_SUCCESS;
            _passPlayer.actionCode = ActionCode.PASS_SUCCESS;

            // パスを受ける
            ball.SetHolder(_receivePlayer);
            _log.involverId = _receivePlayer.matchId;
            _log.involverAction = ActionCode.RECEIVE;
        }
        else
        {
            _log.holderAction = ActionCode.PASS_FAIL;
            _passPlayer.actionCode = ActionCode.PASS_FAIL;
            
            // パスカット
            if (result.interceptor != null)
            {
                // インターセプト者を中点に移動
                result.interceptor.coordinate = result.interceptPoint;
                ball.SetHolder(result.interceptor);
                ball.coordinate = result.interceptPoint;
                _log.involverId = result.interceptor.matchId;
                _log.involverAction = ActionCode.INTERCEPT;  // インターセプト
                matchState.UpdateAttackingTeam();  // 攻撃側チームが変わった
            }
        }
    }

    bool TryDribble(Player _dribblePlayer, int _goalY, PeriodLog _log)
    {
        // (1) 目の前の相手を抜き去るドリブル
        Player nearestEnemy = FindNearestEnemyWithin2Grids(_dribblePlayer.coordinate, _dribblePlayer.teamSideCode);
        
        if (nearestEnemy != null)
        {
            // ドリブル能力と守備能力を比較
            bool isSuccess = _dribblePlayer.playerStatus.dribbleInt > nearestEnemy.playerStatus.defenseInt;
            
            if (isSuccess)
            {
                // 成功：相手の前方1グリッドの位置に移動
                _log.holderAction = ActionCode.DRIBBLE_SUCCESS;
                _dribblePlayer.actionCode = ActionCode.DRIBBLE_SUCCESS;
                
                // 前方方向を決定（HOME: Y+方向、AWAY: Y-方向）
                int dy;
                if (_dribblePlayer.teamSideCode == TeamSideCode.HOME)
                {
                    dy = 1;  // Y+方向が前方
                }
                else
                {
                    dy = -1;  // Y-方向が前方
                }
                
                // 相手の前方1グリッドの位置を計算
                int newY = System.Math.Clamp(nearestEnemy.coordinate.y + dy, 0, GridEvaluator.HEIGHT - 1);
                Coordinate newCoord = new Coordinate(nearestEnemy.coordinate.x, newY);
                
                // 移動先に味方がいる場合は移動をキャンセル
                if (!IsOccupiedByTeammate(newCoord, _dribblePlayer, allPlayerList))
                {
                    _dribblePlayer.coordinate = newCoord;
                    ball.coordinate = _dribblePlayer.coordinate;
                    _log.involverId = nearestEnemy.matchId;
                    _log.involverAction = ActionCode.DRIBBLE_BREAKTHROUGH;  // ドリブル突破
                }
                else
                {
                    // 味方がいるため移動できず、現在位置に留まる
                    _log.involverId = -1;
                    _log.involverAction = ActionCode.NONE;
                }
                return true;
            }
            else
            {
                // 失敗：相手の後方1グリッドの位置に移動し、攻守交代
                _log.holderAction = ActionCode.DRIBBLE_FAIL;
                _dribblePlayer.actionCode = ActionCode.DRIBBLE_FAIL;
                
                // 後方方向を決定（HOME: Y-方向、AWAY: Y+方向）
                int dy;
                if (_dribblePlayer.teamSideCode == TeamSideCode.HOME)
                {
                    dy = -1;  // Y-方向が後方
                }
                else
                {
                    dy = 1;  // Y+方向が後方
                }
                
                // 相手の後方1グリッドの位置を計算
                int newY = System.Math.Clamp(nearestEnemy.coordinate.y + dy, 0, GridEvaluator.HEIGHT - 1);
                Coordinate newCoord = new Coordinate(nearestEnemy.coordinate.x, newY);
                
                _dribblePlayer.coordinate = newCoord;
                _dribblePlayer.hasBall = false;
                ball.SetHolder(nearestEnemy);
                ball.coordinate = nearestEnemy.coordinate;
                _log.involverId = nearestEnemy.matchId;
                _log.involverAction = ActionCode.TACKLE;  // タックル
                matchState.UpdateAttackingTeam();  // 攻撃側チームが変わった
                return true;
            }
        }
        else
        {
            // (2) 前のスペースに走り込むドリブル
            // maxMovableIntだけ前進を試みる
            int maxMove = _dribblePlayer.playerVariable.maxMovableInt;
            
            // 前方方向を決定（HOME: Y+方向、AWAY: Y-方向）
            int dy;
            if (_dribblePlayer.teamSideCode == TeamSideCode.HOME)
            {
                dy = 1;  // Y+方向が前方
            }
            else
            {
                dy = -1;  // Y-方向が前方
            }
            
            // maxMovableIntから1まで順番に試行
            Coordinate newCoord = _dribblePlayer.coordinate;  // デフォルトは現在位置
            bool foundEmptySpace = false;
            
            for (int move = maxMove; move >= 1; move--)
            {
                int newY = System.Math.Clamp(_dribblePlayer.coordinate.y + (dy * move), 0, GridEvaluator.HEIGHT - 1);
                Coordinate testCoord = new Coordinate(_dribblePlayer.coordinate.x, newY);
                
                // その座標に誰もいないかチェック
                if (!IsOccupiedByAnyone(testCoord, _dribblePlayer, allPlayerList))
                {
                    newCoord = testCoord;
                    foundEmptySpace = true;
                    break;
                }
            }
            
            if (foundEmptySpace)
            {
                // 空いている位置が見つかった
                _log.holderAction = ActionCode.DRIBBLE_SUCCESS;
                _dribblePlayer.actionCode = ActionCode.DRIBBLE_SUCCESS;
                _dribblePlayer.coordinate = newCoord;
                ball.coordinate = _dribblePlayer.coordinate;
                _log.involverId = -1;
                _log.involverAction = ActionCode.NONE;
                return true;
            }
            else
            {
                // ドリブル不可能：空いている位置が見つからなかった
                return false;
            }
        }
    }

    Player GetBallHolder()
    {
        foreach (Player player in allPlayerList)
        {
            if (player.hasBall) return player;
        }
        return null;
    }

    Player FindBestPassTargetPlayer(Player _passPlayer)
    {
        Team team;
        if (_passPlayer.teamSideCode == TeamSideCode.HOME)
        {
            team = homeTeam;
        }
        else
        {
            team = awayTeam;
        }

        // team.playerListから自分を除外したリストを作成
        List<Player> tempPlayerList = new List<Player>(team.playerList);
        tempPlayerList.Remove(_passPlayer);
        
        // ランダムに3人選ぶ
        List<Player> selectedList = new List<Player>();
        System.Random random = new System.Random();
        for (int i = 0; i < 3; i++)
        {
            int randomIndex = random.Next(tempPlayerList.Count);
            selectedList.Add(tempPlayerList[randomIndex]);
            tempPlayerList.RemoveAt(randomIndex);
        }

        // 選択した3人の中で最も距離が近い選手を探す
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
    Player FindNearestEnemyWithin2Grids(Coordinate _coord, TeamSideCode _teamSideCode)
    {
        Player nearest = null;
        int minDist = int.MaxValue;

        foreach (Player player in allPlayerList)
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

    // 指定座標に誰か（味方・敵問わず）がいるかチェック
    bool IsOccupiedByAnyone(Coordinate _coord, Player _self, Player[] _allPlayers)
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

    void ResetAfterGoal(TeamSideCode _kickoffTeamSideCode, PeriodLog _log)
    {
        // 全選手を初期配置に戻す
        for (int i = 0; i < allPlayerList.Length; i++)
        {
            Player player = allPlayerList[i];
            player.hasBall = false;
            
            // 初期配置を取得（Teamのフォーメーション座標から）
            Coordinate initialCoord;
            if (player.teamSideCode == TeamSideCode.HOME)
            {
                // Homeチーム
                int playerIndex = i;  // Homeは0-10
                initialCoord = homeTeam.formationCoordinates[playerIndex];
            }
            else
            {
                // Awayチーム
                int playerIndex = i - 11;  // Awayは11-21なので、-11して0-10に
                initialCoord = awayTeam.formationCoordinates[playerIndex];
            }
            
            player.coordinate = initialCoord;
            player.intentCoordinate = initialCoord;
        }

        // キックオフチームのFWがボール保持
        Team team;
        if (_kickoffTeamSideCode == TeamSideCode.HOME)
        {
            team = homeTeam;
        }
        else
        {
            team = awayTeam;
        }
        Player kickoffPlayer = team.playerList[9];  // FW1
        ball.SetHolder(kickoffPlayer);
        matchState.UpdateAttackingTeam();  // 攻撃側チームを更新

        // ボール位置も初期配置に合わせる
        ball.coordinate = kickoffPlayer.coordinate;
        
        // キックオフ選手のインデックスを記録
        if (_kickoffTeamSideCode == TeamSideCode.HOME)
        {
            _log.kickoffPlayerIndex = 9;  // HomeのFW1は9
        }
        else
        {
            _log.kickoffPlayerIndex = 20;  // AwayのFW1は20
        }
    }
}
