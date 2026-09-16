using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

public class SwitchBackGround : MonoBehaviour
{
    public static SwitchBackGround Instance { get; private set; }

    [SerializeField] private Sprite[] backs;
    public Image image;

    private void Awake()
    {
        // シングルトン初期化
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public void StartProject()
    {
        image = GetComponent<Image>();
    }

    public async UniTask SwitchBack(BackImage num)
    {
        await FadeManager.Instance.FadeIn();
        image.sprite = backs[(int)num];
        await FadeManager.Instance.FadeOut();
    }
}

public enum BackImage
{
    Soto = 0,
    Naka = 1,
    Phone = 2,
    Bomb = 3,
    Title = 4
}