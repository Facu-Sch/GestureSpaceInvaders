// Colocar este archivo en: Assets/Editor/EnemyGridEditor.cs
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(EnemyGrid))]
public class EnemyGridEditor : Editor
{
    private EnemyGrid _grid;
    private Vector2   _scroll;
    private int       _desiredRowCount = -1;

    // Foldouts por fila
    private readonly Dictionary<int, bool> _foldouts = new();

    private static readonly Color[] TypeColors =
    {
        Color.white,
        new Color(1.0f, 0.4f, 0.4f),   // Enemy1 — rojo
        new Color(0.4f, 0.8f, 1.0f),   // Enemy2 — azul
        new Color(0.4f, 1.0f, 0.5f),   // Enemy3 — verde
        new Color(1.0f, 0.8f, 0.3f),   // Enemy4 — amarillo
    };

    private void OnEnable()
    {
        _grid = (EnemyGrid)target;
        if (_grid.rowConfigs != null)
            _desiredRowCount = _grid.rowConfigs.Count;
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        // ── Campos estándar (todo menos rowConfigs) ────────────────────────────
        DrawPropertiesExcluding(serializedObject, "rowConfigs");

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("━━  Configuración de Filas  ━━", EditorStyles.boldLabel);
        EditorGUILayout.Space(4);

        int cols = Mathf.Max(1, _grid.columns);

        // ── Control de cantidad de filas ───────────────────────────────────────
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Cantidad de filas", GUILayout.Width(140));
        _desiredRowCount = EditorGUILayout.IntField(_desiredRowCount, GUILayout.Width(40));
        _desiredRowCount = Mathf.Max(1, _desiredRowCount);

        GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
        if (GUILayout.Button("Regenerar filas", GUILayout.Height(22)))
        {
            Undo.RecordObject(_grid, "Regenerar filas");
            RegenerateRows(_desiredRowCount, cols);
            EditorUtility.SetDirty(_grid);
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.EndVertical();

        EditorGUILayout.Space(6);

        if (_grid.rowConfigs == null) _grid.rowConfigs = new List<RowConfig>();

        // ── Lista de filas ─────────────────────────────────────────────────────
        _scroll = EditorGUILayout.BeginScrollView(_scroll,
            GUILayout.MaxHeight(Mathf.Min(600, _grid.rowConfigs.Count * 130 + 20)));

        for (int r = 0; r < _grid.rowConfigs.Count; r++)
        {
            var row = _grid.rowConfigs[r];
            if (!_foldouts.ContainsKey(r)) _foldouts[r] = true;

            // Calcula uso actual de la fila
            int totalMax = 0, totalMin = 0;
            foreach (var t in row.types) { totalMax += t.max; totalMin += t.min; }
            totalMax = Mathf.Min(totalMax, cols);
            float fill = cols > 0 ? (float)totalMax / cols : 0f;
            Color fillColor = fill >= 1f ? new Color(1f, 0.4f, 0.3f)
                            : fill > 0.7f ? new Color(1f, 0.8f, 0.2f)
                            : new Color(0.3f, 0.85f, 0.4f);

            // ── Header de la fila ──────────────────────────────────────────────
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            EditorGUILayout.BeginHorizontal();
            _foldouts[r] = EditorGUILayout.Foldout(_foldouts[r],
                $"Fila {r + 1}  —  {row.label}", true, EditorStyles.boldLabel);

            // Badge de uso
            var badgeStyle = new GUIStyle(EditorStyles.miniLabel)
                { alignment = TextAnchor.MiddleRight, fontStyle = FontStyle.Bold };
            badgeStyle.normal.textColor = fillColor;
            GUILayout.Label($"{totalMax}/{cols} celdas", badgeStyle, GUILayout.Width(80));
            EditorGUILayout.EndHorizontal();

            // Barra de uso total
            DrawFillBar(fill, fillColor, 8);

            if (_foldouts[r])
            {
                EditorGUILayout.Space(4);

                // Etiqueta
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField("Etiqueta", GUILayout.Width(60));
                row.label = EditorGUILayout.TextField(row.label);
                EditorGUILayout.EndHorizontal();

                EditorGUILayout.Space(4);

                // ── Tipos de enemigo en esta fila ──────────────────────────────
                for (int t = 0; t < row.types.Count; t++)
                {
                    var tc = row.types[t];
                    DrawEnemyTypeConfig(row, tc, t, cols);
                }

                EditorGUILayout.Space(4);

                // Botón agregar tipo (max 4 tipos distintos)
                GUI.enabled = row.types.Count < 4;
                GUI.backgroundColor = new Color(0.5f, 1f, 0.5f);
                if (GUILayout.Button("+ Agregar tipo de enemigo"))
                {
                    Undo.RecordObject(_grid, "Agregar tipo");
                    int usedMax = 0;
                    foreach (var tc in row.types) usedMax += tc.max;
                    int freeSlots = Mathf.Max(0, cols - usedMax);
                    row.types.Add(new EnemyTypeConfig
                    {
                        enemyType = 1,
                        min       = 0,
                        max       = freeSlots
                    });
                    EditorUtility.SetDirty(_grid);
                }
                GUI.backgroundColor = Color.white;
                GUI.enabled = true;
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.Space(4);
        }

        EditorGUILayout.EndScrollView();

        serializedObject.ApplyModifiedProperties();

        if (GUI.changed) EditorUtility.SetDirty(_grid);
    }

    // ── Dibuja la config de un tipo de enemigo dentro de una fila ─────────────
    private void DrawEnemyTypeConfig(RowConfig row, EnemyTypeConfig tc, int index, int cols)
    {
        // Budget disponible para este tipo (excluye lo que usan los demás)
        int otherMax = 0;
        for (int i = 0; i < row.types.Count; i++)
            if (i != index) otherMax += row.types[i].max;
        int budget = Mathf.Max(0, cols - otherMax); // máximo que puede tomar este tipo

        Color typeColor = tc.enemyType >= 1 && tc.enemyType <= 4
            ? TypeColors[tc.enemyType] : Color.white;

        // Fondo coloreado por tipo
        var prevBg = GUI.backgroundColor;
        GUI.backgroundColor = Color.Lerp(typeColor, Color.gray, 0.6f);
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        GUI.backgroundColor = prevBg;

        // Encabezado del tipo
        EditorGUILayout.BeginHorizontal();

        // Selector de tipo con color
        var labelStyle = new GUIStyle(EditorStyles.boldLabel);
        labelStyle.normal.textColor = typeColor;
        EditorGUILayout.LabelField($"Enemy {tc.enemyType}", labelStyle, GUILayout.Width(70));

        int newType = EditorGUILayout.IntSlider(tc.enemyType, 1, 4);
        if (newType != tc.enemyType)
        {
            Undo.RecordObject(_grid, "Cambiar tipo");
            tc.enemyType = newType;
        }

        // Botón eliminar
        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
        if (GUILayout.Button("✕", GUILayout.Width(24), GUILayout.Height(18)))
        {
            Undo.RecordObject(_grid, "Eliminar tipo");
            row.types.RemoveAt(index);
            EditorUtility.SetDirty(_grid);
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.EndVertical();
            return;
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        // ── Slider de Máximo ───────────────────────────────────────────────────
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Máx", GUILayout.Width(28));

        // El slider va de 0 a budget (lo que no usan los otros tipos)
        int newMax = EditorGUILayout.IntSlider(tc.max, 0, budget);
        EditorGUILayout.LabelField($"/{cols}", GUILayout.Width(28));
        EditorGUILayout.EndHorizontal();

        if (newMax != tc.max)
        {
            Undo.RecordObject(_grid, "Cambiar máximo");
            int delta = newMax - tc.max;
            tc.max = newMax;

            // Si el máx baja, arrastra el mín con él
            tc.min = Mathf.Min(tc.min, tc.max);

            // Si el máx sube, compensamos bajando otros tipos (de atrás hacia adelante)
            if (delta > 0)
                RedistributeDown(row, index, delta);

            EditorUtility.SetDirty(_grid);
        }

        // Barra de máximo
        DrawFillBar((float)tc.max / Mathf.Max(1, cols), typeColor, 6);

        // ── Slider de Mínimo ───────────────────────────────────────────────────
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Mín", GUILayout.Width(28));
        int newMin = EditorGUILayout.IntSlider(tc.min, 0, tc.max);
        EditorGUILayout.LabelField($"/{tc.max}", GUILayout.Width(28));
        EditorGUILayout.EndHorizontal();

        if (newMin != tc.min)
        {
            Undo.RecordObject(_grid, "Cambiar mínimo");
            tc.min = newMin;
            EditorUtility.SetDirty(_grid);
        }

        // Barra de mínimo (sobre la misma base)
        DrawFillBar((float)tc.min / Mathf.Max(1, cols), Color.Lerp(typeColor, Color.white, 0.4f), 4);

        EditorGUILayout.EndVertical();
        EditorGUILayout.Space(2);
    }

    // Cuando un tipo sube su max, reduce otros empezando por el último
    private void RedistributeDown(RowConfig row, int changedIdx, int toReduce)
    {
        for (int i = row.types.Count - 1; i >= 0 && toReduce > 0; i--)
        {
            if (i == changedIdx) continue;
            var other = row.types[i];
            int take = Mathf.Min(other.max, toReduce);
            other.max -= take;
            other.min  = Mathf.Min(other.min, other.max);
            toReduce  -= take;
        }
    }

    // ── Barra de progreso visual ───────────────────────────────────────────────
    private void DrawFillBar(float fill, Color color, float height)
    {
        Rect rect = EditorGUILayout.GetControlRect(false, height);
        rect.x     += 2; rect.width -= 4;
        EditorGUI.DrawRect(rect, new Color(0.15f, 0.15f, 0.15f, 0.8f));
        if (fill > 0f)
        {
            var filled = new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fill), rect.height);
            EditorGUI.DrawRect(filled, color);
        }
    }

    // ── Regenera la lista de filas ─────────────────────────────────────────────
    private void RegenerateRows(int count, int cols)
    {
        if (_grid.rowConfigs == null) _grid.rowConfigs = new List<RowConfig>();

        // Tipos default de arriba a abajo: 4,3,3,2,1 (igual que el juego original)
        int[] defaultTypes = { 4, 3, 3, 2, 1 };

        // Agrega filas faltantes
        while (_grid.rowConfigs.Count < count)
        {
            int r    = _grid.rowConfigs.Count;
            int type = r < defaultTypes.Length ? defaultTypes[r] : 1;
            _grid.rowConfigs.Add(new RowConfig
            {
                label = $"Fila {r + 1}",
                types = new List<EnemyTypeConfig>
                {
                    new EnemyTypeConfig
                    {
                        enemyType = type,
                        min       = Mathf.FloorToInt(cols * 0.3f),
                        max       = cols
                    }
                }
            });
        }

        // Remueve filas sobrantes
        while (_grid.rowConfigs.Count > count)
            _grid.rowConfigs.RemoveAt(_grid.rowConfigs.Count - 1);

        _desiredRowCount = count;
    }
}