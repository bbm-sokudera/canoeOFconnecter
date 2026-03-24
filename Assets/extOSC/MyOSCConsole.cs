using UnityEngine;
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
        AllowAll,   // 全て転送
        AllowList,  // 以下のルールに一致するもの「だけ」転送
        BlockList   // 以下のルールに一致するもの「以外」を転送
    }

    public enum ExpectedValueType
    {
        Any,    // 型はなんでもOK
        Int,    // 整数のみ
        Float,  // 小数のみ
        String  // 文字列のみ
    }

    [System.Serializable]
    public class OSCForwardRule
    {
        public string address = "/example";
        
        [Tooltip("引数（インデックスや型）での絞り込みを有効にするか")]
        public bool filterByArgument = false;
        
        [Tooltip("チェックする引数のインデックス（何番目か）")]
        public int valueIndex = 0;
        
        [Tooltip("期待するデータ型（Int, Float等）")]
        public ExpectedValueType expectedType = ExpectedValueType.Any;
    }

    #endregion

    [Header("監視したい条件を追加")]
    public List<OSCFilter> filterList = new List<OSCFilter>();

    [Header("表示設定")]
    public int maxLogCount = 20;

    [Header("転送(Forward)設定")]
    [Tooltip("転送機能を有効にするか")]
    public bool enableForwarding = true;
    public string targetHost = "127.0.0.1";
    public int targetPort = 8000;

    [Tooltip("転送フィルターの動作モード")]
    public ForwardFilterMode forwardFilterMode = ForwardFilterMode.AllowAll;
    
    [Header("転送フィルターのルール設定")]
    [Tooltip("AllowList / BlockList で使用するルール")]
    public List<OSCForwardRule> forwardRules = new List<OSCForwardRule>();

    private List<string> filteredLogs = new List<string>();
    private HashSet<OSCReceiver> hookedReceivers = new HashSet<OSCReceiver>();
    private Vector2 scrollPosition;

    private OSCTransmitter _forwardTransmitter;

    void Start()
    {
        // 転送用のTransmitterを自動生成
        _forwardTransmitter = gameObject.AddComponent<OSCTransmitter>();
        _forwardTransmitter.RemoteHost = targetHost;
        _forwardTransmitter.RemotePort = targetPort;
    }

    void Update()
    {
        if (_forwardTransmitter != null)
        {
            if (_forwardTransmitter.RemoteHost != targetHost) _forwardTransmitter.RemoteHost = targetHost;
            if (_forwardTransmitter.RemotePort != targetPort) _forwardTransmitter.RemotePort = targetPort;
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
    }

    private void OnAnyMessageReceived(OSCMessage message)
    {
        // --- 1. 監視(Monitor)用処理 ---
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
                        if (filteredLogs.Count > maxLogCount)
                            filteredLogs.RemoveAt(filteredLogs.Count - 1);
                    }
                }
            }
        }

        // --- 2. 転送(Forward)用処理 ---
        if (enableForwarding && _forwardTransmitter != null)
        {
            bool shouldSend = false;

            if (forwardFilterMode == ForwardFilterMode.AllowAll)
            {
                shouldSend = true;
            }
            else
            {
                bool matchAnyRule = false;
                foreach (var rule in forwardRules)
                {
                    if (IsMatchRule(message, rule))
                    {
                        matchAnyRule = true;
                        break;
                    }
                }

                if (forwardFilterMode == ForwardFilterMode.AllowList)
                {
                    shouldSend = matchAnyRule; // ルールに合致したものだけ送る
                }
                else if (forwardFilterMode == ForwardFilterMode.BlockList)
                {
                    shouldSend = !matchAnyRule; // ルールに合致したもの「以外」を送る
                }
            }

            if (shouldSend)
            {
                _forwardTransmitter.Send(message);
            }
        }
    }

    /// <summary>
    /// メッセージが転送ルールに合致しているかを判定する
    /// </summary>
    private bool IsMatchRule(OSCMessage message, OSCForwardRule rule)
    {
        // 1. アドレスが不一致なら除外
        if (message.Address != rule.address) return false;

        // 2. 引数による絞り込みが無効なら、アドレスが一致した時点で条件クリア
        if (!rule.filterByArgument) return true;

        // 3. 引数の数が足りない場合は除外
        if (rule.valueIndex < 0 || rule.valueIndex >= message.Values.Count) return false;

        // 4. 引数の型(Type)をチェック
        var val = message.Values[rule.valueIndex];
        switch (rule.expectedType)
        {
            case ExpectedValueType.Int:
                if (val.Type != extOSC.OSCValueType.Int) return false;
                break;
            case ExpectedValueType.Float:
                if (val.Type != extOSC.OSCValueType.Float) return false;
                break;
            case ExpectedValueType.String:
                if (val.Type != extOSC.OSCValueType.String) return false;
                break;
            case ExpectedValueType.Any:
            default:
                break; // Anyなら何でもOK
        }

        return true;
    }

    void OnGUI()
    {
        GUI.backgroundColor = new Color(0, 0.1f, 0.3f, 0.95f);
        GUILayout.BeginArea(new Rect(20, 20, 500, 600), "OSC TARGET MONITOR & FORWARDER", "Window");
        
        string forwardStatus = enableForwarding ? $"<color=green>Forwarding to {targetHost}:{targetPort}</color>" : "<color=red>Forwarding OFF</color>";
        GUILayout.Label(forwardStatus, new GUIStyle(GUI.skin.label) { richText = true });
        GUILayout.Space(5);

        scrollPosition = GUILayout.BeginScrollView(scrollPosition);
        GUIStyle logStyle = new GUIStyle(GUI.skin.label) { fontSize = 13 };
        logStyle.normal.textColor = Color.cyan;

        lock(filteredLogs)
        {
            foreach (var log in filteredLogs)
            {
                GUILayout.Label(log, logStyle);
            }
        }

        GUILayout.EndScrollView();
        if (GUILayout.Button("Clear Logs")) filteredLogs.Clear();
        GUILayout.EndArea();
    }
}