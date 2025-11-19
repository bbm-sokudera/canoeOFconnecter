#!/usr/bin/env python3
"""
OSCManagerテストスクリプト

OSCManagerの動作をテストするためのスクリプト
位置情報(x,y,z)を送信し、OSCManagerからの返信を受信します

使用方法:
    pip install python-osc
    python osc_manager_test.py
"""

from pythonosc import udp_client, dispatcher, osc_server
import time
import threading
import math

class OSCManagerTester:
    def __init__(self, send_host="127.0.0.1", send_port=7001, receive_port=7002):
        """
        OSCManagerテスター

        Args:
            send_host: Unity側のIPアドレス
            send_port: Unity側の受信ポート
            receive_port: このスクリプトの受信ポート（Unity側の送信ポート）
        """
        self.send_host = send_host
        self.send_port = send_port
        self.receive_port = receive_port

        # OSC送信クライアント
        self.client = udp_client.SimpleUDPClient(send_host, send_port)

        # OSC受信サーバー
        self.dispatcher = dispatcher.Dispatcher()
        self.dispatcher.map("/output", self.on_output_received)
        self.server = osc_server.ThreadingOSCUDPServer(
            ("0.0.0.0", receive_port), self.dispatcher
        )

        # 受信した値を記録
        self.received_values = []
        self.last_received_value = None

        # サーバースレッド
        self.server_thread = None

    def start_server(self):
        """受信サーバーを起動"""
        self.server_thread = threading.Thread(target=self.server.serve_forever, daemon=True)
        self.server_thread.start()
        print(f"OSC受信サーバー起動: ポート {self.receive_port}")

    def stop_server(self):
        """受信サーバーを停止"""
        if self.server:
            self.server.shutdown()
            print("OSC受信サーバー停止")

    def on_output_received(self, address, *args):
        """OSCManagerからの出力を受信"""
        if len(args) > 0:
            value = args[0]
            self.last_received_value = value
            self.received_values.append(value)
            print(f"  <<< 受信: {address} = {value}")

    def send_position(self, x, y, z):
        """位置情報を送信"""
        self.client.send_message("/position", [x, y, z])
        print(f"  >>> 送信: /position ({x:.3f}, {y:.3f}, {z:.3f})")
        time.sleep(0.1)  # 応答待ち

    def test_x_axis_negative(self):
        """X軸の負の値テスト"""
        print("\n" + "="*60)
        print("テスト1: X軸 - 負の値の変化")
        print("="*60)
        self.received_values.clear()

        # 初期値
        print("\n1. 初期値設定: x=-1.0")
        self.send_position(-1.0, 0.0, 0.0)

        # より負になる（増加）→ 0を期待
        print("\n2. より負になる: x=-1.0 → x=-2.0 (期待値: 0)")
        self.send_position(-2.0, 0.0, 0.0)

        # さらに負になる（増加）→ 0を期待
        print("\n3. さらに負になる: x=-2.0 → x=-3.5 (期待値: 0)")
        self.send_position(-3.5, 0.0, 0.0)

        # 0に近づく（減少）→ 1を期待
        print("\n4. 0に近づく: x=-3.5 → x=-2.0 (期待値: 1)")
        self.send_position(-2.0, 0.0, 0.0)

        # さらに0に近づく（減少）→ 1を期待
        print("\n5. さらに0に近づく: x=-2.0 → x=-0.5 (期待値: 1)")
        self.send_position(-0.5, 0.0, 0.0)

        print(f"\n受信した値: {self.received_values}")

    def test_x_axis_positive(self):
        """X軸の正の値テスト"""
        print("\n" + "="*60)
        print("テスト2: X軸 - 正の値の変化")
        print("="*60)
        self.received_values.clear()

        # 初期値
        print("\n1. 初期値設定: x=1.0")
        self.send_position(1.0, 0.0, 0.0)

        # より正になる（増加）→ 2を期待
        print("\n2. より正になる: x=1.0 → x=2.0 (期待値: 2)")
        self.send_position(2.0, 0.0, 0.0)

        # さらに正になる（増加）→ 2を期待
        print("\n3. さらに正になる: x=2.0 → x=3.5 (期待値: 2)")
        self.send_position(3.5, 0.0, 0.0)

        # 0に近づく（減少）→ 3を期待
        print("\n4. 0に近づく: x=3.5 → x=2.0 (期待値: 3)")
        self.send_position(2.0, 0.0, 0.0)

        # さらに0に近づく（減少）→ 3を期待
        print("\n5. さらに0に近づく: x=2.0 → x=0.5 (期待値: 3)")
        self.send_position(0.5, 0.0, 0.0)

        print(f"\n受信した値: {self.received_values}")

    def test_y_axis(self):
        """Y軸のテスト"""
        print("\n" + "="*60)
        print("テスト3: Y軸 - 値の変化（Inspectorでyに切り替えてください）")
        print("="*60)

        input("\nInspectorで 'Selected Axis' を Y に変更してからEnterを押してください...")

        self.received_values.clear()

        print("\n1. 初期値設定: y=2.0")
        self.send_position(0.0, 2.0, 0.0)

        print("\n2. より正になる: y=2.0 → y=4.0 (期待値: 2)")
        self.send_position(0.0, 4.0, 0.0)

        print("\n3. 0に近づく: y=4.0 → y=1.0 (期待値: 3)")
        self.send_position(0.0, 1.0, 0.0)

        print(f"\n受信した値: {self.received_values}")

    def test_z_axis(self):
        """Z軸のテスト"""
        print("\n" + "="*60)
        print("テスト4: Z軸 - 値の変化（Inspectorでzに切り替えてください）")
        print("="*60)

        input("\nInspectorで 'Selected Axis' を Z に変更してからEnterを押してください...")

        self.received_values.clear()

        print("\n1. 初期値設定: z=-5.0")
        self.send_position(0.0, 0.0, -5.0)

        print("\n2. より負になる: z=-5.0 → z=-8.0 (期待値: 0)")
        self.send_position(0.0, 0.0, -8.0)

        print("\n3. 0に近づく: z=-8.0 → z=-3.0 (期待値: 1)")
        self.send_position(0.0, 0.0, -3.0)

        print(f"\n受信した値: {self.received_values}")

    def test_sine_wave(self):
        """サインカーブで連続的な値の変化をテスト"""
        print("\n" + "="*60)
        print("テスト5: サインカーブによる連続変化")
        print("="*60)
        self.received_values.clear()

        print("\nサインカーブで20回の値を送信します...")
        print("期待される動作:")
        print("  - 負の領域で減少→増加: 1→0")
        print("  - 0を越えて正の領域へ: 2")
        print("  - 正の領域で減少: 3")
        print("  - 0を越えて負の領域へ: 0")

        for i in range(20):
            angle = (i / 20) * 2 * math.pi
            x = math.sin(angle) * 5
            self.send_position(x, 0.0, 0.0)
            time.sleep(0.2)

        print(f"\n受信した値の遷移: {self.received_values}")

    def test_custom_values(self):
        """カスタム値の設定テスト"""
        print("\n" + "="*60)
        print("テスト6: カスタム値の設定")
        print("="*60)

        print("\nInspectorで以下の値を変更してください:")
        print("  Negative Increase Value: 10")
        print("  Negative Decrease Value: 20")
        print("  Positive Increase Value: 30")
        print("  Positive Decrease Value: 40")

        input("\n設定したらEnterを押してください...")

        self.received_values.clear()

        print("\n1. x=-1.0")
        self.send_position(-1.0, 0.0, 0.0)

        print("\n2. x=-2.0 (期待値: 10)")
        self.send_position(-2.0, 0.0, 0.0)

        print("\n3. x=-1.0 (期待値: 20)")
        self.send_position(-1.0, 0.0, 0.0)

        print("\n4. x=1.0 (期待値: 30)")
        self.send_position(1.0, 0.0, 0.0)

        print("\n5. x=0.5 (期待値: 40)")
        self.send_position(0.5, 0.0, 0.0)

        print(f"\n受信した値: {self.received_values}")

    def interactive_mode(self):
        """対話モード"""
        print("\n" + "="*60)
        print("対話モード")
        print("="*60)
        print("コマンド:")
        print("  pos <x> <y> <z>  - 位置を送信")
        print("  x <value>        - X軸のみ送信 (y=0, z=0)")
        print("  y <value>        - Y軸のみ送信 (x=0, z=0)")
        print("  z <value>        - Z軸のみ送信 (x=0, y=0)")
        print("  quit             - 終了")
        print("="*60)

        while True:
            try:
                command = input("\nコマンド> ").strip()

                if command == "quit":
                    break

                parts = command.split()
                if len(parts) == 0:
                    continue

                cmd = parts[0].lower()

                if cmd == "pos" and len(parts) >= 4:
                    x, y, z = float(parts[1]), float(parts[2]), float(parts[3])
                    self.send_position(x, y, z)

                elif cmd == "x" and len(parts) >= 2:
                    x = float(parts[1])
                    self.send_position(x, 0.0, 0.0)

                elif cmd == "y" and len(parts) >= 2:
                    y = float(parts[1])
                    self.send_position(0.0, y, 0.0)

                elif cmd == "z" and len(parts) >= 2:
                    z = float(parts[1])
                    self.send_position(0.0, 0.0, z)

                else:
                    print("不明なコマンド")

            except ValueError as e:
                print(f"エラー: {e}")
            except KeyboardInterrupt:
                print("\n中断されました")
                break


def main():
    import sys

    send_host = "127.0.0.1"
    send_port = 7001
    receive_port = 7002

    if len(sys.argv) >= 2:
        send_host = sys.argv[1]
    if len(sys.argv) >= 3:
        send_port = int(sys.argv[2])
    if len(sys.argv) >= 4:
        receive_port = int(sys.argv[3])

    print("="*60)
    print("OSCManagerテストツール")
    print("="*60)
    print(f"送信先: {send_host}:{send_port}")
    print(f"受信ポート: {receive_port}")
    print()
    print("Unityで以下の準備をしてください:")
    print("1. 空のGameObjectを作成")
    print("2. OSCManager.csをアタッチ")
    print("3. Inspector設定:")
    print("   - Receive Port: 7001")
    print("   - Transmit Host: 127.0.0.1")
    print("   - Transmit Port: 7002")
    print("4. Playボタンを押して実行")
    print()

    input("準備ができたらEnterを押してください...")

    tester = OSCManagerTester(send_host, send_port, receive_port)
    tester.start_server()

    time.sleep(0.5)

    try:
        print("\nテストメニュー:")
        print("1. X軸 - 負の値テスト")
        print("2. X軸 - 正の値テスト")
        print("3. Y軸テスト")
        print("4. Z軸テスト")
        print("5. サインカーブテスト")
        print("6. カスタム値テスト")
        print("7. 対話モード")
        print("8. すべて実行 (1,2,5)")

        choice = input("\n選択 (1-8): ").strip()

        if choice == "1":
            tester.test_x_axis_negative()
        elif choice == "2":
            tester.test_x_axis_positive()
        elif choice == "3":
            tester.test_y_axis()
        elif choice == "4":
            tester.test_z_axis()
        elif choice == "5":
            tester.test_sine_wave()
        elif choice == "6":
            tester.test_custom_values()
        elif choice == "7":
            tester.interactive_mode()
        elif choice == "8":
            tester.test_x_axis_negative()
            time.sleep(2)
            tester.test_x_axis_positive()
            time.sleep(2)
            tester.test_sine_wave()
        else:
            print("無効な選択です")

    except KeyboardInterrupt:
        print("\n\n中断されました")
    except Exception as e:
        print(f"\nエラー: {e}")
        import traceback
        traceback.print_exc()
    finally:
        tester.stop_server()


if __name__ == "__main__":
    main()
