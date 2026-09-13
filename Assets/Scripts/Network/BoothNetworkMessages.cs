using System;
using UnityEngine;

namespace BoothNetwork
{
    // ==========================================
    // 1. 送信側メッセージ定義 (Unity -> Device)
    // ==========================================

    [Serializable]
    public class BaseCommandMessage
    {
        public string device;
        public string command;
    }

    [Serializable]
    public class CommandWithMessage<T> : BaseCommandMessage
    {
        public T parameter;

        public CommandWithMessage(string device, string command, T parameter)
        {
            this.device = device;
            this.command = command;
            this.parameter = parameter;
        }
    }

    [Serializable]
    public class SimpleCommandMessage : BaseCommandMessage
    {
        public SimpleCommandMessage(string device, string command)
        {
            this.device = device;
            this.command = command;
        }
    }

    [Serializable]
    public class TimerParam
    {
        public int timer;
        public int beep;
    }

    [Serializable]
    public class TalkParam
    {
        public int no;
    }

    // ==========================================
    // 2. 受信側メッセージ定義 (Device -> Unity)
    // ==========================================

    /// <summary>
    /// 受信したJSONのヘッダー判定用
    /// </summary>
    [Serializable]
    public class ReceiveHeader
    {
        public string device;
        public string command;
    }

    /// <summary>
    /// 時計操作パラメータ (RotateSwitchOperation)
    /// </summary>
    [Serializable]
    public class ClockParam
    {
        public string hour;
        public string minutes;
    }

    [Serializable]
    public class ClockMessage
    {
        public ClockParam parameter;
    }

    /// <summary>
    /// 配線切断パラメータ (WireCut: 1〜5)
    /// </summary>
    [Serializable]
    public class WireParam
    {
        public int no;
    }

    [Serializable]
    public class WireMessage
    {
        public WireParam parameter;
    }

    /// <summary>
    /// トグルスイッチ (相手仕様: device="toggleSwitch", status="on"|"off")
    /// </summary>
    [Serializable]
    public class ToggleSwitchParam
    {
        public int no;
        public string status;
    }

    [Serializable]
    public class ToggleSwitchMessage
    {
        public string device;
        public ToggleSwitchParam parameter;
    }

    /// <summary>
    /// プッシュボタン (相手仕様: device="pushButton", status="press"|"release")
    /// </summary>
    [Serializable]
    public class PushButtonParam
    {
        public int no;
        public string status;
    }

    [Serializable]
    public class PushButtonMessage
    {
        public string device;
        public PushButtonParam parameter;
    }
}
