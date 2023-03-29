using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using UnityEditor;
using UnityEngine;

public class CheckRepeatRes : SubEditorWindow
{
    private static string resPath = "Assets/resdata/texture";
    private bool isCheckFinish = false;
    private int allFileCount = 0;
    private int finishFileCount = 0;
    private List<string> checkResMD5 = new List<string>();
    private Dictionary<string, List<string>> repeatResPathDic = new Dictionary<string, List<string>>();
    private Dictionary<string, EditorFoldout> foldoutDic = new Dictionary<string, EditorFoldout>();
    private Vector2 scrollPos = Vector2.zero;
    private Rect resPathRect = new Rect(10, 55, 700, 20);
    private string progressTitle = "正在查找重复资源";
    private string progressInfo = "开始查找";

    private string foleKeyFormat = "[{0}] - md5:{1}";
    public override void DrawSelf()
    {
        GUILayout.Label("--------查找重复资源", GUILayout.Height(30));
        EditorGUILayout.BeginHorizontal();
        resPath = EditorGUILayout.TextField("resPath/资源:", resPath);
        resPath = OnDrawElementAcceptDrop(resPathRect, resPath);
        if (GUILayout.Button("开始查找", GUILayout.Width(100)))
        {
            OnCheck();
        }
        if (!isCheckFinish && allFileCount > 0 && allFileCount > finishFileCount)
        {
            EditorUtility.DisplayProgressBar(progressTitle, progressInfo.ToString(), finishFileCount * 100 / allFileCount * 0.01f);
        }
        EditorGUILayout.EndHorizontal();

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
        if (isCheckFinish)
        {
            EditorUtility.ClearProgressBar();
            foreach (KeyValuePair<string, List<string>> keyValuePair in repeatResPathDic)
            {
                string md5Key = keyValuePair.Key;
                if (!foldoutDic.ContainsKey(md5Key))
                {
                    EditorFoldout fo = new EditorFoldout()
                    {
                        key = string.Format(foleKeyFormat, keyValuePair.Value.Count, md5Key),
                        showItemPath = keyValuePair.Value,
                        isShowSelect = true,
                        btns = new List<FoldoutBtn>()
                        {
                            new FoldoutBtn()
                            {
                                btnName = "删除资源",
                                OnClick = DelAllRes,
                            },
                            new FoldoutBtn()
                            {
                                btnName = "查找依赖",
                                OnClick = CheckDepend,
                            },

                        }
                    };
                    foldoutDic.Add(md5Key, fo);
                }
                EditorFoldout foldout = foldoutDic[md5Key];
                foldout.DrawSelf(typeof(Texture));
            }
        }
        EditorGUILayout.EndScrollView();
    }
    private void CheckDepend(List<string> _paths)
    {
        ShowDepenWindow.DrawWindow(_paths);
    }
    private void DelAllRes(List<string> _paths)
    {
        if (EditorUtility.DisplayDialog("删除确认", "是否删除该栏目下所有选中资源", "删除", "取消"))
        {
            isCheckFinish = false;
            string dataPath = Application.dataPath.Replace("/", @"\") + @"\";
            foreach (string _path in _paths)
            {
                string shortPath = "Assets" + @"\" + _path.Replace(dataPath, "");
                shortPath = shortPath.Replace(@"\\", "/");
                AssetDatabase.DeleteAsset(shortPath);
            }
            OnCheck();
        }
    }
    private async void OnCheck()
    {
        checkResMD5.Clear();
        repeatResPathDic.Clear();
        foldoutDic.Clear();

        await Task.Run(() =>
        {
            allFileCount = 0;
            CheckRes(resPath, ".png", (_path) => allFileCount++);

            isCheckFinish = false;
            CheckRes(resPath, ".png", (_path) => CheckMD5(_path));
            GetRepeatRes();
            allFileCount = 0;
            finishFileCount = 0;
            isCheckFinish = true;
        });
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
        progressInfo = filePath;
        finishFileCount++;
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
}
