#!/usr/bin/env python3
"""
OSCXPositionYVectorManager テストスクリプト

X軸の位置とY軸のベクトル方向を組み合わせた判定をテストします

使用方法:
    pip install python-osc
    python osc_x_position_y_vector_test.py
"""

from pythonosc import udp_client, dispatcher, osc_server
import time
import threading

class XPositionYVectorTester:
    def __init__(self, send_host="127.0.0.1", send_port=7001, receive_port=7002):
        """
        X位置+Yベクトルテスター
        """
        self.send_host = send_host
        self.send_port = send_port
        self.receive_port = receive_port

        # OSC送信クライアント
        self.client = udp_client.SimpleUDPClient(send_host, send_port)

        # OSC受信サーバー
        self.dispatcher = dispatcher.Dispatcher()
        self.dispatcher.map("/direction", self.on_direction_received)
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

    def on_direction_received(self, address, *args):
        """方向値を受信"""
        if len(args) > 0:
            value = args[0]
            self.last_value = value
            self.received_values.append(value)

            # 値を解釈
            direction_name = self.interpret_value(value)
            print(f"  <<< [RECEIVED] {address} = {value} ({direction_name})")

    def interpret_value(self, value):
        """値を方向名に変換"""
        mapping = {
            0: "Left Forward",
            2: "Right Forward",
            1: "Left Backward",
            3: "Right Backward"
        }
        return mapping.get(value, "Unknown")

    def send_position(self, x, y, z):
        """位置情報を送信"""
        self.current_pos = [x, y, z]
        self.client.send_message("/position", [x, y, z])
        print(f"  >>> [SEND] /position ({x:.3f}, {y:.3f}, {z:.3f})")
        time.sleep(0.1)

    def move_to(self, x, y, z, steps=5, delay=0.1):
        """現在位置から指定位置まで移動"""
        start_x, start_y, start_z = self.current_pos

        for i in range(steps + 1):
            t = i / steps
            pos_x = start_x + (x - start_x) * t
            pos_y = start_y + (y - start_y) * t
            pos_z = start_z + (z - start_z) * t
            self.send_position(pos_x, pos_y, pos_z)
            time.sleep(delay)

    def test_left_forward(self):
        """左＋前方向のテスト"""
        print("\n" + "="*70)
        print("テスト1: 左＋前方向")
        print("="*70)
        print("期待される動作:")
        print("  - X < 0 (左側) かつ Y+ (前方向) → 0 (Left Forward)")
        print("="*70)

        self.received_values.clear()

        print("\n1. 初期位置: 左側 (-1, 0, 0)")
        self.send_position(-1.0, 0.0, 0.0)
        time.sleep(0.5)

        print("2. Y+方向に移動: (-1, 0, 0) → (-1, 0.5, 0)")
        self.move_to(-1.0, 0.5, 0.0)
        time.sleep(0.5)

        print(f"\n結果: 受信した値 = {self.received_values}")
        if 0 in self.received_values:
            print("✓ テスト成功! Left Forward (0) が検出されました")
        else:
            print(f"✗ テスト失敗: 期待値 0 が受信されませんでした")

    def test_right_forward(self):
        """右＋前方向のテスト"""
        print("\n" + "="*70)
        print("テスト2: 右＋前方向")
        print("="*70)
        print("期待される動作:")
        print("  - X >= 0 (右側) かつ Y+ (前方向) → 2 (Right Forward)")
        print("="*70)

        self.received_values.clear()

        print("\n1. 初期位置: 右側 (1, 0, 0)")
        self.send_position(1.0, 0.0, 0.0)
        time.sleep(0.5)

        print("2. Y+方向に移動: (1, 0, 0) → (1, 0.5, 0)")
        self.move_to(1.0, 0.5, 0.0)
        time.sleep(0.5)

        print(f"\n結果: 受信した値 = {self.received_values}")
        if 2 in self.received_values:
            print("✓ テスト成功! Right Forward (2) が検出されました")
        else:
            print(f"✗ テスト失敗: 期待値 2 が受信されませんでした")

    def test_left_backward(self):
        """左＋後方向のテスト"""
        print("\n" + "="*70)
        print("テスト3: 左＋後方向")
        print("="*70)
        print("期待される動作:")
        print("  - X < 0 (左側) かつ Y- (後方向) → 1 (Left Backward)")
        print("="*70)

        self.received_values.clear()

        print("\n1. 初期位置: 左側 (-1, 0.5, 0)")
        self.send_position(-1.0, 0.5, 0.0)
        time.sleep(0.5)

        print("2. Y-方向に移動: (-1, 0.5, 0) → (-1, 0, 0)")
        self.move_to(-1.0, 0.0, 0.0)
        time.sleep(0.5)

        print(f"\n結果: 受信した値 = {self.received_values}")
        if 1 in self.received_values:
            print("✓ テスト成功! Left Backward (1) が検出されました")
        else:
            print(f"✗ テスト失敗: 期待値 1 が受信されませんでした")

    def test_right_backward(self):
        """右＋後方向のテスト"""
        print("\n" + "="*70)
        print("テスト4: 右＋後方向")
        print("="*70)
        print("期待される動作:")
        print("  - X >= 0 (右側) かつ Y- (後方向) → 3 (Right Backward)")
        print("="*70)

        self.received_values.clear()

        print("\n1. 初期位置: 右側 (1, 0.5, 0)")
        self.send_position(1.0, 0.5, 0.0)
        time.sleep(0.5)

        print("2. Y-方向に移動: (1, 0.5, 0) → (1, 0, 0)")
        self.move_to(1.0, 0.0, 0.0)
        time.sleep(0.5)

        print(f"\n結果: 受信した値 = {self.received_values}")
        if 3 in self.received_values:
            print("✓ テスト成功! Right Backward (3) が検出されました")
        else:
            print(f"✗ テスト失敗: 期待値 3 が受信されませんでした")

    def test_threshold(self):
        """閾値のテスト"""
        print("\n" + "="*70)
        print("テスト5: Y軸ベクトル閾値")
        print("="*70)
        print("設定: Y Vector Magnitude Threshold = 0.1")
        print("期待される動作:")
        print("  - Y変化 < 0.1 → 検出しない")
        print("  - Y変化 >= 0.1 → 検出する")
        print("="*70)

        self.received_values.clear()

        print("\n1. 初期位置: (-1, 0, 0)")
        self.send_position(-1.0, 0.0, 0.0)
        time.sleep(0.5)

        print("2. 小さい移動（Y=0.05）: (-1, 0, 0) → (-1, 0.05, 0)")
        self.send_position(-1.0, 0.05, 0.0)
        time.sleep(0.5)
        print(f"   検出: {'なし' if len(self.received_values) == 0 else 'あり'} (期待: なし)")

        print("3. 閾値以上の移動（Y=0.2）: (-1, 0.05, 0) → (-1, 0.25, 0)")
        self.send_position(-1.0, 0.25, 0.0)
        time.sleep(0.5)
        print(f"   検出: {'なし' if len(self.received_values) == 0 else 'あり'} (期待: あり)")

        print(f"\n結果: 受信した値 = {self.received_values}")

    def test_crossing_center(self):
        """中心を跨ぐ移動のテスト"""
        print("\n" + "="*70)
        print("テスト6: X軸中心を跨ぐ移動")
        print("="*70)
        print("期待される動作:")
        print("  - 左側 → 右側に移動しながらY+方向 → Left Forward → Right Forward")
        print("="*70)

        self.received_values.clear()

        print("\n1. 初期位置: 左側 (-1, 0, 0)")
        self.send_position(-1.0, 0.0, 0.0)
        time.sleep(0.5)

        print("2. 左側でY+移動: (-1, 0, 0) → (-1, 0.3, 0)")
        self.move_to(-1.0, 0.3, 0.0, steps=3)
        time.sleep(0.5)

        print("3. 右側に移動: (-1, 0.3, 0) → (1, 0.3, 0)")
        self.move_to(1.0, 0.3, 0.0, steps=3)
        time.sleep(0.5)

        print("4. 右側でY+移動: (1, 0.3, 0) → (1, 0.6, 0)")
        self.move_to(1.0, 0.6, 0.0, steps=3)
        time.sleep(0.5)

        print(f"\n結果: 受信した値 = {self.received_values}")
        print("期待: 0 (Left Forward) と 2 (Right Forward) が含まれる")

    def test_zigzag(self):
        """ジグザグ移動のテスト"""
        print("\n" + "="*70)
        print("テスト7: ジグザグ移動（左右＋前後）")
        print("="*70)

        self.received_values.clear()

        positions = [
            (-1, 0, 0, "初期位置（左）"),
            (-1, 0.3, 0, "左＋前"),
            (-1, 0, 0, "左＋後"),
            (1, 0, 0, "右に移動"),
            (1, 0.3, 0, "右＋前"),
            (1, 0, 0, "右＋後"),
        ]

        for i, (x, y, z, desc) in enumerate(positions):
            print(f"\n{i+1}. {desc}: ({x}, {y}, {z})")
            self.send_position(x, y, z)
            time.sleep(0.4)

        print(f"\n結果: 受信した値 = {self.received_values}")
        print("期待: [0, 1, 2, 3] または類似の組み合わせ")

    def interactive_mode(self):
        """対話モード"""
        print("\n" + "="*70)
        print("対話モード")
        print("="*70)
        print("コマンド:")
        print("  pos <x> <y> <z>  - 絶対位置に移動")
        print("  left             - X=-1に移動")
        print("  right            - X=1に移動")
        print("  forward          - Y+0.3移動")
        print("  backward         - Y-0.3移動")
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

                if command == "left":
                    self.send_position(-1.0, self.current_pos[1], self.current_pos[2])
                    continue

                if command == "right":
                    self.send_position(1.0, self.current_pos[1], self.current_pos[2])
                    continue

                if command == "forward":
                    self.send_position(self.current_pos[0], self.current_pos[1] + 0.3, self.current_pos[2])
                    continue

                if command == "backward":
                    self.send_position(self.current_pos[0], self.current_pos[1] - 0.3, self.current_pos[2])
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
    print("OSCXPositionYVectorManager テストツール")
    print("="*70)
    print(f"送信先: {send_host}:{send_port}")
    print(f"受信ポート: {receive_port}")
    print()
    print("Unityで以下の準備をしてください:")
    print("1. 空のGameObjectを作成")
    print("2. OSCXPositionYVectorManager.csをアタッチ")
    print("3. Inspector設定:")
    print("   - Receive Port: 7001")
    print("   - Receive Address: /position")
    print("   - X Center Position: 0")
    print("   - Y Vector Magnitude Threshold: 0.1")
    print("   - Left Forward Value: 0")
    print("   - Right Forward Value: 2")
    print("   - Left Backward Value: 1")
    print("   - Right Backward Value: 3")
    print("   - Transmit Host: 127.0.0.1")
    print("   - Transmit Port: 7002")
    print("   - Transmit Address: /direction")
    print("4. Playボタンを押して実行")
    print()

    input("準備ができたらEnterを押してください...")

    tester = XPositionYVectorTester(send_host, send_port, receive_port)
    tester.start_server()

    time.sleep(0.5)

    try:
        print("\nテストメニュー:")
        print("1. 左＋前方向テスト (0)")
        print("2. 右＋前方向テスト (2)")
        print("3. 左＋後方向テスト (1)")
        print("4. 右＋後方向テスト (3)")
        print("5. Y軸ベクトル閾値テスト")
        print("6. X軸中心を跨ぐ移動テスト")
        print("7. ジグザグ移動テスト")
        print("8. 対話モード")
        print("9. すべてのテスト実行 (1-7)")

        choice = input("\n選択 (1-9): ").strip()

        if choice == "1":
            tester.test_left_forward()
        elif choice == "2":
            tester.test_right_forward()
        elif choice == "3":
            tester.test_left_backward()
        elif choice == "4":
            tester.test_right_backward()
        elif choice == "5":
            tester.test_threshold()
        elif choice == "6":
            tester.test_crossing_center()
        elif choice == "7":
            tester.test_zigzag()
        elif choice == "8":
            tester.interactive_mode()
        elif choice == "9":
            tester.test_left_forward()
            time.sleep(1)
            tester.test_right_forward()
            time.sleep(1)
            tester.test_left_backward()
            time.sleep(1)
            tester.test_right_backward()
            time.sleep(1)
            tester.test_threshold()
            time.sleep(1)
            tester.test_crossing_center()
            time.sleep(1)
            tester.test_zigzag()
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
