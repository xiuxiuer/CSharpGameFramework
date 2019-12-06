﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEditor;
using UnityEditor.UI;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.MemoryProfiler;
using UnityEditorInternal;
using GameFramework;

public class BatchResourceProcessWindow : EditorWindow
{
    internal static void InitWindow(ResourceEditWindow resEdit)
    {
        BatchResourceProcessWindow window = (BatchResourceProcessWindow)EditorWindow.GetWindow(typeof(BatchResourceProcessWindow));
        window.Init(resEdit);
        window.Show();
    }

    private void Init(ResourceEditWindow resEdit)
    {
        m_ResourceEditWindow = resEdit;
        m_IsReady = true;
    }

    private void OnGUI()
    {
        bool handle = false;
        int deleteIndex = -1;
        EditorGUILayout.BeginHorizontal();
        ResourceEditUtility.EnableSaveAndReimport = EditorGUILayout.Toggle("允许SaveAndReimport", ResourceEditUtility.EnableSaveAndReimport);
        ResourceEditUtility.ForceSaveAndReimport = EditorGUILayout.Toggle("强制SaveAndReimport", ResourceEditUtility.ForceSaveAndReimport);
        EditorGUILayout.EndHorizontal();
        var rt = EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("加载", EditorStyles.toolbarButton)) {
            DeferAction(w => Load());
        }
        if (GUILayout.Button("添加", EditorStyles.toolbarButton)) {
            m_IsReady = false;
            if (m_List.Count > 0) {
                var last = m_List[m_List.Count - 1];
                m_List.Add(new ResourceEditUtility.BatchProcessInfo { DslPath = last.DslPath, ResPath = last.ResPath });
            } else {
                m_List.Add(new ResourceEditUtility.BatchProcessInfo());
            }
            m_IsReady = true;
        }
        EditorGUILayout.EndHorizontal();
        if (m_IsReady) {
            m_Pos = EditorGUILayout.BeginScrollView(m_Pos);
            for (int i = 0; i < m_List.Count; ++i) {
                EditorGUILayout.BeginHorizontal();
                var info = m_List[i];
                EditorGUILayout.LabelField("资源:", GUILayout.Width(40));
                EditorGUILayout.LabelField(info.ResPath);
                if (GUILayout.Button("选择", GUILayout.Width(40))) {
                    m_IsReady = false;
                    string res = EditorUtility.OpenFolderPanel("选择资源路径", string.Empty, string.Empty);
                    if (!string.IsNullOrEmpty(res)) {
                        if (IsAssetPath(res)) {
                            info.ResPath = FilePathToRelativePath(res);
                        } else {
                            EditorUtility.DisplayDialog("错误", "必须选择本unity工程的资源路径！", "确定");
                        }
                    }
                    m_IsReady = true;
                }
                EditorGUILayout.LabelField("脚本:", GUILayout.Width(40));
                EditorGUILayout.LabelField(info.DslPath);
                if (GUILayout.Button("选择", EditorStyles.toolbarButton, GUILayout.Width(40))) {
                    m_IsReady = false;
                    string res = EditorUtility.OpenFilePanel("选择dsl处理脚本", string.Empty, "dsl");
                    if (!string.IsNullOrEmpty(res)) {
                        info.DslPath = FilePathToRelativePath(res);
                    }
                    m_IsReady = true;
                }
                if (GUILayout.Button("删除", EditorStyles.toolbarButton, GUILayout.Width(40))) {
                    deleteIndex = i;
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("保存", EditorStyles.toolbarButton)) {
            DeferAction(w => Save());
        }
        if (GUILayout.Button("处理", EditorStyles.toolbarButton)) {
            handle = true;
        }
        EditorGUILayout.EndHorizontal();
        if (deleteIndex >= 0) {
            m_List.RemoveAt(deleteIndex);
        }
        if (handle) {
            int ct = m_List.Count;
            int ix = 0;
            m_ResourceEditWindow.QueueProcessBegin();
            foreach (var info in m_List) {
                if (!string.IsNullOrEmpty(info.DslPath)) {
                    m_ResourceEditWindow.QueueProcess(RelativePathToFilePath(info.DslPath), RelativePathToFilePath(info.ResPath), ix, ct);
                }
                ++ix;
            }
            m_ResourceEditWindow.QueueProcessEnd();
            DeferAction(w => w.Close());
        }

        ExecuteDeferredActions();
    }

    private void ExecuteDeferredActions()
    {
        if (m_InActions)
            return;
        try {
            m_InActions = true;
            while (m_Actions.Count > 0) {
                var action = m_Actions.Dequeue();
                if (null != action) {
                    action(this);
                }
            }
        } finally {
            m_InActions = false;
        }
    }
    private void DeferAction(Action<BatchResourceProcessWindow> action)
    {
        m_Actions.Enqueue(action);
    }

    private void Load()
    {
        m_IsReady = false;
        try {
            string path = EditorUtility.OpenFilePanel("请指定要加载的批量处理文件", string.Empty, "txt");
            if (!string.IsNullOrEmpty(path) && File.Exists(path)) {
                int i = 0;
                try {
                    var txt = File.ReadAllText(path);
                    var lines = txt.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    m_List.Clear();
                    int curCount = 1;
                    int totalCount = lines.Length;
                    for (i = 1; i < lines.Length; ++i) {
                        var fields = lines[i].Split('\t');
                        var resPath = fields[0];
                        var dslPath = fields[1];

                        var item = new ResourceEditUtility.BatchProcessInfo { ResPath = resPath, DslPath = dslPath };
                        m_List.Add(item);

                        ++curCount;
                        EditorUtility.DisplayProgressBar("加载进度", string.Format("{0}/{1}", curCount, totalCount), curCount * 1.0f / totalCount);
                    }
                } catch (Exception ex) {
                    EditorUtility.DisplayDialog("异常", string.Format("line {0} exception {1}\n{2}", i, ex.Message, ex.StackTrace), "ok");
                }
                EditorUtility.ClearProgressBar();
            }
        } finally {
            m_IsReady = true;
        }
    }

    private void Save()
    {
        m_IsReady = false;
        try {
            if (m_List.Count > 0) {
                string path = EditorUtility.SaveFilePanel("请指定要保存批量处理的文件", string.Empty, "batch", "txt");
                if (!string.IsNullOrEmpty(path)) {
                    if (File.Exists(path)) {
                        File.Delete(path);
                    }
                    using (StreamWriter sw = new StreamWriter(path)) {
                        sw.WriteLine("res_path\tdsl_path");
                        int curCount = 0;
                        int totalCount = m_List.Count;
                        foreach (var item in m_List) {
                            sw.WriteLine("{0}\t{1}", item.ResPath, item.DslPath);
                            ++curCount;
                            EditorUtility.DisplayProgressBar("保存进度", string.Format("{0}/{1}", curCount, totalCount), curCount * 1.0f / totalCount);
                        }
                        sw.Close();
                        EditorUtility.ClearProgressBar();
                    }
                }
            } else {
                EditorUtility.DisplayDialog("错误", "没有要保存的数据！", "ok");
            }
        } finally {
            m_IsReady = true;
        }
    }
    
    private bool IsAssetPath(string path)
    {
        return ResourceEditUtility.IsAssetPath(path);
    }
    private string FilePathToRelativePath(string path)
    {
        return ResourceEditUtility.FilePathToRelativePath(path);
    }
    private string RelativePathToFilePath(string path)
    {
        return ResourceEditUtility.RelativePathToFilePath(path);
    }
    private string GetRootPath()
    {
        return ResourceEditUtility.GetRootPath();
    }

    private bool m_IsReady = false;
    private bool m_InActions = false;
    private Queue<Action<BatchResourceProcessWindow>> m_Actions = new Queue<Action<BatchResourceProcessWindow>>();

    private ResourceEditWindow m_ResourceEditWindow = null;
    private bool m_UseReimport = false;
    private List<ResourceEditUtility.BatchProcessInfo> m_List = new List<ResourceEditUtility.BatchProcessInfo>();
    private Vector2 m_Pos = Vector2.zero;
}

public class ResourceCommandWindow : EditorWindow
{
    internal static void InitWindow(ResourceEditWindow resEdit, string content, object obj, object item)
    {
        ResourceCommandWindow window = (ResourceCommandWindow)EditorWindow.GetWindow(typeof(ResourceCommandWindow));
        window.Init(resEdit, content, obj, item);
        window.Show();
    }

    private void Init(ResourceEditWindow resEdit, string content, object obj, object item)
    {
        m_ResourceEditWindow = resEdit;
        m_Content = content;
        m_Object = obj;
        m_Item = item;
        m_IsReady = true;
    }

    private void OnGUI()
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.TextArea(m_Content, EditorStyles.textArea, GUILayout.MinHeight(160), GUILayout.MaxHeight(this.position.height - 60));
        EditorGUILayout.EndHorizontal();
        var rt = EditorGUILayout.BeginHorizontal();
        m_Command = EditorGUILayout.TextField(m_Command, EditorStyles.toolbarTextField, GUILayout.MinWidth(200), GUILayout.MaxWidth(this.position.width - 52));
        if (GUILayout.Button("run", EditorStyles.toolbarButton, GUILayout.Width(40))) {
            DeferAction(w => Run());
        }
        EditorGUILayout.EndHorizontal();
        if (m_IsReady) {
            m_Pos = EditorGUILayout.BeginScrollView(m_Pos);
            while (m_Results.Count > 20) {
                m_Results.Dequeue();
            }
            foreach (var info in m_Results) {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.TextArea(info, EditorStyles.textArea, GUILayout.MinWidth(200), GUILayout.MaxWidth(this.position.width), GUILayout.MinHeight(16), GUILayout.MaxHeight(32));
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.EndHorizontal();

        ExecuteDeferredActions();
    }
    private void Run()
    {
        m_ScriptableInfo.Content = m_Content;
        m_ScriptableInfo.Results = m_Results;
        m_ScriptableInfo.ResourceEditWindow = m_ResourceEditWindow;
        m_ScriptableInfo.ResourceProcessor = ResourceProcessor.Instance;
        m_ScriptableInfo.ResourceEditWindowType = typeof(ResourceEditWindow);
        m_ScriptableInfo.ResourceProcessorType = typeof(ResourceProcessor);
        m_ScriptableInfo.ResourceEditUtilityType = typeof(ResourceEditUtility);
        var r = ResourceEditUtility.EvalScript(m_Command, ResourceProcessor.Instance.Params, m_Object, m_Item, new Dictionary<string, object> { { "@context", m_ScriptableInfo } });
        if (null != r) {
            m_Results.Enqueue(r.ToString());
        }
        else {
            m_Results.Enqueue("no return value.");
        }
        m_Content = m_ScriptableInfo.Content;
    }

    private void ExecuteDeferredActions()
    {
        if (m_InActions)
            return;
        try {
            m_InActions = true;
            while (m_Actions.Count > 0) {
                var action = m_Actions.Dequeue();
                if (null != action) {
                    action(this);
                }
            }
        }
        finally {
            m_InActions = false;
        }
    }
    private void DeferAction(Action<ResourceCommandWindow> action)
    {
        m_Actions.Enqueue(action);
    }

    internal class ScriptableInfo
    {
        internal string Content;
        internal Queue<string> Results;
        internal ResourceEditWindow ResourceEditWindow;
        internal ResourceProcessor ResourceProcessor;
        internal Type ResourceEditWindowType;
        internal Type ResourceProcessorType;
        internal Type ResourceEditUtilityType;
    }
    private ScriptableInfo m_ScriptableInfo = new ScriptableInfo();
    
    private bool m_IsReady = false;
    private bool m_InActions = false;
    private Queue<Action<ResourceCommandWindow>> m_Actions = new Queue<Action<ResourceCommandWindow>>();

    private string m_Content = string.Empty;
    private Queue<string> m_Results = new Queue<string>();
    private string m_Command = string.Empty;
    private object m_Object = null;
    private object m_Item = null;
    private ResourceEditWindow m_ResourceEditWindow = null;
    private Vector2 m_Pos = Vector2.zero;
}
