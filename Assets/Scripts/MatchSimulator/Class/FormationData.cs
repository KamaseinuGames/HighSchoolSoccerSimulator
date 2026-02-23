// フォーメーション全体の「形」を定義するクラス
// 10個のスロット（GK除く）に対して、各シーンの座標を持つ
// GKは全フォーメーション共通で固定座標
// 将来的にScriptableObjectに差し替え予定
[System.Serializable]
public class FormationData
{
    // GK固定座標（全フォーメーション共通）
    public static readonly Coordinate GK_BASE_COORDINATE = new Coordinate(35, 2);

    public string formationNameStr;
    public FormationSlot[] slotArray;

    public FormationData(string _formationNameStr, FormationSlot[] _slotArray)
    {
        formationNameStr = _formationNameStr;
        slotArray = _slotArray;
    }

    // 暫定: 4-4-2フォーメーションのサンプルデータ（Home基準、AwayはY反転して使用）
    // データ元: SpreadSheet/Coords/442.json（GeoGebraから出力）
    // CK座標は左コーナー基準（x=0側）で定義。右コーナーの場合はX反転して使用
    public static FormationData CreateDefault442()
    {
        FormationSlot[] slotArray = new FormationSlot[10];

        // 左SB
        slotArray[0] = new FormationSlot(
            "DF", "SB",
            new Coordinate(13, 34),
            new Coordinate(13, 42),
            new Coordinate(13, 42),
            new Coordinate(13, 65),
            new Coordinate(25, 93)
        );

        // 左CB
        slotArray[1] = new FormationSlot(
            "DF", "CB",
            new Coordinate(28, 33),
            new Coordinate(28, 41),
            new Coordinate(28, 41),
            new Coordinate(28, 65),
            new Coordinate(30, 90)
        );

        // 右CB
        slotArray[2] = new FormationSlot(
            "DF", "CB",
            new Coordinate(42, 33),
            new Coordinate(42, 41),
            new Coordinate(42, 41),
            new Coordinate(42, 65),
            new Coordinate(38, 88)
        );

        // 右SB
        slotArray[3] = new FormationSlot(
            "DF", "SB",
            new Coordinate(57, 34),
            new Coordinate(57, 42),
            new Coordinate(57, 42),
            new Coordinate(57, 65),
            new Coordinate(45, 91)
        );

        // 左WMF
        slotArray[4] = new FormationSlot(
            "MF", "WMF",
            new Coordinate(13, 42),
            new Coordinate(13, 54),
            new Coordinate(13, 54),
            new Coordinate(25, 85),
            new Coordinate(30, 95)
        );

        // 左CMF
        slotArray[5] = new FormationSlot(
            "MF", "CMF",
            new Coordinate(27, 41),
            new Coordinate(27, 53),
            new Coordinate(27, 53),
            new Coordinate(32, 87),
            new Coordinate(35, 93)
        );

        // 右CMF
        slotArray[6] = new FormationSlot(
            "MF", "CMF",
            new Coordinate(43, 41),
            new Coordinate(43, 53),
            new Coordinate(43, 53),
            new Coordinate(35, 80),
            new Coordinate(40, 90)
        );

        // 右WMF
        slotArray[7] = new FormationSlot(
            "MF", "WMF",
            new Coordinate(57, 42),
            new Coordinate(57, 54),
            new Coordinate(57, 54),
            new Coordinate(45, 78),
            new Coordinate(50, 85)
        );

        // ST
        slotArray[8] = new FormationSlot(
            "FW", "ST",
            new Coordinate(25, 49),
            new Coordinate(25, 65),
            new Coordinate(25, 69),
            new Coordinate(30, 93),
            new Coordinate(28, 96)
        );

        // CF
        slotArray[9] = new FormationSlot(
            "FW", "CF",
            new Coordinate(45, 49),
            new Coordinate(45, 65),
            new Coordinate(45, 69),
            new Coordinate(40, 95),
            new Coordinate(35, 60)
        );

        return new FormationData("4-4-2", slotArray);
    }
}

