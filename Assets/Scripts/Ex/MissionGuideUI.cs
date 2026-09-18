using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class MissionGuideUI : MonoBehaviour
{

    public static MissionGuideUI Instance { get; private set; }

    [SerializeField] private Text guideText;
    [SerializeField] private float fadeDuration = 0.3f;

    private CanvasGroup canvasGroup;
    private RectTransform rectTransform;
    private Vector2 defaultAnchoredPosition;

    public const string TextAct01 = "部屋の手がかりを調べ、切断するコードの色を特定せよ";
    public const string TextAct02 = "運命の2択。正解の配線を見極めて切断せよ";

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        canvasGroup = GetComponent<CanvasGroup>();
        rectTransform = GetComponent<RectTransform>();

        if (guideText == null) guideText = GetComponentInChildren<Text>();

        if (rectTransform != null)
        {
            defaultAnchoredPosition = rectTransform.anchoredPosition;
        }

        canvasGroup.alpha = 0f;
        canvasGroup.blocksRaycasts = false;
    }

    /// <summary>
    /// メッセージを表示（位置指定がない場合は初期位置）
    /// </summary>
    public void Show(string message, Vector2? anchoredPosition = null)
    {
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = anchoredPosition ?? defaultAnchoredPosition;
        }

        if (guideText != null)
        {
            guideText.text = message;
        }

        canvasGroup.UniFade(1f, fadeDuration).Forget();
    }

    public void Show(Vector2? anchoredPosition = null)
    {
        Show(TextAct01, anchoredPosition);
    }

    /// <summary>
    /// ガイドを非表示にする
    /// </summary>
    public void Hide()
    {
        canvasGroup.UniFade(0f, fadeDuration).Forget();
    }

}
