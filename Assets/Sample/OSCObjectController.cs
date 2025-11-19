using UnityEngine;
using extOSC;

/// <summary>
/// OSCメッセージを受信してUnityオブジェクトを制御するサンプル
/// 位置、回転、スケールなどをOSCで制御できます
/// </summary>
public class OSCObjectController : MonoBehaviour
{
    #region Public Vars

    [Header("OSC Settings")]
    [Tooltip("OSC受信ポート番号")]
    public int receivePort = 7001;

    [Header("Control Target")]
    [Tooltip("制御対象のGameObject（空の場合は自身を制御）")]
    public GameObject targetObject;

    [Header("OSC Addresses")]
    [Tooltip("位置制御用のOSCアドレス (x, y, z)")]
    public string positionAddress = "/object/position";

    [Tooltip("回転制御用のOSCアドレス (x, y, z)")]
    public string rotationAddress = "/object/rotation";

    [Tooltip("スケール制御用のOSCアドレス (x, y, z) または (uniform)")]
    public string scaleAddress = "/object/scale";

    [Tooltip("色制御用のOSCアドレス (r, g, b) または (r, g, b, a)")]
    public string colorAddress = "/object/color";

    [Tooltip("表示/非表示制御用のOSCアドレス (0 or 1)")]
    public string visibilityAddress = "/object/visible";

    [Header("Control Options")]
    [Tooltip("位置の変更を有効にする")]
    public bool enablePositionControl = true;

    [Tooltip("回転の変更を有効にする")]
    public bool enableRotationControl = true;

    [Tooltip("スケールの変更を有効にする")]
    public bool enableScaleControl = true;

    [Tooltip("色の変更を有効にする")]
    public bool enableColorControl = true;

    [Tooltip("表示/非表示の制御を有効にする")]
    public bool enableVisibilityControl = true;

    #endregion

    #region Private Vars

    private OSCReceiver _receiver;
    private Transform _targetTransform;
    private Renderer _targetRenderer;
    private Material _targetMaterial;

    #endregion

    #region Unity Methods

    void Start()
    {
        // ターゲットオブジェクトの設定
        if (targetObject == null)
        {
            targetObject = gameObject;
        }

        _targetTransform = targetObject.transform;
        _targetRenderer = targetObject.GetComponent<Renderer>();

        // マテリアルのインスタンスを作成（元のマテリアルを変更しないため）
        if (_targetRenderer != null)
        {
            _targetMaterial = _targetRenderer.material;
        }

        // OSC Receiverの作成
        _receiver = gameObject.AddComponent<OSCReceiver>();
        _receiver.LocalPort = receivePort;

        // OSCメッセージのバインド
        if (enablePositionControl)
        {
            _receiver.Bind(positionAddress, OnPositionReceived);
            Debug.Log($"Position control enabled: {positionAddress}");
        }

        if (enableRotationControl)
        {
            _receiver.Bind(rotationAddress, OnRotationReceived);
            Debug.Log($"Rotation control enabled: {rotationAddress}");
        }

        if (enableScaleControl)
        {
            _receiver.Bind(scaleAddress, OnScaleReceived);
            Debug.Log($"Scale control enabled: {scaleAddress}");
        }

        if (enableColorControl && _targetRenderer != null)
        {
            _receiver.Bind(colorAddress, OnColorReceived);
            Debug.Log($"Color control enabled: {colorAddress}");
        }

        if (enableVisibilityControl && _targetRenderer != null)
        {
            _receiver.Bind(visibilityAddress, OnVisibilityReceived);
            Debug.Log($"Visibility control enabled: {visibilityAddress}");
        }

        Debug.Log($"OSC Object Controller started on port {receivePort}");
        Debug.Log($"Controlling object: {targetObject.name}");
    }

    void OnDestroy()
    {
        if (_receiver != null)
        {
            _receiver.Close();
        }

        // マテリアルのインスタンスを破棄
        if (_targetMaterial != null)
        {
            Destroy(_targetMaterial);
        }
    }

    #endregion

    #region OSC Callback Methods

    /// <summary>
    /// 位置制御のOSCメッセージを受信
    /// 形式: /object/position x y z
    /// </summary>
    private void OnPositionReceived(OSCMessage message)
    {
        if (message.Values.Count >= 3)
        {
            float x = message.Values[0].FloatValue;
            float y = message.Values[1].FloatValue;
            float z = message.Values[2].FloatValue;

            _targetTransform.position = new Vector3(x, y, z);
            Debug.Log($"[OSC] Position set to: ({x}, {y}, {z})");
        }
    }

    /// <summary>
    /// 回転制御のOSCメッセージを受信
    /// 形式: /object/rotation x y z (Euler angles)
    /// </summary>
    private void OnRotationReceived(OSCMessage message)
    {
        if (message.Values.Count >= 3)
        {
            float x = message.Values[0].FloatValue;
            float y = message.Values[1].FloatValue;
            float z = message.Values[2].FloatValue;

            _targetTransform.rotation = Quaternion.Euler(x, y, z);
            Debug.Log($"[OSC] Rotation set to: ({x}, {y}, {z})");
        }
    }

    /// <summary>
    /// スケール制御のOSCメッセージを受信
    /// 形式1: /object/scale uniform (全軸同じスケール)
    /// 形式2: /object/scale x y z (各軸個別のスケール)
    /// </summary>
    private void OnScaleReceived(OSCMessage message)
    {
        if (message.Values.Count >= 1)
        {
            if (message.Values.Count == 1)
            {
                // Uniform scale
                float uniform = message.Values[0].FloatValue;
                _targetTransform.localScale = new Vector3(uniform, uniform, uniform);
                Debug.Log($"[OSC] Scale set to: {uniform}");
            }
            else if (message.Values.Count >= 3)
            {
                // Non-uniform scale
                float x = message.Values[0].FloatValue;
                float y = message.Values[1].FloatValue;
                float z = message.Values[2].FloatValue;
                _targetTransform.localScale = new Vector3(x, y, z);
                Debug.Log($"[OSC] Scale set to: ({x}, {y}, {z})");
            }
        }
    }

    /// <summary>
    /// 色制御のOSCメッセージを受信
    /// 形式1: /object/color r g b (RGB値 0.0-1.0)
    /// 形式2: /object/color r g b a (RGBA値 0.0-1.0)
    /// </summary>
    private void OnColorReceived(OSCMessage message)
    {
        if (_targetMaterial == null) return;

        if (message.Values.Count >= 3)
        {
            float r = message.Values[0].FloatValue;
            float g = message.Values[1].FloatValue;
            float b = message.Values[2].FloatValue;
            float a = message.Values.Count >= 4 ? message.Values[3].FloatValue : 1.0f;

            _targetMaterial.color = new Color(r, g, b, a);
            Debug.Log($"[OSC] Color set to: ({r}, {g}, {b}, {a})");
        }
    }

    /// <summary>
    /// 表示/非表示制御のOSCメッセージを受信
    /// 形式: /object/visible 0 or 1
    /// </summary>
    private void OnVisibilityReceived(OSCMessage message)
    {
        if (_targetRenderer == null) return;

        if (message.Values.Count >= 1)
        {
            int visible = message.Values[0].IntValue;
            _targetRenderer.enabled = (visible != 0);
            Debug.Log($"[OSC] Visibility set to: {(visible != 0)}");
        }
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// 受信ポートを動的に変更
    /// </summary>
    public void ChangeReceivePort(int newPort)
    {
        if (_receiver != null)
        {
            _receiver.LocalPort = newPort;
            receivePort = newPort;
            Debug.Log($"OSC Receiver port changed to {newPort}");
        }
    }

    /// <summary>
    /// ターゲットオブジェクトを動的に変更
    /// </summary>
    public void ChangeTarget(GameObject newTarget)
    {
        if (newTarget != null)
        {
            targetObject = newTarget;
            _targetTransform = newTarget.transform;
            _targetRenderer = newTarget.GetComponent<Renderer>();

            if (_targetRenderer != null)
            {
                // 古いマテリアルインスタンスを破棄
                if (_targetMaterial != null)
                {
                    Destroy(_targetMaterial);
                }
                _targetMaterial = _targetRenderer.material;
            }

            Debug.Log($"Target changed to: {newTarget.name}");
        }
    }

    #endregion
}
