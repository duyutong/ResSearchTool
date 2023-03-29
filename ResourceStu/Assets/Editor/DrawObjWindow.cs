using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public class DrawObjWindow : EditorWindow
{
    private static string resPath;
    private static string prePath;
    private bool isCheckDepend = false;
    private bool isCheckFinish = false;
    private static string dataPath;
    private List<string> resGuidList = new List<string>();
    private List<string> prefabPathList = new List<string>();
    private Dictionary<string, List<string>> dependPrefabPathDic = new Dictionary<string, List<string>>();
    private List<string> checkResMD5 = new List<string>();
    private Dictionary<string,List<string>> repeatResPathDic = new Dictionary<string,List<string>>();
    private Dictionary<string, EditorFoldout> foldoutDic = new Dictionary<string, EditorFoldout>();
    private Vector2 scrollPos = Vector2.zero;

    //[MenuItem("Tool/打开资源查看器Test")]
    private static void DrawWindow()
    {
        dataPath = Application.dataPath.Replace("/", @"\") + @"\";
        Rect wr = new Rect(0, 0, 700, 300);
        DrawObjWindow window = (DrawObjWindow)GetWindowWithRect(typeof(DrawObjWindow), wr, true, "资源查看器");
        window.Focus();
        window.Show();
    }
    private void OnGUI()
    {
        EditorGUILayout.BeginVertical(); 
        //图片资源目录
        Rect resPathRect = new Rect(10, 5, 700, 20);
        resPath = EditorGUILayout.TextField("resPath/资源:", resPath);
        resPath = OnDrawElementAcceptDrop(resPathRect, resPath);
        //预制资源目录
        EditorGUI.BeginDisabledGroup(!isCheckDepend);
        Rect prePathRect = new Rect(10, 30, 700, 20);
        prePath = EditorGUILayout.TextField("prePath/资源:", prePath);
        prePath = OnDrawElementAcceptDrop(prePathRect, prePath);
        EditorGUI.EndDisabledGroup();
        //是否进行依赖对比
        isCheckDepend = EditorGUILayout.ToggleLeft("依赖对比", isCheckDepend);
        //查找按钮
        if (GUILayout.Button("开始查找"))
        {

            Debug.Log("开始查找");
            isCheckFinish = false;
            //EditorUtility.DisplayProgressBar("查找重复资源", "正在查找...", progressCur * 100/progressMax * 0.1f);
            //重复资源展示
            CheckRepeatRes();
            //查找对资源有依赖的预制体
            CheckDependPrefab();
        }
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        //展示给定范围内对该资源有依赖的预制体
        if (isCheckDepend && isCheckFinish) 
        {
            foreach (KeyValuePair<string, List<string>> keyValuePair in dependPrefabPathDic) 
            {
                string resGuid = keyValuePair.Key;
                string resPath = AssetDatabase.GUIDToAssetPath(resGuid);
                string shortPath = "Assets" + @"\" + resPath.Replace(dataPath, "");
                shortPath = shortPath.Replace(@"\\", "/");
                if (!foldoutDic.ContainsKey(resGuid))
                {
                    EditorFoldout fo = new EditorFoldout()
                    {
                        key = shortPath,
                        showItemPath = keyValuePair.Value
                    };
                    foldoutDic.Add(resGuid, fo);
                }
                EditorFoldout foldout = foldoutDic[resGuid];
                foldout.DrawSelf(typeof(GameObject));
            }
            
        }
        //展示给定范围内重复的图片资源
        if (!isCheckDepend && isCheckFinish)
        {
            foreach (KeyValuePair<string, List<string>> keyValuePair in repeatResPathDic) 
            {
                string md5Key = keyValuePair.Key;
                if (!foldoutDic.ContainsKey(md5Key)) 
                {
                    EditorFoldout fo = new EditorFoldout() 
                    {
                        key = md5Key,
                        showItemPath = keyValuePair.Value
                    };
                    foldoutDic.Add(md5Key, fo);
                }
                EditorFoldout foldout = foldoutDic[md5Key];
                foldout.DrawSelf(typeof(Texture));
            }
        }
        EditorGUILayout.EndScrollView();
        EditorGUILayout.EndVertical();
    }

    private async void CheckDependPrefab()
    {
        foldoutDic.Clear();
        resGuidList.Clear();
        prefabPathList.Clear();
        dependPrefabPathDic.Clear();
        if (!isCheckDepend) return;
        CheckRes(resPath, ".png", (_path) => CheckGUID(_path));
        await Task.Run(() => 
        {
            CheckRes(prePath, ".prefab", (_path) => { prefabPathList.Add(_path); });
            GetDependPrefab();
            isCheckFinish = true;
            Debug.Log("查找完成！");
        });
    }
    private void CheckGUID(string filePath) 
    {
        dataPath = Application.dataPath.Replace("/", @"\") + @"\";
        string shortPath = "Assets" + @"\" + filePath.Replace(dataPath, "");
        string checkPath = shortPath.Replace(@"\", "/");
        string resGuid = AssetDatabase.AssetPathToGUID(checkPath);
        if (string.IsNullOrEmpty(resGuid)) return;
        resGuidList.Add(resGuid);
    }
    private void CheckDepend(string resGuid) 
    {
        if (!dependPrefabPathDic.ContainsKey(resGuid)) dependPrefabPathDic.Add(resGuid,new List<string>());
        foreach (string prefabPath in prefabPathList) 
        {
            string fileStrInfo = File.ReadAllText(prefabPath);
            if (fileStrInfo.IndexOf(resGuid) > 0) dependPrefabPathDic[resGuid].Add(prefabPath);
        }
    }
    private async void CheckRepeatRes()
    {
        checkResMD5.Clear();
        repeatResPathDic.Clear();
        foldoutDic.Clear();
        if (isCheckDepend) return;
        await Task.Run(() => 
        {
            CheckRes(resPath,".png",(_path)=>CheckMD5(_path)); 
            GetRepeatRes();
            isCheckFinish = true;
            Debug.Log("查找完成！");
        });
    }
    private void GetDependPrefab()
    {
        foreach (string _guid in resGuidList) CheckDepend(_guid);
        List<string> removeGuid = new List<string>();
        foreach (KeyValuePair<string, List<string>> keyValuePair in dependPrefabPathDic)
        {
            if (keyValuePair.Value.Count > 0) continue;
            removeGuid.Add(keyValuePair.Key);
        }
        foreach (string remove in removeGuid) dependPrefabPathDic.Remove(remove);
    }
    private void GetRepeatRes()
    {
        List<string> removeMD5 = new List<string>();
        foreach (KeyValuePair<string, List<string>> keyValuePair in repeatResPathDic)
        {
            if (keyValuePair.Value.Count > 1) continue;
            removeMD5.Add(keyValuePair.Key);
        }
        foreach (string remove in removeMD5) repeatResPathDic.Remove(remove);
    }

    private void CheckRes(string path,string extension, Action<string> action = null)
    {
        if (string.IsNullOrEmpty(path)) return;
        if (File.Exists(path)) action?.Invoke(path);
        else
        {
            string[] vs = Directory.GetDirectories(path);
            foreach (string v in vs) { CheckRes(v, extension, action); }
            DirectoryInfo directory = Directory.CreateDirectory(path);
            FileInfo[] fileInfos = directory.GetFiles();
            foreach (FileInfo info in fileInfos)
            {
                if (string.IsNullOrEmpty(info.FullName)) continue;
                if (info.Extension != extension) continue;
                action?.Invoke(info.FullName);
            }
        }
    }
    private void CheckMD5(string filePath) 
    {
        string md5 = GetMD5(filePath);
        if (checkResMD5.Contains(md5)) repeatResPathDic[md5].Add(filePath);
        else
        {
            checkResMD5.Add(md5);
            repeatResPathDic.Add(md5, new List<string>() { filePath });
        }
    }
    private string OnDrawElementAcceptDrop(Rect rect, string label)
    {
        if (!rect.Contains(Event.current.mousePosition)) return label;
        if (DragAndDrop.paths == null || DragAndDrop.paths.Length == 0) return label;
        if (!string.IsNullOrEmpty(DragAndDrop.paths[0]))
        {
            if (Event.current.type == EventType.DragUpdated || Event.current.type == EventType.DragPerform)
                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            if (Event.current.type == EventType.DragPerform)
            {
                DragAndDrop.AcceptDrag();
                GUI.changed = true;
                label = DragAndDrop.paths[0];
            }
        }
        return label;
    }
    private static string GetMD5(string filePath)
    {
        using (FileStream file = File.OpenRead(filePath))
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] bytes = md5.ComputeHash(file);
            file.Close();
            StringBuilder stringBuilder = new StringBuilder();
            foreach (byte b in bytes) stringBuilder.Append(b.ToString("x2"));

            return stringBuilder.ToString();
        }
    }
}
