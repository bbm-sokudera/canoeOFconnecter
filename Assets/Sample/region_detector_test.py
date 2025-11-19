#!/usr/bin/env python3
"""
RegionDetectorテストスクリプト（OSC入力モード用）

RegionDetectorがOSC入力モード（useFrameSource = false）の時に、
領域のアクティブ化をシミュレートしてシーケンス検出をテストします。

使用方法:
    pip install python-osc
    python region_detector_test.py
"""

from pythonosc import udp_client, dispatcher, osc_server
import time
import threading

class RegionDetectorTester:
    def __init__(self, send_host="127.0.0.1", send_port=7003, receive_port=7002):
        """
        RegionDetectorテスター

        Args:
            send_host: Unity側のIPアドレス
            send_port: Unity側の受信ポート（RegionDetectorのOSC受信ポート）
            receive_port: このスクリプトの受信ポート（OSCManagerからのpaddle送信ポート）
        """
        self.send_host = send_host
        self.send_port = send_port
        self.receive_port = receive_port

        # OSC送信クライアント
        self.client = udp_client.SimpleUDPClient(send_host, send_port)

        # OSC受信サーバー（paddleメッセージ用）
        self.dispatcher = dispatcher.Dispatcher()
        self.dispatcher.map("/paddle", self.on_paddle_received)
        self.server = osc_server.ThreadingOSCUDPServer(
            ("0.0.0.0", receive_port), self.dispatcher
        )

        # 受信したpaddle値を記録
        self.received_paddles = []
        self.last_paddle = None

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

    def on_paddle_received(self, address, *args):
        """OSCManagerから/paddleメッセージを受信"""
        if len(args) > 0:
            paddle_num = args[0]
            self.last_paddle = paddle_num
            self.received_paddles.append(paddle_num)
            print(f"  <<< [PADDLE RECEIVED] {address} = {paddle_num}")

    def send_region_active(self, region_index):
        """領域アクティブ化メッセージを送信"""
        self.client.send_message("/region/active", region_index)
        print(f"  >>> [SEND] /region/active {region_index}")
        time.sleep(0.05)

    def test_sequence_2_to_3(self):
        """シーケンステスト: 領域2 -> 領域3 (paddle 1 期待)"""
        print("\n" + "="*60)
        print("テスト1: 領域2 -> 領域3 (右前進)")
        print("期待される結果: /paddle 1")
        print("="*60)
        self.received_paddles.clear()

        print("\n1. 領域2をアクティブ化")
        self.send_region_active(2)
        time.sleep(0.2)

        print("2. 領域3をアクティブ化")
        self.send_region_active(3)
        time.sleep(0.5)

        print(f"\n結果: 受信したpaddle = {self.received_paddles}")
        if 1 in self.received_paddles:
            print("✓ テスト成功!")
        else:
            print("✗ テスト失敗: paddle 1 が受信されませんでした")

    def test_sequence_0_to_1(self):
        """シーケンステスト: 領域0 -> 領域1 (paddle 2 期待)"""
        print("\n" + "="*60)
        print("テスト2: 領域0 -> 領域1 (左前進)")
        print("期待される結果: /paddle 2")
        print("="*60)
        self.received_paddles.clear()

        print("\n1. 領域0をアクティブ化")
        self.send_region_active(0)
        time.sleep(0.2)

        print("2. 領域1をアクティブ化")
        self.send_region_active(1)
        time.sleep(0.5)

        print(f"\n結果: 受信したpaddle = {self.received_paddles}")
        if 2 in self.received_paddles:
            print("✓ テスト成功!")
        else:
            print("✗ テスト失敗: paddle 2 が受信されませんでした")

    def test_sequence_3_to_2(self):
        """シーケンステスト: 領域3 -> 領域2 (paddle 3 期待)"""
        print("\n" + "="*60)
        print("テスト3: 領域3 -> 領域2 (右後進)")
        print("期待される結果: /paddle 3")
        print("="*60)
        self.received_paddles.clear()

        print("\n1. 領域3をアクティブ化")
        self.send_region_active(3)
        time.sleep(0.2)

        print("2. 領域2をアクティブ化")
        self.send_region_active(2)
        time.sleep(0.5)

        print(f"\n結果: 受信したpaddle = {self.received_paddles}")
        if 3 in self.received_paddles:
            print("✓ テスト成功!")
        else:
            print("✗ テスト失敗: paddle 3 が受信されませんでした")

    def test_sequence_1_to_0(self):
        """シーケンステスト: 領域1 -> 領域0 (paddle 4 期待)"""
        print("\n" + "="*60)
        print("テスト4: 領域1 -> 領域0 (左後進)")
        print("期待される結果: /paddle 4")
        print("="*60)
        self.received_paddles.clear()

        print("\n1. 領域1をアクティブ化")
        self.send_region_active(1)
        time.sleep(0.2)

        print("2. 領域0をアクティブ化")
        self.send_region_active(0)
        time.sleep(0.5)

        print(f"\n結果: 受信したpaddle = {self.received_paddles}")
        if 4 in self.received_paddles:
            print("✓ テスト成功!")
        else:
            print("✗ テスト失敗: paddle 4 が受信されませんでした")

    def test_invalid_sequence(self):
        """無効なシーケンステスト: 領域0 -> 領域2 (何も送信されないはず)"""
        print("\n" + "="*60)
        print("テスト5: 無効なシーケンス 領域0 -> 領域2")
        print("期待される結果: paddleメッセージなし")
        print("="*60)
        self.received_paddles.clear()

        print("\n1. 領域0をアクティブ化")
        self.send_region_active(0)
        time.sleep(0.2)

        print("2. 領域2をアクティブ化")
        self.send_region_active(2)
        time.sleep(0.5)

        print(f"\n結果: 受信したpaddle = {self.received_paddles}")
        if len(self.received_paddles) == 0:
            print("✓ テスト成功! (paddleメッセージなし)")
        else:
            print(f"✗ テスト失敗: paddle {self.received_paddles} が受信されました")

    def test_continuous_sequence(self):
        """連続シーケンステスト"""
        print("\n" + "="*60)
        print("テスト6: 連続シーケンス")
        print("2->3 (paddle 1), 3->2 (paddle 3), 2->3 (paddle 1)")
        print("="*60)
        self.received_paddles.clear()

        print("\n1. 領域2をアクティブ化")
        self.send_region_active(2)
        time.sleep(0.2)

        print("2. 領域3をアクティブ化")
        self.send_region_active(3)
        time.sleep(0.3)

        print("3. 領域2をアクティブ化")
        self.send_region_active(2)
        time.sleep(0.3)

        print("4. 領域3をアクティブ化")
        self.send_region_active(3)
        time.sleep(0.5)

        print(f"\n結果: 受信したpaddle = {self.received_paddles}")
        expected = [1, 3, 1]
        if self.received_paddles == expected:
            print("✓ テスト成功!")
        else:
            print(f"✗ テスト失敗: 期待値 {expected} != 実際 {self.received_paddles}")

    def interactive_mode(self):
        """対話モード"""
        print("\n" + "="*60)
        print("対話モード")
        print("="*60)
        print("コマンド:")
        print("  0-3        - 領域0-3をアクティブ化")
        print("  s [start] [end] - シーケンステスト (例: s 2 3)")
        print("  quit       - 終了")
        print("="*60)

        while True:
            try:
                command = input("\nコマンド> ").strip()

                if command == "quit":
                    break

                if command.isdigit():
                    region = int(command)
                    if 0 <= region <= 3:
                        self.send_region_active(region)
                    else:
                        print("領域番号は0-3の範囲で指定してください")

                elif command.startswith("s "):
                    parts = command.split()
                    if len(parts) >= 3:
                        start = int(parts[1])
                        end = int(parts[2])
                        print(f"\nシーケンステスト: {start} -> {end}")
                        self.received_paddles.clear()
                        self.send_region_active(start)
                        time.sleep(0.2)
                        self.send_region_active(end)
                        time.sleep(0.5)
                        print(f"受信したpaddle: {self.received_paddles}")
                    else:
                        print("使用方法: s [開始領域] [終了領域]")

                else:
                    print("不明なコマンド")

            except ValueError:
                print("エラー: 無効な入力")
            except KeyboardInterrupt:
                print("\n中断されました")
                break


def main():
    import sys

    send_host = "127.0.0.1"
    send_port = 7003  # RegionDetectorのOSC受信ポート
    receive_port = 7002  # OSCManagerの送信ポート

    if len(sys.argv) >= 2:
        send_host = sys.argv[1]
    if len(sys.argv) >= 3:
        send_port = int(sys.argv[2])
    if len(sys.argv) >= 4:
        receive_port = int(sys.argv[3])

    print("="*60)
    print("RegionDetector テストツール（OSC入力モード）")
    print("="*60)
    print(f"送信先: {send_host}:{send_port} (/region/active)")
    print(f"受信ポート: {receive_port} (/paddle)")
    print()
    print("Unityで以下の準備をしてください:")
    print("1. RegionDetectorがアタッチされたGameObjectを用意")
    print("2. Inspector設定:")
    print("   ☐ Use Frame Source: チェックを外す (OSC入力モード)")
    print("   - OSC Receive Port: 7003")
    print("   - Region Active Address: /region/active")
    print("3. OSCManager設定:")
    print("   - Transmit Port: 7002")
    print("4. Playボタンを押して実行")
    print()

    input("準備ができたらEnterを押してください...")

    tester = RegionDetectorTester(send_host, send_port, receive_port)
    tester.start_server()

    time.sleep(0.5)

    try:
        print("\nテストメニュー:")
        print("1. シーケンステスト: 領域2->3 (paddle 1)")
        print("2. シーケンステスト: 領域0->1 (paddle 2)")
        print("3. シーケンステスト: 領域3->2 (paddle 3)")
        print("4. シーケンステスト: 領域1->0 (paddle 4)")
        print("5. 無効なシーケンステスト")
        print("6. 連続シーケンステスト")
        print("7. 対話モード")
        print("8. すべてのシーケンステスト実行 (1-6)")

        choice = input("\n選択 (1-8): ").strip()

        if choice == "1":
            tester.test_sequence_2_to_3()
        elif choice == "2":
            tester.test_sequence_0_to_1()
        elif choice == "3":
            tester.test_sequence_3_to_2()
        elif choice == "4":
            tester.test_sequence_1_to_0()
        elif choice == "5":
            tester.test_invalid_sequence()
        elif choice == "6":
            tester.test_continuous_sequence()
        elif choice == "7":
            tester.interactive_mode()
        elif choice == "8":
            tester.test_sequence_2_to_3()
            time.sleep(1)
            tester.test_sequence_0_to_1()
            time.sleep(1)
            tester.test_sequence_3_to_2()
            time.sleep(1)
            tester.test_sequence_1_to_0()
            time.sleep(1)
            tester.test_invalid_sequence()
            time.sleep(1)
            tester.test_continuous_sequence()
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
