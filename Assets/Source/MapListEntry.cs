using Mirror;
using UnityEngine;
using UnityEngine.Video;

public class MapListEntry : ScriptableObject
{
    [Scene]
    public string m_SceneFilename;
    public Sprite m_Thumbnail;
    public string m_DisplayName;
    public VideoClip m_SceneVideoClip;
    public TextAsset m_ObjectivesDesc;

    // 0 - easy, 1 - medium, 2 - hard
    [Header("Budget allocation")]
    [Tooltip("[0] - Easy, [1] - Medium, [2] - Hard")]
    public int[] m_BudgetAlloc = new int[3];


    public void OnValidate()
    {
        // check budget allocation
        for (int i = 0; i < m_BudgetAlloc.Length; i++)
        {
            if (m_BudgetAlloc[i] < 0)
            {
                m_BudgetAlloc[i] = 0;
                Debug.Log("The budget cannot be less than 0.");
            }
        }
    }
}
