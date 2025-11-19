#!/usr/bin/env python3
"""
OSCManager 条件軸テストスクリプト

条件軸機能をテストするためのスクリプト
メイン軸とは別の軸が指定範囲内にある場合のみ、メイン軸の変化を検出することを確認します

使用方法:
    pip install python-osc
    python osc_conditional_axis_test.py
"""

from pythonosc import udp_client, dispatcher, osc_server
import time
import threading

class ConditionalAxisTester:
    def __init__(self, send_host="127.0.0.1", send_port=7001, receive_port=7002):
        """
        条件軸テスター

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
        print(f"[Tester] OSC受信サーバー起動: ポート {self.receive_port}")

    def stop_server(self):
        """受信サーバーを停止"""
        if self.server:
            self.server.shutdown()
            print("[Tester] OSC受信サーバー停止")

    def on_output_received(self, address, *args):
        """OSCManagerからの出力を受信"""
        if len(args) > 0:
            value = args[0]
            self.last_received_value = value
            self.received_values.append(value)
            print(f"  <<< [OUTPUT RECEIVED] {address} = {value}")

    def send_position(self, x, y, z):
        """位置情報を送信"""
        self.client.send_message("/position", [x, y, z])
        print(f"  >>> [SEND] /position ({x:.3f}, {y:.3f}, {z:.3f})")
        time.sleep(0.1)

    def test_conditional_axis_in_range(self):
        """条件軸が範囲内の場合のテスト"""
        print("\n" + "="*70)
        print("テスト1: 条件軸が範囲内にある場合")
        print("="*70)
        print("設定:")
        print("  - メイン軸: X")
        print("  - 条件軸: Z")
        print("  - 条件範囲: -1.0 ~ 1.0")
        print("\n期待される動作:")
        print("  Z が範囲内 → X の変化を検出して値を送信")
        print("="*70)

        self.received_values.clear()

        print("\n1. 初期値: x=-1.0, z=0.0 (Z は範囲内)")
        self.send_position(-1.0, 0.0, 0.0)

        print("2. X を変化: x=-2.0, z=0.0 (Z は範囲内)")
        print("   → X が負の方向に増加 → 0 を送信するはず")
        self.send_position(-2.0, 0.0, 0.0)
        time.sleep(0.2)

        print("3. X を変化: x=-1.0, z=0.5 (Z は範囲内)")
        print("   → X が 0 に近づく → 1 を送信するはず")
        self.send_position(-1.0, 0.0, 0.5)
        time.sleep(0.2)

        print(f"\n結果: 受信した値 = {self.received_values}")
        if len(self.received_values) == 2:
            print("✓ テスト成功! (2つの値が送信された)")
        else:
            print(f"✗ テスト失敗: 期待値 2 != 実際 {len(self.received_values)}")

    def test_conditional_axis_out_of_range(self):
        """条件軸が範囲外の場合のテスト"""
        print("\n" + "="*70)
        print("テスト2: 条件軸が範囲外にある場合")
        print("="*70)
        print("設定:")
        print("  - メイン軸: X")
        print("  - 条件軸: Z")
        print("  - 条件範囲: -1.0 ~ 1.0")
        print("\n期待される動作:")
        print("  Z が範囲外 → X の変化を無視（値を送信しない）")
        print("="*70)

        self.received_values.clear()

        print("\n1. 初期値: x=-1.0, z=2.0 (Z は範囲外)")
        self.send_position(-1.0, 0.0, 2.0)

        print("2. X を変化: x=-2.0, z=2.0 (Z は範囲外)")
        print("   → Z が範囲外なので、X の変化は無視されるはず")
        self.send_position(-2.0, 0.0, 2.0)
        time.sleep(0.2)

        print("3. X を変化: x=-1.0, z=-2.0 (Z は範囲外)")
        print("   → Z が範囲外なので、X の変化は無視されるはず")
        self.send_position(-1.0, 0.0, -2.0)
        time.sleep(0.2)

        print(f"\n結果: 受信した値 = {self.received_values}")
        if len(self.received_values) == 0:
            print("✓ テスト成功! (値が送信されなかった)")
        else:
            print(f"✗ テスト失敗: 値が送信されました {self.received_values}")

    def test_conditional_axis_boundary(self):
        """条件軸の境界値テスト"""
        print("\n" + "="*70)
        print("テスト3: 条件軸の境界値")
        print("="*70)
        print("設定:")
        print("  - メイン軸: X")
        print("  - 条件軸: Z")
        print("  - 条件範囲: -1.0 ~ 1.0")
        print("\n期待される動作:")
        print("  Z = -1.0 (境界値) → 範囲内として処理")
        print("  Z = 1.0 (境界値) → 範囲内として処理")
        print("  Z = -1.001 (範囲外) → 無視")
        print("  Z = 1.001 (範囲外) → 無視")
        print("="*70)

        self.received_values.clear()

        print("\n1. 初期値: x=-1.0, z=-1.0 (境界値、範囲内)")
        self.send_position(-1.0, 0.0, -1.0)

        print("2. X を変化: x=-2.0, z=-1.0 (境界値、範囲内)")
        print("   → 送信されるはず")
        self.send_position(-2.0, 0.0, -1.0)
        time.sleep(0.2)

        print("3. X を変化: x=-1.0, z=1.0 (境界値、範囲内)")
        print("   → 送信されるはず")
        self.send_position(-1.0, 0.0, 1.0)
        time.sleep(0.2)

        print("4. X を変化: x=-2.0, z=-1.001 (範囲外)")
        print("   → 無視されるはず")
        self.send_position(-2.0, 0.0, -1.001)
        time.sleep(0.2)

        print("5. X を変化: x=-1.0, z=1.001 (範囲外)")
        print("   → 無視されるはず")
        self.send_position(-1.0, 0.0, 1.001)
        time.sleep(0.2)

        print(f"\n結果: 受信した値 = {self.received_values}")
        if len(self.received_values) == 2:
            print("✓ テスト成功! (境界値のみ処理された)")
        else:
            print(f"✗ テスト失敗: 期待値 2 != 実際 {len(self.received_values)}")

    def test_conditional_axis_crossing_boundary(self):
        """条件軸が範囲をまたぐ場合のテスト"""
        print("\n" + "="*70)
        print("テスト4: 条件軸が範囲内外を行き来する場合")
        print("="*70)
        print("設定:")
        print("  - メイン軸: X")
        print("  - 条件軸: Z")
        print("  - 条件範囲: -1.0 ~ 1.0")
        print("\n期待される動作:")
        print("  Z が範囲内に入ったり出たりする間、X の変化は適切に処理される")
        print("="*70)

        self.received_values.clear()

        print("\n1. x=-1.0, z=0.0 (範囲内)")
        self.send_position(-1.0, 0.0, 0.0)

        print("2. x=-2.0, z=0.5 (範囲内) → 送信")
        self.send_position(-2.0, 0.0, 0.5)
        time.sleep(0.2)

        print("3. x=-1.0, z=2.0 (範囲外) → 無視")
        self.send_position(-1.0, 0.0, 2.0)
        time.sleep(0.2)

        print("4. x=-2.0, z=2.0 (範囲外) → 無視")
        self.send_position(-2.0, 0.0, 2.0)
        time.sleep(0.2)

        print("5. x=-1.0, z=0.0 (範囲内に戻る) → 送信")
        self.send_position(-1.0, 0.0, 0.0)
        time.sleep(0.2)

        print("6. x=-2.0, z=0.0 (範囲内) → 送信")
        self.send_position(-2.0, 0.0, 0.0)
        time.sleep(0.2)

        print(f"\n結果: 受信した値 = {self.received_values}")
        if len(self.received_values) == 3:
            print("✓ テスト成功! (範囲内の時のみ処理された)")
        else:
            print(f"✗ テスト失敗: 期待値 3 != 実際 {len(self.received_values)}")

    def test_different_conditional_axes(self):
        """異なる条件軸のテスト"""
        print("\n" + "="*70)
        print("テスト5: 異なる条件軸の組み合わせ")
        print("="*70)
        print("\nテストケース A: メイン軸=X, 条件軸=Y")
        print("  Inspectorで以下を設定してください:")
        print("    - Selected Axis: X")
        print("    - Enable Conditional Axis: ON")
        print("    - Conditional Axis: Y")
        print("    - Conditional Axis Min: -0.5")
        print("    - Conditional Axis Max: 0.5")

        input("\n設定したらEnterを押してください...")

        self.received_values.clear()

        print("\n1. x=-1.0, y=0.0 (Y は範囲内)")
        self.send_position(-1.0, 0.0, 0.0)

        print("2. x=-2.0, y=0.3 (Y は範囲内) → 送信")
        self.send_position(-2.0, 0.3, 0.0)
        time.sleep(0.2)

        print("3. x=-1.0, y=1.0 (Y は範囲外) → 無視")
        self.send_position(-1.0, 1.0, 0.0)
        time.sleep(0.2)

        print(f"\n結果: 受信した値 = {self.received_values}")
        if len(self.received_values) == 1:
            print("✓ テスト成功!")
        else:
            print(f"✗ テスト失敗: 期待値 1 != 実際 {len(self.received_values)}")

    def interactive_mode(self):
        """対話モード"""
        print("\n" + "="*70)
        print("対話モード")
        print("="*70)
        print("コマンド:")
        print("  pos <x> <y> <z>  - 位置を送信")
        print("  x <value>        - X軸のみ変更 (他は0)")
        print("  y <value>        - Y軸のみ変更 (他は0)")
        print("  z <value>        - Z軸のみ変更 (他は0)")
        print("  clear            - 受信値をクリア")
        print("  show             - 受信値を表示")
        print("  quit             - 終了")
        print("="*70)

        while True:
            try:
                command = input("\nコマンド> ").strip()

                if command == "quit":
                    break

                if command == "clear":
                    self.received_values.clear()
                    print("受信値をクリアしました")
                    continue

                if command == "show":
                    print(f"受信した値: {self.received_values}")
                    continue

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

    print("="*70)
    print("OSCManager 条件軸テストツール")
    print("="*70)
    print(f"送信先: {send_host}:{send_port}")
    print(f"受信ポート: {receive_port}")
    print()
    print("Unityで以下の準備をしてください:")
    print("1. OSCManagerがアタッチされたGameObjectを用意")
    print("2. Inspector設定:")
    print("   - Receive Port: 7001")
    print("   - Receive Address: /position")
    print("   - Selected Axis: X")
    print("   ☑ Enable Conditional Axis: ON")
    print("   - Conditional Axis: Z")
    print("   - Conditional Axis Min: -1.0")
    print("   - Conditional Axis Max: 1.0")
    print("   - Transmit Port: 7002")
    print("   - Transmit Address: /output")
    print("3. Playボタンを押して実行")
    print()

    input("準備ができたらEnterを押してください...")

    tester = ConditionalAxisTester(send_host, send_port, receive_port)
    tester.start_server()

    time.sleep(0.5)

    try:
        print("\nテストメニュー:")
        print("1. 条件軸が範囲内の場合")
        print("2. 条件軸が範囲外の場合")
        print("3. 条件軸の境界値テスト")
        print("4. 条件軸が範囲をまたぐ場合")
        print("5. 異なる条件軸の組み合わせ")
        print("6. 対話モード")
        print("7. すべてのテスト実行 (1-4)")

        choice = input("\n選択 (1-7): ").strip()

        if choice == "1":
            tester.test_conditional_axis_in_range()
        elif choice == "2":
            tester.test_conditional_axis_out_of_range()
        elif choice == "3":
            tester.test_conditional_axis_boundary()
        elif choice == "4":
            tester.test_conditional_axis_crossing_boundary()
        elif choice == "5":
            tester.test_different_conditional_axes()
        elif choice == "6":
            tester.interactive_mode()
        elif choice == "7":
            tester.test_conditional_axis_in_range()
            time.sleep(1)
            tester.test_conditional_axis_out_of_range()
            time.sleep(1)
            tester.test_conditional_axis_boundary()
            time.sleep(1)
            tester.test_conditional_axis_crossing_boundary()
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
