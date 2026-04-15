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
    [Tooltip("終了確認後の待機時間（秒）")]
    public float waitBeforeLaunch = 1.0f;

    private Coroutine monitorCoroutine;
    private Coroutine switchCoroutine;
    private string currentActiveProcessName;
    private IntPtr unityWindowHandle;
    private int selectedIndex = -1;

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
        UnityEngine.Debug.Log($"[AppLauncher] Index {num} selected.");
    }

    public void SetDeleteMode(bool active) { } // インターフェース互換性のため維持

    public void Execute() {
        if (selectedIndex == -1) return;

        // 7キー -> Index 0, 8キー -> Index 1, 9キー -> Index 2
        int targetIdx = selectedIndex - 7; 
        
        LaunchByIndex(targetIdx);

        selectedIndex = -1;
    }
    #endregion

    #region App Logic

    /// <summary>
    /// アプリを起動する。二重起動防止のため、対象（および相互排他対象）を終了してから起動する。
    /// </summary>
    public void LaunchByIndex(int index) {
        if (index < 0 || index >= configData.apps.Count) return;

        if (switchCoroutine != null) StopCoroutine(switchCoroutine);

        List<int> targetsToClose = new List<int>();

        // 全てのケースで「自分自身」は終了対象（二重起動防止）
        targetsToClose.Add(index);

        // 相互排他ルール (0番と2番)
        if (index == 0) targetsToClose.Add(2);
        else if (index == 2) targetsToClose.Add(0);

        // 終了プロセスを経てから起動するコルーチンを開始
        switchCoroutine = StartCoroutine(SwitchAppSequence(targetsToClose, index));
    }

    /// <summary>
    /// 対象アプリ群を終了し、完全に閉じたのを確認＋待機してから次を起動する
    /// </summary>
    private IEnumerator SwitchAppSequence(List<int> closeIndices, int launchIdx) {
        UnityEngine.Debug.Log($"[AppLauncher] Cleanup start for launching Index {launchIdx}");
        
        // 1. 対象のアプリをすべて閉じる
        foreach (int idx in closeIndices) {
            CloseAppByIndex(idx);
        }

        // 2. すべての対象プロセスが終了するまで待機（最大5秒）
        float timer = 0;
        bool anyRunning = true;
        while (timer < 5.0f && anyRunning) {
            anyRunning = false;
            foreach (int idx in closeIndices) {
                if (idx < 0 || idx >= configData.apps.Count) continue;
                string procName = Path.GetFileNameWithoutExtension(configData.apps[idx].path);
                if (Process.GetProcessesByName(procName).Length > 0) {
                    anyRunning = true;
                    break;
                }
            }
            if (anyRunning) {
                timer += 0.5f;
                yield return new WaitForSeconds(0.5f);
            }
        }

        // 3. 完全に終了した後、指定秒数さらに待機（安定化のため）
        yield return new WaitForSeconds(waitBeforeLaunch);

        // 4. アプリ起動
        ExecuteLaunch(launchIdx);
    }

    private void ExecuteLaunch(int index) {
        string path = configData.apps[index].path;
        if (!File.Exists(path)) {
            UnityEngine.Debug.LogError($"[AppLauncher] File not found: {path}");
            return;
        }

        currentActiveProcessName = Path.GetFileNameWithoutExtension(path);
        try {
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
            UnityEngine.Debug.Log($"[AppLauncher] Launched: {currentActiveProcessName}");
        } catch (Exception e) {
            UnityEngine.Debug.LogError($"[AppLauncher] Launch failed: {e.Message}");
        }

        if (monitorCoroutine != null) StopCoroutine(monitorCoroutine);
        monitorCoroutine = StartCoroutine(ForceFocusLoop());
    }

    public void CloseAppByIndex(int index) {
        if (index < 0 || index >= configData.apps.Count) return;
        
        string procName = Path.GetFileNameWithoutExtension(configData.apps[index].path);
        Process[] running = Process.GetProcessesByName(procName);
        
        foreach (Process p in running) {
            try {
                p.CloseMainWindow();
                if (!p.WaitForExit(2000)) p.Kill();
            } catch (Exception e) {
                UnityEngine.Debug.LogWarning($"[AppLauncher] Failed to close {procName}: {e.Message}");
            }
        }
    }

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

    IEnumerator ForceFocusLoop() {
        while (true) {
            yield return new WaitForSeconds(checkInterval);
            if (string.IsNullOrEmpty(currentActiveProcessName)) continue;

            Process[] ps = Process.GetProcessesByName(currentActiveProcessName);
            if (ps.Length > 0) {
                IntPtr hWnd = ps[0].MainWindowHandle;
                if (hWnd != IntPtr.Zero && GetForegroundWindow() != hWnd) {
                    SetUnityAlwaysOnTop(false);
                    if (IsIconic(hWnd)) ShowWindow(hWnd, SW_RESTORE);
                    SetForegroundWindow(hWnd);
                    yield return new WaitForSeconds(0.1f);
                    SetUnityAlwaysOnTop(true);
                }
            }
        }
    }
    #endregion
}