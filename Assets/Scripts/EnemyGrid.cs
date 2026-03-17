using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[System.Serializable]
public class EnemyTypeConfig
{
    public int enemyType = 1;
    public int min       = 0;
    public int max       = 4;
}

[System.Serializable]
public class RowConfig
{
    public string                label = "Fila";
    public List<EnemyTypeConfig> types = new();
}

public class EnemyGrid : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject enemy1Prefab;
    public GameObject enemy2Prefab;
    public GameObject enemy3Prefab;
    public GameObject enemy4Prefab;

    [Header("Canvas")]
    public RectTransform canvasRect;

    [Header("Player")]
    public Player player;

    [Header("Grilla")]
    public int columns = 11;

    [Header("Márgenes (unidades Unity)")]
    public float marginTop  = 1f;
    public float marginSide = 1f;
    public float rowSpacing = 2f;

    [Header("Configuración por fila (arriba → abajo)")]
    public List<RowConfig> rowConfigs = new();

    [Header("Velocidad")]
    public float ticksPerSecond     = 1f;
    [Range(0.05f, 1f)]
    public float stepDownPercent    = 0.25f;
    public float maxSpeedMultiplier = 4f;

    private static readonly int[] TypeHP     = { 0, 1, 1, 2, 3 };
    private static readonly int[] TypePoints = { 0, 1, 2, 3, 4 };

    public class RowData
    {
        public Transform   root;
        public List<Enemy> enemies    = new();
        public float       dir        = 1f;
        public float       minLocalX;
        public float       maxLocalX;
        public Enemy[]     cells;

        public bool  IsCellOccupied(int col) =>
            cells != null && col >= 0 && col < cells.Length && cells[col] != null;
        public Enemy GetEnemyAt(int col) =>
            cells != null && col >= 0 && col < cells.Length ? cells[col] : null;
        public void  ClearCell(int col)
        {
            if (cells != null && col >= 0 && col < cells.Length) cells[col] = null;
        }
    }

    public IReadOnlyList<RowData> Rows => _rows;

    private readonly List<RowData> _rows = new();
    private float _tickTimer;
    private float _tickInterval;
    private float _cellSize;
    private float _xBound;
    private float _gameOverY;
    private float _canvasTopW;
    private float _canvasBottomW;
    private int   _total;
    private int   _alive;

    // ── Inicialización ─────────────────────────────────────────────────────────

    private IEnumerator Start()
    {
        yield return null;
        CalculateLayout();
        SpawnGrid();
        _total        = _alive;
        _tickInterval = 1f / ticksPerSecond;
    }

    private void CalculateLayout()
    {
        Rect  rect    = canvasRect.rect;
        float canvasW = rect.width;
        float canvasH = rect.height;

        _canvasTopW    = canvasRect.transform.TransformPoint(new Vector3(0f,  canvasH * 0.5f, 0f)).y;
        _canvasBottomW = canvasRect.transform.TransformPoint(new Vector3(0f, -canvasH * 0.5f, 0f)).y;

        float usableW  = canvasW - marginSide * 2f;
        _cellSize      = usableW / columns;
        _xBound        = usableW * 0.5f - _cellSize * 0.5f;
        _gameOverY     = _canvasBottomW;

        if (player != null)
        {
            player.InitFromGrid(canvasRect, _cellSize, _xBound);

            // Posiciona el player en local space relativo a GameSpace,
            // igual que las filas, para que se mueva con la jerarquía.
            player.transform.localPosition = new Vector3(
                0f,
                _canvasBottomW - transform.position.y + _cellSize,
                0f);
        }
    }

    // ── Spawning ───────────────────────────────────────────────────────────────

    private void SpawnGrid()
    {
        GameObject[] prefabs = { null, enemy1Prefab, enemy2Prefab, enemy3Prefab, enemy4Prefab };

        float startWorldY = _canvasTopW - marginTop;

        for (int r = 0; r < rowConfigs.Count; r++)
        {
            var rowGO = new GameObject($"Row_{r}");
            rowGO.transform.SetParent(transform, false);

            float worldY = startWorldY - r * rowSpacing;
            rowGO.transform.position = new Vector3(
                canvasRect.transform.position.x,
                worldY,
                canvasRect.transform.position.z);

            var rowData = new RowData
            {
                root  = rowGO.transform,
                dir   = Random.value > 0.5f ? 1f : -1f,
                cells = new Enemy[columns]
            };

            SpawnRowEnemies(rowConfigs[r], prefabs, rowData);
            _rows.Add(rowData);
        }
    }

    private void SpawnRowEnemies(RowConfig config, GameObject[] prefabs, RowData rowData)
    {
        float startX = -(_cellSize * (columns - 1)) * 0.5f;

        var typePool = new List<int>();
        foreach (var tc in config.types)
        {
            if (tc.enemyType < 1 || tc.enemyType > 4) continue;
            if (prefabs[tc.enemyType] == null) continue;
            int count = Random.Range(tc.min, tc.max + 1);
            for (int i = 0; i < count; i++) typePool.Add(tc.enemyType);
        }

        Shuffle(typePool);

        var shuffledCols = new List<int>();
        for (int c = 0; c < columns; c++) shuffledCols.Add(c);
        Shuffle(shuffledCols);

        int toPlace = Mathf.Min(typePool.Count, columns);
        float minX = float.MaxValue, maxX = float.MinValue;

        for (int i = 0; i < toPlace; i++)
        {
            int   col    = shuffledCols[i];
            int   type   = typePool[i];
            float localX = startX + col * _cellSize;

            var go = Instantiate(prefabs[type], rowData.root);
            go.transform.localPosition = new Vector3(localX, 0f, 0f);
            go.transform.localRotation = Quaternion.identity;

            // Usa método privado ScaleToCell
            ScaleToCell(go, _cellSize * 0.75f);

            if (!go.TryGetComponent<BoxCollider>(out var boxCol))
                boxCol = go.AddComponent<BoxCollider>();
            boxCol.isTrigger = true;

            if (!go.TryGetComponent<Rigidbody>(out var rb))
                rb = go.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity  = false;

            var enemy = go.GetComponent<Enemy>() ?? go.AddComponent<Enemy>();
            enemy.Initialize(TypeHP[type], TypePoints[type]);

            int capturedCol = col;
            enemy.OnDied += e => HandleEnemyDied(e, rowData, capturedCol);

            rowData.enemies.Add(enemy);
            rowData.cells[col] = enemy;
            _alive++;

            if (localX < minX) minX = localX;
            if (localX > maxX) maxX = localX;
        }

        rowData.minLocalX = toPlace > 0 ? minX : 0f;
        rowData.maxLocalX = toPlace > 0 ? maxX : 0f;
    }

    // ── Game loop ──────────────────────────────────────────────────────────────

    private void Update()
    {
        if (!GameManager.Instance.IsGameActive()) return;
        _tickTimer += Time.deltaTime;
        if (_tickTimer < _tickInterval) return;
        _tickTimer -= _tickInterval;
        Tick();
    }

    private void Tick()
    {
        float halfCell = _cellSize * 0.5f;
        float stepDown = _cellSize * stepDownPercent;

        // ── 1. Movimiento horizontal por fila ─────────────────────────
        foreach (var row in _rows)
        {
            if (row.enemies.Count == 0) continue;

            float curX  = row.root.localPosition.x;
            float nextX = curX + row.dir * _cellSize;

            float edge = row.dir > 0
                ? nextX + row.maxLocalX + halfCell
                : nextX + row.minLocalX - halfCell;

            if (row.dir > 0 && edge > _xBound)
            {
                nextX   = _xBound - row.maxLocalX - halfCell;
                row.dir = -1f;
            }
            else if (row.dir < 0 && edge < -_xBound)
            {
                nextX   = -_xBound - row.minLocalX + halfCell;
                row.dir = 1f;
            }

            var pos = row.root.localPosition;
            pos.x = nextX;
            row.root.localPosition = pos;
        }

        // ── 2. Todas las filas bajan juntas ───────────────────────────
        foreach (var row in _rows)
        {
            row.root.position = new Vector3(
                row.root.position.x,
                row.root.position.y - stepDown,
                row.root.position.z);
        }

        // ── 3. Chequeo de game over ───────────────────────────────────
        float gameOverY = canvasRect.transform.TransformPoint(
            new Vector3(0f, -canvasRect.rect.height * 0.5f, 0f)).y;

        foreach (var row in _rows)
        {
            if (row.enemies.Count == 0) continue;
            foreach (var enemy in row.enemies)
            {
                if (enemy == null) continue;
                if (enemy.transform.position.y <= gameOverY)
                {
                    GameManager.Instance.OnEnemiesReachedBottom();
                    return;
                }
            }
        }
    }

    // ── Callbacks ──────────────────────────────────────────────────────────────

    private void HandleEnemyDied(Enemy enemy, RowData row, int col)
    {
        row.enemies.Remove(enemy);
        row.ClearCell(col);
        _alive--;
        GameManager.Instance.AddScore(enemy.Points);

        if (_alive == 0) { GameManager.Instance.OnGridCleared(); return; }

        RecalculateRowBounds(row);

        float t = 1f - (float)_alive / _total;
        _tickInterval = (1f / ticksPerSecond) / Mathf.Lerp(1f, maxSpeedMultiplier, t);
    }

    private void RecalculateRowBounds(RowData row)
    {
        float minX = float.MaxValue, maxX = float.MinValue;
        foreach (var e in row.enemies)
        {
            if (e == null) continue;
            float lx = e.transform.localPosition.x;
            if (lx < minX) minX = lx;
            if (lx > maxX) maxX = lx;
        }
        if (row.enemies.Count > 0)
        {
            row.minLocalX = minX;
            row.maxLocalX = maxX;
        }
    }

    // ── Gizmos ─────────────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        if (canvasRect == null) return;

        float canvasW   = canvasRect.rect.width;
        float canvasH   = canvasRect.rect.height;
        float usableW   = canvasW - marginSide * 2f;
        float cs        = usableW / columns;
        float startX    = -usableW * 0.5f + cs * 0.5f;
        int   numRows   = rowConfigs != null ? rowConfigs.Count : 0;

        Transform ct = canvasRect.transform;

        float topW    = ct.TransformPoint(new Vector3(0f,  canvasH * 0.5f, 0f)).y;
        float bottomW = ct.TransformPoint(new Vector3(0f, -canvasH * 0.5f, 0f)).y;
        float startY  = topW - marginTop;
        float centerX = ct.position.x;

        Gizmos.matrix = Matrix4x4.identity;

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.2f);
        for (int r = 0; r < numRows; r++)
        {
            float cy = startY - r * rowSpacing;
            for (int c = 0; c < columns; c++)
            {
                float cx = centerX + startX + c * cs;
                DrawWireRectWorld(cx, cy, cs * 0.96f, cs * 0.96f);
            }
        }

        Gizmos.color = new Color(1f, 1f, 0f, 0.4f);
        DrawWireRectWorld(centerX, (topW + bottomW) * 0.5f, usableW, canvasH);

        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.9f);
        Gizmos.DrawLine(new Vector3(centerX - canvasW * 0.5f, bottomW, ct.position.z),
                        new Vector3(centerX + canvasW * 0.5f, bottomW, ct.position.z));

        Gizmos.color = new Color(0f, 1f, 0.4f, 0.4f);
        DrawWireRectWorld(centerX, bottomW + cs, cs * 0.75f, cs * 0.75f);

        if (Application.isPlaying)
        {
            foreach (var row in _rows)
            {
                for (int c = 0; c < columns; c++)
                {
                    var enemy = row.GetEnemyAt(c);
                    if (enemy == null) continue;
                    Gizmos.color = new Color(0f, 1f, 0f, 0.35f);
                    DrawWireRectWorld(enemy.transform.position.x,
                                     enemy.transform.position.y,
                                     cs * 0.75f, cs * 0.75f);
                }
            }
        }
    }

    private void DrawWireRectWorld(float cx, float cy, float w, float h)
    {
        float hw = w * 0.5f, hh = h * 0.5f;
        float z  = canvasRect.transform.position.z;
        var tl = new Vector3(cx - hw, cy + hh, z);
        var tr = new Vector3(cx + hw, cy + hh, z);
        var br = new Vector3(cx + hw, cy - hh, z);
        var bl = new Vector3(cx - hw, cy - hh, z);
        Gizmos.DrawLine(tl, tr); Gizmos.DrawLine(tr, br);
        Gizmos.DrawLine(br, bl); Gizmos.DrawLine(bl, tl);
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    private void ScaleToCell(GameObject go, float targetSize)
    {
        go.transform.localScale = Vector3.one;
        var meshFilters = go.GetComponentsInChildren<MeshFilter>();
        if (meshFilters.Length == 0) return;

        Bounds bounds = meshFilters[0].sharedMesh.bounds;
        foreach (var mf in meshFilters) bounds.Encapsulate(mf.sharedMesh.bounds);

        float currentSize = Mathf.Max(bounds.size.x, bounds.size.y);
        if (currentSize <= 0f) return;
        go.transform.localScale = Vector3.one * (targetSize / currentSize);
    }

    private void Shuffle<T>(List<T> list)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}