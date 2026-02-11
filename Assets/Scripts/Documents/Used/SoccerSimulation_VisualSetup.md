# サッカーシミュレーション可視化 - セットアップガイド

## 概要

このガイドでは、シミュレーション結果を画面上で可視化するための手順を説明します。

---

## 1. 必要なオブジェクト構成

```
Hierarchy:
├── SimulatorManager (空のGameObject)
│   └── SimulatorMatch.cs をアタッチ
│
└── Canvas (UI)
    ├── ImagePitch (ピッチの背景、選手・ボールの親)
    ├── TextScore (スコア表示)
    ├── TextMinute (分表示)
    └── SliderSeek (シークバー)
```

---

## 2. 詳細セットアップ手順

### Step 1: Canvas を作成

1. Hierarchy で右クリック → **UI → Canvas**
2. Canvas の設定:
   - **Render Mode**: `Screen Space - Overlay`
   - **Canvas Scaler**:
     - UI Scale Mode: `Scale With Screen Size`
     - Reference Resolution: `1080 x 1920`（縦画面の場合）

---

### Step 2: ImagePitch を作成

1. Canvas を右クリック → **UI → Image**
2. 名前を `ImagePitch` に変更
3. RectTransform の設定:
   - **Anchor**: 左下（Left-Bottom）に設定
   - **Pivot**: (0, 0)
   - **Pos X**: 40
   - **Pos Y**: 200
   - **Width**: 560 (7グリッド × 80px)
   - **Height**: 800 (10グリッド × 80px)
4. Image コンポーネント:
   - **Color**: 緑色 (#3A8C3A など)

---

### Step 3: 選手・ボールのプレハブを作成

#### PrefabPlayer（選手ドット）

1. ImagePitch を右クリック → **UI → Image**
2. 名前を `PrefabPlayer` に変更
3. RectTransform の設定:
   - **Width**: 50
   - **Height**: 50
4. Image コンポーネント:
   - **Source Image**: Unity標準の `Knob` を使用（丸い画像）
   - または、丸い画像をインポートして設定
5. プレハブ化:
   - `PrefabPlayer` を **Assets/Prefabs** フォルダにドラッグ
   - Hierarchy の `PrefabPlayer` は削除

#### PrefabBall（ボールドット）

1. 同様に `PrefabBall` を作成
2. RectTransform の設定:
   - **Width**: 30
   - **Height**: 30
3. Image コンポーネント:
   - **Color**: 白色
4. プレハブ化

---

### Step 4: UI テキストを作成

#### TextScore

1. Canvas を右クリック → **UI → Text - TextMeshPro**
2. 名前を `TextScore` に変更
3. RectTransform の設定:
   - **Anchor**: 上中央（Top-Center）
   - **Pos Y**: -50
   - **Width**: 400
   - **Height**: 50
4. TextMeshPro コンポーネント:
   - **Font Size**: 36
   - **Alignment**: Center
   - **Color**: 白色

#### TextMinute

1. 同様に `TextMinute` を作成
2. TextScore の下に配置

---

### Step 5: SliderSeek を作成

1. Canvas を右クリック → **UI → Slider**
2. 名前を `SliderSeek` に変更
3. RectTransform の設定:
   - **Anchor**: 下中央（Bottom-Center）
   - **Pos Y**: 100
   - **Width**: 500

---

### Step 6: SimulatorDigest をアタッチ

1. Canvas に `SimulatorDigest.cs` をアタッチ
2. Inspector で以下を設定:

| フィールド | 設定内容 |
|-----------|---------|
| **Seconds Per Minute** | 0.5（1分 = 0.5秒） |
| **Cell Size** | 80（1グリッド = 80px） |
| **Image Pitch TF** | ImagePitch をドラッグ |
| **Prefab Ball** | PrefabBall プレハブをドラッグ |
| **Prefab Player** | PrefabPlayer プレハブをドラッグ |
| **Text Score** | TextScore をドラッグ |
| **Text Minute** | TextMinute をドラッグ |
| **Slider Seek** | SliderSeek をドラッグ |
| **Color Home** | 青色 |
| **Color Away** | 赤色 |
| **Color Ball** | 白色 |

---

### Step 7: SimulatorMatch に Digest を接続

1. SimulatorManager を選択
2. SimulatorMatch コンポーネントの **Digest** フィールドに、
   Canvas にアタッチした SimulatorDigest をドラッグ

---

## 3. 動作確認

1. **Play** ボタンを押す
2. コンソールにシミュレーションログが出力される
3. 0.5秒ごとに選手とボールが移動する

---

## 4. グリッド座標の視覚化（オプション）

デバッグ用にグリッド線を表示したい場合:

1. ImagePitch に以下のように子オブジェクトを追加:
   - 縦線 7本（Image, Width: 2, Height: 800）
   - 横線 10本（Image, Width: 560, Height: 2）

または、`Grid Layout Group` を使用してマス目を作成することもできます。

---

## 5. トラブルシューティング

### 選手が表示されない
- ImagePitch の Anchor/Pivot が (0, 0) になっているか確認
- プレハブが正しく設定されているか確認

### 位置がずれる
- Cell Size と ImagePitch のサイズが合っているか確認
- 7グリッド × Cell Size = ImagePitch の Width

### ボールが選手の後ろに隠れる
- PrefabBall の Sibling Index を一番後ろにする（SetAsLastSibling）
