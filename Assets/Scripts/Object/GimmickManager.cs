using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using BoothNetwork;
public enum SectionID { Act_01, Act_02 }
public class GimmickManager : MonoBehaviour
{
    public static GimmickManager Instance { get; private set; }

    public BaseObject[] objects_Act01;
    public BaseObject[] objects_Act02;

    public event Action Gimmick01Cleared;

    private bool[] gimmick01States;

    void Awake()
    {
        // シングルトン初期化
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        gimmick01States = new bool[objects_Act01.Length];
        BoothNetworkService.OnToggleSwitchChanged += OnToggleSwitchChanged;
    }

    void OnDestroy()
    {
        BoothNetworkService.OnToggleSwitchChanged -= OnToggleSwitchChanged;
    }

    public void StartProject()
    {
        GimmickHide(SectionID.Act_01);
        GimmickHide(SectionID.Act_02);
    }

    public async UniTask Gimmick01Active()
    {
        if (objects_Act01 == null || objects_Act01.Length != 4)
        {
            Debug.LogError($"[GimmickManager] objects_Act01 は4個必要です。現在: {objects_Act01?.Length ?? 0}");
            return;
        }

        Array.Clear(gimmick01States, 0, gimmick01States.Length);
        GimmickHide(SectionID.Act_01);
        foreach (var item in objects_Act01) item.ShowObject();
        Debug.Log("[GimmickManager] Gimmick01 の入力待機を開始しました。");

        await UniTask.WaitUntil(() => Array.TrueForAll(gimmick01States, state => state));
        Debug.Log("[GimmickManager] Gimmick01 の4入力が完了しました。");
        Gimmick01Cleared?.Invoke();
    }

    private void OnToggleSwitchChanged(int no, bool isOn)
    {
        Debug.Log($"[GimmickManager] トグル受信 no={no}, isOn={isOn}");
        int index = no - 1;
        if (index < 0 || index >= gimmick01States.Length)
        {
            Debug.LogWarning($"[GimmickManager] 対応外のトグル番号です: {no}");
            return;
        }

        gimmick01States[index] = isOn;
        if (isOn) objects_Act01[index].ActiveObject();
        else objects_Act01[index].HideObject();
    }

    public void GimmickHide(SectionID id)
    {
        switch (id)
        {
            case SectionID.Act_01:
                foreach (var item in objects_Act01) { item.HideObject(); }
                break;
            case SectionID.Act_02:
                foreach (var item in objects_Act02) { item.HideObject(); }
                break;
        }
    }
    public void GimmickActive(SectionID id)
    {
        switch (id)
        {
            case SectionID.Act_01:
                foreach (var item in objects_Act01) { item.ActiveObject(); }
                break;
            case SectionID.Act_02:
                foreach (var item in objects_Act02) { item.ActiveObject(); }
                break;
        }
    }

}