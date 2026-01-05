using UnityEngine;
using UnityEngine.Events;
using extOSC;
using System.Collections.Generic;
using System.IO;
using System.Linq;

/// <summary>
/// JSON設定ファイルで複数のOSCアドレスを管理する汎用OSCマネージャー
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

        [System.NonSerialized]
        public float floatValue;
        [System.NonSerialized]
        public int intValue;
        [System.NonSerialized]
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

    #endregion

    #region Private Variables

    private Dictionary<int, OSCReceiver> _receivers = new Dictionary<int, OSCReceiver>(); // port → receiver
    private OSCTransmitter _transmitter; // 単一のtransmitter（送信先は動的に変更）
    private OSCConfig _config;
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
            if (receiver != null)
                receiver.Close();
        }

        if (_transmitter != null)
            _transmitter.Close();
    }

    #endregion

    #region Configuration Management

    /// <summary>
    /// JSON設定ファイルをロード
    /// </summary>
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

            // パラメータを辞書に登録
            _allParams.Clear();
            foreach (var addr in _config.receive)
            {
                foreach (var param in addr.parameters)
                {
                    _allParams[param.name] = param;
                }
            }
            foreach (var addr in _config.transmit)
            {
                foreach (var param in addr.parameters)
                {
                    _allParams[param.name] = param;
                }
            }

            LogDebug($"Configuration loaded: {_config.receive.Count} receive addresses, {_config.transmit.Count} transmit addresses");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[MultiAddressOSCManager] Failed to load configuration: {e.Message}");
            CreateDefaultConfiguration();
        }
    }

    /// <summary>
    /// デフォルト設定を作成
    /// </summary>
    void CreateDefaultConfiguration()
    {
        _config = new OSCConfig();

        // 受信アドレス: /state
        var stateAddr = new OSCReceiveAddress
        {
            address = "/state",
            port = 7001,
            parameters = new List<OSCParameter>
            {
                new OSCParameter { name = "State", index = 0, type = "int" }
            }
        };
        _config.receive.Add(stateAddr);

        // 受信アドレス: /position
        var posAddr = new OSCReceiveAddress
        {
            address = "/position",
            port = 7001,
            parameters = new List<OSCParameter>
            {
                new OSCParameter { name = "PositionX", index = 0, type = "float" },
                new OSCParameter { name = "PositionY", index = 1, type = "float" },
                new OSCParameter { name = "PositionZ", index = 2, type = "float" },
                new OSCParameter { name = "VectorX", index = 3, type = "float" },
                new OSCParameter { name = "VectorY", index = 4, type = "float" },
                new OSCParameter { name = "VectorZ", index = 5, type = "float" },
                new OSCParameter { name = "BoxX", index = 6, type = "float" },
                new OSCParameter { name = "BoxY", index = 7, type = "float" },
                new OSCParameter { name = "BoxZ", index = 8, type = "float" }
            }
        };
        _config.receive.Add(posAddr);

        // 送信アドレス: /quiz
        var quizAddr = new OSCTransmitAddress
        {
            address = "/quiz",
            host = "127.0.0.1",
            port = 7002,
            parameters = new List<OSCParameter>
            {
                new OSCParameter { name = "QuizChoice", index = 0, type = "int" }
            }
        };
        _config.transmit.Add(quizAddr);

        // 送信アドレス: /paddle
        var paddleAddr = new OSCTransmitAddress
        {
            address = "/paddle",
            host = "127.0.0.1",
            port = 7002,
            parameters = new List<OSCParameter>
            {
                new OSCParameter { name = "PaddleDirection", index = 0, type = "int" }
            }
        };
        _config.transmit.Add(paddleAddr);

        // 送信アドレス: /cropbox/autoheight
        var cropboxAddr = new OSCTransmitAddress
        {
            address = "/cropbox/autoheight",
            host = "127.0.0.1",
            port = 7002,
            parameters = new List<OSCParameter>
            {
                new OSCParameter { name = "AutoHeight", index = 0, type = "int" }
            }
        };
        _config.transmit.Add(cropboxAddr);

        // パラメータ登録
        _allParams.Clear();
        foreach (var addr in _config.receive)
        {
            foreach (var param in addr.parameters)
            {
                _allParams[param.name] = param;
            }
        }
        foreach (var addr in _config.transmit)
        {
            foreach (var param in addr.parameters)
            {
                _allParams[param.name] = param;
            }
        }
    }

    /// <summary>
    /// 設定をJSONファイルに保存
    /// </summary>
    public void SaveConfiguration()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, configFilePath);
        string directory = Path.GetDirectoryName(filePath);

        if (!Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

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

    #region OSC Initialization

    /// <summary>
    /// OSC受信機・送信機を初期化
    /// </summary>
    void InitializeOSC()
    {
        if (_config == null)
        {
            Debug.LogError("[MultiAddressOSCManager] Configuration is null.");
            return;
        }

        // Receivers初期化（ポートごとに1つのReceiver）
        foreach (var addr in _config.receive)
        {
            if (!_receivers.ContainsKey(addr.port))
            {
                var receiver = gameObject.AddComponent<OSCReceiver>();
                receiver.LocalPort = addr.port;
                _receivers[addr.port] = receiver;
                LogDebug($"OSC Receiver created on port {addr.port}");
            }

            // アドレスをバインド
            var receiver = _receivers[addr.port];
            receiver.Bind(addr.address, (msg) => OnOSCMessageReceived(addr.address, msg));
            LogDebug($"Bound {addr.address} on port {addr.port}");
        }

        // Transmitter初期化（1つだけ、送信時にアドレスを指定）
        if (_config.transmit.Count > 0)
        {
            _transmitter = gameObject.AddComponent<OSCTransmitter>();
            // デフォルトは最初の送信先
            var first = _config.transmit[0];
            _transmitter.RemoteHost = first.host;
            _transmitter.RemotePort = first.port;
            LogDebug($"OSC Transmitter initialized: {first.host}:{first.port}");
        }
    }

    #endregion

    #region OSC Receive

    /// <summary>
    /// OSCメッセージ受信時のコールバック
    /// </summary>
    void OnOSCMessageReceived(string address, OSCMessage message)
    {
        var addrConfig = _config.receive.FirstOrDefault(a => a.address == address);
        if (addrConfig == null)
            return;

        LogDebug($"Received OSC message on {address} with {message.Values.Count} values");

        // 各パラメータの値を更新
        foreach (var param in addrConfig.parameters)
        {
            if (param.index >= message.Values.Count)
                continue;

            switch (param.type.ToLower())
            {
                case "float":
                    param.floatValue = message.Values[param.index].FloatValue;
                    break;
                case "int":
                    param.intValue = message.Values[param.index].IntValue;
                    break;
                case "string":
                    param.stringValue = message.Values[param.index].StringValue;
                    break;
            }
        }

        // イベント発火
        onMessageReceived?.Invoke(address);
    }

    #endregion

    #region OSC Transmit

    /// <summary>
    /// 指定したアドレスにOSCメッセージを送信
    /// </summary>
    public void SendMessage(string address)
    {
        if (_transmitter == null || _config == null)
            return;

        var addrConfig = _config.transmit.FirstOrDefault(a => a.address == address);
        if (addrConfig == null)
        {
            Debug.LogWarning($"[MultiAddressOSCManager] Transmit address not found: {address}");
            return;
        }

        // Transmitterの送信先を更新
        _transmitter.RemoteHost = addrConfig.host;
        _transmitter.RemotePort = addrConfig.port;

        var message = new OSCMessage(address);

        // パラメータをインデックス順にソート
        var sortedParams = addrConfig.parameters.OrderBy(p => p.index).ToList();

        foreach (var param in sortedParams)
        {
            switch (param.type.ToLower())
            {
                case "float":
                    message.AddValue(OSCValue.Float(param.floatValue));
                    break;
                case "int":
                    message.AddValue(OSCValue.Int(param.intValue));
                    break;
                case "string":
                    message.AddValue(OSCValue.String(param.stringValue));
                    break;
            }
        }

        _transmitter.Send(message);
        LogDebug($"Sent OSC message to {address}");
    }

    #endregion

    #region Public API

    /// <summary>
    /// パラメータのFloat値を取得
    /// </summary>
    public float GetFloat(string paramName, float defaultValue = 0f)
    {
        if (_allParams.TryGetValue(paramName, out OSCParameter param))
        {
            return param.floatValue;
        }
        return defaultValue;
    }

    /// <summary>
    /// パラメータのInt値を取得
    /// </summary>
    public int GetInt(string paramName, int defaultValue = 0)
    {
        if (_allParams.TryGetValue(paramName, out OSCParameter param))
        {
            return param.intValue;
        }
        return defaultValue;
    }

    /// <summary>
    /// パラメータのString値を取得
    /// </summary>
    public string GetString(string paramName, string defaultValue = "")
    {
        if (_allParams.TryGetValue(paramName, out OSCParameter param))
        {
            return param.stringValue;
        }
        return defaultValue;
    }

    /// <summary>
    /// パラメータのFloat値を設定
    /// </summary>
    public void SetFloat(string paramName, float value)
    {
        if (_allParams.TryGetValue(paramName, out OSCParameter param))
        {
            param.floatValue = value;
        }
        else
        {
            Debug.LogWarning($"[MultiAddressOSCManager] Parameter not found: {paramName}");
        }
    }

    /// <summary>
    /// パラメータのInt値を設定
    /// </summary>
    public void SetInt(string paramName, int value)
    {
        if (_allParams.TryGetValue(paramName, out OSCParameter param))
        {
            param.intValue = value;
        }
        else
        {
            Debug.LogWarning($"[MultiAddressOSCManager] Parameter not found: {paramName}");
        }
    }

    /// <summary>
    /// パラメータのString値を設定
    /// </summary>
    public void SetString(string paramName, string value)
    {
        if (_allParams.TryGetValue(paramName, out OSCParameter param))
        {
            param.stringValue = value;
        }
        else
        {
            Debug.LogWarning($"[MultiAddressOSCManager] Parameter not found: {paramName}");
        }
    }

    /// <summary>
    /// Vector3として取得
    /// </summary>
    public Vector3 GetVector3(string xParamName, string yParamName, string zParamName)
    {
        return new Vector3(
            GetFloat(xParamName),
            GetFloat(yParamName),
            GetFloat(zParamName)
        );
    }

    /// <summary>
    /// 全設定を取得
    /// </summary>
    public OSCConfig GetConfig()
    {
        return _config;
    }

    #endregion

    #region Debug

    void LogDebug(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[MultiAddressOSCManager] {message}");
        }
    }

    #endregion
}
