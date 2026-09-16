using BoothNetwork;
using Cysharp.Threading.Tasks;
using UniTLib.Debug;
using UnityEngine;
using UnityEngine.UI;

public class GimmickObjManager : MonoBehaviour
{
    public static GimmickObjManager Instance { get; private set; }

    public GimmickObject[] Act_01;

    bool isButton = false;
    public bool testBool = false;
    private bool isCouplet = false;

    [Header("Flower")]
    [SerializeField] private GameObject[] flowers;
    [SerializeField] private GameObject flowerClearObject;
    private bool[] flowerStates;
    private bool isFlowerClear = false;
    private const int FlowerSwitchCount = 4;

    [Header("Clock")]
    public GameObject clockLong;
    public GameObject clockShote;
    [SerializeField] private RectTransform clockCenter;
    [SerializeField] private float targetHour = 3f;
    [SerializeField] private float targetMinute = 15f;
    [SerializeField] private float clockTolerance = 5f;
    private bool isClockClear = false;
    private float currentHourAngle = 0f;
    private float currentMinuteAngle = 0f;
    [Header("TV")]
    [SerializeField] private GameObject[] tvButtons;
    private readonly int[] tvPattern = { 1, 2, 3, 3, 2, 1, 1, 2, 1, 3, 1, 2, 3 };
    private int tvCurrentStep = 0;
    private bool tvSequenceActive = false;
    private bool isTVClear = false;

    void Awake()
    {
        // シングルトン初期化
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        InitializeFlowerStates();
        if (flowerClearObject != null) flowerClearObject.SetActive(false);
    }

    private void OnEnable()
    {
        BoothNetworkService.OnToggleSwitchChanged += HandleToggleSwitchChanged;
        BoothNetworkService.OnClockRotated += HandleClockRotated;
        BoothNetworkService.OnPushButtonChanged += HandlePushButtonChanged;
    }

    private void OnDisable()
    {
        BoothNetworkService.OnToggleSwitchChanged -= HandleToggleSwitchChanged;
        BoothNetworkService.OnClockRotated -= HandleClockRotated;
        BoothNetworkService.OnPushButtonChanged -= HandlePushButtonChanged;
    }

    private void InitializeFlowerStates()
    {
        int flowerCount = flowers == null ? 0 : Mathf.Min(flowers.Length, FlowerSwitchCount);
        flowerStates = new bool[flowerCount];
        isFlowerClear = false;
    }

    private void HandleToggleSwitchChanged(int no, bool isOn)
    {
        if (no < 1 || no > FlowerSwitchCount) return;
        if (flowers == null || flowers.Length == 0) return;

        int index = no - 1;
        if (index >= flowers.Length) return;

        flowerStates[index] = isOn;

        GameObject flower = flowers[index];
        if (flower == null) return;

        SetFlowerChildrenActive(flower, false);
        if (isOn) _ = ActivateFlowerChildrenAfterDelay(index, flower);

        Animator animator = flower.GetComponent<Animator>();
        if (animator == null) animator = flower.GetComponentInChildren<Animator>();
        if (animator != null)
        {
            animator.SetTrigger(isOn ? "On" : "Off");
        }

        isFlowerClear = IsAllFlowerOn();
    }

    private async UniTask ActivateFlowerChildrenAfterDelay(int flowerIndex, GameObject flower)
    {
        await UniTask.WaitForSeconds(1f);

        if (flowerStates == null || flowerIndex >= flowerStates.Length || !flowerStates[flowerIndex]) return;
        SetFlowerChildrenActive(flower, true);
    }

    private static void SetFlowerChildrenActive(GameObject flower, bool isActive)
    {
        for (int childIndex = 0; childIndex < flower.transform.childCount; childIndex++)
        {
            flower.transform.GetChild(childIndex).gameObject.SetActive(isActive);
        }
    }

    private bool IsAllFlowerOn()
    {
        if (flowerStates == null || flowerStates.Length == 0) return false;

        for (int i = 0; i < flowerStates.Length; i++)
        {
            if (!flowerStates[i]) return false;
        }

        return true;
    }

    private void HandleClockRotated(string hourValue, string minuteValue)
    {
        if (clockLong == null || clockShote == null) return;

        float hour = ParseClockValue(hourValue, targetHour);
        float minute = ParseClockValue(minuteValue, targetMinute);

        currentHourAngle = GetHourAngle(hour);
        currentMinuteAngle = GetMinuteAngle(minute);

        ApplyClockHand(clockLong.transform, currentHourAngle);
        ApplyClockHand(clockShote.transform, currentMinuteAngle);

        isClockClear = Mathf.Abs(currentHourAngle - GetHourAngle(targetHour)) <= clockTolerance
            && Mathf.Abs(currentMinuteAngle - GetMinuteAngle(targetMinute)) <= clockTolerance;
    }

    private static float ParseClockValue(string value, float fallback)
    {
        if (string.IsNullOrWhiteSpace(value)) return fallback;

        if (float.TryParse(value, out float result)) return result;
        return fallback;
    }

    private static float GetHourAngle(float hour)
    {
        float normalized = ((hour % 12) + 12f) % 12f;
        return normalized * 30f;
    }

    private static float GetMinuteAngle(float minute)
    {
        float normalized = ((minute % 60) + 60f) % 60f;
        return normalized * 6f;
    }

    private static void ApplyClockHand(Transform hand, float angle)
    {
        if (hand == null) return;
        hand.localRotation = Quaternion.Euler(0f, 0f, -angle);
    }

    private void SetupClockHand(GameObject handObject)
    {
        if (handObject == null || clockCenter == null) return;

        Transform hand = handObject.transform;
        if (hand.parent != clockCenter)
        {
            hand.SetParent(clockCenter, true);
        }
    }

    public void StartProject()
    {
        ObjHide();
        InitializeFlowerStates();
        InitializeClockState();
    }

    private void InitializeClockState()
    {
        isClockClear = false;
        currentHourAngle = GetHourAngle(targetHour);
        currentMinuteAngle = GetMinuteAngle(targetMinute);

        SetupClockHand(clockLong);
        SetupClockHand(clockShote);

        if (clockLong != null) ApplyClockHand(clockLong.transform, currentHourAngle);
        if (clockShote != null) ApplyClockHand(clockShote.transform, currentMinuteAngle);
    }

    private void HandlePushButtonChanged(int no, bool isPressed)
    {
        // 押した瞬間だけを処理し、離したときの通知は無視する
        if (!isPressed) return;
        if (!tvSequenceActive) return;

        int expectedNo = tvPattern[tvCurrentStep];
        if (no == expectedNo)
        {
            UTLog.Log($"[TV] correct button: {no}").Tag("TVGimmick");
            tvCurrentStep++;

            if (tvCurrentStep >= tvPattern.Length)
            {
                tvSequenceActive = false;
                isTVClear = true;
                return;
            }

            // 正解した瞬間に次の候補を強調表示する
            HighlightTVButton(tvPattern[tvCurrentStep]);
            return;
        }

        UTLog.Log($"[TV] wrong button: expected={expectedNo}, input={no}").Tag("TVGimmick");
        ResetTVSequence();
    }

    private void ResetTVSequence()
    {
        tvCurrentStep = 0;
        tvSequenceActive = true;
        HighlightTVButton(tvPattern[0]);
    }

    private void ResetTVButtonHighlights()
    {
        if (tvButtons == null) return;

        for (int i = 0; i < tvButtons.Length; i++)
        {
            var button = tvButtons[i];
            if (button == null) continue;

            var image = button.GetComponent<Image>();
            if (image == null) continue;

            var color = image.color;
            color.a = 0.35f;
            image.color = color;
        }
    }

    private void SetTVButtonColor(Color color)
    {
        if (tvButtons == null) return;

        for (int i = 0; i < tvButtons.Length; i++)
        {
            var button = tvButtons[i];
            if (button == null) continue;

            var image = button.GetComponent<Image>();
            if (image == null) continue;

            image.color = color;
        }
    }

    private void HighlightTVButton(int buttonIndex)
    {
        ResetTVButtonHighlights();

        if (tvButtons == null || buttonIndex < 1 || buttonIndex > tvButtons.Length) return;

        var button = tvButtons[buttonIndex - 1];
        if (button == null) return;

        var image = button.GetComponent<Image>();
        if (image == null) return;

        var color = image.color;
        color.a = 1f;
        image.color = color;
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
            case HintID.Flower: await FlowerGimmick(); testBool = true; break;
            case HintID.Clock: await ClockGimmick(); break;
            case HintID.TV: await TVGimmick(); break;
            case HintID.Schedule: await UniTask.WaitForSeconds(2); break;
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

    async UniTask FlowerGimmick()
    {
        var obj = FindAnyObjectByType<GimmickObject>();
        obj.GimmickOn();

        InitializeFlowerStates();
        isFlowerClear = false;

        //---------------------
        // 4本あるトグルスイッチ操作から取得して対応する花をOn状態にする
        // OnにするときOn、OffはOffでTriggerをオブジェクトのアニメーターに送る
        // すべてonになったらクリア
        // 二秒まつ
        //---------------------

        await UniTask.WaitUntil(() => isFlowerClear);
        await TextManager.Instance.ShowText(obj.data);

        if (flowerClearObject != null)
        {
            flowerClearObject.SetActive(true);
            await UniTask.WaitForSeconds(2f);
            flowerClearObject.SetActive(false);
        }

        obj.GimmickOff();
    }

    async UniTask ClockGimmick()
    {
        var obj = FindAnyObjectByType<GimmickObject>();
        obj.GimmickOn();
        //---------------------
        // 2本あるつまみ操作から取得して針を動かす
        // 既定の時間に針の位置が合ったらクリア
        // 二秒まつ
        //---------------------

        InitializeClockState();
        isClockClear = false;

        await UniTask.WaitUntil(() => isClockClear);
        await UniTask.WaitForSeconds(2f);
        obj.GimmickOff();

    }

    async UniTask TVGimmick()
    {
        var obj = FindAnyObjectByType<GimmickObject>();
        obj.GimmickOn();
        //---------------------
        // 3個あるボタン操作を取得してリズムゲームを行う
        // クリアしたらすすむ失敗したら最初から
        // 押す順番は赤 → 黄 → 白 → 　白 → 黄 → 赤 →　赤 → 黄 → 赤 → 白 → 赤 → 黄 → 白
        // BTN1が赤、BTN2が黄、BTN3が白
        // 押すボタンに強調表示をして押させる
        // 二秒まつ
        //---------------------

        isTVClear = false;
        tvCurrentStep = 0;
        tvSequenceActive = true;
        ResetTVButtonHighlights();
        HighlightTVButton(tvPattern[0]);

        await UniTask.WaitUntil(() => isTVClear);
        SetTVButtonColor(Color.magenta);
        await UniTask.WaitForSeconds(2f);
        SetTVButtonColor(Color.white);
        obj.GimmickOff();
    }
}

public enum HintID
{
    Flower,
    Clock,
    TV,
    Schedule,
}