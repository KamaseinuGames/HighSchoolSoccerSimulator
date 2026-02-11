# 参照型（Reference Type）と値型（Value Type）の違い

## 現在のコード（参照型 - `class`）

```csharp
public class Player  // ← class = 参照型
{
    public bool hasBall;
}

// 動作
Player kickoffPlayer = homeTeam.playerList[9];
// → kickoffPlayerは「同じオブジェクトへの参照」を保持

ball.SetHolder(kickoffPlayer);
// → _playerパラメータも「同じオブジェクトへの参照」

_player.hasBall = true;
// → 元のhomeTeam.playerList[9]も変更される！✅
```

**メモリ上の状態:**
```
homeTeam.playerList[9] ──┐
                         │
                         ├──> [Playerオブジェクト] ← 実際のデータ
                         │
kickoffPlayer ──────────┘
                         │
SetHolder(_player) ──────┘
```

## もし値型（`struct`）だった場合

```csharp
public struct Player  // ← struct = 値型
{
    public bool hasBall;
}

// 動作
Player kickoffPlayer = homeTeam.playerList[9];
// → kickoffPlayerは「オブジェクト全体のコピー」を保持

ball.SetHolder(kickoffPlayer);
// → _playerパラメータも「コピー」を受け取る

_player.hasBall = true;
// → コピーだけが変更される。元のhomeTeam.playerList[9]は変更されない！❌
```

**メモリ上の状態:**
```
homeTeam.playerList[9] ──> [PlayerオブジェクトA] ← 元のデータ（変更されない）

kickoffPlayer ──────────> [PlayerオブジェクトB] ← コピー

SetHolder(_player) ──────> [PlayerオブジェクトC] ← さらにコピー
```

## 主な違い

| 項目 | 参照型（class） | 値型（struct） |
|------|----------------|---------------|
| 宣言 | `public class Player` | `public struct Player` |
| 代入 | 参照をコピー | オブジェクト全体をコピー |
| メモリ | ヒープに配置 | スタックまたはインライン配置 |
| 変更の影響 | すべての参照に反映 | コピーごとに独立 |
| null許容 | 可能 | 不可（Nullable<T>で可能） |
| 継承 | 可能 | 不可 |

## 実際の例

### 参照型（現在のコード）
```csharp
Player p1 = new Player(1, "Player1", true, status);
Player p2 = p1;  // 参照をコピー

p2.hasBall = true;
// → p1.hasBallもtrueになる（同じオブジェクトを参照しているため）
```

### 値型（もしstructだった場合）
```csharp
Player p1 = new Player(1, "Player1", true, status);
Player p2 = p1;  // オブジェクト全体をコピー

p2.hasBall = true;
// → p1.hasBallはfalseのまま（別々のオブジェクトのため）
```

## このプロジェクトでclassを使う理由

- 選手データは複数の場所から参照される（homeTeam.playerList、allPlayerListなど）
- 一箇所で変更したら、すべての参照に反映される必要がある
- メモリ効率が良い（大きなオブジェクトをコピーしない）
- Unityのシリアライゼーションでもclassが推奨される場合が多い
