using UnityEngine;
using System;
using System.IO;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections;
using System.Runtime.InteropServices;

[Serializable]
public class AppConfig {
    public string name;
    public string path;
}

[Serializable]
public class AppList {
    public List<AppConfig> apps = new List<AppConfig>();
}

public class AppLauncher : MonoBehaviour {
    // --- Win32 API ---
    [DllImport("user32.dll")] private static extern IntPtr GetActiveWindow();
    [DllImport("user32.dll")] [return: MarshalAs(UnmanagedType.Bool)] private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
    [DllImport("user32.dll")] private static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern bool IsIconic(IntPtr hWnd);
    [DllImport("user32.dll")] private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")] private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);
    [DllImport("user32.dll")] private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
    [DllImport("user32.dll")] private static extern short GetAsyncKeyState(int vKey);

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const int SW_RESTORE = 9;

    private const int VK_RETURN = 0x0D;
    private const int VK_0 = 0x30;
    private const int VK_NUM0 = 0x60;

    [Header("Config設定")]
    public UnityEngine.Object configFile;
    public AppList configData = new AppList();

    [Header("監視・入力設定")]
    public float checkInterval = 3.0f;
    private Coroutine monitorCoroutine;
    private string currentActiveProcessName;
    private IntPtr unityWindowHandle;
    
    private int selectedIndex = -1;
    private bool isDeleteMode = false;
    private bool isEnterPressed = false;

    void Awake() {
        LoadConfig();
        if (!Application.isEditor) unityWindowHandle = GetActiveWindow();
    }

    void Start() {
        SetUnityAlwaysOnTop(true);
    }

    void Update() {
        // 1〜9 の入力を監視
        for (int i = 1; i <= 9; i++) {
            if (((GetAsyncKeyState(0x30 + i) & 0x8000) != 0) || ((GetAsyncKeyState(0x60 + i) & 0x8000) != 0)) {
                if (selectedIndex != i) {
                    selectedIndex = i;
                    isDeleteMode = false; // 新しい番号が選ばれたら削除モードをリセット
                    UnityEngine.Debug.Log($"Selected App Index: {selectedIndex}");
                }
            }
        }

        // 番号が選ばれている状態で「0」が押されたら削除フラグを立てる
        if (selectedIndex != -1) {
            if (((GetAsyncKeyState(VK_0) & 0x8000) != 0) || ((GetAsyncKeyState(VK_NUM0) & 0x8000) != 0)) {
                if (!isDeleteMode) {
                    isDeleteMode = true;
                    UnityEngine.Debug.Log($"Delete Mode Active for App: {selectedIndex}");
                }
            }
        }

        // Enterキー判定
        bool enterDown = (GetAsyncKeyState(VK_RETURN) & 0x8000) != 0;
        if (enterDown && !isEnterPressed) {
            isEnterPressed = true;
            if (selectedIndex != -1) {
                // インデックスは1から始まる想定なので、リストの0番目に対応させるため -1 する
                int targetIdx = selectedIndex - 1;

                if (isDeleteMode) {
                    CloseAppByIndex(targetIdx);
                } else {
                    LaunchByIndex(targetIdx);
                }

                // 実行後にリセット
                selectedIndex = -1;
                isDeleteMode = false;
            }
        } else if (!enterDown) {
            isEnterPressed = false;
        }
    }

    [ContextMenu("Load Config Now")]
    public void LoadConfig() {
#if UNITY_EDITOR
        string path = UnityEditor.AssetDatabase.GetAssetPath(configFile);
        if(!string.IsNullOrEmpty(path)) path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
#else
        string path = Path.Combine(Application.streamingAssetsPath, "apps_config.json");
#endif
        if (File.Exists(path)) {
            string json = File.ReadAllText(path);
            JsonUtility.FromJsonOverwrite(json, configData);
            UnityEngine.Debug.Log("Config Loaded.");
        }
    }

    public void SetUnityAlwaysOnTop(bool top) {
        if (unityWindowHandle == IntPtr.Zero) return;
        IntPtr order = top ? HWND_TOPMOST : HWND_NOTOPMOST;
        SetWindowPos(unityWindowHandle, order, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
    }

    // --- 起動処理 ---
    public void LaunchByIndex(int index) {
        if (index < 0 || index >= configData.apps.Count) return;
        
        string targetPath = configData.apps[index].path;
        currentActiveProcessName = Path.GetFileNameWithoutExtension(targetPath);
        
        UnityEngine.Debug.Log($"Launching: {currentActiveProcessName}");
        ExecuteLaunch(targetPath, currentActiveProcessName);
        
        if (monitorCoroutine != null) StopCoroutine(monitorCoroutine);
        monitorCoroutine = StartCoroutine(ForceFocusLoop());
    }

    private void ExecuteLaunch(string path, string procName) {
        Process[] running = Process.GetProcessesByName(procName);
        // 応答なしプロセスがあれば殺してから再起動
        if (running.Length > 0 && !running[0].Responding) running[0].Kill();
        
        if (running.Length == 0 || !running[0].Responding) {
            try { 
                Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true }); 
            }
            catch (Exception e) { UnityEngine.Debug.LogError($"Launch Error: {e.Message}"); }
        }
    }

    // --- 終了処理 ---
    public void CloseAppByIndex(int index) {
        if (index < 0 || index >= configData.apps.Count) return;

        string targetPath = configData.apps[index].path;
        string procName = Path.GetFileNameWithoutExtension(targetPath);
        
        Process[] running = Process.GetProcessesByName(procName);
        if (running.Length == 0) {
            UnityEngine.Debug.Log($"{procName} は起動していません。");
            return;
        }

        foreach (Process p in running) {
            try {
                UnityEngine.Debug.Log($"Closing: {procName}");
                p.CloseMainWindow(); // 優しく終了
                if (!p.WaitForExit(2000)) p.Kill(); // 2秒待ってダメなら強制終了
            } catch (Exception e) {
                UnityEngine.Debug.LogError($"Close Error: {e.Message}");
            }
        }

        // 監視中のアプリを消したなら監視を止める
        if (currentActiveProcessName == procName && monitorCoroutine != null) {
            StopCoroutine(monitorCoroutine);
            monitorCoroutine = null;
        }
    }

    // --- 監視ループ ---
    IEnumerator ForceFocusLoop() {
        while (true) {
            yield return new WaitForSeconds(checkInterval);
            if (string.IsNullOrEmpty(currentActiveProcessName)) continue;

            Process[] ps = Process.GetProcessesByName(currentActiveProcessName);
            if (ps.Length > 0) {
                IntPtr targetHWnd = ps[0].MainWindowHandle;
                if (targetHWnd == IntPtr.Zero) continue;

                if (GetForegroundWindow() != targetHWnd) {
                    SetUnityAlwaysOnTop(false); 
                    ForceActivateWindow(targetHWnd);
                    yield return new WaitForSeconds(0.1f); 
                    SetUnityAlwaysOnTop(true);
                }
            }
        }
    }

    private void ForceActivateWindow(IntPtr hWnd) {
        if (IsIconic(hWnd)) ShowWindow(hWnd, SW_RESTORE);
        uint foreThread = GetWindowThreadProcessId(GetForegroundWindow(), IntPtr.Zero);
        uint appThread = GetWindowThreadProcessId(hWnd, IntPtr.Zero);
        if (foreThread != appThread) {
            AttachThreadInput(foreThread, appThread, true);
            SetForegroundWindow(hWnd);
            AttachThreadInput(foreThread, appThread, false);
        } else {
            SetForegroundWindow(hWnd);
        }
    }
}