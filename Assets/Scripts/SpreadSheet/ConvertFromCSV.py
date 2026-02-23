import json
import csv
import sys
import os
import glob
import re
import shutil

"""
    python ConvertFromCSV.py [folder_name]
    指定されたフォルダ配下のCSVファイルまたは単一のCSVファイルを読み込んでJSONに変換し、CSVファイル/フォルダを移動する
"""
# コマンドからフォルダ名を読み取る
args = sys.argv
if len(args) < 2:
    sys.exit(1)

folder_name = args[1]

# 現在のスクリプトがあるディレクトリを基準にCSVパスを設定
script_dir = os.path.dirname(os.path.abspath(__file__))
csv_folder_path = os.path.join(script_dir, f"{folder_name}.csv")
csv_file_path = os.path.join(script_dir, f"{folder_name}.csv")

# 出力用のJSONフォルダを作成
json_folder = "/Volumes/KIOKIA500GB/Unity/Projects/HighSchoolSoccerSimulator/Assets/Scripts/SpreadSheet/Json"
os.makedirs(json_folder, exist_ok=True)

# 削除対象のフォルダパスを設定
folders_to_clean = [
    json_folder,
    "/Volumes/KIOKIA500GB/Unity/Projects/HighSchoolSoccerSimulator/Assets/Database/JsonConvert"
]

# 指定されたフォルダ内の全てのファイルを削除
for folder in folders_to_clean:
    if os.path.exists(folder):
        for file in os.listdir(folder):
            file_path = os.path.join(folder, file)
            if os.path.isfile(file_path):  # ファイルの場合のみ削除
                os.remove(file_path)

# CSVファイルを検索（フォルダまたは単一ファイル）
csv_files = []

if os.path.isdir(csv_folder_path):
    # フォルダの場合：フォルダ内のCSVファイルを検索
    csv_pattern = os.path.join(csv_folder_path, "*.csv")
    csv_files = glob.glob(csv_pattern)
elif os.path.isfile(csv_file_path):
    # 単一ファイルの場合：そのファイルを使用
    csv_files = [csv_file_path]

if not csv_files:
    sys.exit(1)

converted_files = []

for csv_file in csv_files:
    try:
        # ファイル名からシート名を抽出
        filename = os.path.basename(csv_file)
        
        # 「-表1-1-1.csv」「-表1-1.csv」「-表1.csv」パターンを除去してシート名を取得
        sheet_name = filename.replace(".csv", "")
        sheet_name = re.sub(r'-表\d+(-\d+)*$', '', sheet_name)
        
        # 単一ファイルの場合、ファイル名がfolder_nameと同じなら、folder_nameをシート名として使用
        if sheet_name == folder_name and len(csv_files) == 1:
            sheet_name = folder_name
        
        # CSVファイルを読み込み
        records = {
            "data": []
        }
        
        with open(csv_file, 'r', encoding='utf-8') as file:
            # CSVの最初の行をヘッダーとして読み込み
            csv_reader = csv.DictReader(file)
            
            for row in csv_reader:
                # 空の行をスキップ
                if not any(value.strip() for value in row.values() if value):
                    continue
                
                # 数値変換を試行
                converted_row = {}
                for key, value in row.items():
                    if value is None or value.strip() == '':
                        converted_row[key] = None
                    else:
                        # 数値変換を試行
                        try:
                            # 整数として変換を試行
                            if '.' not in value:
                                converted_row[key] = int(value)
                            else:
                                converted_row[key] = float(value)
                        except ValueError:
                            # 文字列として保持
                            converted_row[key] = value.strip()
                
                records["data"].append(converted_row)
        
        # JSONファイルとして出力
        output_path = os.path.join(json_folder, f"{sheet_name}.json")
        with open(output_path, "w", encoding="utf-8") as output_json:
            json.dump(records, output_json, ensure_ascii=False, indent=4)
        
        converted_files.append(csv_file)
        
    except Exception as e:
        pass

# 変換完了後、CSVファイル/フォルダをCSVディレクトリに移動
csv_destination_dir = os.path.join(script_dir, "CSV")
os.makedirs(csv_destination_dir, exist_ok=True)

if os.path.isdir(csv_folder_path):
    # フォルダの場合
    try:
        destination_path = os.path.join(csv_destination_dir, f"{folder_name}.csv")
        
        # 移動先に同名のフォルダが存在する場合は削除
        if os.path.exists(destination_path):
            shutil.rmtree(destination_path)
        
        # CSVフォルダを移動
        shutil.move(csv_folder_path, destination_path)
        
    except Exception as e:
        pass
elif os.path.isfile(csv_file_path):
    # 単一ファイルの場合
    try:
        destination_path = os.path.join(csv_destination_dir, f"{folder_name}.csv")
        
        # 移動先に同名のファイルが存在する場合は削除
        if os.path.exists(destination_path):
            if os.path.isfile(destination_path):
                os.remove(destination_path)
            else:
                shutil.rmtree(destination_path)
        
        # CSVファイルを移動
        shutil.move(csv_file_path, destination_path)
        
    except Exception as e:
        pass 