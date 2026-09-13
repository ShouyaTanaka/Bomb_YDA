using Cysharp.Threading.Tasks;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using UnityEngine;

namespace BoothNetwork
{
    public class BoothNetworkService : MonoBehaviour
    {
        private static BoothNetworkService instance;
        public static BoothNetworkService Instance => instance;

        private ClientWebSocket webSocket;
        private CancellationTokenSource cts;

        // ==========================================
        // メイン側（ゲーム本体）が受け取る受信イベント
        // ==========================================
        public static event Action OnConnected;
        public static event Action OnDisconnected;

        // 黒電話
        public static event Action OnPickUpPhone;
        public static event Action OnHangUpPhone;

        // 爆弾各ギミック
        public static event Action<string, string> OnClockRotated;          // hour, minutes
        public static event Action<int, bool> OnToggleSwitchChanged;        // no (1-4), isOn
        public static event Action<int, bool> OnPushButtonChanged;          // no (1-3), isPressed
        public static event Action<int> OnWireCut;                          // no (1-5)

        // デバイス側自律判定
        public static event Action OnBombClear;
        public static event Action OnBombMiss;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            if (instance == null)
            {
                GameObject go = new GameObject("[BoothNetworkService]");
                instance = go.AddComponent<BoothNetworkService>();
                DontDestroyOnLoad(go);
            }
        }

        private void Start()
        {
            ConnectAsync().Forget();
        }

        private async UniTaskVoid ConnectAsync()
        {
            cts = new CancellationTokenSource();
            BoothConfig config = BoothConfig.LoadOrCreate();
            string url = config.GetWebSocketUrl();

            try
            {
                webSocket = new ClientWebSocket();
                Debug.Log($"[BoothNetwork] 接続開始: {url}");
                await webSocket.ConnectAsync(new Uri(url), cts.Token);
                Debug.Log("[BoothNetwork] サーバーに接続しました");

                OnConnected?.Invoke();
                ReceiveLoopAsync(cts.Token).Forget();
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BoothNetwork] 接続エラー: {ex.Message}");
            }
        }

        private async UniTaskVoid ReceiveLoopAsync(CancellationToken token)
        {
            byte[] buffer = new byte[4096];

            try
            {
                while (webSocket != null && webSocket.State == WebSocketState.Open && !token.IsCancellationRequested)
                {
                    using (var ms = new MemoryStream())
                    {
                        WebSocketReceiveResult result;
                        do
                        {
                            result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), token);
                            ms.Write(buffer, 0, result.Count);
                        } while (!result.EndOfMessage);

                        if (result.MessageType == WebSocketMessageType.Close)
                        {
                            await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closed by server", token);
                            break;
                        }

                        string json = Encoding.UTF8.GetString(ms.ToArray());
                        // メインスレッドでパース & イベント発火
                        await UniTask.SwitchToMainThread();
                        DispatchMessage(json);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                Debug.LogError($"[BoothNetwork] 受信ループ例外: {ex.Message}");
            }
            finally
            {
                OnDisconnected?.Invoke();
            }
        }

        private void DispatchMessage(string json)
        {

            // 受信した生JSONをコンソールに出力
            Debug.Log($"<color=#00ffff>[BoothNetwork 受信]</color> {json}");

            ReceiveHeader header = JsonUtility.FromJson<ReceiveHeader>(json);
            if (header == null) return;

            // 1. デバイス直形式: toggleSwitch
            if (header.device == "toggleSwitch")
            {
                var msg = JsonUtility.FromJson<ToggleSwitchMessage>(json);
                if (msg != null && msg.parameter != null)
                {
                    bool isOn = msg.parameter.status == "on";
                    OnToggleSwitchChanged?.Invoke(msg.parameter.no, isOn);
                }
                return;
            }

            // 2. デバイス直形式: pushButton
            if (header.device == "pushButton")
            {
                var msg = JsonUtility.FromJson<PushButtonMessage>(json);
                if (msg != null && msg.parameter != null)
                {
                    bool isPressed = msg.parameter.status == "press";
                    OnPushButtonChanged?.Invoke(msg.parameter.no, isPressed);
                }
                return;
            }

            // 3. 通常コマンド形式 (command 判定)
            switch (header.command)
            {
                case "PickUpPhone":
                    OnPickUpPhone?.Invoke();
                    break;
                case "HangUpPhone":
                    OnHangUpPhone?.Invoke();
                    break;
                case "RotateSwitchOperation":
                    var clockMsg = JsonUtility.FromJson<ClockMessage>(json);
                    if (clockMsg?.parameter != null)
                    {
                        OnClockRotated?.Invoke(clockMsg.parameter.hour, clockMsg.parameter.minutes);
                    }
                    break;
                case "WireCut":
                    var wireMsg = JsonUtility.FromJson<WireMessage>(json);
                    if (wireMsg?.parameter != null)
                    {
                        OnWireCut?.Invoke(wireMsg.parameter.no);
                    }
                    break;
                case "BombClear":
                    OnBombClear?.Invoke();
                    break;
                case "BombMiss":
                    OnBombMiss?.Invoke();
                    break;
            }
        }

        // ==========================================
        // メイン側（ゲーム本体）から呼び出す送信メソッド群
        // ==========================================

        public static void SendStart()
        {
            SendJson(new SimpleCommandMessage("Bomb", "Start"));
        }

        public static void SendUpdateTimer(int seconds, bool beep = true)
        {
            var param = new TimerParam { timer = seconds, beep = beep ? 1 : 0 };
            SendJson(new CommandWithMessage<TimerParam>("Bomb", "UpdateTimer", param));
        }

        public static void SendRingTheBell()
        {
            SendJson(new SimpleCommandMessage("Phone", "RingTheBell"));
        }

        public static void SendTalkMessage(int messageNo)
        {
            var param = new TalkParam { no = messageNo };
            SendJson(new CommandWithMessage<TalkParam>("Phone", "TalkMessage", param));
        }

        public static void SendGameClear()
        {
            SendJson(new SimpleCommandMessage("Bomb", "GameClear"));
        }

        public static void SendGameFail()
        {
            SendJson(new SimpleCommandMessage("Bomb", "GameFail"));
        }

        public static void SendReset()
        {
            SendJson(new SimpleCommandMessage("Bomb", "Reset"));
        }

        private static void SendJson(object payload)
        {
            if (instance == null || instance.webSocket == null || instance.webSocket.State != WebSocketState.Open)
            {
                Debug.LogWarning("[BoothNetwork] 未接続のため送信できませんでした。");
                return;
            }

            string json = JsonUtility.ToJson(payload);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            instance.webSocket.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, CancellationToken.None).AsUniTask().Forget();
        }

        private void OnDestroy()
        {
            cts?.Cancel();
            cts?.Dispose();
            if (webSocket != null)
            {
                webSocket.Dispose();
                webSocket = null;
            }
        }
    }
}
