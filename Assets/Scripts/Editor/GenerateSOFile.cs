using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// JsonConvert_BASEとJsonPackedフォルダ内のクラスから、対応する_SOクラスを自動生成する
/// </summary>
public class GenerateSOFile
{
    private static string jsonPackedDir = Path.Combine("Assets", "Scripts", "Class", "JsonPacked");
    private static string jsonPackedSODir = Path.Combine("Assets", "Scripts", "Class", "JsonPacked_SO");
    private static string jsonConvertBaseDir = Path.Combine("Assets", "Scripts", "Class", "JsonConvert_BASE");
    private static string jsonConvertSODir = Path.Combine("Assets", "Scripts", "Class", "JsonConvert_SO");

    /// <summary>
    /// 全てのBASE/JsonPackedクラスから対応するSOクラスを生成
    /// </summary>
    public static void GenerateAllSOClasses()
    {
        int generatedCount = 0;
        int skippedCount = 0;

        // JsonPackedフォルダからJsonPacked_SOを生成
        UnityEngine.Debug.Log("=== Generating JsonPacked_SO classes ===");
        GenerateSOClassesForFolder(jsonPackedDir, jsonPackedSODir, false, ref generatedCount, ref skippedCount);

        // JsonConvert_BASEフォルダからJsonConvert_SOを生成
        UnityEngine.Debug.Log("=== Generating JsonConvert_SO classes ===");
        GenerateSOClassesForFolder(jsonConvertBaseDir, jsonConvertSODir, true, ref generatedCount, ref skippedCount);

        AssetDatabase.Refresh();
        UnityEngine.Debug.Log($"SO class generation completed. Generated: {generatedCount}, Skipped: {skippedCount}");
    }

    /// <summary>
    /// 指定フォルダ内のクラスから対応するSOクラスを生成
    /// </summary>
    private static void GenerateSOClassesForFolder(string sourceDir, string targetDir, bool isScriptableObject, ref int generatedCount, ref int skippedCount)
    {
        if (!Directory.Exists(sourceDir))
        {
            UnityEngine.Debug.LogWarning($"Source directory not found: {sourceDir}");
            return;
        }

        // ディレクトリが存在しない場合は作成
        if (!Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        // ソースフォルダ内の.csファイルを取得
        string[] csFiles = Directory.GetFiles(sourceDir, "*.cs");

        foreach (string csFile in csFiles)
        {
            string fileName = Path.GetFileNameWithoutExtension(csFile);
            
            // _BASEサフィックスを除去
            string baseClassName = fileName;
            if (baseClassName.EndsWith("_BASE"))
            {
                baseClassName = baseClassName.Substring(0, baseClassName.Length - 5);
            }

            // SOクラス名とファイルパスを決定
            string soClassName = baseClassName + "_SO";
            string soFilePath = Path.Combine(targetDir, soClassName + ".cs");

            // 既に存在する場合はスキップ
            if (File.Exists(soFilePath))
            {
                UnityEngine.Debug.Log($"Skipped (already exists): {soClassName}.cs");
                skippedCount++;
                continue;
            }

            // SOクラスを生成
            if (isScriptableObject)
            {
                GenerateScriptableObjectSOClass(soClassName, baseClassName, soFilePath);
            }
            else
            {
                GenerateSimpleSOClass(soClassName, fileName, soFilePath);
            }

            generatedCount++;
            UnityEngine.Debug.Log($"Generated: {soClassName}.cs");
        }
    }

    /// <summary>
    /// ScriptableObjectを継承するSOクラスを生成（JsonConvert_SO用）
    /// </summary>
    private static void GenerateScriptableObjectSOClass(string soClassName, string baseClassName, string filePath)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("using System.Collections;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using System;");
        sb.AppendLine();
        sb.AppendLine($"[CreateAssetMenu(menuName = \"MyScriptable/Create {soClassName}\")]");
        sb.AppendLine($"public class {soClassName} : ScriptableObject");
        sb.AppendLine("{");
        sb.AppendLine($"    // ここに{baseClassName}_BASEと同じフィールドを追加してください");
        sb.AppendLine($"    // または{baseClassName}_BASEを継承するように変更してください");
        sb.AppendLine("} ");

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }

    /// <summary>
    /// 基底クラスを継承する単純なSOクラスを生成（JsonPacked_SO用）
    /// </summary>
    private static void GenerateSimpleSOClass(string soClassName, string baseClassName, string filePath)
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("using System.Collections;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using UnityEngine;");
        sb.AppendLine("using System;");
        sb.AppendLine();
        sb.AppendLine("[Serializable]");
        sb.AppendLine($"public class {soClassName} : {baseClassName} {{ }}");

        File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
    }
}
