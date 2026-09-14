using System;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using UniTLib.Debug;
using UnityEngine;

public class GimmickObjManager : MonoBehaviour
{
    public static GimmickObjManager Instance { get; private set; }

    public GimmickObject[] Act_01;

    bool isButton = false;
    public bool testBool = false;
    private bool isCouplet = false;

    void Awake()
    {
        // シングルトン初期化
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void StartProject()
    {
        ObjHide();
    }

    public async void ObjRoute(HintID hint, SectionData data, RectTransform rtf)
    {
        if (isButton == true) return;
        isButton = true;

        // 選択アニメーション

        ObjHide();

        await ObjSwitch(hint, data, rtf);

        ObjHide();
        ObjActive();

        isButton = false;
    }

    // ヒントごとに別の処理入れたいとき用（多分いらない）
    public async UniTask ObjSwitch(HintID id, SectionData data, RectTransform rtf)
    {
        if (isCouplet) isCouplet = false;
        _ = CenterUI(rtf);
        switch (id)
        {
            case HintID.Flower: await TextManager.Instance.ShowText(data); testBool = true; break;
            case HintID.Clock: await TextManager.Instance.ShowText(data); break;
            case HintID.TV: await UniTask.WaitForSeconds(2); break;
            case HintID.Schedule: break;
            default: UTLog.Error("不正なObjIDが指定されました").Tag("HintManager"); break;
        }
        isCouplet = true;
    }

    public async UniTask CenterUI(RectTransform rtf)
    {
        Vector2 defaultPosition = rtf.anchoredPosition;

        rtf.anchoredPosition = new Vector2(0, 0);

        await UniTask.WaitUntil(() => isCouplet);

        rtf.anchoredPosition = defaultPosition;
    }

    public void ObjHide()
    {
        foreach (var item in Act_01) { item.HideObject(); }
    }
    public void ObjActive()
    {
        foreach (var item in Act_01) { item.ActiveObject(); }
    }
}


public enum HintID
{
    Flower,
    Clock,
    TV,
    Schedule,
}