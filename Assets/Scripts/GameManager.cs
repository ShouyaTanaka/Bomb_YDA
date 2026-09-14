using BoothNetwork;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;
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
    public SectionData Act01_a_Data;
    public SectionData Act01_b_Data;
    public SectionData Act01_c_Data;
    public SectionData Story04_Data;
    public SectionData Act02_a_Data;
    public SectionData Act02_b_Data;
    public SectionData Act02_c_Data;
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

    // 正解導線番号。ここを切ると進行する。
    private readonly HashSet<int> correctWireNumbers = new HashSet<int> { 1, 2, 3 };
    private int correctWireCutCount = 0;

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
        BoothNetworkService.OnWireCut += HandleWireCut;
        BoothNetworkService.OnBombClear += HandleBombClear;
        BoothNetworkService.OnBombMiss += HandleBombMiss;
    }

    private void OnDestroy()
    {
        BoothNetworkService.OnWireCut -= HandleWireCut;
        BoothNetworkService.OnBombClear -= HandleBombClear;
        BoothNetworkService.OnBombMiss -= HandleBombMiss;
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

    private void HandleWireCut(int no)
    {
        if (mainState.Value != GameState.Act01 && mainState.Value != GameState.Act02)
        {
            UTLog.Log($"[Wire] wire cut while inactive: {no}").Tag("GameManager");
            return;
        }

        if (!correctWireNumbers.Contains(no))
        {
            switch (mainState.Value)
            {
                case GameState.Act01:
                    UTLog.Log($"[Wire] invalid wire cut: {no}. -> BadEnd01").Tag("GameManager");
                    SetGameState(GameState.BadEnd01);
                    break;
                case GameState.Act02:
                    UTLog.Log($"[Wire] invalid wire cut: {no}. -> BadEnd02").Tag("GameManager");
                    SetGameState(GameState.BadEnd02);
                    break;
            }
            return;
        }

        correctWireCutCount++;
        UTLog.Log($"[Wire] correct wire cut: {no}, count={correctWireCutCount}").Tag("GameManager");
    }

    private void ResetWireProgress()
    {
        correctWireCutCount = 0;
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
        SetGameState(GameState.Act01);
    }

    private async UniTask OnAct01()
    {
        UTLog.Log("Act01 state").Tag("GameManager");
        UTLog.Log("Act01 ギミック 開始").Tag("Act01");
        // -- 主にイベント処理で3つの正しい配線を切るためのギミックをやる -- //
        // -- １・花をすべて咲かせて後ろから光らせた色で示唆 -- //
        // -- ２・時計を合わせさせて正しい時間が出たら時間割の色で示唆 -- //
        // -- ３・テレビを押したら中央表示にしてリズムゲームをさせる 成功したら画面を光らせる色で示唆 -- //
        await SwitchBackGround.Instance.SwitchBack(BackImage.Naka);

        GimmickObjManager.Instance.ObjActive();

        ResetWireProgress();
        await UniTask.WaitUntil(() => correctWireCutCount >= 3 && mainState.Value != GameState.BadEnd01 && mainState.Value != GameState.BadEnd02);
        correctWireCutCount = 0;

        GimmickObjManager.Instance.ObjHide();

        // await UniTask.WaitUntil(()=>net.Instance.___);

        // -- 成功 -- //
        UTLog.Log("Act01 ギミック 成功分岐").Tag("Act01");
        await TextManager.Instance.ShowText(Act01_b_Data);
        SetGameState(GameState.Story02);
    }

    private async UniTask OnBadEnd01()
    {
        UTLog.Log("BadEnd01 state").Tag("GameManager");
        await TextManager.Instance.ShowText(BadEnd01_Data);
        await GameReset();
    }

    private async UniTask OnStory02()
    {
        UTLog.Log("Story02 state").Tag("GameManager");
        await TextManager.Instance.ShowText(Story02_Data);
        SetGameState(GameState.Act02);
    }

    private async UniTask OnAct02()
    {
        UTLog.Log("Act02 state").Tag("GameManager");
        await TextManager.Instance.ShowText(Act02_a_Data);
        UTLog.Log("Act02 ギミック ").Tag("Act02");

        // 最終判定はマイコン側から送られてくる BombClear / BombMiss によって決める
        isAct2ResultReceived = false;
        act2FinalResult = false;
        await UniTask.WaitUntil(() => isAct2ResultReceived);

        if (act2FinalResult)
        {
            UTLog.Log("Act02 ギミック 成功分岐").Tag("Act02");
            await TextManager.Instance.ShowText(Act02_b_Data);
            SetGameState(GameState.GoodEnd);
        }
        else
        {
            UTLog.Log("Act02 ギミック 失敗分岐").Tag("Act02");
            await TextManager.Instance.ShowText(Act02_c_Data);
            SetGameState(GameState.BadEnd02);
        }
    }

    private async UniTask OnGoodEnd()
    {
        UTLog.Log("GoodEnd state").Tag("GameManager");
        await TextManager.Instance.ShowText(GoodEnd_Data);
        await GameReset();
    }

    private async UniTask OnBadEnd02()
    {
        UTLog.Log("BadEnd02 state").Tag("GameManager");
        await TextManager.Instance.ShowText(BadEnd02_Data);
        await GameReset();
    }

    public async UniTask GameReset()
    {
        await SwitchBackGround.Instance.SwitchBack(BackImage.Soto);
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