// ステータスから算出されるパラメータを管理するクラス
[System.Serializable]
public class PlayerVariable
{
    // 試合パラメータ
    public int maxMovableInt; // 最大移動可能距離（5秒1ピリオド）
    public Coordinate[] movableAreaOffensive; // 攻撃時の行動可能範囲（四角形の4頂点、時計回り）
    public Coordinate[] movableAreaDefensive; // 守備時の行動可能範囲（四角形の4頂点、時計回り）

    public PlayerVariable(PlayerStatus _playerStatus, int _playerId, TeamSideCode _teamSideCode)
    {
        this.maxMovableInt = GetMaxMovableInt(_playerStatus.speedInt);
        Coordinate[] areaList = GetMovableArea(_playerId, _teamSideCode);
        // 現在は攻撃時と守備時で同じ座標を設定（将来的に変更可能）
        this.movableAreaOffensive = areaList;
        this.movableAreaDefensive = areaList;
    }

    // スピードに応じた最大移動可能距離を取得（5秒1ピリオド、9〜1で1刻み）
    static int GetMaxMovableInt(int _speedInt)
    {
        if (_speedInt >= 90)
        {
            return 9;  // 5秒で約9m（時速約35km/h相当）
        }
        else if (_speedInt >= 80)
        {
            return 8;  // 5秒で約8m（時速約30-35km/h相当）
        }
        else if (_speedInt >= 70)
        {
            return 7;  // 5秒で約7m（時速約25-30km/h相当）
        }
        else if (_speedInt >= 60)
        {
            return 6;  // 5秒で約6m（時速約25-30km/h相当）
        }
        else if (_speedInt >= 50)
        {
            return 5;  // 5秒で約5m（時速約20-25km/h相当）
        }
        else if (_speedInt >= 40)
        {
            return 4;  // 5秒で約4m（時速約20km/h相当）
        }
        else if (_speedInt >= 30)
        {
            return 3;  // 5秒で約3m（時速約15-20km/h相当）
        }
        else if (_speedInt >= 20)
        {
            return 2;  // 5秒で約2m（時速約10-15km/h相当）
        }
        else
        {
            return 1;  // 5秒で約1m（時速約10km/h以下）
        }
    }

    // 役割とチームサイドに基づいて行動可能範囲を取得
    static Coordinate[] GetMovableArea(int _playerId, TeamSideCode _teamSideCode)
    {
        int role = _playerId % 100;  // チーム内インデックス（0=GK, 1-4=DF, 5-8=MF, 9-10=FW）
        const int WIDTH = 71;    // 横: 0〜70
        const int HEIGHT = 101;   // 縦: 0〜100
        const int MID_Y = 50;     // 中央のY座標

        Coordinate[] areaList;

        if (role == 0)
        {
            // GK: ゴール前（ペナルティエリア内、約16m）
            // ラインがY=15なので、内部はY=0～14
            const int PENALTY_AREA_DEPTH = 14;  // ペナルティエリアの深さ（Y=0～14、ラインはY=15）
            const int PENALTY_AREA_LEFT = 15;   // ペナルティエリア左端
            const int PENALTY_AREA_RIGHT = 55;  // ペナルティエリア右端
            
            if (_teamSideCode == TeamSideCode.HOME)
            {
                // HOME側: ゴール前（y: 0〜14、x: 15〜55、ラインはY=15）
                areaList = new Coordinate[]
                {
                    new Coordinate(PENALTY_AREA_LEFT, 0),
                    new Coordinate(PENALTY_AREA_LEFT, PENALTY_AREA_DEPTH),
                    new Coordinate(PENALTY_AREA_RIGHT, PENALTY_AREA_DEPTH),
                    new Coordinate(PENALTY_AREA_RIGHT, 0)
                };
            }
            else
            {
                // AWAY側: ゴール前（y: 86〜100、x: 15〜55、ラインはY=85）
                int goalY = HEIGHT - 1;
                int penaltyAreaTop = goalY - PENALTY_AREA_DEPTH;
                areaList = new Coordinate[]
                {
                    new Coordinate(PENALTY_AREA_LEFT, penaltyAreaTop),
                    new Coordinate(PENALTY_AREA_LEFT, goalY),
                    new Coordinate(PENALTY_AREA_RIGHT, goalY),
                    new Coordinate(PENALTY_AREA_RIGHT, penaltyAreaTop)
                };
            }
        }
        else if (role <= 4)
        {
            // DF: ディフェンダー
            const int PENALTY_AREA_LEFT = 15;   // ペナルティエリア左端
            const int PENALTY_AREA_RIGHT = 55;  // ペナルティエリア右端
            const int CENTER_X = 35;            // ゴール中央
            
            if (role == 1)
            {
                // DF1: サイドバック（左サイド、ライン際からペナルティエリアまで）
                if (_teamSideCode == TeamSideCode.HOME)
                {
                    areaList = new Coordinate[]
                    {
                        new Coordinate(0, 0),
                        new Coordinate(0, MID_Y),
                        new Coordinate(PENALTY_AREA_LEFT, MID_Y),
                        new Coordinate(PENALTY_AREA_LEFT, 0)
                    };
                }
                else
                {
                    areaList = new Coordinate[]
                    {
                        new Coordinate(0, MID_Y),
                        new Coordinate(0, HEIGHT - 1),
                        new Coordinate(PENALTY_AREA_LEFT, HEIGHT - 1),
                        new Coordinate(PENALTY_AREA_LEFT, MID_Y)
                    };
                }
            }
            else if (role == 2)
            {
                // DF2: ペナルティエリアからゴール中央まで（左側2分割分）
                if (_teamSideCode == TeamSideCode.HOME)
                {
                    areaList = new Coordinate[]
                    {
                        new Coordinate(PENALTY_AREA_LEFT, 0),
                        new Coordinate(PENALTY_AREA_LEFT, MID_Y),
                        new Coordinate(CENTER_X, MID_Y),
                        new Coordinate(CENTER_X, 0)
                    };
                }
                else
                {
                    areaList = new Coordinate[]
                    {
                        new Coordinate(PENALTY_AREA_LEFT, MID_Y),
                        new Coordinate(PENALTY_AREA_LEFT, HEIGHT - 1),
                        new Coordinate(CENTER_X, HEIGHT - 1),
                        new Coordinate(CENTER_X, MID_Y)
                    };
                }
            }
            else if (role == 3)
            {
                // DF3: ゴール中央からペナルティエリア右端まで（右側2分割分）
                if (_teamSideCode == TeamSideCode.HOME)
                {
                    areaList = new Coordinate[]
                    {
                        new Coordinate(CENTER_X, 0),
                        new Coordinate(CENTER_X, MID_Y),
                        new Coordinate(PENALTY_AREA_RIGHT, MID_Y),
                        new Coordinate(PENALTY_AREA_RIGHT, 0)
                    };
                }
                else
                {
                    areaList = new Coordinate[]
                    {
                        new Coordinate(CENTER_X, MID_Y),
                        new Coordinate(CENTER_X, HEIGHT - 1),
                        new Coordinate(PENALTY_AREA_RIGHT, HEIGHT - 1),
                        new Coordinate(PENALTY_AREA_RIGHT, MID_Y)
                    };
                }
            }
            else
            {
                // DF4: サイドバック（右サイド、ペナルティエリアからライン際まで）
                if (_teamSideCode == TeamSideCode.HOME)
                {
                    areaList = new Coordinate[]
                    {
                        new Coordinate(PENALTY_AREA_RIGHT, 0),
                        new Coordinate(PENALTY_AREA_RIGHT, MID_Y),
                        new Coordinate(WIDTH - 1, MID_Y),
                        new Coordinate(WIDTH - 1, 0)
                    };
                }
                else
                {
                    areaList = new Coordinate[]
                    {
                        new Coordinate(PENALTY_AREA_RIGHT, MID_Y),
                        new Coordinate(PENALTY_AREA_RIGHT, HEIGHT - 1),
                        new Coordinate(WIDTH - 1, HEIGHT - 1),
                        new Coordinate(WIDTH - 1, MID_Y)
                    };
                }
            }
        }
        else if (role <= 8)
        {
            // MF: ミッドフィールダー（X軸を4分割、Y軸は全部）
            int mfIndex = role - 5;  // 0〜3
            int segmentWidth = WIDTH / 4;  // 17.5 → 17（整数で計算）
            int leftX = mfIndex * segmentWidth;
            int rightX;
            if (mfIndex == 3)
            {
                rightX = WIDTH - 1;  // 最後のセグメントは右端まで
            }
            else
            {
                rightX = (mfIndex + 1) * segmentWidth - 1;
            }
            
            areaList = new Coordinate[]
            {
                new Coordinate(leftX, 0),
                new Coordinate(leftX, HEIGHT - 1),
                new Coordinate(rightX, HEIGHT - 1),
                new Coordinate(rightX, 0)
            };
        }
        else
        {
            // FW: アタッカー（相手陣側の半分、X軸を2分割）
            int fwIndex = role - 9;  // 0 or 1
            int halfWidth = WIDTH / 2;  // 35
            
            if (_teamSideCode == TeamSideCode.HOME)
            {
                // HOME側: 上半分（y: 50〜100）
                if (fwIndex == 0)
                {
                    // FW1: 左半分
                    areaList = new Coordinate[]
                    {
                        new Coordinate(0, MID_Y),
                        new Coordinate(0, HEIGHT - 1),
                        new Coordinate(halfWidth - 1, HEIGHT - 1),
                        new Coordinate(halfWidth - 1, MID_Y)
                    };
                }
                else
                {
                    // FW2: 右半分
                    areaList = new Coordinate[]
                    {
                        new Coordinate(halfWidth, MID_Y),
                        new Coordinate(halfWidth, HEIGHT - 1),
                        new Coordinate(WIDTH - 1, HEIGHT - 1),
                        new Coordinate(WIDTH - 1, MID_Y)
                    };
                }
            }
            else
            {
                // AWAY側: 下半分（y: 0〜50）
                if (fwIndex == 0)
                {
                    // FW1: 左半分
                    areaList = new Coordinate[]
                    {
                        new Coordinate(0, 0),
                        new Coordinate(0, MID_Y),
                        new Coordinate(halfWidth - 1, MID_Y),
                        new Coordinate(halfWidth - 1, 0)
                    };
                }
                else
                {
                    // FW2: 右半分
                    areaList = new Coordinate[]
                    {
                        new Coordinate(halfWidth, 0),
                        new Coordinate(halfWidth, MID_Y),
                        new Coordinate(WIDTH - 1, MID_Y),
                        new Coordinate(WIDTH - 1, 0)
                    };
                }
            }
        }

        return areaList;
    }

    // 指定座標が攻撃時の行動可能範囲内かどうかを判定
    public bool IsInMovableAreaOffensive(Coordinate _coord)
    {
        return IsInPolygon(_coord, movableAreaOffensive);
    }

    // 指定座標が守備時の行動可能範囲内かどうかを判定
    public bool IsInMovableAreaDefensive(Coordinate _coord)
    {
        return IsInPolygon(_coord, movableAreaDefensive);
    }

    // 指定座標が多角形内にあるかを判定（点が多角形内にあるかの判定）
    bool IsInPolygon(Coordinate _coord, Coordinate[] _areaList)
    {
        if (_areaList == null || _areaList.Length < 3)
        {
            return true;  // 範囲が設定されていない場合は全範囲許可
        }

        // レイキャスティングアルゴリズム（Ray Casting Algorithm）を使用
        // 点から右方向に無限に伸ばした線が、多角形の辺と交差する回数をカウント
        // 奇数回なら内部、偶数回なら外部
        int intersectionCount = 0;
        int pointCount = _areaList.Length;

        for (int i = 0; i < pointCount; i++)
        {
            Coordinate p1 = _areaList[i];
            Coordinate p2 = _areaList[(i + 1) % pointCount];

            // 水平線（y = _coord.y）が辺と交差するかチェック
            if ((p1.y > _coord.y) != (p2.y > _coord.y))
            {
                // 交差点のX座標を計算
                float intersectionX = (float)(_coord.y - p1.y) * (p2.x - p1.x) / (p2.y - p1.y) + p1.x;
                if (_coord.x < intersectionX)
                {
                    intersectionCount++;
                }
            }
        }

        return (intersectionCount % 2) == 1;
    }
}
