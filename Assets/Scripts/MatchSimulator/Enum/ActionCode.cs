// ボールに関与した具体的な行動（Action）を表すEnum
public enum ActionCode
{
    NONE,        // 行動なし

    // ボール保持者の行動
    PASS_SUCCESS,        // パス成功
    PASS_FAIL,           // パス失敗
    DRIBBLE_SUCCESS,     // ドリブル成功
    DRIBBLE_FAIL,        // ドリブル失敗
    DRIBBLE_BREAKTHROUGH, // ドリブル突破（相手を抜き去る）
    SHOOT_SUCCESS,       // シュート成功（ゴール）
    SHOOT_FAIL,          // シュート失敗

    // ボール非保持者の行動（ボールに関与する行動）
    RECEIVE,             // パスを受ける
    INTERCEPT,           // インターセプト（パスをカットする）
    TACKLE,              // タックル（ボール保持者にタックル）
    SHOOT_CATCH          // シュートキャッチ（GKがシュートをキャッチする）
}
