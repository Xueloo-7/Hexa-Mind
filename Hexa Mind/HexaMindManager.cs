using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct Grid
{
    public GameObject _obj;
    public bool _isNull;
    public int _mindID;
}

public class HexaMindManager : MonoBehaviour
{
    public int _rows = 5;  // 网格行数
    public int _cols = 5;  // 网格列数
    public float _cellSize = 1f;  // 单元格大小

    public GameObject _gridCellPrefab;  // 网格单元预制件（白色方块）

    private Dictionary<Vector3Int, Grid> _gridCells = new Dictionary<Vector3Int, Grid>(); // 存储网格单元信息
    private Dictionary<Vector3Int, GameObject> _gridObjects = new Dictionary<Vector3Int, GameObject>(); // 储存网格默认grid object
    private int _emptyCellsCount;  // 当前空网格数量
    private List<Mind> _currentMinds = new List<Mind>(); // 当前组转的思维

    void Start()
    {
        CreateGrid();
    }

    // 创建初始网格
    void CreateGrid()
    {
        for (int row = 0; row < _rows; row++)
        {
            for (int col = 0; col < _cols; col++)
            {
                Vector3Int gridPos = new Vector3Int(col, row, 0);

                if (!_gridCells.ContainsKey(gridPos)) // 如果不包含该grid且该grid不存在，创建新grid
                {
                    if (_gridObjects.ContainsKey(gridPos)) // 该grid存在
                    {
                        // 激活该grid
                        _gridObjects[gridPos].SetActive(true);
                        _gridCells[gridPos] = new Grid
                        {
                            _obj = _gridObjects[gridPos],
                            _isNull = true,
                            _mindID = -1
                        };
                    }
                    else // 该grid不存在
                    {
                        // 生成新grid
                        Vector3 worldPos = new Vector3(col * _cellSize, row * _cellSize, 0);
                        GameObject cell = Instantiate(_gridCellPrefab, worldPos, Quaternion.identity, transform);

                        _gridCells[gridPos] = new Grid
                        {
                            _obj = null,
                            _isNull = true,
                            _mindID = -1
                        };  // 记录网格单元
                        _gridObjects[gridPos] = cell;
                    }
                }
                
            }
        }
        UpdateEmptyCount();
    }

    public void ResizeGrid()
    {
        List<Mind> minds = new List<Mind>(_currentMinds); // 创建副本
        // 先释放所有思维
        for (int i = 0;i < minds.Count; i++)
        {
            minds[i].Release();
        }
        // 重新绘制整个grid
        _gridCells.Clear();
        foreach (Transform child in transform) 
            child.gameObject.SetActive(false);
        CreateGrid();

        // 再调用所有之前存在的思维，让它们重新放置一次
        for (int i = 0; i < minds.Count; i++)
        {
            minds[i].Place();
        }
    }

    // 更新统计当前空网格数
    void UpdateEmptyCount()
    {
        _emptyCellsCount = 0;
        foreach (var grid in _gridCells.Values)
        {
            if (grid._isNull)
            {
                _emptyCellsCount++;
            }
        }
    }
    public void InsertMind(Mind mind)
    {
        if(!_currentMinds.Contains(mind))
            _currentMinds.Add(mind);
    }
    public void RemoveMind(Mind mind)
    {
        if (_currentMinds.Contains(mind))
            _currentMinds.Remove(mind);
    }
    public List<Mind> Minds()
    {
        return _currentMinds;
    }
    public int EmptyGrid()
    {
        return _emptyCellsCount;
    }

    public void PlaceBlock(Vector3Int gridPos, GameObject gridObj, int Id)
    {
        Grid grid = _gridCells[gridPos];

        grid._isNull = false;
        grid._obj = gridObj;
        grid._mindID = Id;
        _gridCells[gridPos] = grid;
        UpdateEmptyCount();
    }

    public void ReleaseBlock(Vector3Int gridPos, GameObject gridObj)
    {
        if (_gridCells.ContainsKey(gridPos))
        {
            if (gridObj != _gridCells[gridPos]._obj)
                return;


            Grid nullGrid = ClearGrid(gridPos);
            _gridCells[gridPos] = nullGrid;
            UpdateEmptyCount();
        }
    }

    Grid ClearGrid(Vector3Int gridPos)
    {
        Grid grid = _gridCells[gridPos];
        grid._isNull = true;
        grid._obj = _gridObjects[gridPos];
        grid._mindID = -1;
        return grid;
    }

    // 将世界坐标转换为网格坐标
    public Vector3Int WorldToGridPosition(Vector3 worldPos)
    {
        int gridX = Mathf.RoundToInt(worldPos.x / _cellSize);
        int gridY = Mathf.RoundToInt(worldPos.y / _cellSize);
        return new Vector3Int(gridX, gridY, 0);
    }

    // 将网格坐标转换为世界坐标
    public Vector3 GridToWorldPosition(Vector3Int gridPos)
    {
        return new Vector3(gridPos.x * _cellSize, gridPos.y * _cellSize, 0);
    }

    // 判断某个网格单元是否可用（即是否为空），允许同id思维返回true
    public bool IsCellAvailable(Vector3Int gridPos, int mindID)
    {
        if (_gridCells.ContainsKey(gridPos))
        {
            if(mindID == _gridCells[gridPos]._mindID) return true;
            else return _gridCells[gridPos]._isNull; // isNull时返回true，代表这个grid是空闲的
        }
        return false; // 脱离思维空间
    }
}
