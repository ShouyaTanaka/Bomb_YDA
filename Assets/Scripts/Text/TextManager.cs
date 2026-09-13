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

            // クリック待機
            await WaitClickAsync();
        }

        TextBox.SetActive(false);
    }

    private async UniTask WaitClickAsync()
    {
        await UniTask.WaitUntil(() =>
            Mouse.current != null &&
            Mouse.current.leftButton.wasPressedThisFrame
        );
    }


}