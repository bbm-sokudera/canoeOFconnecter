using UnityEngine;

/// <summary>
/// MultiAddressOSCManagerの使用例
/// </summary>
public class MultiAddressOSCExample : MonoBehaviour
{
    [Header("References")]
    public MultiAddressOSCManager oscManager;

    void Update()
    {
        if (oscManager == null)
            return;

        // 受信データの取得例
        int state = oscManager.GetInt("State");
        Vector3 position = oscManager.GetVector3("PositionX", "PositionY", "PositionZ");
        float vectorY = oscManager.GetFloat("VectorY");

        // 状態に応じた処理
        switch (state)
        {
            case 0: // その他
                break;
            case 1: // キャリブレーション開始
                Debug.Log("Calibration started");
                break;
            case 2: // キャリブレーション終了
                Debug.Log("Calibration finished");
                break;
            case 3: // チュートリアル
                Debug.Log("Tutorial mode");
                break;
            case 4: // クイズ
                HandleQuizMode(vectorY);
                break;
            case 5: // ゲーム
                HandleGameMode(position, vectorY);
                break;
        }
    }

    /// <summary>
    /// クイズモードの処理
    /// </summary>
    void HandleQuizMode(float vectorY)
    {
        // VectorYの値に基づいて選択を判定
        if (Input.GetKeyDown(KeyCode.RightArrow))
        {
            // 右選択
            oscManager.SetInt("QuizChoice", 1);
            oscManager.SendMessage("/quiz");
            Debug.Log("Quiz: Right selected");
        }
        else if (Input.GetKeyDown(KeyCode.LeftArrow))
        {
            // 左選択
            oscManager.SetInt("QuizChoice", 2);
            oscManager.SendMessage("/quiz");
            Debug.Log("Quiz: Left selected");
        }
    }

    /// <summary>
    /// ゲームモードの処理
    /// </summary>
    void HandleGameMode(Vector3 position, float vectorY)
    {
        // パドル操作の判定
        bool isRight = position.x >= 0;
        bool isForward = vectorY > 0.1f;
        bool isBackward = vectorY < -0.1f;

        int paddleDirection = 0;

        if (isForward)
        {
            paddleDirection = isRight ? 1 : 2; // 1: 右前進, 2: 左前進
        }
        else if (isBackward)
        {
            paddleDirection = isRight ? 3 : 4; // 3: 右後進, 4: 左後進
        }

        if (paddleDirection > 0)
        {
            oscManager.SetInt("PaddleDirection", paddleDirection);
            oscManager.SendMessage("/paddle");
            Debug.Log($"Paddle: {paddleDirection}");
        }
    }

    /// <summary>
    /// AutoHeightを送信する例
    /// </summary>
    public void SendAutoHeight(int value)
    {
        oscManager.SetInt("AutoHeight", value);
        oscManager.SendMessage("/cropbox/autoheight");
        Debug.Log($"AutoHeight sent: {value}");
    }

    /// <summary>
    /// 設定をリロード
    /// </summary>
    [ContextMenu("Reload Configuration")]
    public void ReloadConfiguration()
    {
        if (oscManager != null)
        {
            oscManager.LoadConfiguration();
            Debug.Log("Configuration reloaded!");
        }
    }
}
