using Cysharp.Threading.Tasks;
using UniTLib.Debug;
using UnityEngine;

public class FadeManager : MonoBehaviour
{
    public static FadeManager Instance { get; private set; }

    [SerializeField] GameObject FadePanel;

    void Awake()
    {
        // シングルトン初期化
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public async UniTask StartProject()
    {
        CanvasGroup cg = FadePanel.GetComponent<CanvasGroup>();
        cg.alpha = 1f;
        FadePanel.SetActive(true);

        await UniTask.WaitForSeconds(1f);

        await FadeOut();
    }

    public async UniTask FadeIn()
    {
        UTLog.Log("FadeIn").Tag("Fade");

        CanvasGroup cg = FadePanel.GetComponent<CanvasGroup>();
        cg.alpha = 0f;

        FadePanel.SetActive(true);

        await cg.UniFade(1f, 1f);
    }

    public async UniTask FadeOut()
    {
        UTLog.Log("FadeOut").Tag("Fade");

        CanvasGroup cg = FadePanel.GetComponent<CanvasGroup>();
        cg.alpha = 1f;

        await cg.UniFade(0f, 1f);

        FadePanel.SetActive(false);
    }

    public async UniTask SwitchScene(string scene)
    {

    }
}


