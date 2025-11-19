using System; // <-- 追加
using System.Collections;
using Orbbec;
using UnityEngine;
using UnityEngine.Events;

namespace OrbbecUnity
{
    [System.Serializable]
    public class DeviceFoundEvent : UnityEvent<Device> {}

    public class OrbbecDevice : MonoBehaviour
    {
        public int deviceIndex;
        public DeviceFoundEvent onDeviceFound;

        [Header("Connection Control")]
        [Tooltip("Start()で自動的にデバイス接続を開始するかどうか")]
        public bool autoConnectOnStart = true;

        private Context context;
        private Device device;
        private Coroutine deviceSearchCoroutine;

        public Device Device
        {
            get
            {
                return device;
            }
        }

        void Start()
        {
            context = OrbbecContext.Instance.Context;
            if(OrbbecContext.Instance.HasInit && autoConnectOnStart)
            {
                StartDeviceConnection();
            }
        }

        void OnDestroy()
        {
            StopDeviceConnection();
            if(device != null)
            {
                device.Dispose();
            }
        }

        /// <summary>
        /// デバイスへの接続を開始する（外部から呼び出し可能）
        /// </summary>
        public void StartDeviceConnection()
        {
            if (!OrbbecContext.Instance.HasInit)
            {
                Debug.LogWarning("[OrbbecDevice] Context is not initialized. Cannot start device connection.");
                return;
            }

            if (deviceSearchCoroutine != null)
            {
                Debug.LogWarning("[OrbbecDevice] Device search is already running.");
                return;
            }

            Debug.Log("[OrbbecDevice] Starting device connection...");
            deviceSearchCoroutine = StartCoroutine(WaitForDevice());
        }

        /// <summary>
        /// デバイスへの接続を停止する（外部から呼び出し可能）
        /// </summary>
        public void StopDeviceConnection()
        {
            if (deviceSearchCoroutine != null)
            {
                Debug.Log("[OrbbecDevice] Stopping device connection...");
                StopCoroutine(deviceSearchCoroutine);
                deviceSearchCoroutine = null;
            }

            if (device != null)
            {
                Debug.Log("[OrbbecDevice] Disposing device...");
                device.Dispose();
                device = null;
            }
        }

        private IEnumerator WaitForDevice()
        {
            while (true)
            {
                yield return new WaitForEndOfFrame();
                context.EnableNetDeviceEnumeration(true);
                DeviceList deviceList = context.QueryDeviceList();
                if (deviceList.DeviceCount() > deviceIndex)
                {
                    device = deviceList.GetDevice((uint)deviceIndex);
                    DeviceInfo deviceInfo = device.GetDeviceInfo();
                    Debug.LogFormat(
                        "Device found: {0} {1} {2:X} {3:X}",
                        deviceInfo.Name(),
                        deviceInfo.SerialNumber(),
                        deviceInfo.Vid(),
                        deviceInfo.Pid());
                    deviceList.Dispose();
                    onDeviceFound?.Invoke(device);
                    deviceSearchCoroutine = null;
                    break;
                }
                else
                {
                    deviceList.Dispose();
                }
            }
        }
    }
}