// 選手のパラメータ（4項目のみ）
// 各値は 0〜100 の範囲
[System.Serializable]
public class PlayerStatus
{
    // 能力パラメータ
    public int speedInt;     // 移動速度（1ピリオドで移動できるグリッド数に影響）
    public int shootInt;     // シュート成功率に影響
    public int passInt;      // パス成功率に影響
    public int dribbleInt;   // ドリブル突破成功率、奪われにくさ
    public int defenseInt;  // 守備能力（ドリブルを止める能力）

    public PlayerStatus(int _speedInt, int _shootInt, int _passInt, int _dribbleInt, int _defenseInt)
    {
        this.speedInt = _speedInt;
        this.shootInt = _shootInt;
        this.passInt = _passInt;
        this.dribbleInt = _dribbleInt;
        this.defenseInt = _defenseInt;
    }

    // ランダムなパラメータを生成（テスト用）
    public static PlayerStatus CreateRandom()
    {
        System.Random rand = new System.Random();
        return new PlayerStatus(
            rand.Next(1, 101),
            rand.Next(1, 101),
            rand.Next(1, 101),
            rand.Next(1, 101),
            rand.Next(1, 101)
        );
    }

    public override string ToString()
    {
        return $"[Spd:{speedInt} Sht:{shootInt} Pas:{passInt} Dri:{dribbleInt} Def:{defenseInt}]";
    }
}
