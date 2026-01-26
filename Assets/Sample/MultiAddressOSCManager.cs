using UnityEngine;
using UnityEngine.Events;
using extOSC;
using System.Collections.Generic;
using System.IO;
using System.Linq;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// JSON設定ファイルで複数のOSCアドレスを管理する汎用OSCマネージャー
/// インスペクターからの入力と送信テスト、Vector3の一括取得に対応
/// </summary>
public class MultiAddressOSCManager : MonoBehaviour
{
    #region Serializable Classes

    [System.Serializable]
    public class OSCParameter
    {
        public string name;          // パラメータ名
        public int index;            // OSCメッセージ内のインデックス
        public string type = "float"; // データ型（float, int, string）

        // インスペクターで直接入力・保存できるようにSerializedに変更
        public float floatValue;
        public int intValue;
        public string stringValue;
    }

    [System.Serializable]
    public class OSCReceiveAddress
    {
        public string address = "/position";
        public int port = 7001;
        public List<OSCParameter> parameters = new List<OSCParameter>();
    }

    [System.Serializable]
    public class OSCTransmitAddress
    {
        public string address = "/output";
        public string host = "127.0.0.1";
        public int port = 7002;
        public List<OSCParameter> parameters = new List<OSCParameter>();
    }

    [System.Serializable]
    public class OSCConfig
    {
        public List<OSCReceiveAddress> receive = new List<OSCReceiveAddress>();
        public List<OSCTransmitAddress> transmit = new List<OSCTransmitAddress>();
    }

    #endregion

    #region Inspector Settings

    [Header("Configuration")]
    [Tooltip("JSON設定ファイルのパス（StreamingAssetsからの相対パス）")]
    public string configFilePath = "multi_osc_config.json";

    [Header("Debug")]
    [Tooltip("デバッグログを出力する")]
    public bool enableDebugLog = true;

    [Header("Events")]
    [Tooltip("OSCメッセージを受信した時のイベント（アドレス名を渡す）")]
    public UnityEvent<string> onMessageReceived;

    [SerializeField]
    private OSCConfig _config;

    #endregion

    #region Private Variables

    private Dictionary<int, OSCReceiver> _receivers = new Dictionary<int, OSCReceiver>();
    private Dictionary<string, OSCTransmitter> _transmitters = new Dictionary<string, OSCTransmitter>();
    private Dictionary<string, OSCParameter> _allParams = new Dictionary<string, OSCParameter>();

    #endregion

    #region Unity Lifecycle

    void Start()
    {
        LoadConfiguration();
        InitializeOSC();
    }

    void OnDestroy()
    {
        foreach (var receiver in _receivers.Values)
        {
            if (receiver != null) receiver.Close();
        }

        foreach (var transmitter in _transmitters.Values)
        {
            if (transmitter != null) transmitter.Close();
        }
    }

    #endregion

    #region Configuration Management

    public void LoadConfiguration()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, configFilePath);

        if (!File.Exists(filePath))
        {
            LogDebug($"Configuration file not found: {filePath}. Creating default configuration.");
            CreateDefaultConfiguration();
            SaveConfiguration();
            return;
        }

        try
        {
            string json = File.ReadAllText(filePath);
            _config = JsonUtility.FromJson<OSCConfig>(json);
            RefreshParameterDictionary();

            LogDebug($"Configuration loaded: {_config.receive.Count} receive, {_config.transmit.Count} transmit addresses");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MultiAddressOSCManager] Failed to load configuration: {e.Message}");
            CreateDefaultConfiguration();
        }
    }

    private void RefreshParameterDictionary()
    {
        _allParams.Clear();
        if (_config == null) return;

        foreach (var addr in _config.receive)
            foreach (var param in addr.parameters) _allParams[param.name] = param;
        foreach (var addr in _config.transmit)
            foreach (var param in addr.parameters) _allParams[param.name] = param;
    }

    void CreateDefaultConfiguration()
    {
        _config = new OSCConfig();

        // デフォルト設定の構築
        var stateAddr = new OSCReceiveAddress { address = "/state", port = 7001 };
        stateAddr.parameters.Add(new OSCParameter { name = "State", index = 0, type = "int" });
        _config.receive.Add(stateAddr);

        var posAddr = new OSCReceiveAddress { address = "/position", port = 7001 };
        posAddr.parameters.Add(new OSCParameter { name = "PositionX", index = 0, type = "float" });
        posAddr.parameters.Add(new OSCParameter { name = "PositionY", index = 1, type = "float" });
        posAddr.parameters.Add(new OSCParameter { name = "PositionZ", index = 2, type = "float" });
        _config.receive.Add(posAddr);

        var cropboxAddr = new OSCTransmitAddress { address = "/cropbox/autoheight", host = "127.0.0.1", port = 8811 };
        cropboxAddr.parameters.Add(new OSCParameter { name = "AutoHeight", index = 0, type = "int" });
        cropboxAddr.parameters.Add(new OSCParameter { name = "SetHeightMargin", index = 1, type = "float" });
        _config.transmit.Add(cropboxAddr);

        RefreshParameterDictionary();
    }

    public void SaveConfiguration()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, configFilePath);
        string directory = Path.GetDirectoryName(filePath);
        if (!Directory.Exists(directory)) Directory.CreateDirectory(directory);

        try
        {
            string json = JsonUtility.ToJson(_config, true);
            File.WriteAllText(filePath, json);
            LogDebug($"Configuration saved to: {filePath}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MultiAddressOSCManager] Failed to save configuration: {e.Message}");
        }
    }

    #endregion

    #region OSC Logic

    void InitializeOSC()
    {
        if (_config == null) return;

        foreach (var addr in _config.receive)
        {
            if (!_receivers.ContainsKey(addr.port))
            {
                var receiver = gameObject.AddComponent<OSCReceiver>();
                receiver.LocalPort = addr.port;
                _receivers[addr.port] = receiver;
            }
            _receivers[addr.port].Bind(addr.address, (msg) => OnOSCMessageReceived(addr.address, msg));
        }

        foreach (var addr in _config.transmit)
        {
            string key = $"{addr.host}:{addr.port}";
            if (!_transmitters.ContainsKey(key))
            {
                var transmitter = gameObject.AddComponent<OSCTransmitter>();
                transmitter.RemoteHost = addr.host;
                transmitter.RemotePort = addr.port;
                _transmitters[key] = transmitter;
            }
        }
    }

    void OnOSCMessageReceived(string address, OSCMessage message)
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
        onMessageReceived?.Invoke(address);
    }

    public void SendMessage(string address)
    {
        if (_config == null) return;
        var addrConfig = _config.transmit.FirstOrDefault(a => a.address == address);
        if (addrConfig == null) return;

        string key = $"{addrConfig.host}:{addrConfig.port}";
        if (!_transmitters.TryGetValue(key, out OSCTransmitter transmitter)) return;

        var message = new OSCMessage(address);
        foreach (var param in addrConfig.parameters.OrderBy(p => p.index))
        {
            if (param.type.ToLower() == "float") message.AddValue(OSCValue.Float(param.floatValue));
            else if (param.type.ToLower() == "int") message.AddValue(OSCValue.Int(param.intValue));
            else if (param.type.ToLower() == "string") message.AddValue(OSCValue.String(param.stringValue));
        }
        transmitter.Send(message);
    }

    #endregion

    #region Public API (Getters/Setters)

    public float GetFloat(string paramName, float defaultValue = 0f) => _allParams.TryGetValue(paramName, out var p) ? p.floatValue : defaultValue;
    public int GetInt(string paramName, int defaultValue = 0) => _allParams.TryGetValue(paramName, out var p) ? p.intValue : defaultValue;
    public string GetString(string paramName, string defaultValue = "") => _allParams.TryGetValue(paramName, out var p) ? p.stringValue : defaultValue;

    // 今回不足していた GetVector3 を復活
    public Vector3 GetVector3(string xParamName, string yParamName, string zParamName)
    {
        return new Vector3(
            GetFloat(xParamName),
            GetFloat(yParamName),
            GetFloat(zParamName)
        );
    }

    public void SetFloat(string name, float v) { if (_allParams.TryGetValue(name, out var p)) p.floatValue = v; }
    public void SetInt(string name, int v) { if (_allParams.TryGetValue(name, out var p)) p.intValue = v; }
    public void SetString(string name, string v) { if (_allParams.TryGetValue(name, out var p)) p.stringValue = v; }

    public OSCConfig GetConfig() => _config;

    #endregion

    void LogDebug(string message) { if (enableDebugLog) Debug.Log($"[MultiAddressOSCManager] {message}"); }
}

#region Editor Extension

#if UNITY_EDITOR
[CustomEditor(typeof(MultiAddressOSCManager))]
public class MultiAddressOSCManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        MultiAddressOSCManager manager = (MultiAddressOSCManager)target;
        var config = manager.GetConfig();
        if (config == null || config.transmit == null) return;

        GUILayout.Space(15);
        GUILayout.Label("Debug: Manual Transmission", EditorStyles.boldLabel);
        GUI.backgroundColor = new Color(0.8f, 0.9f, 1f);
        foreach (var addr in config.transmit)
        {
            EditorGUILayout.BeginHorizontal("box");
            EditorGUILayout.LabelField(addr.address, EditorStyles.miniLabel, GUILayout.Width(120));
            if (GUILayout.Button($"Send to {addr.port}")) manager.SendMessage(addr.address);
            EditorGUILayout.EndHorizontal();
        }
        GUI.backgroundColor = Color.white;
        GUILayout.Space(10);
        if (GUILayout.Button("Save All Values to JSON", GUILayout.Height(30))) manager.SaveConfiguration();
    }
}
#endif

#endregion