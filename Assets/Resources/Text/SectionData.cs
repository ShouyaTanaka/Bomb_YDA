using UnityEngine;

[System.Serializable]
public class SectionText
{
    public string Name;

    [TextArea(3, 10)]
    public string Content;

    [Tooltip("0以下の場合は手動クリック待機。0より大きい場合は指定秒数後に自動で次へ進む")]
    public float AutoAdvanceTime = 0f;
}

[CreateAssetMenu(fileName = "ScenarioData", menuName = "Novel/Scenarios Data")]
public class SectionData : ScriptableObject
{
    public SectionText[] texts;
}
