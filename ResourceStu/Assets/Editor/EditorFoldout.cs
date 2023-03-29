using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class EditorFoldout
{
    public string key;
    public bool foldout = false;
    public bool isShowSelect = false;
    public List<FoldoutBtn> btns = new List<FoldoutBtn>();

    public List<string> _showItemPath = new List<string>();
    private Dictionary<int,bool> selectDic = new Dictionary<int,bool>();
    private List<Object> itemList = new List<Object>();

    public List<string> showItemPath
    {
        set
        {
            _showItemPath = value;
            selectDic.Clear();
            for (int i = 0; i < _showItemPath.Count; i++) selectDic.Add(i, true);
        }
        get { return _showItemPath; }
    }
    public void DrawSelf(Type objType)
    {
        EditorGUILayout.BeginHorizontal();
        foldout = EditorGUILayout.Foldout(foldout, key);
        if (btns.Count > 0)
        {
            foreach (FoldoutBtn _btn in btns)
            {
                if (!string.IsNullOrEmpty(_btn.btnName) && _btn.OnClick != null)
                {
                    if (GUILayout.Button(_btn.btnName, GUILayout.Width(100)))
                    {
                        if (isShowSelect)
                        {
                            List<string> pathPara = new List<string>();
                            foreach (KeyValuePair<int, bool> keyValuePair in selectDic)
                            {
                                if (!keyValuePair.Value) continue;
                                pathPara.Add(showItemPath[keyValuePair.Key]);
                            }
                            _btn.OnClick?.Invoke(pathPara);
                        }
                        else
                        {
                            _btn.OnClick?.Invoke(showItemPath);
                        }
                    }
                }
            }
        }
        EditorGUILayout.EndHorizontal();
        if (foldout)
        {
            string dataPath = Application.dataPath.Replace("/", @"\") + @"\";
            for (int i = 0; i < showItemPath.Count; i++)
            {
                int index = i;
                string _path = showItemPath[index];
                string shortPath = "Assets" + @"\" + _path.Replace(dataPath, "");
                if (itemList.Count <= index) itemList.Add(AssetDatabase.LoadAssetAtPath(shortPath, objType));
                EditorGUILayout.BeginHorizontal();
                itemList[index] = EditorGUILayout.ObjectField(itemList[index], objType, false, GUILayout.Width(500));
                if (isShowSelect) selectDic[index] = GUILayout.Toggle(selectDic[index], new GUIContent());
                EditorGUILayout.EndHorizontal();
            }
        }
    }
}
public class FoldoutBtn 
{
    public string btnName;
    public Action<List<string>> OnClick;
}
