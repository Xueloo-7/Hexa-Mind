using UnityEngine;

public class MindManager : MonoBehaviour
{
    public GameObject mindPrefab;  // 思维预制件（包含Mind脚本的父对象）
    public MindSO[] minds;  // 多个思维的ScriptableObject
    private int idRecord;

    void Start()
    {
        GenerateMinds();
    }

    // 生成多个思维
    void GenerateMinds()
    {
        foreach (var mindSO in minds)
        {
            // 实例化一个思维对象（包含多个方块）
            GameObject mindObject = Instantiate(mindPrefab, Vector3.zero, Quaternion.identity);
            Mind mind = mindObject.GetComponent<Mind>();
            mind.GenerateMind(mindSO, idRecord);
            idRecord++;
        }
    }
}
