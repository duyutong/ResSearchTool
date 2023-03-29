using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public class ShowDepenWindow : SubEditorWindow
{
    private static string prePath = "Assets/resdata/prefab/uiprefab";
    private static string comPath = "Assets/resdata/texture/commond";
    private static string _comPath = "Assets/resdata/texture/commond";
    private static string dataPath;
    private static List<string> resPaths = new List<string>();
    private static string mergeResGuid;
    private static string mergeResPath;
    private static bool isMergResExists = false;
    private static Texture mergRes;
    private static int mergCopyCount = 0;

    private bool isCheckFinish = false;
    private int allFileCount = 0;
    private int finishFileCount = 0;
    private List<string> resGuidList = new List<string>();
    private List<string> prefabPathList = new List<string>();
    private Dictionary<string, List<string>> dependPrefabPathDic = new Dictionary<string, List<string>>();
    private Dictionary<string, EditorFoldout> foldoutDic = new Dictionary<string, EditorFoldout>();
    private Vector2 scrollPos = Vector2.zero;
    private Rect prePathRect = new Rect(10, 30, 700, 20);
    private Rect comPathRect = new Rect(10, 50, 700, 20);
    private string progressTitle = "正在查找依赖";
    private string progressInfo = "开始查找";
    private bool isMerg = false;

    //private Regex spriteRegex = new Regex(@"m_Sprite: {fileID: 21300000, guid: ([^>\n\s]+), type: 3}");
    public static void DrawWindow(List<string> _paths)
    {
        mergRes = null;
        mergCopyCount = 0;
        resPaths = _paths;

        dataPath = Application.dataPath.Replace("/", @"\") + @"\";
        CopyTempMergRes();

        Rect wr = new Rect(0, 0, 700, 200);
        ShowDepenWindow window = (ShowDepenWindow)GetWindowWithRect(typeof(ShowDepenWindow), wr, true, "依赖关系");
        window.Focus();
        window.Show();
    }
    private void OnDestroy()
    {
        if (!isMerg && !isMergResExists) DelMergRes();
        mergRes = null;
        resPaths.Clear();
        AssetDatabase.Refresh();
    }
    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        #region 显示图片/该图片为合并的预置图片
        if (mergCopyCount == 2 && mergRes == null) { LoadMergResInWindow(); mergCopyCount = 0; }
        mergRes = EditorGUILayout.ObjectField(mergRes, typeof(Texture), false, GUILayout.Width(60), GUILayout.Height(70)) as Texture;
        #endregion
        EditorGUILayout.BeginVertical();

        GUILayout.Label("处理依赖/合并资源", GUILayout.Height(30));

        EditorGUILayout.BeginHorizontal();
        #region prePath/预制做文件
        prePath = EditorGUILayout.TextField("prePath/预制做文件:", prePath);
        prePath = OnDrawElementAcceptDrop(prePathRect, prePath);
        #endregion
        #region 开始查找
        if (GUILayout.Button("开始查找", GUILayout.Width(100)))
        {
            OnCheck();
        }
        #endregion
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        #region comPath/公共资源文件夹
        comPath = EditorGUILayout.TextField("comPath/公共资源文件夹:", comPath);
        comPath = OnDrawElementAcceptDrop(comPathRect, comPath);
        #endregion
        #region 处理中途改变公共资源目录的情况
        if (comPath != _comPath && Directory.Exists(comPath))
        {
            isCheckFinish = false;
            if (!isMergResExists) DelMergRes();
            CopyTempMergRes();
            _comPath = comPath;
            mergRes = null;
            AssetDatabase.Refresh();
        }
        #endregion
        #region 是否合并资源
        if (GUILayout.Button("是否合并资源", GUILayout.Width(100)))
        {
            OnMerge();
        }
        #endregion
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndHorizontal();

        #region 展示进度条
        if (!isCheckFinish && allFileCount > 0 && allFileCount > finishFileCount)
        {
            EditorUtility.DisplayProgressBar(progressTitle, progressInfo.ToString(), finishFileCount * 100 / allFileCount * 0.01f);
        }
        #endregion

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        #region 加载依赖预制体对象
        if (isCheckFinish)
        {
            EditorUtility.ClearProgressBar();
            foreach (KeyValuePair<string, List<string>> keyValuePair in dependPrefabPathDic)
            {
                string resGuid = keyValuePair.Key;
                string resPath = AssetDatabase.GUIDToAssetPath(resGuid);
                string shortPath = "Assets" + @"\" + resPath.Replace(dataPath, "");
                shortPath = shortPath.Replace(@"\", "/");
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
        #endregion
        EditorGUILayout.EndScrollView();
    }

    private async void OnMerge()
    {
        if (EditorUtility.DisplayDialog("合并确认", "合并资源并将资源迁移到公共目录", "合并", "取消"))
        {
            isMerg = true;
            #region 获取依赖预制体对象的集合
            if (dependPrefabPathDic.Count == 0)
            {
                foldoutDic.Clear();
                resGuidList.Clear();
                prefabPathList.Clear();
                dependPrefabPathDic.Clear();

                foreach (string _resPath in resPaths) CheckGUID(_resPath);
                await Task.Run(() =>
                {
                    allFileCount = 0;
                    int resNum = resGuidList.Count;
                    int prefabNum = 0;
                    CheckRes(prePath, ".prefab", (_path) => prefabNum++);
                    allFileCount = resNum * prefabNum;

                    isCheckFinish = false;
                    CheckRes(prePath, ".prefab", (_path) => { prefabPathList.Add(_path); });

                    foreach (string _guid in resGuidList)
                    {
                        if (!dependPrefabPathDic.ContainsKey(_guid)) dependPrefabPathDic.Add(_guid, new List<string>());
                        foreach (string prefabPath in prefabPathList)
                        {
                            string fileStrInfo = File.ReadAllText(prefabPath);
                            if (fileStrInfo.IndexOf(_guid) > 0) dependPrefabPathDic[_guid].Add(prefabPath);
                            finishFileCount++;
                            progressInfo = prefabPath;
                        }
                    }
                    List<string> removeGuid = new List<string>();
                    foreach (KeyValuePair<string, List<string>> keyValuePair in dependPrefabPathDic)
                    {
                        if (keyValuePair.Value.Count > 0) continue;
                        removeGuid.Add(keyValuePair.Key);
                    }
                    foreach (string remove in removeGuid) dependPrefabPathDic.Remove(remove);

                    allFileCount = 0;
                    finishFileCount = 0;
                });
            }
            #endregion

            #region 开始合并
            await Task.Run(() =>
            {
                isCheckFinish = false;
                
                foldoutDic.Clear();
                resGuidList.Clear();
                prefabPathList.Clear();
                if (!string.IsNullOrEmpty(mergeResGuid))
                {
                    Dictionary<string, string> newFilePath = new Dictionary<string, string>();
                    foreach (KeyValuePair<string, List<string>> keyValuePair in dependPrefabPathDic)
                    {
                        string oldeGuid = keyValuePair.Key;
                        if (oldeGuid != mergeResGuid)
                        {
                            List<string> prefabPaths = keyValuePair.Value;
                            allFileCount = prefabPaths.Count;
                            foreach (string _path in prefabPaths)
                            {
                                string newFileStrInfo = File.ReadAllText(_path);
                                newFileStrInfo = newFileStrInfo.Replace(oldeGuid, mergeResGuid);
                                newFilePath.Add(_path, newFileStrInfo);

                                if (File.Exists(_path)) File.Delete(_path);
                                finishFileCount++;
                            }
                        }
                    }
                    allFileCount = 0;
                    finishFileCount = 0;

                    allFileCount = newFilePath.Count;
                    foreach (KeyValuePair<string, string> keyValuePair in newFilePath)
                    {
                        string _path = keyValuePair.Key;
                        string _info = keyValuePair.Value;
                        
                        using (FileStream file = File.Create(_path))
                        {
                            byte[] bytes = Encoding.UTF8.GetBytes(_info);
                            file.Write(bytes, 0, bytes.Length);
                            file.Flush();
                            file.Close();
                        }
                        finishFileCount++;
                    }
                    allFileCount = 0;
                    finishFileCount = 0;

                    allFileCount = resPaths.Count;
                    foreach (string _path in resPaths)
                    {
                        if (File.Exists(_path))
                        {
                            string _filePath = _path.Replace(@"\", "/");
                            if (!_filePath.Contains(mergeResPath))
                            {
                                string matePath = _path.Replace(".png", ".png.mate");
                                File.Delete(_path);
                                File.Delete(matePath);
                            }
                        }
                        finishFileCount++;
                    }
                }
                allFileCount = 0;
                finishFileCount = 0;
            });
            #endregion
        }
    }
    private static void CopyTempMergRes()
    {
        int mergSourceIndex = 0;
        for (int i = 0; i < resPaths.Count; i++)
        {
            int _index = i;
            string _path = resPaths[_index];
            string _filePath = _path.Replace(@"\", "/");
            if (_filePath.Contains(_comPath)) { mergSourceIndex = _index; break; }
        }

        string filePath = resPaths[mergSourceIndex].Replace(@"\", "/");
        string metaPath = filePath.Replace(".png", ".png.meta");
        string[] pathInfo = filePath.Split('/');
        string fileName = pathInfo[pathInfo.Length - 1];
        string mateName = fileName.Replace(".png", ".png.meta");
        string checkPath = _comPath + "/" + fileName;
        mergeResPath = checkPath;
        mergeResGuid = AssetDatabase.AssetPathToGUID(mergeResPath);

        CopyMergFile(filePath, _comPath + "/" + fileName);
        CopyMergFile(metaPath, _comPath + "/" + mateName);
    }
    private async static void CopyMergFile(string sourcePath, string targetPath)
    {
        if (File.Exists(targetPath))
        {
            isMergResExists = true;
            mergCopyCount = 2;
            return;
        }
        else isMergResExists = false;
        await Task.Run(() =>
        {
            try
            {
                FileStream readFile = File.OpenRead(sourcePath);
                FileStream writFile = File.Create(targetPath);
                byte[] btyes = new byte[2048];
                //依次读取文件信息，每次最多读2048个字节
                int contentLength = readFile.Read(btyes, 0, btyes.Length);
                while (contentLength != 0)
                {
                    //循环的写入流对象
                    writFile.Write(btyes, 0, contentLength);
                    contentLength = readFile.Read(btyes, 0, btyes.Length);
                }
                readFile.Close();
                writFile.Close();
                mergCopyCount++;
            }
            catch (Exception ex)
            {
                Debug.LogError(ex.Message);
            }
        });
    }
    private static void LoadMergResInWindow()
    {
        AssetDatabase.Refresh();
        string loadPath = mergeResPath.Replace("/", @"\");
        mergRes = AssetDatabase.LoadAssetAtPath(loadPath, typeof(Texture)) as Texture;
    }

    private void DelMergRes()
    {
        string filePath = resPaths[0].Replace(@"\", "/");
        string[] pathInfo = filePath.Split('/');
        string fileName = pathInfo[pathInfo.Length - 1];
        string mateName = fileName.Replace(".png", ".png.meta");

        string delFilePath = _comPath + "/" + fileName;
        string delMatePath = _comPath + "/" + mateName;

        if (File.Exists(delFilePath)) File.Delete(delFilePath);
        if (File.Exists(delMatePath)) File.Delete(delMatePath);
        isMerg = false;
        mergRes = null;
    }


    private async void OnCheck()
    {
        foldoutDic.Clear();
        resGuidList.Clear();
        prefabPathList.Clear();
        dependPrefabPathDic.Clear();

        foreach (string _resPath in resPaths) CheckGUID(_resPath);
        await Task.Run(() =>
        {
            allFileCount = 0;
            int resNum = resGuidList.Count;
            int prefabNum = 0;
            CheckRes(prePath, ".prefab", (_path) => prefabNum++);
            allFileCount = resNum * prefabNum;

            isCheckFinish = false;
            CheckRes(prePath, ".prefab", (_path) => { prefabPathList.Add(_path); });

            foreach (string _guid in resGuidList)
            {
                if (!dependPrefabPathDic.ContainsKey(_guid)) dependPrefabPathDic.Add(_guid, new List<string>());
                foreach (string prefabPath in prefabPathList)
                {
                    string fileStrInfo = File.ReadAllText(prefabPath);
                    if (fileStrInfo.IndexOf(_guid) > 0) dependPrefabPathDic[_guid].Add(prefabPath);
                    finishFileCount++;
                    progressInfo = prefabPath;
                }
            }
            List<string> removeGuid = new List<string>();
            foreach (KeyValuePair<string, List<string>> keyValuePair in dependPrefabPathDic)
            {
                if (keyValuePair.Value.Count > 0) continue;
                removeGuid.Add(keyValuePair.Key);
            }
            foreach (string remove in removeGuid) dependPrefabPathDic.Remove(remove);

            allFileCount = 0;
            finishFileCount = 0;
            isCheckFinish = true;
        });
    }
    private void CheckGUID(string filePath)
    {
        string shortPath = "Assets" + @"\" + filePath.Replace(dataPath, "");
        string checkPath = shortPath.Replace(@"\", "/");
        string resGuid = AssetDatabase.AssetPathToGUID(checkPath);
        if (string.IsNullOrEmpty(resGuid)) return;
        resGuidList.Add(resGuid);
    }
}
