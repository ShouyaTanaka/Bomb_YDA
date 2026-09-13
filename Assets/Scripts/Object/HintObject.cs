using UnityEngine;
using UnityEngine.UI;

public class HintObject : MonoBehaviour
{
    [SerializeField] SectionID section;
    [SerializeField] HintID gimmick;
    [SerializeField] SectionData data;

    public bool isClick = false;
    bool isActive = false;

    public void Start()
    {
        Button button = GetComponent<Button>();
        HintManager hm = HintManager.Instance;
        // 押されたオブジェクトはisClick = true
        button.onClick.AddListener(async () => { isClick = true; hm.ObjRoute(section, gimmick, data, transform); });
    }

    public void StartProject()
    {
        gameObject.SetActive(false);
    }

    public void ActiveObject()
    {
        isActive = true;

        gameObject.SetActive(true);

        // クリック待機でWaitUntil?でアニメーション
    }
    public void HideObject()
    {
        // 押されたものを残す
        if (isClick) { isClick = false; return; }

        // すでに非表示のものはスキップ
        if (!isActive) return;

        // 非表示処理
        gameObject.SetActive(false);

        isActive = false;
    }
}