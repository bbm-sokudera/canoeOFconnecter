using UnityEngine;

public class OSCButtonDispatcher : MonoBehaviour
{
    public MultiAddressOSCManager oscManager;
    
    // インスペクターから設定
    public string address = "/mode/set";
    public string parameterName = "ModeSelect";

    // ButtonのOnClickからこれを呼ぶ
    public void SendIntMode(int modeValue)
    {
        if (oscManager == null) return;

        oscManager.SetInt(parameterName, modeValue);
        oscManager.SendMessage(address);
        Debug.Log($"[OSC Dispatcher] Sent {modeValue} to {address}");
    }
}