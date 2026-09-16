using Cysharp.Threading.Tasks;
using UniRx;
using UniTLib.Debug;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class TextManager : MonoBehaviour
{
    public static TextManager Instance { get; private set; }

    [SerializeField] private GameObject TextBox;
    [SerializeField] private Text nameText;
    [SerializeField] private Text mainText;

    private void Awake()
    {
        // シングルトン初期化
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void StartProject()
    {
        TextBox.SetActive(false);
    }

    public async UniTask ShowText(SectionData data)
    {
        if (data == null) { UTLog.Warning("SectionData null error!!").Tag("TextManager"); return; }

        TextBox.SetActive(true);

        foreach (var text in data.texts)
        {
            UTLog.Log(" name: " + text.Name + "  text: " + text.Content).Tag("TextManager");

            nameText.text = text.Name;
            mainText.text = text.Content;

            if (text.AutoAdvanceTime > 0f)
            {
                // 音声用：指定秒数を自動待機（ミリ秒換算）
                await UniTask.Delay((int)(text.AutoAdvanceTime * 1000));
            }
            else
            {
                // クリック待機
                await WaitClickAsync();
            }

        }

        TextBox.SetActive(false);
    }

    private async UniTask WaitClickAsync()
    {
        // 次フレームまで待機
        await UniTask.Yield(PlayerLoopTiming.Update);

        float idleTimer = 0f;
        const float idleThreshold = 5f;
        bool isGuidanceShown = false;

        while (true)
        {

            // クリック検知で待機終了
            bool isClicked = Input.GetMouseButtonDown(0) ||
                            (Input.touchCount > 0 && Input.GetTouch(0).phase == UnityEngine.TouchPhase.Began);

            if (isClicked)
            {
                break;
            }

            // 放置時間の計測
            idleTimer += Time.unscaledDeltaTime;

            if (!isGuidanceShown && idleTimer >= idleThreshold)
            {
                isGuidanceShown = true;
                GuidanceTextUI.Instance.Show("画面タッチで次に進みます。");
            }

            await UniTask.Yield(PlayerLoopTiming.Update);
        }

        // クリックされたらガイダンスを確実に非表示にする
        if (isGuidanceShown)
        {
            GuidanceTextUI.Instance.Hide();
        }

        // 1フレーム待つ
        await UniTask.Yield(PlayerLoopTiming.Update);
    }


}