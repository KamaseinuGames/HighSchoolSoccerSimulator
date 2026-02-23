using UnityEngine;
using UnityEditor;
using System.Collections;
using System.IO;
using System;
using System.Reflection;

public class MyMenuItems
{
    static private string dirPath = Path.Combine("Assets", "Scripts", "SpreadSheet", "Json");
    static private string defaultOutputPath = Path.Combine("Assets", "Database", "JsonConvert");

    [MenuItem("MyTools/BASE/JsonPackedから_SOクラスを生成 %g")]
    private static void GenerateSOClassesFromBASE()
    {
        try
        {
            GenerateSOFile.GenerateAllSOClasses();
        }
        catch (System.Exception e)
        {
            UnityEngine.Debug.LogError($"Error generating SO classes: {e.Message}\n{e.StackTrace}");
        }
    }

    [MenuItem("MyTools/JsonConvertにSOを生成 %h")]
    private static void CreateAllScriptableAssets()
    {
        CreateAllScriptableAssetsToDirectory(defaultOutputPath);
    }

    [MenuItem("MyTools/ディレクトリを指定してSOを生成")]
    private static void CreateAllScriptableAssetsToCustomDirectory()
    {
        string selectedPath = EditorUtility.OpenFolderPanel("Select Output Directory", "Assets", "");
        
        if (string.IsNullOrEmpty(selectedPath))
        {
            UnityEngine.Debug.Log("Operation cancelled.");
            return;
        }

        // Unityプロジェクト内のパスに変換
        string relativePath = GetRelativePath(selectedPath);
        if (string.IsNullOrEmpty(relativePath))
        {
            UnityEngine.Debug.LogError("Selected directory must be within the Unity project.");
            return;
        }

        CreateAllScriptableAssetsToDirectory(relativePath);
    }

    private static void CreateAllScriptableAssetsToDirectory(string outputDirectory)
    {
        if (!Directory.Exists(dirPath))
        {
            UnityEngine.Debug.LogError($"JSON directory not found: {dirPath}");
            return;
        }

        string[] jsonFilePaths = Directory.GetFiles(dirPath, "*.json");

        if (jsonFilePaths.Length == 0)
        {
            UnityEngine.Debug.LogWarning($"No JSON files found in: {dirPath}");
            return;
        }

        UnityEngine.Debug.Log($"Found {jsonFilePaths.Length} JSON files. Creating ScriptableObjects in: {outputDirectory}");

        int successCount = 0;
        int errorCount = 0;

        foreach (string path in jsonFilePaths)
        {
            try
            {
                string jsonFileName = Path.GetFileNameWithoutExtension(path);
                // JSONファイル名に_BASEサフィックスを追加して基底クラス名を作成
                string parentClassName = jsonFileName + "_BASE";

                Type type = GetTypeByClassName(parentClassName);
                if (type == null)
                {
                    UnityEngine.Debug.LogError($"Type not found for: {parentClassName}");
                    errorCount++;
                    continue;
                }

                var genericType = typeof(CreateScriptableObjectFromJSON<>).MakeGenericType(type);
                dynamic obj = Activator.CreateInstance(genericType);
                
                // 出力ディレクトリを設定
                obj.SetOutputDirectory(outputDirectory);
                
                // JSONファイル名（_BASEなし）をScriptableObject名として使用
                obj.CreateAssets(path, parentClassName, type);
                successCount++;
            }
            catch (System.Exception e)
            {
                UnityEngine.Debug.LogError($"Error processing {path}: {e.Message}");
                errorCount++;
            }
        }

        UnityEngine.Debug.Log($"ScriptableObject creation completed. Success: {successCount}, Errors: {errorCount}");
    }

    /// <param name="className"></param>
    /// <returns></returns>
    public static Type GetTypeByClassName(string className)
    {
        foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
        {
            foreach (Type type in assembly.GetTypes())
            {
                if (type.Name == className || type.ToString() == className)
                {
                    return type;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// 絶対パスをUnityプロジェクト内の相対パスに変換
    /// </summary>
    /// <param name="absolutePath"></param>
    /// <returns></returns>
    private static string GetRelativePath(string absolutePath)
    {
        string projectPath = Application.dataPath;
        projectPath = projectPath.Substring(0, projectPath.Length - 6); // "Assets"を除去

        if (absolutePath.StartsWith(projectPath))
        {
            string relativePath = absolutePath.Substring(projectPath.Length);
            if (relativePath.StartsWith("/") || relativePath.StartsWith("\\"))
            {
                relativePath = relativePath.Substring(1);
            }
            return relativePath.Replace("\\", "/");
        }

        return null;
    }
}