using BoothNetwork;
using Cysharp.Threading.Tasks;
using UniRx;
using UniTLib.Debug;
using UnityEngine;


// ゲームのながれ //

// -- 起動 -- //
// -- 導入テキストの表示 -- //
// -- 爆弾を強調表示 -- //
// -- 一回目の電話 -- //
// -- 自由操作に移行 -- //

// -- 主にイベント処理で3つの正しい配線を切るためのギミックをやる -- //
// -- １・花をすべて咲かせて後ろから光らせた色で示唆 -- //
// -- ２・時計を合わせさせて正しい時間が出たら時間割の色で示唆 -- //
// -- ３・テレビを押したら中央表示にしてリズムゲームをさせる 成功したら画面を光らせる色で示唆 -- //

// -- 正しい線を切らせる ＞ 成功=続行 失敗=バッドエンド -- //
// -- 二回目の電話 -- //
// -- 二分の一で切らせる ＞ 成功=クリア 失敗=バッドエンド -- //
// -- 終了 -- //


public class GameManager : MonoBehaviour
{
    private ReactiveProperty<GameState> mainState = new ReactiveProperty<GameState>(GameState.Start);

    public static GameManager Instance { get; private set; }

    public SectionData Story01_Data;
    public SectionData Story02_Data;
    public SectionData Story03_Data;
    public SectionData Story04_Data;
    public SectionData Story06_Data;
    public SectionData Story07_Data;
    public SectionData GoodEnd_Data;
    public SectionData BadEnd01_Data;
    public SectionData BadEnd02_Data;

    enum WireColor { Red, Blue, Green, Yellow, Purple }


    // Test
    public bool act1_trigger = true;
    public bool act2_trigger = true;

    // 最終判定はマイコンからのイベントで決める
    private bool act2FinalResult = false;
    private bool isAct2ResultReceived = false;
    private bool isPhonePickedUp = false;
    private bool isPhoneHungUp = false;

    private bool isHalfClearReceived = false;

    private bool moveTimer = false;

    void Awake()
    {
        // シングルトン初期化
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    private void Start()
    {
        // GameStateの監視
        mainState.Subscribe(state => OnGameStateChanged(state)).AddTo(this);
        BoothNetworkService.OnHalfClear += HandleHalfClear;
        BoothNetworkService.OnWireClear += HandleWireClear;
        BoothNetworkService.OnWireFail += HandleWireFail;
        BoothNetworkService.OnReset += HandleReset;
        BoothNetworkService.OnBombClear += HandleBombClear;
        BoothNetworkService.OnBombMiss += HandleBombMiss;
        BoothNetworkService.OnPickUpPhone += HandlePickUpPhone;
        BoothNetworkService.OnHangUpPhone += HandleHangUpPhone;
    }

    private void OnDestroy()
    {
        BoothNetworkService.OnHalfClear -= HandleHalfClear;
        BoothNetworkService.OnWireClear -= HandleWireClear;
        BoothNetworkService.OnWireFail -= HandleWireFail;
        BoothNetworkService.OnReset -= HandleReset;
        BoothNetworkService.OnBombClear -= HandleBombClear;
        BoothNetworkService.OnBombMiss -= HandleBombMiss;
        BoothNetworkService.OnHangUpPhone -= HandleHangUpPhone;
        BoothNetworkService.OnPickUpPhone -= HandlePickUpPhone;
    }

    private void HandlePickUpPhone()
    {
        isPhonePickedUp = true;
    }

    private void HandleHangUpPhone()
    {
        isPhoneHungUp = true;
    }

    private async UniTask WaitForPhonePickup(int messageNo)
    {
        if (moveTimer) moveTimer = false;
        isPhonePickedUp = false;
        BoothNetworkService.SendRingTheBell();
        await UniTask.WaitUntil(() => isPhonePickedUp);

        isPhoneHungUp = false;
        BoothNetworkService.SendTalkMessage(messageNo);
        await UniTask.WaitUntil(() => isPhoneHungUp);
        moveTimer = true;
    }

    private void HandleBombClear()
    {
        isAct2ResultReceived = true;
        act2FinalResult = true;
        UTLog.Log("[Bomb] Final result: Clear").Tag("GameManager");
    }

    private void HandleBombMiss()
    {
        isAct2ResultReceived = true;
        act2FinalResult = false;
        UTLog.Log("[Bomb] Final result: Miss").Tag("GameManager");
    }

    private void HandleHalfClear()
    {
        if (mainState.Value != GameState.Act01)
        {
            UTLog.Log("[Wire] half clear received while inactive").Tag("GameManager");
            return;
        }

        isHalfClearReceived = true;
        UTLog.Log("[Wire] half clear received").Tag("GameManager");
    }

    private void HandleWireFail()
    {
        UTLog.Log("[Wire] fail received -> BadEnd02").Tag("GameManager");
        SetGameState(GameState.BadEnd02);
    }

    private void HandleReset()
    {
        UTLog.Log("[Admin] reset received").Tag("GameManager");
        GameReset().Forget();
    }

    private void HandleWireClear()
    {
        if (mainState.Value != GameState.Act02)
        {
            UTLog.Log("[Wire] clear received while inactive").Tag("GameManager");
            return;
        }

        isAct2ResultReceived = true;
        act2FinalResult = true;
        UTLog.Log("[Wire] clear received -> GoodEnd").Tag("GameManager");
    }

    private async UniTask TimerUpdate()
    {
        int timer = 180;
        while (timer >= 0 && (mainState.Value == GameState.Act01 || mainState.Value == GameState.Story02 || mainState.Value == GameState.Act02))
        {
            if (!moveTimer)
            {
                await UniTask.WaitUntil(() => moveTimer ||
                    (mainState.Value != GameState.Act01 &&
                     mainState.Value != GameState.Story02 &&
                     mainState.Value != GameState.Act02));
                continue;
            }

            BoothNetworkService.SendUpdateTimer(timer);

            if (timer == 0)
            {
                SetGameState(GameState.BadEnd01);
                return;
            }

            await UniTask.Delay(1000);
            if (moveTimer)
            {
                timer--;
            }
        }
    }

    private async void OnGameStateChanged(GameState state)
    {
        switch (state)
        {
            case GameState.Start:
                await OnStart();
                break;
            case GameState.Story01:
                await OnStory01();
                break;
            case GameState.Act01:
                await OnAct01();
                break;
            case GameState.BadEnd01:
                await OnBadEnd01();
                break;
            case GameState.Story02:
                await OnStory02();
                break;
            case GameState.Act02:
                await OnAct02();
                break;
            case GameState.GoodEnd:
                await OnGoodEnd();
                break;
            case GameState.BadEnd02:
                await OnBadEnd02();
                break;
        }
    }

    private async UniTask OnStart()
    {
        // -- 起動 -- //
        UTLog.Log("Start state").Tag("GameManager");
        TextManager.Instance.StartProject();
        GimmickObjManager.Instance.StartProject();
        await FadeManager.Instance.StartProject();
        BoothNetworkService.SendReset();
        SetGameState(GameState.Story01);
    }

    private async UniTask OnStory01()
    {
        // -- 導入テキストの表示 -- //
        UTLog.Log("Story01 state").Tag("GameManager");
        await TextManager.Instance.ShowText(Story01_Data);
        await SwitchBackGround.Instance.SwitchBack(BackImage.Naka);
        await TextManager.Instance.ShowText(Story02_Data);
        // -- 爆弾を強調表示 -- //
        await SwitchBackGround.Instance.SwitchBack(BackImage.Bomb);
        await TextManager.Instance.ShowText(Story03_Data);
        // -- 一回目の電話 -- //
        await SwitchBackGround.Instance.SwitchBack(BackImage.Phone);
        await WaitForPhonePickup(1);

        await SwitchBackGround.Instance.SwitchBack(BackImage.Bomb);
        await TextManager.Instance.ShowText(Story04_Data);
        SetGameState(GameState.Act01);
    }

    private async UniTask OnAct01()
    {
        UTLog.Log("Act01 state").Tag("GameManager");
        UTLog.Log("Act01 ギミック 開始").Tag("Act01");

        isHalfClearReceived = false;
        BoothNetworkService.SendStart();
        moveTimer = true;
        TimerUpdate().Forget();

        // -- 主にイベント処理で3つの正しい配線を切るためのギミックをやる -- //
        // -- １・花をすべて咲かせて後ろから光らせた色で示唆 -- //
        // -- ２・時計を合わせさせて正しい時間が出たら時間割の色で示唆 -- //
        // -- ３・テレビを押したら中央表示にしてリズムゲームをさせる 成功したら画面を光らせる色で示唆 -- //
        await SwitchBackGround.Instance.SwitchBack(BackImage.Naka);

        GimmickObjManager.Instance.ObjActive();

        await UniTask.WaitUntil(() => isHalfClearReceived || mainState.Value != GameState.Act01);
        if (mainState.Value != GameState.Act01)
        {
            return;
        }

        GimmickObjManager.Instance.ObjHide();

        // -- 成功 -- //
        UTLog.Log("Act01 ギミック 成功分岐").Tag("Act01");
        moveTimer = false;
        await TextManager.Instance.ShowText(Story06_Data);
        SetGameState(GameState.Story02);
    }

    private async UniTask OnStory02()
    {
        UTLog.Log("Story02 state").Tag("GameManager");
        // -- 二回目の電話 -- //
        await WaitForPhonePickup(2);
        SetGameState(GameState.Act02);
    }

    private async UniTask OnAct02()
    {
        UTLog.Log("Act02 state").Tag("GameManager");
        BoothNetworkService.SendStartWire();
        await TextManager.Instance.ShowText(Story07_Data);
        UTLog.Log("Act02 ギミック ").Tag("Act02");

        // 最終判定はマイコン側から送られてくる BombClear / BombMiss によって決める
        isAct2ResultReceived = false;
        act2FinalResult = false;
        await UniTask.WaitUntil(() => isAct2ResultReceived || mainState.Value != GameState.Act02);
        if (mainState.Value != GameState.Act02)
        {
            return;
        }

        if (act2FinalResult)
        {
            UTLog.Log("Act02 ギミック 成功分岐").Tag("Act02");
            BoothNetworkService.SendGameClear();
            SetGameState(GameState.GoodEnd);
        }
        else
        {
            UTLog.Log("Act02 ギミック 失敗分岐").Tag("Act02");
            SetGameState(GameState.BadEnd02);
        }
    }

    private async UniTask OnGoodEnd()
    {
        UTLog.Log("GoodEnd state").Tag("GameManager");
        await TextManager.Instance.ShowText(GoodEnd_Data);
        await GameReset();
    }

    private async UniTask OnBadEnd01()
    {
        UTLog.Log("BadEnd01 state").Tag("GameManager");
        BoothNetworkService.SendGameFail();
        await TextManager.Instance.ShowText(BadEnd01_Data);
        await WaitForPhonePickup(3);
        await GameReset();
    }

    private async UniTask OnBadEnd02()
    {
        UTLog.Log("BadEnd02 state").Tag("GameManager");
        BoothNetworkService.SendGameFail();
        await TextManager.Instance.ShowText(BadEnd02_Data);
        await WaitForPhonePickup(4);
        await GameReset();
    }

    public async UniTask GameReset()
    {
        await SwitchBackGround.Instance.SwitchBack(BackImage.Soto);
        BoothNetworkService.SendReset();
        mainState.Value = GameState.Start;
    }

    // 外部から変更
    public void SetGameState(GameState state)
    {
        mainState.Value = state;
    }

    // 現在の取得
    public GameState GetGameState()
    {
        return mainState.Value;
    }
}

public enum GameState
{
    Start = 0,
    Story01 = 10,
    Act01 = 20,
    BadEnd01 = 25,
    Story02 = 30,
    Act02 = 40,
    GoodEnd = 50,
    BadEnd02 = 55
}