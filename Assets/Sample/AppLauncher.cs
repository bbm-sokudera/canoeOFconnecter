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
    // --- Windows API ---
    [DllImport("user32.dll")]
    private static extern IntPtr GetActiveWindow();
    [DllImport("user32.dll")]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int x, int y, int cx, int cy, uint uFlags);
    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);
    [DllImport("user32.dll")]
    private static extern bool IsIconic(IntPtr hWnd);
    [DllImport("user32.dll")]
    private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();
    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, IntPtr ProcessId);
    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    private static readonly IntPtr HWND_TOPMOST = new IntPtr(-1);
    private static readonly IntPtr HWND_NOTOPMOST = new IntPtr(-2);
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOMOVE = 0x0002;
    private const int SW_RESTORE = 9;

    // キーコード定義
    private const int VK_RETURN = 0x0D;    // Enter
    private const int VK_NUMPAD_ENTER = 0x0D; // Windowsでは通常のEnterと同じ0x0Dで判定されることが多いですが、念のため
    private const int VK_0 = 0x30;         // メインキーボードの0
    private const int VK_NUM0 = 0x60;      // テンキーの0

    [Header("Config設定")]
    public UnityEngine.Object configFile;
    public AppList configData;

    [Header("監視・入力設定")]
    public float checkInterval = 3.0f;
    private Coroutine monitorCoroutine;
    private string currentActiveProcessName;
    private IntPtr unityWindowHandle;
    private int selectedIndex = -1;

    // 連打防止用
    private bool isEnterPressed = false;

    void Awake() {
        LoadConfig();
        if (!Application.isEditor) unityWindowHandle = GetActiveWindow();
    }

    void Start() {
        SetUnityAlwaysOnTop(true);
    }

    void Update() {
        // --- 数字キーの判定 (メイン 0-9 & テンキー 0-9) ---
        for (int i = 0; i <= 9; i++) {
            bool mainKey = (GetAsyncKeyState(VK_0 + i) & 0x8000) != 0;
            bool numpadKey = (GetAsyncKeyState(VK_NUM0 + i) & 0x8000) != 0;

            if (mainKey || numpadKey) {
                if (selectedIndex != i) {
                    selectedIndex = i;
                    UnityEngine.Debug.Log($"Selected Index: {selectedIndex}");
                }
            }
        }

        // --- Enterキー判定 (裏でも効く) ---
        bool enterDown = (GetAsyncKeyState(VK_RETURN) & 0x8000) != 0;
        
        if (enterDown) {
            if (!isEnterPressed) { // 押し下げられた瞬間だけ実行
                isEnterPressed = true;
                if (selectedIndex != -1) {
                    LaunchByIndex(selectedIndex);
                }
            }
        } else {
            isEnterPressed = false; // 離されたらリセット
        }
    }

    [ContextMenu("Load Config Now")]
    public void LoadConfig() {
#if UNITY_EDITOR
        string path = UnityEditor.AssetDatabase.GetAssetPath(configFile);
        if(!string.IsNullOrEmpty(path))
            path = Path.GetFullPath(Path.Combine(Application.dataPath, "..", path));
#else
        string path = Path.Combine(Application.streamingAssetsPath, "apps_config.json");
#endif
        if (File.Exists(path)) {
            string json = File.ReadAllText(path);
            JsonUtility.FromJsonOverwrite(json, configData);
        }
    }

    public void SetUnityAlwaysOnTop(bool top) {
        if (unityWindowHandle == IntPtr.Zero) return;
        IntPtr order = top ? HWND_TOPMOST : HWND_NOTOPMOST;
        SetWindowPos(unityWindowHandle, order, 0, 0, 0, 0, SWP_NOMOVE | SWP_NOSIZE);
    }

    public void LaunchByIndex(int index) {
        if (index < 0 || index >= configData.apps.Count) return;

        string targetPath = configData.apps[index].path;
        currentActiveProcessName = Path.GetFileNameWithoutExtension(targetPath);
        
        ExecuteLaunch(targetPath, currentActiveProcessName);

        if (monitorCoroutine != null) StopCoroutine(monitorCoroutine);
        monitorCoroutine = StartCoroutine(ForceFocusLoop());
    }

    private void ExecuteLaunch(string path, string procName) {
        Process[] running = Process.GetProcessesByName(procName);
        if (running.Length > 0 && running[0].Responding) {
            UnityEngine.Debug.Log($"{procName} is already running.");
        } else {
            if (running.Length > 0) running[0].Kill();
            try {
                Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
            } catch (Exception e) {
                UnityEngine.Debug.LogError($"Launch Error: {e.Message}");
            }
        }
    }

    IEnumerator ForceFocusLoop() {
        while (true) {
            yield return new WaitForSeconds(checkInterval);
            Process[] ps = Process.GetProcessesByName(currentActiveProcessName);
            if (ps.Length > 0) {
                IntPtr targetHWnd = ps[0].MainWindowHandle;
                if (targetHWnd == IntPtr.Zero) continue;

                if (GetForegroundWindow() != targetHWnd) {
                    SetUnityAlwaysOnTop(false); 
                    ForceActivateWindow(targetHWnd);
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