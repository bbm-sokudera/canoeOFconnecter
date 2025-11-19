#!/usr/bin/env python3
"""
OSC送信テストスクリプト

このスクリプトは、UnityのOSC受信機能をテストするためのOSCメッセージを送信します。
python-oscライブラリが必要です: pip install python-osc

使用方法:
    python osc_sender_test.py
"""

from pythonosc import udp_client
import time
import math

def send_test_messages(ip="127.0.0.1", port=7001):
    """
    テスト用のOSCメッセージを送信

    Args:
        ip: 送信先のIPアドレス
        port: 送信先のポート番号
    """
    # OSCクライアントの作成
    client = udp_client.SimpleUDPClient(ip, port)

    print(f"OSCメッセージを送信開始: {ip}:{port}")
    print("=" * 50)

    # 1. Float値のテスト
    print("\n1. Float値の送信テスト")
    for i in range(5):
        value = 3.14 * (i + 1)
        client.send_message("/float", value)
        print(f"   送信: /float {value:.2f}")
        time.sleep(0.5)

    # 2. Int値のテスト
    print("\n2. Int値の送信テスト")
    for i in range(5):
        value = 10 * (i + 1)
        client.send_message("/int", value)
        print(f"   送信: /int {value}")
        time.sleep(0.5)

    # 3. String値のテスト
    print("\n3. String値の送信テスト")
    messages = ["Hello", "OSC", "from", "Python", "!"]
    for msg in messages:
        client.send_message("/string", msg)
        print(f"   送信: /string '{msg}'")
        time.sleep(0.5)

    # 4. 複数の値のテスト
    print("\n4. 複数の値の送信テスト")
    client.send_message("/multiple", [1.5, 42, "test"])
    print(f"   送信: /multiple [1.5, 42, 'test']")
    time.sleep(0.5)

    client.send_message("/multiple", [2.5, 100, "hello"])
    print(f"   送信: /multiple [2.5, 100, 'hello']")
    time.sleep(0.5)

    print("\n" + "=" * 50)
    print("基本テスト完了")


def send_object_control_test(ip="127.0.0.1", port=7001):
    """
    オブジェクト制御用のOSCメッセージを送信

    Args:
        ip: 送信先のIPアドレス
        port: 送信先のポート番号
    """
    client = udp_client.SimpleUDPClient(ip, port)

    print(f"\nオブジェクト制御テストを開始: {ip}:{port}")
    print("=" * 50)

    # 1. 位置制御のテスト
    print("\n1. 位置制御テスト（円運動）")
    for i in range(20):
        angle = (i / 20) * 2 * math.pi
        x = math.cos(angle) * 5
        z = math.sin(angle) * 5
        y = 1.0

        client.send_message("/object/position", [x, y, z])
        print(f"   位置: ({x:.2f}, {y:.2f}, {z:.2f})")
        time.sleep(0.1)

    # 2. 回転制御のテスト
    print("\n2. 回転制御テスト（Y軸回転）")
    for i in range(36):
        angle = i * 10
        client.send_message("/object/rotation", [0, angle, 0])
        print(f"   回転: (0, {angle}, 0)")
        time.sleep(0.05)

    # 3. スケール制御のテスト
    print("\n3. スケール制御テスト（パルス）")
    for i in range(10):
        scale = 0.5 + (math.sin(i * 0.5) + 1) * 0.5
        client.send_message("/object/scale", scale)
        print(f"   スケール: {scale:.2f}")
        time.sleep(0.2)

    # 4. 色制御のテスト
    print("\n4. 色制御テスト（レインボー）")
    for i in range(10):
        hue = i / 10
        r, g, b = hsv_to_rgb(hue, 1.0, 1.0)
        client.send_message("/object/color", [r, g, b])
        print(f"   色: RGB({r:.2f}, {g:.2f}, {b:.2f})")
        time.sleep(0.3)

    # 5. 表示/非表示制御のテスト
    print("\n5. 表示/非表示制御テスト")
    for i in range(6):
        visible = i % 2
        client.send_message("/object/visible", visible)
        print(f"   表示: {bool(visible)}")
        time.sleep(0.5)

    print("\n" + "=" * 50)
    print("オブジェクト制御テスト完了")


def hsv_to_rgb(h, s, v):
    """
    HSV色空間からRGB色空間への変換

    Args:
        h: 色相 (0.0-1.0)
        s: 彩度 (0.0-1.0)
        v: 明度 (0.0-1.0)

    Returns:
        tuple: (r, g, b) 各値は0.0-1.0
    """
    if s == 0.0:
        return v, v, v

    h = h * 6.0
    i = int(h)
    f = h - i
    p = v * (1.0 - s)
    q = v * (1.0 - s * f)
    t = v * (1.0 - s * (1.0 - f))

    i = i % 6

    if i == 0:
        return v, t, p
    if i == 1:
        return q, v, p
    if i == 2:
        return p, v, t
    if i == 3:
        return p, q, v
    if i == 4:
        return t, p, v
    if i == 5:
        return v, p, q


def interactive_mode(ip="127.0.0.1", port=7001):
    """
    対話モードでOSCメッセージを送信

    Args:
        ip: 送信先のIPアドレス
        port: 送信先のポート番号
    """
    client = udp_client.SimpleUDPClient(ip, port)

    print(f"\n対話モード開始: {ip}:{port}")
    print("=" * 50)
    print("コマンド例:")
    print("  float 3.14         -> /float 3.14")
    print("  int 42             -> /int 42")
    print("  string Hello       -> /string 'Hello'")
    print("  pos 1 2 3          -> /object/position [1, 2, 3]")
    print("  rot 0 90 0         -> /object/rotation [0, 90, 0]")
    print("  scale 1.5          -> /object/scale 1.5")
    print("  color 1 0 0        -> /object/color [1, 0, 0]")
    print("  visible 1          -> /object/visible 1")
    print("  quit               -> 終了")
    print("=" * 50)

    while True:
        try:
            command = input("\nコマンド> ").strip()

            if command == "quit":
                break

            parts = command.split()
            if len(parts) == 0:
                continue

            cmd = parts[0].lower()

            if cmd == "float" and len(parts) >= 2:
                value = float(parts[1])
                client.send_message("/float", value)
                print(f"送信: /float {value}")

            elif cmd == "int" and len(parts) >= 2:
                value = int(parts[1])
                client.send_message("/int", value)
                print(f"送信: /int {value}")

            elif cmd == "string" and len(parts) >= 2:
                value = " ".join(parts[1:])
                client.send_message("/string", value)
                print(f"送信: /string '{value}'")

            elif cmd == "pos" and len(parts) >= 4:
                x, y, z = float(parts[1]), float(parts[2]), float(parts[3])
                client.send_message("/object/position", [x, y, z])
                print(f"送信: /object/position [{x}, {y}, {z}]")

            elif cmd == "rot" and len(parts) >= 4:
                x, y, z = float(parts[1]), float(parts[2]), float(parts[3])
                client.send_message("/object/rotation", [x, y, z])
                print(f"送信: /object/rotation [{x}, {y}, {z}]")

            elif cmd == "scale" and len(parts) >= 2:
                value = float(parts[1])
                client.send_message("/object/scale", value)
                print(f"送信: /object/scale {value}")

            elif cmd == "color" and len(parts) >= 4:
                r, g, b = float(parts[1]), float(parts[2]), float(parts[3])
                client.send_message("/object/color", [r, g, b])
                print(f"送信: /object/color [{r}, {g}, {b}]")

            elif cmd == "visible" and len(parts) >= 2:
                value = int(parts[1])
                client.send_message("/object/visible", value)
                print(f"送信: /object/visible {value}")

            else:
                print("不明なコマンドまたは引数が不足しています")

        except ValueError as e:
            print(f"エラー: 値の変換に失敗しました - {e}")
        except KeyboardInterrupt:
            print("\n中断されました")
            break
        except Exception as e:
            print(f"エラー: {e}")

    print("対話モード終了")


if __name__ == "__main__":
    import sys

    # デフォルト設定
    target_ip = "127.0.0.1"
    target_port = 7001

    # コマンドライン引数の処理
    if len(sys.argv) >= 2:
        target_ip = sys.argv[1]
    if len(sys.argv) >= 3:
        target_port = int(sys.argv[2])

    print("OSC送信テストツール")
    print(f"送信先: {target_ip}:{target_port}")
    print("\n選択してください:")
    print("1. 基本テスト（自動）")
    print("2. オブジェクト制御テスト（自動）")
    print("3. 対話モード（手動）")
    print("4. すべて実行")

    try:
        choice = input("\n選択 (1-4): ").strip()

        if choice == "1":
            send_test_messages(target_ip, target_port)
        elif choice == "2":
            send_object_control_test(target_ip, target_port)
        elif choice == "3":
            interactive_mode(target_ip, target_port)
        elif choice == "4":
            send_test_messages(target_ip, target_port)
            time.sleep(1)
            send_object_control_test(target_ip, target_port)
            time.sleep(1)
            interactive_mode(target_ip, target_port)
        else:
            print("無効な選択です")

    except KeyboardInterrupt:
        print("\n\n中断されました")
    except Exception as e:
        print(f"\nエラー: {e}")
        import traceback
        traceback.print_exc()
