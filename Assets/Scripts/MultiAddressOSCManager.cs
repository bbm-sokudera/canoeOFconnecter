using UnityEngine;
using UnityEngine.Events;
using extOSC;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// InputCentralManagerからの通知を受け取り、KeyCodeが一致するショートカットを実行する受動型OSCマネージャー
/// </summary>
public class MultiAddressOSCManager : MonoBehaviour
{
    #region Serializable Classes

    [Serializable]
    public class OSCInternalParam 
    {
        public string name;
        public int index;
        public string type = "float";
        public float floatValue;
        public int intValue;
        public string stringValue;
    }

    [Serializable]
    public class OSCInternalReceiveAddr
    {
        public string address = "/position";
        public int port = 7001;
        public List<OSCInternalParam> parameters = new List<OSCInternalParam>();
    }

    [Serializable]
    public class OSCInternalTransmitAddr
    {
        public string address = "/output";
        public string host = "127.0.0.1";
        public int port = 7002;
        public List<OSCInternalParam> parameters = new List<OSCInternalParam>();
    }

    [Serializable]
    public class OSCInternalConfig
    {
        public List<OSCInternalReceiveAddr> receive = new List<OSCInternalReceiveAddr>();
        public List<OSCInternalTransmitAddr> transmit = new List<OSCInternalTransmitAddr>();
    }

    [Serializable]
    public class OSCInternalShortcut
    {
        public string label = "New Shortcut";
        [Tooltip("ここに設定したキーとInputCentralManagerからの入力が一致した時に実行されます")]
        public KeyCode key; 
        public string targetAddress;
        public string targetParameterName;
        public float sendFloatValue;
        public int sendIntValue;
        public string sendStringValue;
    }

    #endregion

    #region Inspector Settings

    [Header("Configuration")]
    public string configFilePath = "multi_osc_config.json";

    [Header("Key Shortcuts")]
    [Tooltip("リストの順番に関係なく、設定した'Key'が一致するものが実行されます")]
    public List<OSCInternalShortcut> keyShortcuts = new List<OSCInternalShortcut>();

    [Header("Debug")]
    public bool enableDebugLog = true;

    [Header("Events")]
    public UnityEvent<string> onMessageReceived;

    [SerializeField]
    private OSCInternalConfig _config;

    #endregion

    #region Private Variables
    private Dictionary<int, OSCReceiver> _receivers = new Dictionary<int, OSCReceiver>();
    private Dictionary<string, OSCTransmitter> _transmitters = new Dictionary<string, OSCTransmitter>();
    private Dictionary<string, OSCInternalParam> _allParams = new Dictionary<string, OSCInternalParam>();
    #endregion

    #region Unity Lifecycle

    void Start()
    {
        LoadConfiguration();
        InitializeOSC();
    }

    void OnDestroy()
    {
        foreach (var receiver in _receivers.Values) if (receiver != null) receiver.Close();
        foreach (var transmitter in _transmitters.Values) if (transmitter != null) transmitter.Close();
    }

    #endregion

    #region Input Central Interface (外部からの命令)

    /// <summary>
    /// 指定された番号(0-9)に基づき、対応するKeyCodeが設定されたショートカットを探して実行する
    /// </summary>
    public void SendByNumber(int number)
    {
        // 入力された数字から、ターゲットとなるKeyCodeを2種類（メインキーとテンキー）生成
        KeyCode targetAlpha = (KeyCode)((int)KeyCode.Alpha0 + number);
        KeyCode targetKeypad = (KeyCode)((int)KeyCode.Keypad0 + number);

        // リストの中から、設定されたKeyがAlphaかKeypadのどちらかに一致するものをすべて探す
        // (同じキーに複数登録があればすべて実行されます)
        var matches = keyShortcuts.Where(s => s.key == targetAlpha || s.key == targetKeypad).ToList();

        if (matches.Count > 0)
        {
            foreach (var shortcut in matches)
            {
                ExecuteShortcut(shortcut);
            }
        }
        else
        {
            LogDebug($"No shortcut found for Key: {targetAlpha} / {targetKeypad}");
        }
    }

    private void ExecuteShortcut(OSCInternalShortcut shortcut)
    {
        if (_allParams.TryGetValue(shortcut.targetParameterName, out var param))
        {
            if (param.type.ToLower() == "float") param.floatValue = shortcut.sendFloatValue;
            else if (param.type.ToLower() == "int") param.intValue = shortcut.sendIntValue;
            else if (param.type.ToLower() == "string") param.stringValue = shortcut.sendStringValue;

            LogDebug($"Executed: [{shortcut.label}] via Key: {shortcut.key}");
            SendMessage(shortcut.targetAddress);
        }
    }

    #endregion

    #region Public API (External Access)

    public float GetFloat(string paramName, float defaultValue = 0f) 
        => _allParams.TryGetValue(paramName, out var p) ? p.floatValue : defaultValue;

    public int GetInt(string paramName, int defaultValue = 0) 
        => _allParams.TryGetValue(paramName, out var p) ? p.intValue : defaultValue;

    public string GetString(string paramName, string defaultValue = "") 
        => _allParams.TryGetValue(paramName, out var p) ? p.stringValue : defaultValue;

    public Vector3 GetVector3(string xParam, string yParam, string zParam)
        => new Vector3(GetFloat(xParam), GetFloat(yParam), GetFloat(zParam));

    public void SetFloat(string name, float v) { if (_allParams.TryGetValue(name, out var p)) p.floatValue = v; }
    public void SetInt(string name, int v) { if (_allParams.TryGetValue(name, out var p)) p.intValue = v; }
    public void SetString(string name, string v) { if (_allParams.TryGetValue(name, out var p)) p.stringValue = v; }

    public OSCInternalConfig GetConfig() => _config;

    #endregion

    #region Configuration Management

    public void LoadConfiguration()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, configFilePath);
        if (!File.Exists(filePath)) { RefreshParameterDictionary(); return; }
        try {
            _config = JsonUtility.FromJson<OSCInternalConfig>(File.ReadAllText(filePath));
            RefreshParameterDictionary();
            LogDebug("Config Loaded.");
        } catch (Exception e) { Debug.LogError($"OSC Load Error: {e.Message}"); }
    }

    private void RefreshParameterDictionary()
    {
        _allParams.Clear();
        if (_config == null) return;
        foreach (var addr in _config.receive) foreach (var p in addr.parameters) _allParams[p.name] = p;
        foreach (var addr in _config.transmit) foreach (var p in addr.parameters) _allParams[p.name] = p;
    }

    public void SaveConfiguration()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, configFilePath);
        File.WriteAllText(filePath, JsonUtility.ToJson(_config, true));
        LogDebug("Config Saved.");
    }

    #endregion

    #region OSC Logic

    void InitializeOSC()
    {
        if (_config == null) return;
        foreach (var addr in _config.receive)
        {
            if (!_receivers.ContainsKey(addr.port)) {
                var r = gameObject.AddComponent<OSCReceiver>();
                r.LocalPort = addr.port;
                _receivers[addr.port] = r;
            }
            _receivers[addr.port].Bind(addr.address, (msg) => {
                UpdateParamsFromMessage(addr.address, msg);
                onMessageReceived?.Invoke(addr.address);
            });
        }
        foreach (var addr in _config.transmit)
        {
            string key = $"{addr.host}:{addr.port}";
            if (!_transmitters.ContainsKey(key)) {
                var t = gameObject.AddComponent<OSCTransmitter>();
                t.RemoteHost = addr.host; t.RemotePort = addr.port;
                _transmitters[key] = t;
            }
        }
    }

    void UpdateParamsFromMessage(string address, OSCMessage message)
    {
        var addrConfig = _config.receive.FirstOrDefault(a => a.address == address);
        if (addrConfig == null) return;
        foreach (var param in addrConfig.parameters)
        {
            if (param.index >= message.Values.Count) continue;
            var val = message.Values[param.index];
            if (param.type.ToLower() == "float") param.floatValue = val.FloatValue;
            else if (param.type.ToLower() == "int") param.intValue = val.IntValue;
            else if (param.type.ToLower() == "string") param.stringValue = val.StringValue;
        }
    }

    public void SendMessage(string address)
    {
        var addrConfig = _config.transmit.FirstOrDefault(a => a.address == address);
        if (addrConfig == null) return;
        string key = $"{addrConfig.host}:{addrConfig.port}";
        if (_transmitters.TryGetValue(key, out OSCTransmitter transmitter)) {
            var message = new OSCMessage(address);
            foreach (var param in addrConfig.parameters.OrderBy(p => p.index)) {
                if (param.type.ToLower() == "float") message.AddValue(OSCValue.Float(param.floatValue));
                else if (param.type.ToLower() == "int") message.AddValue(OSCValue.Int(param.intValue));
                else if (param.type.ToLower() == "string") message.AddValue(OSCValue.String(param.stringValue));
            }
            transmitter.Send(message);
        }
    }

    void LogDebug(string message) { if (enableDebugLog) Debug.Log($"[MultiAddressOSCManager] {message}"); }

    #endregion
}

#if UNITY_EDITOR
[CustomEditor(typeof(MultiAddressOSCManager))]
public class MultiAddressOSCManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawDefaultInspector();
        MultiAddressOSCManager m = (MultiAddressOSCManager)target;
        GUILayout.Space(10);
        if (GUILayout.Button("Save All to JSON")) m.SaveConfiguration();
        serializedObject.ApplyModifiedProperties();
    }
}
#endif