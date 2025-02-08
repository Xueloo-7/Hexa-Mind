using UnityEngine;

public class Mind : MonoBehaviour
{
    [SerializeField] MindSO SO;
    [SerializeField] GameObject blockPrefab;  // 方块的预制体
    [SerializeField] int id;
    private bool isDragging = false;
    private int selectBlockIndex; // 被鼠标点击的方块索引
    private Vector3 dragOffset; // 拖动时的偏移
    private Vector3Int[] releaseGridPos; // 记录松开前的grid pos，那么松开后就能用于脱离思维空间
    private HexaMindManager gridManager;
    private Mind ghost;

    void Start()
    {
        gridManager = FindFirstObjectByType<HexaMindManager>(); // 获取网格管理器
    }

    // 生成思维对象
    public void GenerateMind(MindSO mindSO, int _id)
    {
        id = _id;
        SO = mindSO;
        // 生成方块
        for (int i = 0; i < mindSO.shape.Length; i++)
        {
            Vector2Int position = mindSO.shape[i];
            GameObject block = Instantiate(blockPrefab, transform);
            block.transform.localPosition = new Vector3(position.x, -position.y, 0);
        }

        // 创建一个复制体负责跟着鼠标
        ghost = Instantiate(this);
        ghost.enabled = false;
        ghost.gameObject.SetActive(false);

        // 复制体染色和本体染色
        for (int i = 0; i < mindSO.shape.Length; i++)
        {
            SpriteRenderer renderer = ghost.transform.GetChild(i).GetComponent<SpriteRenderer>();
            renderer.color = mindSO.color;
            Color color = renderer.color;
            color.a = 0.2f;
            renderer.color = color; // ghost变半透明
            renderer.sortingOrder = 1;

            renderer = transform.GetChild(i).GetComponent<SpriteRenderer>();
            renderer.color = mindSO.color; // 设置本体颜色
            renderer.sortingOrder = 1;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && isDragging)
        {
            // 旋转角度
            ghost.transform.rotation = Quaternion.Euler(0f, 0f, ghost.transform.rotation.eulerAngles.z + 90);
            UpdateDragOffset();

        }
        if (Input.GetMouseButtonDown(0) && IsMouseOver())
        {
            StartDragging();
        }

        if (isDragging)
        {
            Drag();
            CheckGridSnap(); // 检测是否可以吸附到网格
        }

        if (Input.GetMouseButtonUp(0) && isDragging)
        {
            EndDragging();
        }
    }


    #region Drag
    // 开始拖动
    void StartDragging()
    {
        ghost.gameObject.SetActive(true);
        isDragging = true;
        UpdateDragOffset(); // 更新位置偏移
        ReleaseRecord(); // 写入松开前的网格坐标
    }

    // 记录松开前所有方块的网格坐标
    void ReleaseRecord()
    {
        releaseGridPos = new Vector3Int[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            Vector3Int gridPos = gridManager.WorldToGridPosition(transform.GetChild(i).position); // 获取方块对应的网格位置
            releaseGridPos[i] = gridPos;
        }
    }

    // 更新位置偏移，提升拖动思维的流畅性
    void UpdateDragOffset()
    {
        ghost.transform.position = MousePos();
        dragOffset = MousePos() - ghost.transform.GetChild(selectBlockIndex).position;
    }

    // 拖动思维对象
    void Drag()
    {
        Vector3 targetPosition = MousePos() + dragOffset;

        // 计算 Snap 过后的整数网格坐标
        Vector3 snappedPosition = new Vector3(Mathf.Round(targetPosition.x), Mathf.Round(targetPosition.y), 0);

        if (CheckGridSnap())
        {
            // 让 ghost 吸附到这个整数坐标
            ghost.transform.position = snappedPosition;
        }
        else
        {
            ghost.transform.position = targetPosition;
        }
    }

    // 停止拖动
    void EndDragging()
    {
        ghost.gameObject.SetActive(false);
        isDragging = false;
        transform.position = ghost.transform.position;
        transform.rotation = ghost.transform.rotation;

        // 本体对齐位置后，先尝试释放，再将方块放入

        ReleaseByRecord();

        Place();
    }
    #endregion


    #region Func
    public void Place()
    {
        if (!CheckGridSnap())
            return;
        foreach (Transform child in transform)
        {
            Vector3 worldPos = child.transform.position; // 计算方块的世界坐标
            Vector3Int gridPos = gridManager.WorldToGridPosition(worldPos); // 获取方块对应的网格位置

            gridManager.PlaceBlock(gridPos, child.gameObject, id);
        }
        gridManager.InsertMind(this);
    }
    public void Release() // 根据当前本体位置释放
    {
        foreach (Transform child in transform)
        {
            Vector3 worldPos = child.transform.position; // 计算方块的世界坐标
            Vector3Int gridPos = gridManager.WorldToGridPosition(worldPos); // 获取方块对应的网格位置

            gridManager.ReleaseBlock(gridPos, child.gameObject);
        }
        gridManager.RemoveMind(this);
    }
    void ReleaseByRecord() // 根据松开前的位置释放
    {
        for(int i = 0; i < releaseGridPos.Length; i++)
        {
            gridManager.ReleaseBlock(releaseGridPos[i], transform.GetChild(i).gameObject);
        }
        gridManager.RemoveMind(this);
        releaseGridPos = null;
    }
    // 检测是否可以吸附到网格
    bool CheckGridSnap()
    {
        bool canSnap = true;

        foreach (Transform child in ghost.transform)
        {
            Vector3 worldPos = child.transform.position; // 计算方块的世界坐标
            Vector3Int gridPos = gridManager.WorldToGridPosition(worldPos); // 获取方块对应的网格位置

            if (!gridManager.IsCellAvailable(gridPos, id)) // 如果有一格网格不为空，就返回false
            {
                canSnap = false;
                break;
            }
        }
        return canSnap;
    }
    // 判断鼠标是否在思维对象上
    bool IsMouseOver()
    {
        Vector3 mousePosition = MousePos();

        // 获取所有与鼠标位置相交的碰撞体
        Collider2D[] colliders = Physics2D.OverlapPointAll(mousePosition);

        Mind topMind = null;
        int highestSortingOrder = int.MinValue;

        foreach (Collider2D collider in colliders)
        {
            Mind mind = collider.GetComponentInParent<Mind>();
            if (mind != null)
            {
                SpriteRenderer renderer = mind.GetComponentInChildren<SpriteRenderer>(); // 获取一个子对象的 Renderer
                int sortingOrder = renderer != null ? renderer.sortingOrder : 0; // 获取 SortingOrder（默认0）

                // 选择 SortingOrder 最高的 Mind
                if (sortingOrder > highestSortingOrder)
                {
                    highestSortingOrder = sortingOrder;
                    topMind = mind;
                }
            }
        }

        // 如果找到最上层的 Mind，并且是当前对象，才允许拖动
        if (topMind == this)
        {
            // 选中 Mind 内部的具体方块
            int childIndex = 0;
            foreach (Transform block in transform)
            {
                Collider2D blockCollider = block.GetComponent<Collider2D>();
                if (blockCollider != null && blockCollider.bounds.Contains(mousePosition))
                {
                    selectBlockIndex = childIndex;
                    return true;
                }
                childIndex++;
            }
        }

        return false;
    }

    Vector3 MousePos()
    {
        Vector3 mousePosition = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mousePosition.z = 0;
        return mousePosition;
    }
    #endregion
}