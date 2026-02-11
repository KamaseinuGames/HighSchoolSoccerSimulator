// 選手の戦術的な意図（Tactics）を表すEnum
public enum TacticsCode
{
    NONE,        // 意図なし

    // 攻撃の戦術
    COUNTER,     // カウンター（速攻）
    POSSESSION,  // ポゼッション（ボール保持）

    // 守備の戦術
    HIGH_PRESS,  // ハイプレス（前からプレス）
    RETREAT      // リトリート（後退して守備）
}
