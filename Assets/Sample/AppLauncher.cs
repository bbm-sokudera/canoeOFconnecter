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

    public void SetDeleteMode(bool active) {
        // 元のインターフェース維持のため残していますが、機能は削除
    }

    public void Execute() {
        if (selectedIndex == -1) return;

        // 7キー -> Index 0, 8キー -> Index 1, 9キー -> Index 2
        int targetIdx = selectedIndex - 7; 
        
        // LaunchByIndex 内で排他制御(0番と2番の入れ替え)を行う
        LaunchByIndex(targetIdx);

        selectedIndex = -1;
    }
    #endregion

    #region App Logic

    /// <summary>
    /// アプリを起動する。0番と2番は相互に終了を確認してから起動する。
    /// </summary>
    public void LaunchByIndex(int index) {
        if (index < 0 || index >= configData.apps.Count) return;

        // 既存の切り替え処理があれば止める
        if (switchCoroutine != null) StopCoroutine(switchCoroutine);

        if (index == 0) {
            // 0番起動時は、2番を終了させてから起動するフローへ
            switchCoroutine = StartCoroutine(SwitchAppSequence(2, 0));
        }
        else if (index == 2) {
            // 2番起動時は、0番を終了させてから起動するフローへ
            switchCoroutine = StartCoroutine(SwitchAppSequence(0, 2));
        }
        else {
            // それ以外（1番など）は通常起動
            ExecuteLaunch(index);
        }
    }

    /// <summary>
    /// 指定インデックスのアプリを終了し、完全に閉じたのを確認してから次を起動する
    /// </summary>
    private IEnumerator SwitchAppSequence(int closeIdx, int launchIdx) {
        UnityEngine.Debug.Log($"[AppLauncher] Switching: Closing Index {closeIdx} -> Launching Index {launchIdx}");
        
        // 1. 対象を閉じる
        CloseAppByIndex(closeIdx);

        // 2. 完全に終了するまで待機（最大5秒）
        if (closeIdx >= 0 && closeIdx < configData.apps.Count) {
            string procName = Path.GetFileNameWithoutExtension(configData.apps[closeIdx].path);
            float timer = 0;
            while (timer < 5.0f) {
                if (Process.GetProcessesByName(procName).Length == 0) break;
                timer += 0.5f;
                yield return new WaitForSeconds(0.5f);
            }
        }

        // 3. 次を起動
        ExecuteLaunch(launchIdx);
    }

    /// <summary>
    /// 実際のOSプロセス起動処理
    /// </summary>
    private void ExecuteLaunch(int index) {
        string path = configData.apps[index].path;
        if (!File.Exists(path)) {
            UnityEngine.Debug.LogError($"[AppLauncher] File not found: {path}");
            return;
        }

        currentActiveProcessName = Path.GetFileNameWithoutExtension(path);
        Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });

        // 監視ループを再開
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