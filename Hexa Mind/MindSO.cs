using UnityEngine;

[CreateAssetMenu(fileName = "NewMind", menuName = "Mind")]
public class MindSO : ScriptableObject
{
    public int energy; // 测试用
    public Color color = Color.white;  // 思维颜色
    public Vector2Int[] shape;  // 思维的形状，由多个方块组成的数组
}
