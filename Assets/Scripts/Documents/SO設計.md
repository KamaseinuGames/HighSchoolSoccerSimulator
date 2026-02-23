# ScriptableObject 設計

フォーメーションと選手配置を制御するための3つのScriptableObject。

---

## 座標系

- 左下 `(0, 0)` 〜 右上 `(70, 100)`、ピッチ中央 `(35, 50)`
- Home: Y=0側、Away: Y=100側
- 全データはHome基準。AwayはY反転して使用

---

## ダイナミックアンカー方式

ボール位置に応じてアンカーが動的に算出される方式。

```
動的アンカー = ベース座標（攻守別） + 追従係数 × (ボール位置 - ピッチ中央)
```

- **ベース座標**: 攻撃時/守備時それぞれの理想位置（FormationDefinitionで定義）
- **追従係数**: ボール移動への追従度合い。攻守別に定義（PositionDefinitionで定義）
- **ダイナミックアンカー = intentCoordinate**: 各選手はアンカーに向かって移動する

チーム全員が同一方向にスライドするため、フォーメーションの形が自然に保たれる。

### チームメイト反発

可動域（矩形制約）は使用しない。代わりに、味方選手間の距離が近すぎる場合に反発力で離す。
これによりフォーメーションの間隔が自然に保たれ、団子状態を防ぐ。

---

## 1. FormationDefinition（フォーメーション座標）

フォーメーションの「形」を定義する。

### FormationDefinition

| フィールド | 型 | 説明 |
|---|---|---|
| formationNameStr | string | フォーメーション名（例: "4-4-2"） |
| slotList | FormationSlot[11] | 11人分のスロット |

### FormationSlot

| フィールド | 型 | 説明 |
|---|---|---|
| defaultPositionGroupStr | string | デフォルトのポジショングループ（"GK" / "DF" / "MF" / "FW"） |
| defaultPositionStr | string | デフォルトのポジション名（例: "CB", "SB"） |
| baseCoordinate | Coordinate | 初期配置座標（キックオフ時） |
| goalKickOffenseCoordinate | Coordinate | ゴールキック攻撃時 = 攻撃ベース座標 |
| goalKickDefenseCoordinate | Coordinate | ゴールキック守備時 = 守備ベース座標 |
| cornerKickOffenseCoordinate | Coordinate | コーナーキック攻撃時 |
| cornerKickDefenseCoordinate | Coordinate | コーナーキック守備時 |

### ポジションエディット

スロットの `positionStr` を変更するだけで、追従係数が切り替わる。

---

## 2. PositionDefinition（ポジション特性）

各ポジションの追従係数を定義する。

### データ構造

| フィールド | 型 | 説明 |
|---|---|---|
| positionStr | string | ポジション名 |
| positionGroupStr | string | グループ（"GK" / "DF" / "MF" / "FW"） |
| offenseFollowXRate | float | 攻撃時X追従係数 |
| offenseFollowYRate | float | 攻撃時Y追従係数 |
| defenseFollowXRate | float | 守備時X追従係数 |
| defenseFollowYRate | float | 守備時Y追従係数 |

### 攻守の追従係数の違い

- **守備時**: X追従を高めに → チーム全体がボール側にコンパクトに絞る
- **攻撃時**: X追従を低めに → 逆サイドに張ってピッチ幅を使う

### ポジション一覧

左右はベース座標で決まるため、L/Rプレフィックスは不要。

| ポジション | グループ | 説明 |
|---|---|---|
| GK  | GK | ゴールキーパー |
| CB  | DF | センターバック |
| SB  | DF | サイドバック |
| WB  | MF | ウイングバック（攻撃的SB） |
| DMF | MF | 守備的MF |
| CMF | MF | セントラルMF |
| OMF | MF | 攻撃的MF |
| WMF | MF | サイドMF |
| CF  | FW | センターフォワード |
| ST  | FW | セカンドトップ |
| WG  | FW | ウイング |

---

## 3. FormationWeightDefinition（フォーメーション重み付け）

※後回し。他のSOが完成してから着手。

---

## 実行時の流れ

```
1. ボール位置を取得
2. 攻守状態を判定
3. FormationDefinitionから攻守別ベース座標を取得（goalKickOffense/Defenseを流用）
4. PositionDefinitionから攻守に応じた追従係数を取得
5. 動的アンカーを算出: goalKickOffense/Defense + followRate × (ball - center)
6. チームメイト反発: 味方が近すぎたらアンカーをズラす
7. intentCoordinate = アンカー（MoveToIntentで移動）
```

セットプレー時はダイナミックアンカーを使わず、専用座標を直接使用する。
