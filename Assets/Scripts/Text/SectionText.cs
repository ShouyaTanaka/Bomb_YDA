using UnityEngine;

[System.Serializable]
public class SectionText
{
    public string Name;

    [TextArea(3, 10)]
    public string Content;
}

[CreateAssetMenu(fileName = "ScenarioData", menuName = "Novel/Scenario Data")]
public class SectionData : ScriptableObject
{
    public SectionText[] texts;
}
