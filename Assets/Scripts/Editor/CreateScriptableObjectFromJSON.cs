using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Text;
using System.Reflection;
using System.Dynamic;


public class CreateScriptableObjectFromJSON<T> 
{
	private string outPutDir = Path.Combine("Assets", "Database", "JsonConvert");
	private string nameForSO = "_SO";

	// 出力ディレクトリを設定するメソッドを追加
	public void SetOutputDirectory(string directory)
	{
		outPutDir = directory;
	}

	public void CreateAssets(string jsonPath, string parentClassName, Type parentType)
	{
		dynamic jsonObj = JsonReaderToCreateInstance(jsonPath);

		string SOParentClassName = GetClassNameForSO(parentClassName, nameForSO);

		var ( parentFieldNames, parentFieldListNames, childClassNames) = GetFieldName(parentClassName);
		var (soParentFieldNames, soParentFieldListNames, soChildClassNames) = GetFieldName(SOParentClassName);

		if (parentFieldListNames.Count == 0)
        {
			dynamic sopObj = UseClassNameCreateInstance(SOParentClassName);
			SetFiledsData(sopObj, jsonObj);
			// _BASEサフィックスを除去してファイル名を生成
			string baseClassName = parentClassName;
			if (baseClassName.EndsWith("_BASE"))
			{
				baseClassName = baseClassName.Substring(0, baseClassName.Length - 5);
			}
            string filename =  baseClassName + ".asset";
			string path = Path.Combine(outPutDir, filename);
			CreateScriptableAsset(path, sopObj);
		}
        else
		{
			List<dynamic> listOfJsonObj = GetFieldsFromObj(jsonObj, true);

			int childCnt = 0;
			int fileNameCnt = 0;

			dynamic sopObj = UseClassNameCreateInstance(SOParentClassName);

			foreach (string listName in parentFieldListNames)
			{
				var listDatas = listOfJsonObj[childCnt];

				foreach (var data in listDatas)
				{
					dynamic socObj = UseClassNameCreateInstance(soChildClassNames[childCnt]);

					SetFiledsData(sopObj, jsonObj);

					SetFiledsData(socObj, data);

					SetListData(sopObj, socObj, listName);

					++fileNameCnt;
				}

				++childCnt;
			}

			// _BASEサフィックスを除去してファイル名を生成
			string baseClassName = parentClassName;
			if (baseClassName.EndsWith("_BASE"))
			{
				baseClassName = baseClassName.Substring(0, baseClassName.Length - 5);
			}
			string filename = baseClassName  + ".asset";
			string path = Path.Combine(outPutDir, filename);

			CreateScriptableAsset(path, sopObj);
		}
	}

	/// <param name="jsonPath"></param>
	/// <returns></returns>
	public dynamic JsonReaderToCreateInstance(string jsonPath)
    {
		string json = File.ReadAllText(jsonPath, Encoding.GetEncoding("utf-8"));

		dynamic jsonObj = JsonUtility.FromJson<T>(json);
		return jsonObj;
	}

	/// <param name="path"></param>
	/// <param name="data"></param>
	public void CreateScriptableAsset(string path, ScriptableObject data)
	{
		try
		{
			// ディレクトリが存在しない場合は作成
			string directory = Path.GetDirectoryName(path);
			if (!Directory.Exists(directory))
			{
				Directory.CreateDirectory(directory);
			}

			// ScriptableObjectの名前をファイル名と一致させる
			string fileName = Path.GetFileNameWithoutExtension(path);
			data.name = fileName;

			var asset = AssetDatabase.LoadAssetAtPath(path, typeof(ScriptableObject));
			if (asset == null)
			{
				// 新規作成
				AssetDatabase.CreateAsset(data, path);
			}
			else
			{
				// 既存ファイルを更新
				EditorUtility.CopySerialized(data, asset);
				EditorUtility.SetDirty(asset);
			}
			
			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}
		catch (System.Exception e)
		{
			UnityEngine.Debug.LogError($"Failed to create/update ScriptableObject at {path}: {e.Message}");
		}
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

	/// <param name="targetClassName"></param>
	/// <returns></returns>
	public (List<string>, List<string>, List<string>) GetFieldName(string targetClassName)
	{
		List<string> parentFieldNames = new List<string>();

		List<string> parentFieldListNames = new List<string>();

		List<string> childClassNames = new List<string>();

		Type type = GetTypeByClassName(targetClassName);
		// dynamic targetObj = Activator.CreateInstance(type);
		dynamic targetObj;
		if (typeof(ScriptableObject).IsAssignableFrom(type))
		{
			targetObj = ScriptableObject.CreateInstance(type);
		}
		else
		{
			targetObj = Activator.CreateInstance(type);
		}
		FieldInfo[] fields = targetObj.GetType().GetFields();
		string searchKey = "System.Collections.Generic.List";

		foreach (FieldInfo field in fields)
        {

			string fieldTypeStr = field.FieldType.ToString();

            if (!fieldTypeStr.Contains(searchKey))
            {

				parentFieldNames.Add(field.Name);
			}
            else
            {
				int sindex = fieldTypeStr.IndexOf("[");
				int eindex = fieldTypeStr.IndexOf("]");

				string genericsName = fieldTypeStr.Substring(sindex + 1, eindex - sindex - 1);
				childClassNames.Add(genericsName);
				parentFieldListNames.Add(field.Name);
			}
		}
		return (parentFieldNames, parentFieldListNames, childClassNames);

	}

	/// <param name="className"></param>
	/// <param name="key"></param>
	/// <returns></returns>
	public string GetClassNameForSO(string className, string key)
    {
		// _BASEサフィックスを除去してから_SOを追加
		string baseClassName = className;
		if (baseClassName.EndsWith("_BASE"))
		{
			baseClassName = baseClassName.Substring(0, baseClassName.Length - 5);
		}
		string combinName = baseClassName + key;
		return combinName;
	}

	/// <param name="className"></param>
	/// <returns></returns>
	public dynamic UseClassNameCreateInstance(string className)
    {
		Type type = GetTypeByClassName(className);
		// dynamic instance = Activator.CreateInstance(type);
		dynamic instance;
		if (typeof(ScriptableObject).IsAssignableFrom(type))
		{
			instance = ScriptableObject.CreateInstance(type);
		}
		else
		{
			instance = Activator.CreateInstance(type);
		}
		return instance;
    }

	/// <param name="genericTypeName"></param>
	/// <returns></returns>
	public dynamic UseGenericTypeCreateInstance(string genericTypeName)
    {
		Type type = GetTypeByClassName(genericTypeName);
		var genericType = typeof(CreateScriptableObjectFromJSON<>).MakeGenericType(type);
		dynamic obj = Activator.CreateInstance(genericType);
		return obj;
	}

	/// <param name="receiveObj">受け取り先オブジェクト</param>
	/// <param name="sendObj"> データ送り先オブジェクト</param>
	public void SetFiledsData( dynamic receiveObj, dynamic sendObj)
    {

		FieldInfo[] getFields = sendObj.GetType().GetFields();

		FieldInfo[] setFields = receiveObj.GetType().GetFields();
		string searchKey = "System.Collections.Generic.List";
		int fieldCnt = 0;

		foreach (FieldInfo field in setFields)
		{
			string fieldTypeStr = field.FieldType.ToString();
			string getfieldTypeStr = getFields[fieldCnt].FieldType.ToString();

            if (!fieldTypeStr.Contains(searchKey))
            {
                field.SetValue(receiveObj, getFields[fieldCnt].GetValue(sendObj));
            }
            ++fieldCnt;
		}
	}

	/// <param name="pObj"></param>
	/// <param name="cObj"></param>
	/// <param name="listName"></param>
	public void SetListData(dynamic pObj, dynamic cObj, string listName)
    {
		var field = pObj.GetType().GetField(listName);
		var instance = field.GetValue(pObj);
		instance.Add(cObj);
	}

	/// <param name="obj"></param>
	public dynamic GetFieldsFromObj(dynamic obj, bool isListGet)
	{
		FieldInfo[] infoFields = obj.GetType().GetFields();
		string searchKey = "System.Collections.Generic.List";

		List<dynamic> outPut = new List<dynamic>();

        if (infoFields.Length < 1)
        {
			return null;
        }

		foreach (FieldInfo field in infoFields)
		{

			string fieldTypeStr = field.FieldType.ToString();

			if (fieldTypeStr.Contains(searchKey))
			{
				if (isListGet)
				{
					outPut.Add(field.GetValue(obj));
				}
			}
			else
			{
				if (!isListGet)
				{
					outPut.Add(field.GetValue(obj));
				}
			}
		}
		return outPut;
	}

}