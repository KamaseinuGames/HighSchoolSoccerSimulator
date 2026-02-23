// ボールに関与した具体的な行動（Action）を表すEnum
public enum ActionCode
{
    NONE,        // 行動なし

    // ボール保持者の行動
    FOUL,                // ファール発生
    HAND,                // ハンド発生
    PASS_SUCCESS,        // パス成功
    PASS_FAIL,           // パス失敗
    ONE_TOUCH_PASS,      // ワンタッチパス
    DRIBBLE_SUCCESS,     // ドリブル成功
    DRIBBLE_FAIL,        // ドリブル失敗
    DRIBBLE_BREAKTHROUGH, // ドリブル突破（相手を抜き去る）
    DRIBBLE_SPILL,       // 競り合いでこぼれ球
    CLEAR,               // クリア
    SHOOT_SUCCESS,       // シュート成功（ゴール）
    SHOOT_FAIL,          // シュート失敗

    // ボール非保持者の行動（ボールに関与する行動）
    RECEIVE,             // パスを受ける
    TRAP_MISS,           // トラップミス（タッチミス）
    INTERCEPT,           // インターセプト（パスをカットする）
    PASS_DEFLECT,        // パスに当てて進路を変える
    TACKLE,              // タックル（ボール保持者にタックル）
    SHOOT_CATCH,         // シュートキャッチ（GKがシュートをキャッチする）
    SHOOT_BLOCK,         // シュートブロック（DFがブロック）
    SHOOT_PARRY,         // シュート弾き（GKが弾く）

    // セットプレー
    THROW_IN,            // スローイン
    CORNER_KICK,         // コーナーキック
    GOAL_KICK,           // ゴールキック
    FREE_KICK,           // フリーキック
    PENALTY_KICK         // ペナルティキック
}
