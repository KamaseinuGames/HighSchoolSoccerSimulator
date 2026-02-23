// ボールの状態
public enum BallStateCode
{
    HOLD,        // 選手が保持中
    LOOSE,       // ルーズボール（誰も持っていない）
    FLYING,      // 空中/移動中（保持者なしで移動）
    DEAD         // アウトやゴール後
}
