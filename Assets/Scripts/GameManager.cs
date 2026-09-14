using Cysharp.Threading.Tasks;
using UniRx;
using UniTLib.Debug;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    private ReactiveProperty<GameState> mainState = new ReactiveProperty<GameState>(GameState.Start);

    public static GameManager Instance { get; private set; }

    public SectionData Story01_Data;
    public SectionData Act01_a_Data;
    public SectionData Act01_b_Data;
    public SectionData Act01_c_Data;
    public SectionData Story02_Data;
    public SectionData Act02_a_Data;
    public SectionData Act02_b_Data;
    public SectionData Act02_c_Data;
    public SectionData GoodEnd_Data;
    public SectionData BadEnd01_Data;
    public SectionData BadEnd02_Data;


    // Test
    public bool act1_trigger = true;
    public bool act2_trigger = true;



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
        UTLog.Log("Start state").Tag("GameManager");
        TextManager.Instance.StartProject();
        GimmickManager.Instance.StartProject();
        await FadeManager.Instance.StartProject();
        SetGameState(GameState.Story01);
    }

    private async UniTask OnStory01()
    {
        UTLog.Log("Story01 state").Tag("GameManager");
        await TextManager.Instance.ShowText(Story01_Data);
        await SwitchBackGround.Instance.SwitchBack(BackImage.Naka);
        SetGameState(GameState.Act01);
    }

    private async UniTask OnAct01()
    {
        UTLog.Log("Act01 state").Tag("GameManager");
        await TextManager.Instance.ShowText(Act01_a_Data);
        await SwitchBackGround.Instance.SwitchBack(BackImage.Hako);
        await TextManager.Instance.ShowText(Act01_a_Data);
        await SwitchBackGround.Instance.SwitchBack(BackImage.Bomb);
        await TextManager.Instance.ShowText(Act01_a_Data);
        await SwitchBackGround.Instance.SwitchBack(BackImage.Naka);
        await TextManager.Instance.ShowText(Act01_a_Data);
        UTLog.Log("Act01 ギミック 開始").Tag("Act01");

        await GimmickManager.Instance.Gimmick01Active();
        GimmickManager.Instance.GimmickHide(SectionID.Act_01);

        if (act1_trigger)
        {
            // -- 成功 -- //
            UTLog.Log("Act01 ギミック 成功分岐").Tag("Act01");
            await TextManager.Instance.ShowText(Act01_b_Data);
            SetGameState(GameState.Story02);
        }
        else
        {
            // -- 失敗 -- //
            UTLog.Log("Act01 ギミック 失敗分岐").Tag("Act01");
            await TextManager.Instance.ShowText(Act01_c_Data);
            SetGameState(GameState.BadEnd01);
        }
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

        // -- ここにギミック２の処理を入れ込む -- //
        // => 結果はフラグで返却

        HintManager.Instance.HintActive(SectionID.Act_02);

        // await UniTask.WaitUntil(() =>
        // {
        //  実機の操作を取得し、既定の操作がされたら進める
        // });

        await UniTask.WaitUntil(() => HintManager.Instance.testbool);

        HintManager.Instance.testbool = false;


        HintManager.Instance.HintHide(SectionID.Act_02);

        if (act1_trigger)
        {
            // -- 成功 -- //
            UTLog.Log("Act02 ギミック 成功分岐").Tag("Act02");
            await TextManager.Instance.ShowText(Act02_b_Data);
            SetGameState(GameState.GoodEnd);
        }
        else
        {
            // -- 失敗 -- //
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