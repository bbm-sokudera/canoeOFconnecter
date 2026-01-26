using UnityEngine;
using UnityEngine.UI;

public class OSCZAxisController : MonoBehaviour
{
    [Header("References")]
    public MultiAddressOSCManager targetManager;

    [Header("UI Elements")]
    public InputField minInputField;
    public Button setMinButton;
    public InputField maxInputField;
    public Button setMaxButton;
    public Text currentZValueText;

    private void Start()
    {
        if (setMinButton != null) setMinButton.onClick.AddListener(() => SetExternalMin(GetCurrentZ()));
        if (setMaxButton != null) setMaxButton.onClick.AddListener(() => SetExternalMax(GetCurrentZ()));
        
        // 初期値ロード（必要であれば）
        UpdateUI();
    }

    void Update() => UpdateCurrentZDisplay();

    // ★重要：AutoHeightControllerから呼ばれる関数
    public void SetExternalMin(float value)
    {
        if (targetManager == null) return;
        
        // Managerの特定のパラメータ（例: BoxZなど）や、内部変数にセット
        // ここでは一旦、分かりやすくログとUI更新
        if (minInputField != null) minInputField.text = value.ToString("F3");
        
        // 必要に応じてOSCManagerのパラメータを更新
        targetManager.SetFloat("BoxZ", value); 
        
        Debug.Log($"[ZAxis] Min Updated to: {value:F3}");
    }

    public void SetExternalMax(float value)
    {
        if (maxInputField != null) maxInputField.text = value.ToString("F3");
    }

    private float GetCurrentZ() => targetManager != null ? targetManager.GetFloat("PositionZ") : 0f;

    private void UpdateCurrentZDisplay()
    {
        if (currentZValueText != null)
            currentZValueText.text = $"Current Z: {GetCurrentZ():F3}";
    }

    private void UpdateUI()
    {
        // JSON等から現在の設定を反映
        if (minInputField != null) minInputField.text = targetManager.GetFloat("BoxZ").ToString("F3");
    }
}