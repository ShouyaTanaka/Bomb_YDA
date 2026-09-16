using Cysharp.Threading.Tasks;
using System;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using UnityEngine;
using UniTLib.Debug;

namespace BoothNetwork
{
    public class BoothNetworkService : MonoBehaviour
    {
        private static BoothNetworkService instance;
        public static BoothNetworkService Instance => instance;

        private ClientWebSocket webSocket;
        private CancellationTokenSource cts;

        // ==========================================
        // ���C�����i�Q�[���{�́j���󂯎���M�C�x���g
        // ==========================================
        public static event Action OnConnected;
        public static event Action OnDisconnected;

        // ���d�b
        public static event Action OnPickUpPhone;
        public static event Action OnHangUpPhone;

        // ���e�e�M�~�b�N
        public static event Action<string, string> OnClockRotated;          // hour, minutes
        public static event Action<int, bool> OnToggleSwitchChanged;        // no (1-4), isOn
        public static event Action<int, bool> OnPushButtonChanged;          // no (1-3), isPressed

        // �f�o�C�X����������
        public static event Action OnBombClear;
        public static event Action OnBombMiss;
        public static event Action OnHalfClear;
        public static event Action OnWireClear;
        public static event Action OnWireFail;
        public static event Action OnReset;

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

                UTLog.Log($"[BoothNetwork] �ڑ��J�n: {url}");
                await webSocket.ConnectAsync(new Uri(url), cts.Token);
                UTLog.Log($"�T�[�o�[�ɐڑ����܂���: {url}").Tag("BoothNetwork");

                OnConnected?.Invoke();
                ReceiveLoopAsync(cts.Token).Forget();
            }
            catch (Exception ex)
            {
                UTLog.Error($"�ڑ��G���[: {ex.Message}").Tag("BoothNetwork");
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
                        // ���C���X���b�h�Ńp�[�X & �C�x���g����
                        await UniTask.SwitchToMainThread();
                        DispatchMessage(json);
                    }
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                UTLog.Error($"[BoothNetwork] ��M���[�v��O: {ex.Message}").Tag("BoothNetwork");
            }
            finally
            {
                OnDisconnected?.Invoke();
            }
        }

        private void DispatchMessage(string json)
        {

            // ��M������JSON���R���\�[���ɏo��
            UTLog.Log($"<color=#00ffff>[BoothNetwork ��M]</color> {json}").Tag("BoothNetwork");
            //Debug.Log($"<color=#00ffff>[BoothNetwork ��M]</color> {json}");

            ReceiveHeader header = JsonUtility.FromJson<ReceiveHeader>(json);
            if (header == null) return;

            // トグルスイッチ (device="Game", command="SwitchOperation")
            if (header.command == "SwitchOperation")
            {
                var msg = JsonUtility.FromJson<SwitchOperationMessage>(json);
                if (msg?.parameter != null)
                {
                    bool isOn = msg.parameter.type == "on";
                    OnToggleSwitchChanged?.Invoke(msg.parameter.no, isOn);
                }
                return;
            }

            // ボタン操作 (device="Game", command="ButtonOperation")
            if (header.command == "ButtonOperation")
            {
                var msg = JsonUtility.FromJson<SwitchOperationMessage>(json);
                if (msg?.parameter != null)
                {
                    bool isPressed = msg.parameter.type == "press";
                    OnPushButtonChanged?.Invoke(msg.parameter.no, isPressed);
                }
                return;
            }

            // 既存仕様 (device="toggleSwitch", parameter.status)
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

            // 2. �f�o�C�X���`��: pushButton
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

            // 3. �ʏ�R�}���h�`�� (command ����)
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
                case "BombClear":
                    OnBombClear?.Invoke();
                    break;
                case "BombMiss":
                    OnBombMiss?.Invoke();
                    break;
                case "WireHalfClear":
                    OnHalfClear?.Invoke();
                    break;
                case "WireClear":
                    OnWireClear?.Invoke();
                    break;
                case "WireFail":
                    OnWireFail?.Invoke();
                    break;
                case "Reset":
                    OnReset?.Invoke();
                    break;

            }
        }

        // ==========================================
        // ���C�����i�Q�[���{�́j����Ăяo�����M���\�b�h�Q
        // ==========================================

        public static void SendStart()
        {
            SendJson(new SimpleCommandMessage("Bomb", "Start"));
        }

        public static void SendStartWire()
        {
            SendJson(new SimpleCommandMessage("Wire", "StartWire"));
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
                UTLog.Warning("���ڑ��̂��ߑ��M�ł��܂���ł����B").Tag("BoothNetwork");
                return;
            }

            string json = JsonUtility.ToJson(payload);

            // ���MJSON���R���\�[���փ��O�o��
            UTLog.Log($"<color=#ffff00>[BoothNetwork ���M]</color> {json}").Tag("BoothNetwork");
            //Debug.Log($"<color=#ffff00>[BoothNetwork ���M]</color> {json}");

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
