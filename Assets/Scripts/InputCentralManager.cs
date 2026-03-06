using UnityEngine;
using System.Runtime.InteropServices;

public class InputCentralManager : MonoBehaviour
{
    [DllImport("user32.dll")]
    private static extern short GetAsyncKeyState(int vKey);

    public MultiAddressOSCManager oscManager;
    public AppLauncher appLauncher;

    private bool[] keyStates = new bool[256];

    void Update()
    {
        if (!Application.isPlaying) return;

        // --- OSC用 (0～6) ---
        // 0x30 はメインキーの '0'、0x60 はテンキーの '0' です
        for (int i = 0; i <= 6; i++) 
        {
            if (CheckKeyDown(0x30 + i) || CheckKeyDown(0x60 + i))
            {
                if (oscManager != null) oscManager.SendByNumber(i);
            }
        }

        // --- AppLauncher用 (7～9) ---
        for (int i = 7; i <= 9; i++)
        {
            if (CheckKeyDown(0x30 + i) || CheckKeyDown(0x60 + i))
            {
                if (appLauncher != null) appLauncher.SelectIndex(i);
            }
        }

        // --- 特殊キー ---
        if (CheckKeyDown(0x6F)) appLauncher?.SetDeleteMode(true); // テンキー /
        if (CheckKeyDown(0x0D)) appLauncher?.Execute();           // Enter
    }

    private bool CheckKeyDown(int vKey)
    {
        bool isDown = (GetAsyncKeyState(vKey) & 0x8000) != 0;
        if (isDown && !keyStates[vKey]) {
            keyStates[vKey] = true;
            return true;
        } else if (!isDown) {
            keyStates[vKey] = false;
        }
        return false;
    }
}