using Cysharp.Threading.Tasks;
using UnityEngine;

public static class CanvasGroupExtensions
{
    //=============================
    #region DoTween代用
    //=============================

    /// <summary>
    /// [Fade処理] ( float { 表示が1.非表示が0 } , float { かける秒数 } )
    /// </summary>
    public static async UniTask UniFade(this CanvasGroup cg, float a, float duration)
    {
        if (duration <= 0f)
        {
            cg.alpha = a;
            return;
        }

        float startAlpha = cg.alpha;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            cg.alpha = Mathf.Lerp(startAlpha, a, t);
            await UniTask.Yield();
        }

        cg.alpha = a;
    }

    #endregion
}