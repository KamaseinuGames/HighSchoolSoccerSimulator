// グリッド座標を表す構造体
// ピッチ: 101×71 グリッド（縦101 × 横71、1m = 1グリッド）
public struct Coordinate
{
    public int x;  // 0〜70（横: 71マス、中央=35）
    public int y;  // 0〜100（縦: 101マス、中央=50、y=0がHomeゴール、y=100がAwayゴール）

    public Coordinate(int _x, int _y)
    {
        x = _x;
        y = _y;
    }

    // 2点間のマンハッタン距離を計算
    public int DistanceTo(Coordinate _other)
    {
        return System.Math.Abs(x - _other.x) + System.Math.Abs(y - _other.y);
    }

    public override string ToString()
    {
        return $"({x}, {y})";
    }

    public static bool operator ==(Coordinate a, Coordinate b)
    {
        return a.x == b.x && a.y == b.y;
    }

    public static bool operator !=(Coordinate a, Coordinate b)
    {
        return !(a == b);
    }

    public override bool Equals(object obj)
    {
        if (obj is Coordinate other)
        {
            return this == other;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return x * 10000 + y;  // yが最大99なので、10000倍で十分
    }
}
