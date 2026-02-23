// プロジェクト全体で使う定数
// tick（ピリオド）関連はここで固定する
public static class Consts
{
    // === 時間（離散ピリオド） ===
    // 1ピリオド = 0.1秒（100ms）
    public const int PERIOD_MILLISECONDS = 100;
    public const int PERIODS_PER_SECOND = 10;
    public const int PERIODS_PER_MINUTE = 600;
    
    // ゴールに近い場合の「シュートを選ぶ確率」（意図とは別の距離要因）
    public const float SHOOT_CLOSE_RANGE_SELECT_PROB = 0.7f;

    // === パス分岐（逸れ/オーバー/ディフレクト） ===
    public const float PASS_OVERHIT_MIN_PROB = 0.03f;
    public const float PASS_OVERHIT_MAX_PROB = 0.18f;
    public const float PASS_STRAY_MIN_PROB = 0.05f;
    public const float PASS_STRAY_MAX_PROB = 0.22f;
    public const float PASS_DEFLECT_WHEN_INTERCEPTOR_PROB = 0.45f;
    public const float ONE_TOUCH_PASS_MIN_PROB = 0.1f;
    public const float ONE_TOUCH_PASS_MAX_PROB = 0.45f;
    public const float TRAP_MISS_MIN_PROB = 0.03f;
    public const float TRAP_MISS_MAX_PROB = 0.2f;
    public const float HAND_BALL_MIN_PROB = 0.02f;
    public const float HAND_BALL_MAX_PROB = 0.12f;

    // === シュート分岐（ブロック/弾き） ===
    public const float SHOOT_BLOCK_BASE_PROB = 0.18f;
    public const float SHOOT_BLOCK_MAX_PROB = 0.45f;
    public const float SHOOT_GK_CATCH_BASE_PROB = 0.55f;
    public const float SHOOT_GK_CATCH_MAX_PROB = 0.9f;

    // === ドリブル競り合い分岐 ===
    public const float DRIBBLE_DUEL_SPILL_BASE_PROB = 0.2f;
    public const float DRIBBLE_DUEL_SPILL_MAX_PROB = 0.55f;
    public const float CLEAR_UNDER_PRESSURE_BASE_PROB = 0.35f;
    public const float CLEAR_UNDER_PRESSURE_MAX_PROB = 0.75f;
    public const float TACKLE_FOUL_BASE_PROB = 0.08f;
    public const float TACKLE_FOUL_MAX_PROB = 0.28f;

    // === セットプレー ===
    // 再開位置に近い選手を使う半径（見つからない場合は最寄り）
    public const int SET_PLAY_NEAR_RADIUS = 25;

    // === ボール飛行（FLYING） ===
    // 飛行中に「触れた」と判定する半径（マンハッタン距離）
    public const int FLIGHT_TOUCH_RADIUS = 1;

    // 到着点での競争判定半径（マンハッタン距離）
    public const int FLIGHT_ARRIVAL_COMPETE_RADIUS = 2;

    // 競争スコアの重み（大きいほどその要素が効く）
    public const int FLIGHT_SCORE_DIST_PENALTY = 200;
    public const int FLIGHT_SCORE_FINAL_HOLDER_BONUS = 200;
    public const int FLIGHT_SCORE_INTENDED_RECEIVER_BONUS = 100;
    public const int FLIGHT_SCORE_RANDOM_RANGE = 50;

    // === 視野角 ===
    // 視野の半角（度）。60なら左右60度ずつ、合計120度の扇形
    public const float VISION_HALF_ANGLE_DEG = 60f;
    // 視野外からのパスを受けた時のトラップミス確率ボーナス（加算）
    public const float TRAP_MISS_OUT_OF_VIEW_BONUS = 0.15f;
    // シュート「打たない」判定用。視野中央の狭い範囲（半角20度）
    public const float VISION_CENTRAL_HALF_ANGLE_DEG = 20f;
}

