using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class ResSearchTool : EditorWindow
{
    private static Dictionary<string, SubEditorWindow> windowDic = new Dictionary<string, SubEditorWindow>();
    private static string[] titleList;
    private static int currIndex = 0;
    [MenuItem("Tool/打开资源查看器")]
    private static void DrawWindow() 
    {
        windowDic.Clear();
        windowDic.Add("查找重复资源", new CheckRepeatRes());
        windowDic.Add("查找依赖", new CheckDepend());

        titleList = new string[windowDic.Count];
        int _index = 0;
        foreach (KeyValuePair<string, SubEditorWindow> keyValuePair in windowDic) 
        {
            titleList[_index] = keyValuePair.Key;
            _index++;
        }

        Rect wr = new Rect(0, 0, 700, 300);
        ResSearchTool window = (ResSearchTool)GetWindowWithRect(typeof(ResSearchTool), wr, true, "资源查看器x");
        window.Focus();
        window.Show();
    }
    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        currIndex = GUILayout.SelectionGrid(currIndex, titleList, titleList.Length);
        EditorGUILayout.EndHorizontal();

        bool isShowWindow = windowDic.Count > 0 && windowDic.ContainsKey(titleList[currIndex]) && windowDic[titleList[currIndex]] != null;
        if (isShowWindow) windowDic[titleList[currIndex]].DrawSelf();
    }
}
