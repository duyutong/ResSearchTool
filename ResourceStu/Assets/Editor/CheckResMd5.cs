using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;
using UnityEditor;
using System.IO;
using System.Text;
using System;

public class CheckResMd5 : EditorWindow
{
    private string fileMD5;
    private static string resPath;
    //[MenuItem("Tool/测试资源MD5信息")]
    private static void DrawWindow()
    {
        Rect wr = new Rect(0, 0, 600, 100);
        CheckResMd5 window = (CheckResMd5)GetWindowWithRect(typeof(CheckResMd5), wr, true, "CheckResMd5");
        window.Show();
    }
    private void OnGUI()
    {
        EditorGUILayout.LabelField("  MD5:", fileMD5);
        
        Rect pathRect = new Rect(10, 25, 500, 20);
        EditorGUILayout.BeginVertical();
        resPath = EditorGUI.TextField(pathRect,"resPath/资源:", resPath);
        resPath = OnDrawElementAcceptDrop(pathRect, resPath);

        Rect btnRect = new Rect(10, 50, 160, 30);
        if (GUI.Button(btnRect, "计算MD5码")) 
        {
            string temp = Directory.GetParent(Application.dataPath) + "/" + resPath;
            string fullPath = temp.Replace(@"\", @"/");
            fileMD5 = GetMD5(fullPath);
        }
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
    private string OnDrawElementAcceptDrop(Rect rect,string label)
    {
        if (!rect.Contains(Event.current.mousePosition)) return label;
        if (DragAndDrop.paths== null || DragAndDrop.paths.Length == 0) return label;
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
    
}
