using UnityEngine;
using System.Collections.Generic;
using extOSC;

public class MyOSCFilterConsole : MonoBehaviour
{
    [System.Serializable]
    public class OSCFilter
    {
        public string address = "/example";
        public int valueIndex = 0; // ここが valueIndex
        public string lastValue = "No Data";
    }

    [Header("監視したい条件を追加")]
    public List<OSCFilter> filterList = new List<OSCFilter>();

    [Header("表示設定")]
    public int maxLogCount = 20;
    
    private List<string> filteredLogs = new List<string>();
    private HashSet<OSCReceiver> hookedReceivers = new HashSet<OSCReceiver>();
    private Vector2 scrollPosition;

    void Update()
    {
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
        foreach (var filter in filterList)
        {
            if (message.Address == filter.address)
            {
                // ここを修正：filter.valueIndex に統一
                if (filter.valueIndex >= 0 && filter.valueIndex < message.Values.Count)
                {
                    var val = message.Values[filter.valueIndex]; // 修正
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
    }

    void OnGUI()
    {
        GUI.backgroundColor = new Color(0, 0.1f, 0.3f, 0.95f);
        GUILayout.BeginArea(new Rect(20, 20, 500, 600), "OSC TARGET MONITOR", "Window");
        
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