using System;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;

public class BaseObject : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private string onTrigger = "On";
    [SerializeField] private string offTrigger = "Off";
    [SerializeField] private float offAnimationDuration;

    private bool isActive = false;
    private CancellationTokenSource hideCancellation;

    public void StartProject()
    {
        gameObject.SetActive(false);
    }

    public void ActiveObject()
    {
        CancelHideAnimation();

        isActive = true;

        gameObject.SetActive(true);
        PlayTrigger(onTrigger);
    }

    public void ShowObject()
    {
        CancelHideAnimation();

        isActive = true;
        gameObject.SetActive(true);
    }

    public void HideObject()
    {

        // すでに非表示のものはスキップ
        if (!isActive) return;

        isActive = false;

        PlayTrigger(offTrigger);
        if (offAnimationDuration > 0f)
        {
            hideCancellation = new CancellationTokenSource();
            HideAfterAnimationAsync(hideCancellation.Token).Forget();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void PlayTrigger(string triggerName)
    {
        if (animator == null || string.IsNullOrEmpty(triggerName)) return;

        animator.ResetTrigger(triggerName);
        animator.SetTrigger(triggerName);
    }

    private async UniTaskVoid HideAfterAnimationAsync(CancellationToken cancellationToken)
    {
        try
        {
            await UniTask.Delay(TimeSpan.FromSeconds(offAnimationDuration), cancellationToken: cancellationToken);
            if (this == null || !isActive) gameObject.SetActive(false);
        }
        catch (OperationCanceledException)
        {
        }
    }

    private void CancelHideAnimation()
    {
        if (hideCancellation == null) return;

        hideCancellation.Cancel();
        hideCancellation.Dispose();
        hideCancellation = null;
    }

    private void OnDestroy()
    {
        CancelHideAnimation();
    }
}
