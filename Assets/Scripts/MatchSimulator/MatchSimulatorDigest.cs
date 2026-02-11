using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

// シミュレーション結果を可視化するクラス（MonoBehaviour）
// Canvasの子オブジェクトとして配置
public class MatchSimulatorDigest : MonoBehaviour
{
    [Header("表示設定")]
    public float secondsPerMinute = 0.5f;  // 1分を何秒で再生するか
    float cellSize;                         // 1グリッドのサイズ（ピクセル、動的に計算）

    [Header("UI参照")]
    public RectTransform RectTransImagePitch;     // ピッチのImage（RectTransform）
    public Image PrefabBall;               // ボールのプレハブ
    public Image PrefabPlayer;             // 選手のプレハブ
    public Image PrefabGridTile;           // グリッドタイルのプレハブ（任意）
    public TextMeshProUGUI TextScore;      // スコア表示
    public TextMeshProUGUI TextMinute;     // 時間表示（0:00形式）
    public TextMeshProUGUI TextPeriod;       // ピリオド数表示
    public Slider SliderSeek;              // シークバー
    public TextMeshProUGUI TextBallHolder;        // ボール保持者表示
    public TextMeshProUGUI TextBallHolderAction;  // ボール保持者の行動表示

    [Header("色設定")]
    public Color colorHome = new Color(0.2f, 0.4f, 1f);   // 青
    public Color colorAway = new Color(1f, 0.3f, 0.3f);   // 赤
    public Color colorBall = Color.white;
    public Color colorGridLight = new Color(0.627f, 1f, 0.753f);     // #A0FFC0（ピッチ基本色）
    public Color colorGridDark = new Color(0.55f, 0.88f, 0.66f);     // 少し濃い緑
    public Color colorLine = Color.white;                             // ラインの色
    public Color colorGoal = new Color(0.5f, 0.5f, 0.5f);            // ゴールの色（グレー）

    // 内部データ
    List<PeriodLog> matchLog;
    Player[] allPlayers;
    Team homeTeam;
    Team awayTeam;
    
    Image[] playerDots;
    Image ballDot;
    
    int currentMinute = 0;
    float timer = 0f;
    bool isPlaying = false;
    bool isProcessingGoal = false;  // ゴール処理中フラグ

    // シミュレーション結果を受け取って初期化
    public void Initialize(List<PeriodLog> _log, Player[] _players, 
                          Team _home, Team _away)
    {
        matchLog = _log;
        allPlayers = _players;
        homeTeam = _home;
        awayTeam = _away;

        // cellSizeを動的に計算（ピッチのサイズに合わせる）
        CalculateCellSize();

        // UI要素を生成
        CreateVisuals();

        // シークバーを設定
        SetupSeekBar();
        
        // 初期化して再生
        currentMinute = 0;
        UpdateVisuals(0);
        isPlaying = true;
    }

    // cellSizeを動的に計算
    void CalculateCellSize()
    {
        if (RectTransImagePitch == null) return;

        // ピッチのRectTransformのサイズを取得
        float pitchWidth = RectTransImagePitch.rect.width;
        float pitchHeight = RectTransImagePitch.rect.height;

        // グリッドサイズに合わせてcellSizeを計算
        float cellSizeX = pitchWidth / GridEvaluator.WIDTH;
        float cellSizeY = pitchHeight / GridEvaluator.HEIGHT;

        // 小さい方に合わせる（アスペクト比を保つ）
        cellSize = Mathf.Min(cellSizeX, cellSizeY);
    }

    void CreateVisuals()
    {
        // 既存のドットを削除
        foreach (Transform child in RectTransImagePitch)
        {
            Destroy(child.gameObject);
        }

        // 背景を薄い緑に設定
        if (RectTransImagePitch != null)
        {
            Image image = RectTransImagePitch.GetComponent<Image>();
            if (image != null)
            {
                image.color = colorGridLight;
            }
        }

        // グリッドタイルを作成（濃い緑を1個飛ばしで配置 + ライン + ゴール）
        CreateGridTiles();

        // 選手ドットを作成
        playerDots = new Image[allPlayers.Length];
        for (int i = 0; i < allPlayers.Length; i++)
        {
            Player player = allPlayers[i];
            Image dot = Instantiate(PrefabPlayer, RectTransImagePitch);
            if (player.teamSideCode == TeamSideCode.HOME)
            {
                dot.color = colorHome;
            }
            else
            {
                dot.color = colorAway;
            }
            dot.gameObject.name = player.playerProfile.nameStr;
            
            // 選手番号を設定（各チームで1〜11）
            int number = (i % 11) + 1;  // Homeは0-10、Awayは11-21なので、%11で1〜11に
            TextMeshProUGUI textNumber = dot.GetComponentInChildren<TextMeshProUGUI>();
            if (textNumber != null)
            {
                textNumber.text = number.ToString();
            }
            
            playerDots[i] = dot;
        }

        // ボールドットを作成
        ballDot = Instantiate(PrefabBall, RectTransImagePitch);
        ballDot.color = colorBall;
        ballDot.gameObject.name = "Ball";
        // ボールは選手より前面に
        ballDot.transform.SetAsLastSibling();
    }

    // グリッドタイルを作成（濃い緑は1個飛ばし / ライン・ゴールは全グリッド）
    void CreateGridTiles()
    {
        int maxY = GridEvaluator.HEIGHT - 1;  // 100

        for (int y = 0; y < GridEvaluator.HEIGHT; y++)
        {
            for (int x = 0; x < GridEvaluator.WIDTH; x++)
            {
                // ゴール位置かどうか判定（X=32〜38、Y=0 or Y=100、7グリッド）
                bool isGoal = (x >= 32 && x <= 38) && (y == 0 || y == maxY);

                // ライン位置かどうか判定（ゴールでなければ）
                bool isLine = !isGoal && IsLinePosition(x, y);

                // 市松模様のダーク位置かどうか
                bool isDark = (x + y) % 2 == 1;

                // ライン・ゴールでない場合は、1個飛ばし（isDarkのみ）
                if (!isGoal && !isLine && !isDark) continue;

                // タイルを作成（プレハブがあれば使用、なければ動的生成）
                Image tile;
                if (PrefabGridTile != null)
                {
                    tile = Instantiate(PrefabGridTile, RectTransImagePitch);
                }
                else
                {
                    // プレハブがない場合は動的に生成
                    GameObject tileObj = new GameObject($"Tile_{x}_{y}");
                    tileObj.transform.SetParent(RectTransImagePitch, false);
                    tile = tileObj.AddComponent<Image>();
                    tile.rectTransform.sizeDelta = new Vector2(cellSize, cellSize);
                }

                // 色を設定：ゴール→グレー、ライン→白、それ以外→濃い緑
                if (isGoal)
                {
                    tile.color = colorGoal;
                    tile.gameObject.name = $"Goal_{x}_{y}";
                }
                else if (isLine)
                {
                    tile.color = colorLine;
                    tile.gameObject.name = $"Line_{x}_{y}";
                }
                else
                {
                    tile.color = colorGridDark;
                    tile.gameObject.name = $"Tile_{x}_{y}";
                }

                // 位置を設定
                tile.rectTransform.anchoredPosition = new Vector2(
                    x * cellSize + cellSize / 2f,
                    y * cellSize + cellSize / 2f
                );

                // 最背面に配置
                tile.transform.SetAsFirstSibling();
            }
        }
    }

    // ライン位置かどうか判定
    bool IsLinePosition(int _x, int _y)
    {
        int maxY = GridEvaluator.HEIGHT - 1;  // 0 ~ 100
        int maxX = GridEvaluator.WIDTH - 1;   // 0 ~ 70
        int centerX = 35;
        int centerY = 50;

        // === センターライン ===
        if (_y == centerY) return true;

        // === センターサークル（半径約9） ===
        float dx = _x - centerX;
        float dy = _y - centerY;
        float dist = Mathf.Sqrt(dx * dx + dy * dy);
        if (dist >= 8.5f && dist <= 9.5f) return true;

        // === タッチライン（左右端） ===
        if (_x == 0 || _x == maxX) return true;

        // === ゴールライン（上下端、ゴール部分を除く） ===
        // ゴールはX=32～38として、それ以外はライン
        if ((_y == 0 || _y == maxY) && (_x < 32 || _x > 38)) return true;

        // === ペナルティエリア（下側: ラインY=15、内部Y=0～14、X=15とX=55） ===
        // 垂直線
        if ((_x == 15 || _x == 55) && _y >= 0 && _y <= 15) return true;
        // 水平線
        if (_y == 15 && _x >= 15 && _x <= 55) return true;

        // === ペナルティエリア（上側: ラインY=85、内部Y=86～100、X=15とX=55） ===
        // 垂直線
        if ((_x == 15 || _x == 55) && _y >= 85 && _y <= maxY) return true;
        // 水平線
        if (_y == 85 && _x >= 15 && _x <= 55) return true;

        // === ゴールエリア（下側: Y=0～5, X=26とX=44） ===
        // 垂直線
        if ((_x == 26 || _x == 44) && _y >= 0 && _y <= 5) return true;
        // 水平線
        if (_y == 5 && _x >= 26 && _x <= 44) return true;

        // === ゴールエリア（上側: Y=95～100, X=26とX=44） ===
        // 垂直線
        if ((_x == 26 || _x == 44) && _y >= 95 && _y <= maxY) return true;
        // 水平線
        if (_y == 95 && _x >= 26 && _x <= 44) return true;

        // === ペナルティアーク（下側: ペナルティスポット(35,11)を中心に半径9） ===
        float dxPenalty1 = _x - centerX;
        float dyPenalty1 = _y - 11;
        float distPenalty1 = Mathf.Sqrt(dxPenalty1 * dxPenalty1 + dyPenalty1 * dyPenalty1);
        if (distPenalty1 >= 8.5f && distPenalty1 <= 9.5f && _y > 15) return true;

        // === ペナルティアーク（上側: ペナルティスポット(35,89)を中心に半径9） ===
        float dxPenalty2 = _x - centerX;
        float dyPenalty2 = _y - (maxY - 11);  // 89
        float distPenalty2 = Mathf.Sqrt(dxPenalty2 * dxPenalty2 + dyPenalty2 * dyPenalty2);
        if (distPenalty2 >= 8.5f && distPenalty2 <= 9.5f && _y < 85) return true;

        return false;
    }

    void SetupSeekBar()
    {
        if (SliderSeek != null)
        {
            SliderSeek.minValue = 0;
            SliderSeek.maxValue = matchLog.Count - 1;
            SliderSeek.onValueChanged.AddListener(OnSeekBarChanged);
        }
    }

    void Update()
    {
        if (!isPlaying || matchLog == null || isProcessingGoal) return;

        timer += Time.deltaTime;
        if (timer >= secondsPerMinute)
        {
            timer = 0f;
            currentMinute++;
            
            if (currentMinute >= matchLog.Count)
            {
                currentMinute = matchLog.Count - 1;
                isPlaying = false;
                return;
            }

            PeriodLog log = matchLog[currentMinute];
            
            // ゴールが発生している場合は特別処理
            if (log.hasGoalFlag)
            {
                StartCoroutine(ProcessGoalAnimation(currentMinute));
            }
            else
            {
                UpdateVisuals(currentMinute);
            }
            
            if (SliderSeek != null)
            {
                SliderSeek.SetValueWithoutNotify(currentMinute);
            }
        }
    }

    // ゴールアニメーション処理
    IEnumerator ProcessGoalAnimation(int minute)
    {
        isProcessingGoal = true;
        isPlaying = false;  // 一旦停止してタイマーもストップ
        
        PeriodLog log = matchLog[minute];
        
        // 1. 現在の状態を表示（アニメーションなし、その場で停止）
        UpdateVisuals(minute, false);
        
        // 2. ボールだけをゴール位置に移動するアニメーション（3ピリオド分の時間）
        Vector2 goalUiCoord = GridToUI(log.goalCoordinate);
        float goalAnimDuration = secondsPerMinute * 3f;  // 3ピリオド分の時間
        
        ballDot.rectTransform.DOAnchorPos(goalUiCoord, goalAnimDuration)
            .SetEase(Ease.OutCubic);
        
        yield return new WaitForSeconds(goalAnimDuration);
        
        // 3. 選手を初期配置に戻すアニメーション
        float resetAnimDuration = secondsPerMinute * 0.3f;
        
        for (int i = 0; i < allPlayers.Length; i++)
        {
            Player player = allPlayers[i];
            Coordinate initialCoord;
            
            // Teamのフォーメーション座標から取得
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
            
            Vector2 initialUiCoord = GridToUI(initialCoord);
            playerDots[i].rectTransform.DOAnchorPos(initialUiCoord, resetAnimDuration)
                .SetEase(Ease.OutQuad);
        }
        
        // ボールも初期配置に移動（キックオフ位置）
        // キックオフ選手の初期配置がボールの位置
        if (log.kickoffPlayerIndex >= 0 && log.kickoffPlayerIndex < allPlayers.Length)
        {
            Player kickoffPlayer = allPlayers[log.kickoffPlayerIndex];
            Coordinate ballInitialCoord;
            
            if (kickoffPlayer.teamSideCode == TeamSideCode.HOME)
            {
                int playerIndex = log.kickoffPlayerIndex;
                ballInitialCoord = homeTeam.formationCoordinates[playerIndex];
            }
            else
            {
                int playerIndex = log.kickoffPlayerIndex - 11;
                ballInitialCoord = awayTeam.formationCoordinates[playerIndex];
            }
            
            Vector2 ballInitialUiCoord = GridToUI(ballInitialCoord);
            ballDot.rectTransform.DOAnchorPos(ballInitialUiCoord, resetAnimDuration)
                .SetEase(Ease.OutQuad);
        }
        
        yield return new WaitForSeconds(resetAnimDuration);
        
        // 4. 次のピリオドから再開
        isProcessingGoal = false;
        isPlaying = true;
        timer = 0f;  // タイマーリセット
    }

    void UpdateVisuals(int _minute, bool _animate = true)
    {
        if (_minute < 0 || _minute >= matchLog.Count) return;

        PeriodLog log = matchLog[_minute];

        // ボール保持者の行動をログ出力
        if (log.holderId >= 0)
        {
            int minute = _minute / Consts.PERIODS_PER_MINUTE;
            int second = (_minute % Consts.PERIODS_PER_MINUTE) / Consts.PERIODS_PER_SECOND;
            string holderNameStr = "";
            for (int idx = 0; idx < allPlayers.Length; idx++)
            {
                if (allPlayers[idx].matchId == log.holderId)
                {
                    holderNameStr = allPlayers[idx].playerProfile.nameStr;
                    break;
                }
            }
            if (TextBallHolder != null)
            {
                TextBallHolder.text = holderNameStr;
            }
            if (TextBallHolderAction != null)
            {
                TextBallHolderAction.text = log.holderAction.ToString();
            }
        }
        float animDuration;  // アニメーションは秒数の80%
        if (_animate)
        {
            animDuration = secondsPerMinute * 0.8f;
        }
        else
        {
            animDuration = 0f;
        }

        // 選手の位置を更新（PeriodLogに記録された位置を使用）
        for (int i = 0; i < allPlayers.Length; i++)
        {
            Vector2 uiCoord = GridToUI(log.playerCoordinates[i]);
            
            if (_animate && animDuration > 0)
            {
                // DOTweenでスムーズに移動
                playerDots[i].rectTransform.DOAnchorPos(uiCoord, animDuration).SetEase(Ease.OutQuad);
            }
            else
            {
                playerDots[i].rectTransform.anchoredPosition = uiCoord;
            }

            // ボール保持者を少し大きく表示
            float scale;
            if (log.playerHasBall[i])
            {
                scale = 1.3f;
            }
            else
            {
                scale = 1f;
            }
            if (_animate && animDuration > 0)
            {
                playerDots[i].rectTransform.DOScale(Vector3.one * scale, animDuration * 0.3f);
            }
            else
            {
                playerDots[i].rectTransform.localScale = Vector3.one * scale;
            }
        }

        // ボールの位置を更新
        Vector2 ballUiCoord = GridToUI(log.ballCoordinate);
        if (_animate && animDuration > 0)
        {
            ballDot.rectTransform.DOAnchorPos(ballUiCoord, animDuration * 0.5f).SetEase(Ease.OutCubic);
        }
        else
        {
            ballDot.rectTransform.anchoredPosition = ballUiCoord;
        }

        // UI更新（5秒1ピリオド）
        if (TextMinute != null)
        {
            int minute = _minute / Consts.PERIODS_PER_MINUTE;
            int second = (_minute % Consts.PERIODS_PER_MINUTE) / Consts.PERIODS_PER_SECOND;
            TextMinute.text = $"{minute}:{second:00}";
        }
        if (TextPeriod != null)
        {
            TextPeriod.text = $"Period: {_minute}";
        }
        if (TextScore != null)
        {
            // 現在の分までのスコアを計算
            int homeScore = 0;
            int awayScore = 0;
            for (int m = 0; m <= _minute; m++)
            {
                PeriodLog periodLog = matchLog[m];
                if (periodLog.holderAction == ActionCode.SHOOT_SUCCESS)
                {
                    // holderIdからチームを判別（id / 100: 0=Home, 1=Away）
                    bool scorerIsHome = periodLog.holderId / 100 == 0;
                    if (scorerIsHome)
                        homeScore++;
                    else
                        awayScore++;
                }
            }
            TextScore.text = $"{homeTeam.nameStr} {homeScore} - {awayScore} {awayTeam.nameStr}";
        }
    }

    // グリッド座標をUI座標に変換
    Vector2 GridToUI(Coordinate _coord)
    {
        // グリッドの中心に配置
        float x = _coord.x * cellSize + cellSize / 2f;
        float y = _coord.y * cellSize + cellSize / 2f;
        return new Vector2(x, y);
    }

    // シークバー変更時（アニメーションなしで即座に移動）
    void OnSeekBarChanged(float _value)
    {
        currentMinute = Mathf.RoundToInt(_value);
        UpdateVisuals(currentMinute, false);  // アニメーションなし
    }

    // 外部から呼び出すコントロール
    public void Play()
    {
        isPlaying = true;
    }

    public void Pause()
    {
        isPlaying = false;
    }

    public void Restart()
    {
        currentMinute = 0;
        timer = 0f;
        UpdateVisuals(0, false);  // アニメーションなし
        isPlaying = true;
    }
}
