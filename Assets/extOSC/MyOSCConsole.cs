using UnityEngine;
using UnityEngine.UI;
using TMPro; // TextMeshProを扱うために追加
using System.Collections.Generic;
using extOSC;

public class MyOSCFilterConsole : MonoBehaviour
{
    #region Settings Classes

    [System.Serializable]
    public class OSCFilter
    {
        public string address = "/example";
        public int valueIndex = 0;
        public string lastValue = "No Data";
    }

    public enum ForwardFilterMode
    {
        AllowAll,
        AllowList,
        BlockList
    }

    public enum ExpectedValueType
    {
        Any,
        Int,
        Float,
        String
    }

    [System.Serializable]
    public class OSCForwardRule
    {
        public string address = "/example";
        public bool filterByArgument = false;
        public int valueIndex = 0;
        public ExpectedValueType expectedType = ExpectedValueType.Any;
    }

    [System.Serializable]
    public class StatusImageSetting
    {
        public string targetAddress = "/box";
        public Image targetUIImage;
        public Sprite connectedSprite;
        public Sprite disconnectedSprite;
        public float timeoutSeconds = 2.0f;
        [HideInInspector] public float lastReceiveTime = -1000f;
    }

    #endregion

    [Header("監視したい条件を追加")]
    public List<OSCFilter> filterList = new List<OSCFilter>();

    [Header("--- GUI Settings ---")]
    public bool showGUI = true;
    public KeyCode toggleGUIKey = KeyCode.F12;
    public int maxLogCount = 20;

    [Header("--- Forwarding Status & Setup ---")]
    public bool enableForwarding = true;
    public string targetHost = "127.0.0.1";
    public int targetPort = 8000;
    public ForwardFilterMode forwardFilterMode = ForwardFilterMode.AllowAll;
    public List<OSCForwardRule> forwardRules = new List<OSCForwardRule>();

    [Header("--- OSC Connection UI Images ---")]
    public List<StatusImageSetting> connectionStatusImages = new List<StatusImageSetting>();

    [Header("--- Participant Counter (体験人数記録) ---")]
    [Tooltip("カウントを表示するTextMeshPro")]
    public TextMeshProUGUI participantCountText;
    
    [Tooltip("カウントアップのトリガーとなるOSCアドレス")]
    public string stateAddress = "/state";
    
    [SerializeField, Tooltip("現在の累計体験人数")]
    private int participantCount = 0;

    private int _lastStateValue = -1; // 前回のState値を保持

    private List<string> filteredLogs = new List<string>();
    private HashSet<OSCReceiver> hookedReceivers = new HashSet<OSCReceiver>();
    private Vector2 consoleScrollPosition;

    private OSCTransmitter _forwardTransmitter;

    void Start()
    {
        _forwardTransmitter = gameObject.AddComponent<OSCTransmitter>();
        _forwardTransmitter.RemoteHost = targetHost;
        _forwardTransmitter.RemotePort = targetPort;
        _forwardTransmitter.enabled = enableForwarding;

        foreach (var setting in connectionStatusImages)
        {
            setting.lastReceiveTime = -1000f; 
        }

        // 初期表示を更新
        UpdateCounterUI();
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleGUIKey))
        {
            showGUI = !showGUI;
        }

        if (_forwardTransmitter != null)
        {
            if (_forwardTransmitter.RemoteHost != targetHost) _forwardTransmitter.RemoteHost = targetHost;
            if (_forwardTransmitter.RemotePort != targetPort) _forwardTransmitter.RemotePort = targetPort;
            if (_forwardTransmitter.enabled != enableForwarding) _forwardTransmitter.enabled = enableForwarding;
        }

        var receivers = FindObjectsOfType<OSCReceiver>();
        foreach (var r in receivers)
        {
            if (!hookedReceivers.Contains(r))
            {
                r.Bind("*", OnAnyMessageReceived);
                hookedReceivers.Add(r);
            }
        }

        UpdateUIImages();
    }

    private void UpdateUIImages()
    {
        foreach (var setting in connectionStatusImages)
        {
            if (setting.targetUIImage == null) continue;
            bool isConnected = (Time.time - setting.lastReceiveTime) < setting.timeoutSeconds;
            Sprite targetSprite = isConnected ? setting.connectedSprite : setting.disconnectedSprite;
            if (setting.targetUIImage.sprite != targetSprite)
            {
                setting.targetUIImage.sprite = targetSprite;
            }
        }
    }

    private void OnAnyMessageReceived(OSCMessage message)
    {
        // --- 1. 体験人数カウント処理 (/state 4 -> 5 を検知) ---
        if (message.Address == stateAddress && message.Values.Count > 0)
        {
            int currentState = message.Values[0].IntValue;

            // 前回の値が4で、今回の値が5になった瞬間だけカウントアップ
            if (_lastStateValue == 4 && currentState == 5)
            {
                participantCount++;
                UpdateCounterUI();
                Debug.Log($"<color=green>[Counter] 体験人数がカウントされました。合計: {participantCount}人</color>");
            }
            
            _lastStateValue = currentState;
        }

        // --- 2. 接続状況画像の受信時間更新 ---
        foreach (var setting in connectionStatusImages)
        {
            if (message.Address == setting.targetAddress)
            {
                setting.lastReceiveTime = Time.time;
            }
        }

        // --- 3. 監視(Monitor)用処理 ---
        foreach (var filter in filterList)
        {
            if (message.Address == filter.address)
            {
                if (filter.valueIndex >= 0 && filter.valueIndex < message.Values.Count)
                {
                    var val = message.Values[filter.valueIndex];
                    string valStr = val.Value.ToString();
                    filter.lastValue = valStr;

                    string logEntry = $"[{System.DateTime.Now:HH:mm:ss}] {message.Address} [{filter.valueIndex}] : {valStr} ({val.Type})";
                    lock(filteredLogs)
                    {
                        filteredLogs.Insert(0, logEntry);
                        if (filteredLogs.Count > maxLogCount) filteredLogs.RemoveAt(filteredLogs.Count - 1);
                    }
                }
            }
        }

        // --- 4. 転送(Forward)用処理 ---
        if (enableForwarding && _forwardTransmitter != null)
        {
            bool shouldSend = false;
            if (forwardFilterMode == ForwardFilterMode.AllowAll) { shouldSend = true; }
            else
            {
                bool matchAnyRule = false;
                foreach (var rule in forwardRules) { if (IsMatchRule(message, rule)) { matchAnyRule = true; break; } }
                if (forwardFilterMode == ForwardFilterMode.AllowList) { shouldSend = matchAnyRule; }
                else if (forwardFilterMode == ForwardFilterMode.BlockList) { shouldSend = !matchAnyRule; }
            }
            if (shouldSend) _forwardTransmitter.Send(message);
        }
    }

    private void UpdateCounterUI()
    {
        if (participantCountText != null)
        {
            participantCountText.text = participantCount.ToString();
        }
    }

    /// <summary>
    /// カウントを0にリセットする（インスペクターや他スクリプトから呼び出し可能）
    /// </summary>
    [ContextMenu("Reset Participant Count")]
    public void ResetCount()
    {
        participantCount = 0;
        UpdateCounterUI();
        Debug.Log("[Counter] 体験人数カウントがリセットされました。");
    }

    private bool IsMatchRule(OSCMessage message, OSCForwardRule rule)
    {
        if (message.Address != rule.address) return false;
        if (!rule.filterByArgument) return true;
        if (rule.valueIndex < 0 || rule.valueIndex >= message.Values.Count) return false;
        var val = message.Values[rule.valueIndex];
        switch (rule.expectedType)
        {
            case ExpectedValueType.Int: if (val.Type != extOSC.OSCValueType.Int) return false; break;
            case ExpectedValueType.Float: if (val.Type != extOSC.OSCValueType.Float) return false; break;
            case ExpectedValueType.String: if (val.Type != extOSC.OSCValueType.String) return false; break;
        }
        return true;
    }

    void OnGUI()
    {
        if (!showGUI) return;
        DrawConsoleWindow();
    }

    private void DrawConsoleWindow()
    {
        GUI.backgroundColor = new Color(0, 0.1f, 0.3f, 0.95f);
        GUILayout.BeginArea(new Rect(20, 20, 500, 600), "OSC TARGET MONITOR & FORWARDER", "Window");
        
        string forwardStatusText = enableForwarding ? $"<color=green>Forwarding to {targetHost}:{targetPort}</color>" : "<color=red>Forwarding OFF</color>";
        GUILayout.Label(forwardStatusText, new GUIStyle(GUI.skin.label) { richText = true, fontSize = 12 });
        
        // カウンターの簡易表示も追加
        GUILayout.Label($"<color=white>Participant Total: {participantCount}</color>", new GUIStyle(GUI.skin.label) { richText = true, fontSize = 14, fontStyle = FontStyle.Bold });
        GUILayout.Space(5);

        consoleScrollPosition = GUILayout.BeginScrollView(consoleScrollPosition);
        GUIStyle logStyle = new GUIStyle(GUI.skin.label) { fontSize = 13 };
        logStyle.normal.textColor = Color.cyan;

        lock(filteredLogs) { foreach (var log in filteredLogs) { GUILayout.Label(log, logStyle); } }

        GUILayout.EndScrollView();
        if (GUILayout.Button("Clear Logs")) filteredLogs.Clear();
        GUILayout.EndArea();
    }
}