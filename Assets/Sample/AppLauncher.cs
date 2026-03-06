using UnityEngine;
using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections;
using System.Runtime.InteropServices;

#region Serializable Classes
[Serializable]
public class AppConfig {
    public string name;
    public string path;
}

[Serializable]
public class AppList {
    public List<AppConfig> apps = new List<AppConfig>();
}
#endregion

public class AppLauncher : MonoBehaviour {
    // --- Win32 API ---
    [DllImport("user32.dll")] private static extern IntPtr GetActiveWindow();
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const int SW_RESTORE = 9;

    [Header("Config設定")]
    public UnityEngine.Object configFile;
    public AppList configData = new AppList();

    [Header("監視設定")]
    public float checkInterval = 3.0f;

    [Header("キー設定 (InputCentralManagerで使用)")]
    [Tooltip("削除（終了）モードに切り替えるキー")]
    public KeyCode deleteModeKey = KeyCode.KeypadDivide;

    private Coroutine monitorCoroutine;
    private string currentActiveProcessName;
    private IntPtr unityWindowHandle;
    private int selectedIndex = -1;
    private bool isDeleteMode = false;

    void Awake() {
        LoadConfig();
        if (!Application.isEditor) unityWindowHandle = GetActiveWindow();
    }

    void Start() { 
        SetUnityAlwaysOnTop(true); 
    }

    #region Input Central Interface

    public void SelectIndex(int num) {
        selectedIndex = num;
        isDeleteMode = false;
        UnityEngine.Debug.Log($"[AppLauncher] App Index {num} selected. Waiting for Enter...");
    }

    public void SetDeleteMode(bool active) {
        if (selectedIndex == -1) return;
        isDeleteMode = active;
        UnityEngine.Debug.Log($"[AppLauncher] Delete Mode: {active}");
    }

    public void Execute() {
        if (selectedIndex == -1) return;

        // 7, 8, 9キー を appsリストの 0, 1, 2番目 に対応させる
        int targetIdx = selectedIndex - 7; 

        if (isDeleteMode) CloseAppByIndex(targetIdx);
        else LaunchByIndex(targetIdx);

        selectedIndex = -1;
        isDeleteMode = false;
    }
    #endregion

    #region App Logic
    [ContextMenu("Load Config Now")]
    public void LoadConfig() {
        string path = "";
#if UNITY_EDITOR
        path = UnityEditor.AssetDatabase.GetAssetPath(configFile);
        if(!string.IsNullOrEmpty(path)) path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
#else
        path = Path.Combine(Application.streamingAssetsPath, "apps_config.json");
#endif
        if (File.Exists(path)) {
            JsonUtility.FromJsonOverwrite(File.ReadAllText(path), configData);
            UnityEngine.Debug.Log("[AppLauncher] Config Loaded.");
        }
    }

    public void SetUnityAlwaysOnTop(bool top) {
        if (unityWindowHandle == IntPtr.Zero) return;
        SetWindowPos(unityWindowHandle, top ? HWND_TOPMOST : HWND_NOTOPMOST, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
    }

    public void LaunchByIndex(int index) {
        if (index < 0 || index >= configData.apps.Count) return;
        string path = configData.apps[index].path;
        currentActiveProcessName = Path.GetFileNameWithoutExtension(path);
        Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
        if (monitorCoroutine != null) StopCoroutine(monitorCoroutine);
        monitorCoroutine = StartCoroutine(ForceFocusLoop());
    }

    public void CloseAppByIndex(int index) {
        if (index < 0 || index >= configData.apps.Count) return;
        string procName = Path.GetFileNameWithoutExtension(configData.apps[index].path);
        Process[] running = Process.GetProcessesByName(procName);
        foreach (Process p in running) { p.CloseMainWindow(); if (!p.WaitForExit(2000)) p.Kill(); }
    }

    IEnumerator ForceFocusLoop() {
        while (true) {
            yield return new WaitForSeconds(checkInterval);
            if (string.IsNullOrEmpty(currentActiveProcessName)) continue;
            Process[] ps = Process.GetProcessesByName(currentActiveProcessName);
            if (ps.Length > 0 && GetForegroundWindow() != ps[0].MainWindowHandle) {
                SetUnityAlwaysOnTop(false);
                IntPtr hWnd = ps[0].MainWindowHandle;
                if (IsIconic(hWnd)) ShowWindow(hWnd, SW_RESTORE);
                SetForegroundWindow(hWnd);
                yield return new WaitForSeconds(0.1f);
                SetUnityAlwaysOnTop(true);
            }
        }
    }
    #endregion
}