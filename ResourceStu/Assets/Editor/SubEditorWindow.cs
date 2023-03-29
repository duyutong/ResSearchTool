using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEngine;

public class SubEditorWindow : EditorWindow
{
    public virtual void DrawSelf() { }
    public void CheckRes(string path, string extension, Action<string> action = null)
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
    public string OnDrawElementAcceptDrop(Rect rect, string label)
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
    public static string GetMD5(string filePath)
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
