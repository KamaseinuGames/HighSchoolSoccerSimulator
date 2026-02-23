"""
GeoGebraの出力データをFormationSlot形式のJSONに変換するスクリプト

使い方:
  python3 geogebra_to_json.py ファイル名

3回の入力を求められます:
  1. base（初期配置座標）         ← ここからポジショングループ・ポジション名も取得
  2. goalKickOffense（GK攻撃時座標 = 攻撃ベース）
  3. goalKickDefense（GK守備時座標 = 守備ベース）

入力例 (GeoGebraから出力された文字列):
  {"DF,SB,(13,34)", "DF,CB,(28,33)", ...}

出力: Coords/ファイル名.json
"""

import json
import os
import re
import sys

STAGE_LIST = ["base", "goalKickOffense", "goalKickDefense"]


def normalize_input(raw_input: str) -> str:
    """スマートクォートや全角文字を正規化"""
    normalized = raw_input.replace("\u201c", '"').replace("\u201d", '"')
    normalized = normalized.replace("\u2018", "'").replace("\u2019", "'")
    normalized = normalized.replace("\uff08", "(").replace("\uff09", ")")
    normalized = normalized.replace("\uff0c", ",")
    return normalized


def parse_coordinates(raw_input: str) -> list[tuple[int, int]]:
    """座標のみを抽出して返す"""
    normalizedStr = normalize_input(raw_input)
    pattern = r"\(\s*(\d+)\s*,\s*(\d+)\s*\)"
    matchList = re.findall(pattern, normalizedStr)
    resultList = []
    for match in matchList:
        resultList.append((int(match[0]), int(match[1])))
    return resultList


def parse_with_positions(raw_input: str) -> list[dict]:
    """ポジション情報+座標をパースして返す（base用）"""
    normalizedStr = normalize_input(raw_input)
    pattern = r"([A-Z]+)\s*,\s*([A-Z]+)\s*,\s*\(\s*(\d+)\s*,\s*(\d+)\s*\)"
    matchList = re.findall(pattern, normalizedStr)
    resultList = []
    for match in matchList:
        resultList.append({
            "defaultPositionGroupStr": match[0],
            "defaultPositionStr": match[1],
            "x": int(match[2]),
            "y": int(match[3]),
        })
    return resultList


def build_json(baseDataList: list[dict], goalKickOffenseCoordList: list[tuple], goalKickDefenseCoordList: list[tuple]) -> dict:
    """FormationSlot形式のJSON辞書を構築"""
    slotList = []
    for i in range(len(baseDataList)):
        slot = {
            "defaultPositionGroupStr": baseDataList[i]["defaultPositionGroupStr"],
            "defaultPositionStr": baseDataList[i]["defaultPositionStr"],
            "baseCoordinate": {
                "x": baseDataList[i]["x"],
                "y": baseDataList[i]["y"],
            },
            "goalKickOffenseCoordinate": {
                "x": goalKickOffenseCoordList[i][0],
                "y": goalKickOffenseCoordList[i][1],
            },
            "goalKickDefenseCoordinate": {
                "x": goalKickDefenseCoordList[i][0],
                "y": goalKickDefenseCoordList[i][1],
            },
        }
        slotList.append(slot)

    return {"data": slotList}


def main():
    if len(sys.argv) < 2:
        print("使い方: python3 geogebra_to_json.py ファイル名")
        print("例: python3 geogebra_to_json.py 442")
        sys.exit(1)

    fileNameStr = sys.argv[1]

    # --- 1. base ---
    print(f"\n[1/3] base の座標を貼り付けてください:")
    rawBaseStr = input()
    baseDataList = parse_with_positions(rawBaseStr)

    if len(baseDataList) == 0:
        print("有効なデータが見つかりませんでした。")
        sys.exit(1)

    slotCountInt = len(baseDataList)
    print(f"  → {slotCountInt}人分のデータを検出")

    # --- 2. goalKickOffense ---
    print(f"\n[2/3] goalKickOffense の座標を貼り付けてください:")
    rawGoalKickOffenseStr = input()
    goalKickOffenseCoordList = parse_coordinates(rawGoalKickOffenseStr)

    if len(goalKickOffenseCoordList) != slotCountInt:
        print(f"エラー: base({slotCountInt}人)と数が一致しません({len(goalKickOffenseCoordList)}人)")
        sys.exit(1)

    print(f"  → {len(goalKickOffenseCoordList)}人分の座標を検出")

    # --- 3. goalKickDefense ---
    print(f"\n[3/3] goalKickDefense の座標を貼り付けてください:")
    rawGoalKickDefenseStr = input()
    goalKickDefenseCoordList = parse_coordinates(rawGoalKickDefenseStr)

    if len(goalKickDefenseCoordList) != slotCountInt:
        print(f"エラー: base({slotCountInt}人)と数が一致しません({len(goalKickDefenseCoordList)}人)")
        sys.exit(1)

    print(f"  → {len(goalKickDefenseCoordList)}人分の座標を検出")

    # --- JSON構築 ---
    jsonData = build_json(baseDataList, goalKickOffenseCoordList, goalKickDefenseCoordList)
    jsonStr = json.dumps(jsonData, indent=4, ensure_ascii=False)

    # --- 出力 ---
    scriptDirStr = os.path.dirname(os.path.abspath(__file__))
    coordsDirStr = os.path.join(scriptDirStr, "Coords")
    os.makedirs(coordsDirStr, exist_ok=True)

    outputPathStr = os.path.join(coordsDirStr, f"{fileNameStr}.json")
    with open(outputPathStr, "w", encoding="utf-8") as file:
        file.write(jsonStr + "\n")

    print(f"\n--- 出力完了 ---")
    print(f"保存先: {outputPathStr}")
    print(f"{slotCountInt}スロット分のデータをJSON化しました。")


if __name__ == "__main__":
    main()
