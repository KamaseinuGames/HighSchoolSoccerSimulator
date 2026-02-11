// 非保持者（攻撃）の戦術的な意図を表すEnum
public enum IntentOffenseCode
{
    NONE,               // 意図なし
    SUPPORT,            // 味方のサポート位置に移動
    RUN_INTO_SPACE,     // スペースへの走り込み
    HOLD_POSITION,      // 現在の場所を維持
    MAKE_WIDTH          // ワイドに広がる
}
