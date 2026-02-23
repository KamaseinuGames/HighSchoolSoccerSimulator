---
description: "JSONファイルからScriptableObject用の4クラスを自動生成する手順"
---

# JSONファイルからSO用クラスを生成するワークフロー

指定されたJSONファイルの構造を解析し、ScriptableObject変換に必要な4つのC#クラスファイルを生成する。

## 前提

- プロジェクトルート: `/Volumes/KIOKIA500GB/Unity/Projects/HighSchoolSoccerSimulator/Assets/Scripts`
- JSONファイルの格納先: `SpreadSheet/Json/`
- クラスファイルの格納先: `Class/` 配下の4つのサブフォルダ

## ステップ

### 1. JSONファイルの構造を解析する

ユーザーが指定したJSONファイルを `view_file` で読み取り、以下を特定する：

- **JSONファイル名**（拡張子なし）: 例 `PositionDefinition` → これが `{JsonName}` となる
- **dataリスト内の各要素のフィールド名と型**: JSONの値から推測する
  - 整数値 → `int`
  - 小数値 → `float`
  - 文字列 → `string`
  - true/false → `bool`（命名は `is` or `has` プレフィックス）

### 2. 子クラス名を決定する

- dataリスト内の1要素を表すクラス名を決める
- JSONファイル名に要素の意味を加えた名前にする
- 例: `PositionDefinition.json` の各要素は「スロット」なので → `PositionDefinitionSlot`
- これが `{SlotName}` となる

### 3. 4つのクラスファイルを作成する

以下の4ファイルを、既存のパターンに **厳密に** 従って作成する。

---

#### 3-1. `Class/JsonPacked/{SlotName}.cs`（データクラス）

dataリスト内の1要素に対応するプレーンクラス。

```csharp
using System.Collections;
using System.Collections.Generic;
using System;

// コメント: このクラスの説明
[Serializable]
public class {SlotName}
{
    // JSONのフィールドと完全に一致する名前・順序で定義
    public string exampleStr;
    public float exampleRate;
    public int exampleOffsetX;
    // ... 全フィールドをここに列挙
}
```

**注意点:**
- フィールド名はJSONのキー名と **完全一致** させること（JsonUtility.FromJsonで使用するため）
- プロジェクトのコーディング規則（種別サフィックス等）よりもJSON一致を優先する
- UnityEngineへの依存は不要（`using UnityEngine;` は書かない）

---

#### 3-2. `Class/JsonPacked_SO/{SlotName}_SO.cs`（SO版子クラス）

基底クラスを継承するだけの空クラス。

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[Serializable]
public class {SlotName}_SO : {SlotName} { }
```

---

#### 3-3. `Class/JsonConvert_BASE/{JsonName}_BASE.cs`（JSONデシリアライズ用親クラス）

JSONのルートオブジェクトに対応するクラス。`data` リストを持つ。

```csharp
using System.Collections;
using System.Collections.Generic;
using System;

[Serializable]
public class {JsonName}_BASE
{
    public List<{SlotName}> data;
}
```

**注意点:**
- フィールド名 `data` はJSONのキー名と一致させること
- JSONのルートに `data` 以外のフィールドがある場合は、それらも追加する

---

#### 3-4. `Class/JsonConvert_SO/{JsonName}_SO.cs`（ScriptableObject親クラス）

ScriptableObjectを継承し、SO版子クラスのリストを持つ。

```csharp
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

[CreateAssetMenu(menuName = "MyScriptable/Create {JsonName}_SO")]
public class {JsonName}_SO : ScriptableObject
{
    public List<{SlotName}_SO> data = new List<{SlotName}_SO>();
} 
```

**注意点:**
- `List`のジェネリック型は `{SlotName}` ではなく `{SlotName}_SO` にすること
- `new List<>()` で初期化すること

---

### 4. 作成後の確認事項

- [ ] 4ファイル全てが正しいディレクトリに配置されているか
- [ ] JSONのフィールド名とクラスのフィールド名が完全一致しているか
- [ ] _BASE クラスの List ジェネリック型が JsonPacked のクラスを参照しているか
- [ ] _SO クラスの List ジェネリック型が JsonPacked_SO のクラスを参照しているか

### 5. ユーザーへの案内

作成が完了したら、Unityで以下の手順でSOを生成できることをユーザーに伝える：

1. **MyTools > BASE/JsonPackedから_SOクラスを生成** （既に_SOクラスを手動作成済みなので、このステップはスキップ可能）
2. **MyTools > JsonConvertにSOを生成**（JSON → ScriptableObjectアセットの生成）

## 既存の実装例

| JSONファイル | JsonPacked | JsonPacked_SO | JsonConvert_BASE | JsonConvert_SO |
|---|---|---|---|---|
| PositionDefinition.json | PositionDefinitionSlot | PositionDefinitionSlot_SO | PositionDefinition_BASE | PositionDefinition_SO |

## エディターツール（本プロジェクト内）

- `Editor/CreateScriptableObjectFromJSON.cs` — JSON読み込み＆SOアセット生成のコアロジック
- `Editor/GenerateSOFile.cs` — _BASEおよびJsonPackedからSOクラスを自動生成
- `Editor/MyMenuItems.cs` — Unityメニューからの実行エントリ
