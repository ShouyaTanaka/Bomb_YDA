using Cysharp.Threading.Tasks;
using System.Threading;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GuidanceTextUI : MonoBehaviour
{
    public static GuidanceTextUI Instance { get; private set; }

    [SerializeField] private Text guidanceText;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private float defaultBlinkDuration = 0.8f; // デフォルトのフェード片道秒数

    private CancellationTokenSource blinkCts;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        if (canvasGroup == null)
        {
            canvasGroup = GetComponent<CanvasGroup>();
        }

        Hide();
    }

    /// <summary>
    /// メッセージを表示し、点滅を開始する
    /// </summary>
    /// <param name="message">表示するテキスト</param>
    /// <param name="isBlinking">点滅させるかどうか</param>
    /// <param name="duration">フェード片道にかかる秒数（省略時はデフォルト値）</param>
    public void Show(string message, bool isBlinking = true, float? duration = null)
    {
        StopBlink();

        if (guidanceText != null)
        {
            guidanceText.text = message;
        }

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = false;

        if (isBlinking)
        {
            float speed = duration.HasValue ? duration.Value : defaultBlinkDuration;
            blinkCts = new CancellationTokenSource();
            BlinkLoopAsync(speed, blinkCts.Token).Forget();
        }
    }

    /// <summary>
    /// 非表示にする
    /// </summary>
    public void Hide()
    {
        StopBlink();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
        }
        if (guidanceText != null)
        {
            guidanceText.text = ""; // 文字列も空にする
        }
    }

    /// <summary>
    /// 画面タッチ/マウスクリックを待機する
    /// </summary>
    public async UniTask WaitForTouchAsync(CancellationToken token = default)
    {
        await UniTask.WaitUntil(() =>
            Input.GetMouseButtonDown(0) ||
            (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began),
            cancellationToken: token);
    }

    private async UniTaskVoid BlinkLoopAsync(float duration, CancellationToken token)
    {
        try
        {
            while (!token.IsCancellationRequested)
            {
                // 1.0 -> 0.2 へフェード
                await canvasGroup.UniFade(0.2f, duration);
                if (token.IsCancellationRequested) break;

                // 0.2 -> 1.0 へフェード
                await canvasGroup.UniFade(1.0f, duration);
                if (token.IsCancellationRequested) break;
            }
        }
        catch (System.OperationCanceledException)
        {
            // キャンセル時
        }
        finally
        {
            // ループを抜けた際に確実にアルファを 0 にする
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
        }
    }

    private void StopBlink()
    {
        if (blinkCts != null)
        {
            blinkCts.Cancel();
            blinkCts.Dispose();
            blinkCts = null;
        }
    }

    private void OnDestroy()
    {
        StopBlink();
    }
}
