using Cysharp.Threading.Tasks;
using UniTLib.Debug;
using UnityEngine;

public class HintManager : MonoBehaviour
{
    public static HintManager Instance { get; private set; }

    public HintObject[] Act_01;
    public HintObject[] Act_02;

    bool isButton = false;

    public bool testbool = false;

    void Awake()
    {
        // シングルトン初期化
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void StartProject()
    {
        HintHide(SectionID.Act_01);
        HintHide(SectionID.Act_02);
    }

    public async void ObjRoute(SectionID section, HintID hint, SectionData data, Transform tf)
    {
        if (isButton == true) return;
        isButton = true;

        // 選択アニメーション

        HintHide(section);

        switch (section)
        {
            case SectionID.Act_01:
                await HintSwitch_Act01(hint, data);
                break;
            case SectionID.Act_02:
                await HintSwitch_Act02(hint, data);
                break;
            default: UTLog.Error("不正なSectionIDが指定されました").Tag("HintManager"); break;
        }

        HintHide(section);
        HintActive(section);

        isButton = false;
    }

    // ヒントごとに別の処理入れたいとき用（多分いらない）
    public async UniTask HintSwitch_Act01(HintID id, SectionData data)
    {
        switch (id)
        {
            case HintID.Obj_01: // SectionDataでヒントの表示
                await TextManager.Instance.ShowText(data);
                testbool = true;
                break;
            case HintID.Obj_02:
                await TextManager.Instance.ShowText(data);
                break;
            case HintID.Obj_03: break;
            case HintID.Obj_04: break;
            case HintID.Obj_05: break;
            case HintID.Obj_06: break;
            case HintID.Obj_07: break;
            case HintID.Obj_08: break;
            case HintID.Obj_09: break;
            case HintID.Obj_10: break;
            default: UTLog.Error("不正なHintIDが指定されました").Tag("HintManager"); break;
        }
    }

    public async UniTask HintSwitch_Act02(HintID id, SectionData data)
    {
        switch (id)
        {
            case HintID.Obj_01:
                await TextManager.Instance.ShowText(data);
                testbool = true;
                break;
            case HintID.Obj_02:
                await TextManager.Instance.ShowText(data);
                break;
            case HintID.Obj_03: break;
            case HintID.Obj_04: break;
            case HintID.Obj_05: break;
            case HintID.Obj_06: break;
            case HintID.Obj_07: break;
            case HintID.Obj_08: break;
            case HintID.Obj_09: break;
            case HintID.Obj_10: break;
            default: UTLog.Error("不正なGimmickIDが指定されました").Tag("GimmickManager"); break;
        }
    }

    public void HintHide(SectionID id)
    {
        switch (id)
        {
            case SectionID.Act_01:
                foreach (var item in Act_01) { item.HideObject(); }
                break;
            case SectionID.Act_02:
                foreach (var item in Act_02) { item.HideObject(); }
                break;
        }
    }
    public void HintActive(SectionID id)
    {
        switch (id)
        {
            case SectionID.Act_01:
                foreach (var item in Act_01) { item.ActiveObject(); }
                break;
            case SectionID.Act_02:
                foreach (var item in Act_02) { item.ActiveObject(); }
                break;
        }
    }
}

public enum SectionID
{
    Act_01,
    Act_02
}
public enum HintID
{
    Obj_01,
    Obj_02,
    Obj_03,
    Obj_04,
    Obj_05,
    Obj_06,
    Obj_07,
    Obj_08,
    Obj_09,
    Obj_10
}