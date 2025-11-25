using UnityEngine;

/// <summary>
/// OSCXPositionYVectorManagerの設定値を取得・設定するためのインターフェース
/// GUI調整ツール作成時に使用
/// </summary>
public class OSCXPositionYVectorConfig : MonoBehaviour
{
    [Header("Target Manager")]
    [Tooltip("設定を読み書きする対象のOSCXPositionYVectorManager")]
    public OSCXPositionYVectorManager targetManager;

    #region 設定値のプロパティ（読み書き可能）

    // ===== Receiver Settings =====
    public int ReceivePort
    {
        get => targetManager?.receivePort ?? 0;
        set { if (targetManager != null) targetManager.receivePort = value; }
    }

    public string ReceiveAddress
    {
        get => targetManager?.receiveAddress ?? "";
        set { if (targetManager != null) targetManager.receiveAddress = value; }
    }

    // ===== Transmitter Settings =====
    public string TransmitHost
    {
        get => targetManager?.transmitHost ?? "";
        set { if (targetManager != null) targetManager.transmitHost = value; }
    }

    public int TransmitPort
    {
        get => targetManager?.transmitPort ?? 0;
        set { if (targetManager != null) targetManager.transmitPort = value; }
    }

    public string TransmitAddress
    {
        get => targetManager?.transmitAddress ?? "";
        set { if (targetManager != null) targetManager.transmitAddress = value; }
    }

    // ===== Position & Vector Settings =====
    public float XCenterPosition
    {
        get => targetManager?.xCenterPosition ?? 0f;
        set { if (targetManager != null) targetManager.xCenterPosition = value; }
    }

    public float YVectorMagnitudeThreshold
    {
        get => targetManager?.yVectorMagnitudeThreshold ?? 0f;
        set { if (targetManager != null) targetManager.yVectorMagnitudeThreshold = value; }
    }

    // ===== Direction Value Settings =====
    public int RightValue
    {
        get => targetManager?.rightValue ?? 0;
        set { if (targetManager != null) targetManager.rightValue = value; }
    }

    public int LeftValue
    {
        get => targetManager?.leftValue ?? 0;
        set { if (targetManager != null) targetManager.leftValue = value; }
    }

    public int RightBackwardValue
    {
        get => targetManager?.rightBackwardValue ?? 0;
        set { if (targetManager != null) targetManager.rightValue = value; }
    }

    public int LeftBackwardValue
    {
        get => targetManager?.leftBackwardValue ?? 0;
        set { if (targetManager != null) targetManager.leftBackwardValue = value; }
    }

    // ===== Conditional Axis Settings =====
    public bool EnableConditionalAxis
    {
        get => targetManager?.enableConditionalAxis ?? false;
        set { if (targetManager != null) targetManager.enableConditionalAxis = value; }
    }

    public float ConditionalAxisMin
    {
        get => targetManager?.conditionalAxisMin ?? 0f;
        set { if (targetManager != null) targetManager.conditionalAxisMin = value; }
    }

    public float ConditionalAxisMax
    {
        get => targetManager?.conditionalAxisMax ?? 0f;
        set { if (targetManager != null) targetManager.conditionalAxisMax = value; }
    }

    // ===== Cooldown Settings =====
    public float CooldownTime
    {
        get => targetManager?.cooldownTime ?? 0f;
        set { if (targetManager != null) targetManager.cooldownTime = value; }
    }

    // ===== Consecutive Side Detection =====
    public bool EnableConsecutiveSideDetection
    {
        get => targetManager?.enableConsecutiveSideDetection ?? false;
        set { if (targetManager != null) targetManager.enableConsecutiveSideDetection = value; }
    }

    public int ConsecutiveSideLimit
    {
        get => targetManager?.consecutiveSideLimit ?? 0;
        set { if (targetManager != null) targetManager.consecutiveSideLimit = value; }
    }

    public int OppositeSideRequiredCount
    {
        get => targetManager?.oppositeSideRequiredCount ?? 0;
        set { if (targetManager != null) targetManager.oppositeSideRequiredCount = value; }
    }

    // ===== Consecutive Backward Detection =====
    public bool EnableConsecutiveBackwardDetection
    {
        get => targetManager?.enableConsecutiveBackwardDetection ?? false;
        set { if (targetManager != null) targetManager.enableConsecutiveBackwardDetection = value; }
    }

    public int ConsecutiveBackwardLimit
    {
        get => targetManager?.consecutiveBackwardLimit ?? 0;
        set { if (targetManager != null) targetManager.consecutiveBackwardLimit = value; }
    }

    public int ForwardRequiredCount
    {
        get => targetManager?.forwardRequiredCount ?? 0;
        set { if (targetManager != null) targetManager.forwardRequiredCount = value; }
    }

    // ===== Advanced Settings =====
    public float YAngleThreshold
    {
        get => targetManager?.yAngleThreshold ?? 0f;
        set { if (targetManager != null) targetManager.yAngleThreshold = value; }
    }

    public bool EnableDebugLog
    {
        get => targetManager?.enableDebugLog ?? false;
        set { if (targetManager != null) targetManager.enableDebugLog = value; }
    }

    public bool ShowGizmo
    {
        get => targetManager?.showGizmo ?? false;
        set { if (targetManager != null) targetManager.showGizmo = value; }
    }

    #endregion

    #region 便利なメソッド

    /// <summary>
    /// 全設定を文字列として取得
    /// </summary>
    public string GetAllSettings()
    {
        if (targetManager == null)
            return "Target Manager is not assigned.";

        return targetManager.GetConfigInfo();
    }

    /// <summary>
    /// 設定を構造化データとして取得
    /// </summary>
    public ConfigData GetConfigData()
    {
        if (targetManager == null)
            return null;

        return new ConfigData
        {
            // Receiver Settings
            receivePort = ReceivePort,
            receiveAddress = ReceiveAddress,

            // Transmitter Settings
            transmitHost = TransmitHost,
            transmitPort = TransmitPort,
            transmitAddress = TransmitAddress,

            // Position & Vector Settings
            xCenterPosition = XCenterPosition,
            yVectorMagnitudeThreshold = YVectorMagnitudeThreshold,

            // Direction Value Settings
            rightValue = RightValue,
            leftValue = LeftValue,
            rightBackwardValue = RightBackwardValue,
            leftBackwardValue = LeftBackwardValue,

            // Conditional Axis Settings
            enableConditionalAxis = EnableConditionalAxis,
            conditionalAxisMin = ConditionalAxisMin,
            conditionalAxisMax = ConditionalAxisMax,

            // Cooldown Settings
            cooldownTime = CooldownTime,

            // Consecutive Side Detection
            enableConsecutiveSideDetection = EnableConsecutiveSideDetection,
            consecutiveSideLimit = ConsecutiveSideLimit,
            oppositeSideRequiredCount = OppositeSideRequiredCount,

            // Consecutive Backward Detection
            enableConsecutiveBackwardDetection = EnableConsecutiveBackwardDetection,
            consecutiveBackwardLimit = ConsecutiveBackwardLimit,
            forwardRequiredCount = ForwardRequiredCount,

            // Advanced Settings
            yAngleThreshold = YAngleThreshold,
            enableDebugLog = EnableDebugLog,
            showGizmo = ShowGizmo
        };
    }

    /// <summary>
    /// 構造化データから設定を適用
    /// </summary>
    public void ApplyConfigData(ConfigData data)
    {
        if (targetManager == null || data == null)
            return;

        // Receiver Settings
        ReceivePort = data.receivePort;
        ReceiveAddress = data.receiveAddress;

        // Transmitter Settings
        TransmitHost = data.transmitHost;
        TransmitPort = data.transmitPort;
        TransmitAddress = data.transmitAddress;

        // Position & Vector Settings
        XCenterPosition = data.xCenterPosition;
        YVectorMagnitudeThreshold = data.yVectorMagnitudeThreshold;

        // Direction Value Settings
        RightValue = data.rightValue;
        LeftValue = data.leftValue;
        RightBackwardValue = data.rightBackwardValue;
        LeftBackwardValue = data.leftBackwardValue;

        // Conditional Axis Settings
        EnableConditionalAxis = data.enableConditionalAxis;
        ConditionalAxisMin = data.conditionalAxisMin;
        ConditionalAxisMax = data.conditionalAxisMax;

        // Cooldown Settings
        CooldownTime = data.cooldownTime;

        // Consecutive Side Detection
        EnableConsecutiveSideDetection = data.enableConsecutiveSideDetection;
        ConsecutiveSideLimit = data.consecutiveSideLimit;
        OppositeSideRequiredCount = data.oppositeSideRequiredCount;

        // Consecutive Backward Detection
        EnableConsecutiveBackwardDetection = data.enableConsecutiveBackwardDetection;
        ConsecutiveBackwardLimit = data.consecutiveBackwardLimit;
        ForwardRequiredCount = data.forwardRequiredCount;

        // Advanced Settings
        YAngleThreshold = data.yAngleThreshold;
        EnableDebugLog = data.enableDebugLog;
        ShowGizmo = data.showGizmo;
    }

    /// <summary>
    /// 位置とカウンターをリセット
    /// </summary>
    public void ResetPosition()
    {
        targetManager?.ResetPosition();
    }

    /// <summary>
    /// 現在の移動ベクトルを取得
    /// </summary>
    public Vector3 GetMovementVector()
    {
        return targetManager?.GetMovementVector() ?? Vector3.zero;
    }

    /// <summary>
    /// 現在のX位置を取得
    /// </summary>
    public OSCXPositionYVectorManager.XPosition GetCurrentXPosition()
    {
        return targetManager?.GetCurrentXPosition() ?? OSCXPositionYVectorManager.XPosition.Right;
    }

    /// <summary>
    /// 現在のY方向を取得
    /// </summary>
    public OSCXPositionYVectorManager.YVectorDirection GetCurrentYDirection()
    {
        return targetManager?.GetCurrentYDirection() ?? OSCXPositionYVectorManager.YVectorDirection.None;
    }

    #endregion

    #region データ構造

    /// <summary>
    /// 設定データ構造体（JSON化可能）
    /// </summary>
    [System.Serializable]
    public class ConfigData
    {
        // Receiver Settings
        public int receivePort;
        public string receiveAddress;

        // Transmitter Settings
        public string transmitHost;
        public int transmitPort;
        public string transmitAddress;

        // Position & Vector Settings
        public float xCenterPosition;
        public float yVectorMagnitudeThreshold;

        // Direction Value Settings
        public int rightValue;
        public int leftValue;
        public int rightBackwardValue;
        public int leftBackwardValue;

        // Conditional Axis Settings
        public bool enableConditionalAxis;
        public float conditionalAxisMin;
        public float conditionalAxisMax;

        // Cooldown Settings
        public float cooldownTime;

        // Consecutive Side Detection
        public bool enableConsecutiveSideDetection;
        public int consecutiveSideLimit;
        public int oppositeSideRequiredCount;

        // Consecutive Backward Detection
        public bool enableConsecutiveBackwardDetection;
        public int consecutiveBackwardLimit;
        public int forwardRequiredCount;

        // Advanced Settings
        public float yAngleThreshold;
        public bool enableDebugLog;
        public bool showGizmo;
    }

    #endregion
}
