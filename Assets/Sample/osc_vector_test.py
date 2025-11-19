#!/usr/bin/env python3
"""
OSCVectorManager テストスクリプト

ベクトルベースのOSC監視機能をテストするスクリプト
位置情報を送信して、移動ベクトルの検出をテストします

使用方法:
    pip install python-osc
    python osc_vector_test.py
"""

from pythonosc import udp_client, dispatcher, osc_server
import time
import threading
import math

class VectorTester:
    def __init__(self, send_host="127.0.0.1", send_port=7001, receive_port=7002):
        """
        ベクトルテスター

        Args:
            send_host: Unity側のIPアドレス
            send_port: Unity側の受信ポート
            receive_port: このスクリプトの受信ポート
        """
        self.send_host = send_host
        self.send_port = send_port
        self.receive_port = receive_port

        # OSC送信クライアント
        self.client = udp_client.SimpleUDPClient(send_host, send_port)

        # OSC受信サーバー
        self.dispatcher = dispatcher.Dispatcher()
        self.dispatcher.map("/vector", self.on_vector_received)
        self.server = osc_server.ThreadingOSCUDPServer(
            ("0.0.0.0", receive_port), self.dispatcher
        )

        # 受信した値を記録
        self.received_values = []
        self.last_value = None

        # サーバースレッド
        self.server_thread = None

        # 現在の位置
        self.current_pos = [0.0, 0.0, 0.0]

    def start_server(self):
        """受信サーバーを起動"""
        self.server_thread = threading.Thread(target=self.server.serve_forever, daemon=True)
        self.server_thread.start()
        print(f"[Tester] OSC受信サーバー起動: ポート {self.receive_port}")

    def stop_server(self):
        """受信サーバーを停止"""
        if self.server:
            self.server.shutdown()
            print("[Tester] OSC受信サーバー停止")

    def on_vector_received(self, address, *args):
        """ベクトルマネージャーからの出力を受信"""
        if len(args) > 0:
            value = args[0]
            self.last_value = value
            self.received_values.append(value)
            print(f"  <<< [RECEIVED] {address} = {value}")

    def send_position(self, x, y, z):
        """位置情報を送信"""
        self.current_pos = [x, y, z]
        self.client.send_message("/position", [x, y, z])
        print(f"  >>> [SEND] /position ({x:.3f}, {y:.3f}, {z:.3f})")
        time.sleep(0.1)

    def move_to(self, x, y, z, steps=10, delay=0.05):
        """現在位置から指定位置まで移動"""
        start_x, start_y, start_z = self.current_pos

        for i in range(steps + 1):
            t = i / steps
            pos_x = start_x + (x - start_x) * t
            pos_y = start_y + (y - start_y) * t
            pos_z = start_z + (z - start_z) * t
            self.send_position(pos_x, pos_y, pos_z)
            time.sleep(delay)

    def test_left_right_movement(self):
        """左右移動のテスト"""
        print("\n" + "="*70)
        print("テスト1: 左右移動（Horizontal 2D モード）")
        print("="*70)
        print("期待される動作:")
        print("  - 右に移動 → rightValue (デフォルト: 1)")
        print("  - 左に移動 → leftValue (デフォルト: 0)")
        print("="*70)

        self.received_values.clear()

        print("\n1. 初期位置: (0, 0, 0)")
        self.send_position(0.0, 0.0, 0.0)
        time.sleep(0.5)

        print("2. 右に移動: (0, 0, 0) → (1, 0, 0)")
        self.move_to(1.0, 0.0, 0.0)
        time.sleep(0.5)

        print("3. さらに右に移動: (1, 0, 0) → (2, 0, 0)")
        self.move_to(2.0, 0.0, 0.0)
        time.sleep(0.5)

        print("4. 左に移動: (2, 0, 0) → (0, 0, 0)")
        self.move_to(0.0, 0.0, 0.0)
        time.sleep(0.5)

        print(f"\n結果: 受信した値 = {self.received_values}")
        print("期待値: [1, 1, 0] または類似")

    def test_magnitude_threshold(self):
        """大きさ閾値のテスト"""
        print("\n" + "="*70)
        print("テスト2: 大きさ閾値")
        print("="*70)
        print("設定: Magnitude Threshold = 0.1")
        print("期待される動作:")
        print("  - 0.1以上の移動 → 検出")
        print("  - 0.1未満の移動 → 無視")
        print("="*70)

        self.received_values.clear()

        print("\n1. 初期位置: (0, 0, 0)")
        self.send_position(0.0, 0.0, 0.0)
        time.sleep(0.5)

        print("2. 小さい移動（0.05）: (0, 0, 0) → (0.05, 0, 0)")
        self.send_position(0.05, 0.0, 0.0)
        time.sleep(0.5)
        print(f"   検出: {'なし' if len(self.received_values) == 0 else 'あり'} (期待: なし)")

        print("3. 閾値以上の移動（0.2）: (0.05, 0, 0) → (0.25, 0, 0)")
        self.send_position(0.25, 0.0, 0.0)
        time.sleep(0.5)
        print(f"   検出: {'なし' if len(self.received_values) == 0 else 'あり'} (期待: あり)")

        print(f"\n結果: 受信した値 = {self.received_values}")

    def test_up_down_movement(self):
        """上下移動のテスト（Vertical 2D モード）"""
        print("\n" + "="*70)
        print("テスト3: 上下移動（Vertical 2D モード）")
        print("="*70)
        print("Inspectorで 'Vector Mode' を 'Vertical2D' に変更してください")
        input("変更したらEnterを押してください...")

        self.received_values.clear()

        print("\n1. 初期位置: (0, 0, 0)")
        self.send_position(0.0, 0.0, 0.0)
        time.sleep(0.5)

        print("2. 上に移動: (0, 0, 0) → (0, 1, 0)")
        self.move_to(0.0, 1.0, 0.0)
        time.sleep(0.5)

        print("3. 下に移動: (0, 1, 0) → (0, -1, 0)")
        self.move_to(0.0, -1.0, 0.0)
        time.sleep(0.5)

        print(f"\n結果: 受信した値 = {self.received_values}")
        print("期待値: [2, 3] (up, down)")

    def test_3d_movement(self):
        """3D移動のテスト"""
        print("\n" + "="*70)
        print("テスト4: 3D移動（Full 3D モード）")
        print("="*70)
        print("Inspectorで 'Vector Mode' を 'Full3D' に変更してください")
        input("変更したらEnterを押してください...")

        self.received_values.clear()

        print("\n1. 初期位置: (0, 0, 0)")
        self.send_position(0.0, 0.0, 0.0)
        time.sleep(0.5)

        print("2. 前に移動: (0, 0, 0) → (0, 0, 1)")
        self.move_to(0.0, 0.0, 1.0)
        time.sleep(0.5)

        print("3. 後ろに移動: (0, 0, 1) → (0, 0, -1)")
        self.move_to(0.0, 0.0, -1.0)
        time.sleep(0.5)

        print("4. 上に移動: (0, 0, -1) → (0, 2, -1)")
        self.move_to(0.0, 2.0, -1.0)
        time.sleep(0.5)

        print(f"\n結果: 受信した値 = {self.received_values}")
        print("期待値: [4, 5, 2] (forward, backward, up)")

    def test_circular_motion(self):
        """円運動のテスト"""
        print("\n" + "="*70)
        print("テスト5: 円運動（Horizontal 2D モード）")
        print("="*70)

        self.received_values.clear()

        print("\n円を描くように移動します...")
        radius = 1.0
        steps = 36

        for i in range(steps + 1):
            angle = (i / steps) * 2 * math.pi
            x = math.cos(angle) * radius
            z = math.sin(angle) * radius
            self.send_position(x, 0.0, z)
            time.sleep(0.05)

        print(f"\n結果: 受信した値の数 = {len(self.received_values)}")
        print(f"受信した値: {self.received_values}")

    def test_zigzag_motion(self):
        """ジグザグ移動のテスト"""
        print("\n" + "="*70)
        print("テスト6: ジグザグ移動")
        print("="*70)

        self.received_values.clear()

        print("\n左右にジグザグ移動します...")

        positions = [
            (0, 0, 0),
            (1, 0, 0),    # 右
            (0.5, 0, 0),  # 左
            (1.5, 0, 0),  # 右
            (0, 0, 0),    # 左
        ]

        for i, (x, y, z) in enumerate(positions):
            print(f"{i+1}. 位置: ({x}, {y}, {z})")
            self.send_position(x, y, z)
            time.sleep(0.3)

        print(f"\n結果: 受信した値 = {self.received_values}")

    def interactive_mode(self):
        """対話モード"""
        print("\n" + "="*70)
        print("対話モード")
        print("="*70)
        print("コマンド:")
        print("  pos <x> <y> <z>  - 絶対位置に移動")
        print("  move <x> <y> <z> - 相対移動")
        print("  reset            - 原点に戻る")
        print("  clear            - 受信値をクリア")
        print("  show             - 受信値を表示")
        print("  quit             - 終了")
        print("="*70)

        while True:
            try:
                command = input("\nコマンド> ").strip()

                if command == "quit":
                    break

                if command == "reset":
                    self.send_position(0.0, 0.0, 0.0)
                    continue

                if command == "clear":
                    self.received_values.clear()
                    print("受信値をクリアしました")
                    continue

                if command == "show":
                    print(f"受信した値: {self.received_values}")
                    print(f"現在位置: ({self.current_pos[0]:.3f}, {self.current_pos[1]:.3f}, {self.current_pos[2]:.3f})")
                    continue

                parts = command.split()
                if len(parts) == 0:
                    continue

                cmd = parts[0].lower()

                if cmd == "pos" and len(parts) >= 4:
                    x, y, z = float(parts[1]), float(parts[2]), float(parts[3])
                    self.send_position(x, y, z)

                elif cmd == "move" and len(parts) >= 4:
                    dx, dy, dz = float(parts[1]), float(parts[2]), float(parts[3])
                    new_x = self.current_pos[0] + dx
                    new_y = self.current_pos[1] + dy
                    new_z = self.current_pos[2] + dz
                    self.send_position(new_x, new_y, new_z)

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

    print("="*70)
    print("OSCVectorManager テストツール")
    print("="*70)
    print(f"送信先: {send_host}:{send_port}")
    print(f"受信ポート: {receive_port}")
    print()
    print("Unityで以下の準備をしてください:")
    print("1. 空のGameObjectを作成")
    print("2. OSCVectorManager.csをアタッチ")
    print("3. Inspector設定:")
    print("   - Receive Port: 7001")
    print("   - Receive Address: /position")
    print("   - Vector Mode: Horizontal2D")
    print("   - Magnitude Threshold: 0.1")
    print("   - Transmit Host: 127.0.0.1")
    print("   - Transmit Port: 7002")
    print("   - Transmit Address: /vector")
    print("4. Playボタンを押して実行")
    print()

    input("準備ができたらEnterを押してください...")

    tester = VectorTester(send_host, send_port, receive_port)
    tester.start_server()

    time.sleep(0.5)

    try:
        print("\nテストメニュー:")
        print("1. 左右移動テスト")
        print("2. 大きさ閾値テスト")
        print("3. 上下移動テスト (Vertical 2D)")
        print("4. 3D移動テスト (Full 3D)")
        print("5. 円運動テスト")
        print("6. ジグザグ移動テスト")
        print("7. 対話モード")
        print("8. 基本テスト実行 (1,2,5,6)")

        choice = input("\n選択 (1-8): ").strip()

        if choice == "1":
            tester.test_left_right_movement()
        elif choice == "2":
            tester.test_magnitude_threshold()
        elif choice == "3":
            tester.test_up_down_movement()
        elif choice == "4":
            tester.test_3d_movement()
        elif choice == "5":
            tester.test_circular_motion()
        elif choice == "6":
            tester.test_zigzag_motion()
        elif choice == "7":
            tester.interactive_mode()
        elif choice == "8":
            tester.test_left_right_movement()
            time.sleep(1)
            tester.test_magnitude_threshold()
            time.sleep(1)
            tester.test_circular_motion()
            time.sleep(1)
            tester.test_zigzag_motion()
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
