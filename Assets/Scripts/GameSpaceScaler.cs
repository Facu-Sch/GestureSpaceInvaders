using UnityEngine;
using TMPro;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// Escala todo el juego modificando las dimensiones del canvas.
/// Como EnemyGrid y Player leen canvasRect.rect dinámicamente,
/// todo se recalcula solo al cambiar el tamaño.
/// </summary>
public class GameSpaceScaler : MonoBehaviour
{
    [Header("Canvas")]
    [SerializeField] private RectTransform canvasRect;

    [Header("Tamaño base (referencia 1x)")]
    [SerializeField] private float baseWidth  = 30f;
    [SerializeField] private float baseHeight = 50f;

    [Header("Escala")]
    [SerializeField][Range(0.1f, 2f)] private float scale = 1f;

    [Header("HUD - ScoreText")]
    [SerializeField] private RectTransform scoreTextRect;
    [SerializeField] private TextMeshProUGUI scoreText;
    [SerializeField] private float baseScoreFontSize = 4f;
    [SerializeField] private Vector2 baseScoreSize   = new Vector2(28f, 4f);

    [Header("HUD - StatusText")]
    [SerializeField] private RectTransform statusTextRect;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private float baseStatusFontSize = 6f;
    [SerializeField] private Vector2 baseStatusSize   = new Vector2(28f, 8f);

    [Header("EnemyGrid")]
    [SerializeField] private EnemyGrid enemyGrid;
    [SerializeField] private float baseRowSpacing = 2f;

    private void Start()     => Apply();
    private void OnValidate() => Apply();

    public void Apply()
    {
        if (canvasRect == null) return;

        // ── Canvas ─────────────────────────────────────────────────────────────
        canvasRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, baseWidth  * scale);
        canvasRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,   baseHeight * scale);

        // ── ScoreText ──────────────────────────────────────────────────────────
        if (scoreTextRect != null)
        {
            scoreTextRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, baseScoreSize.x * scale);
            scoreTextRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,   baseScoreSize.y * scale);
        }
        if (scoreText != null)
            scoreText.fontSize = baseScoreFontSize * scale;

        // ── StatusText ─────────────────────────────────────────────────────────
        if (statusTextRect != null)
        {
            statusTextRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, baseStatusSize.x * scale);
            statusTextRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical,   baseStatusSize.y * scale);
        }
        if (statusText != null)
            statusText.fontSize = baseStatusFontSize * scale;

        // ── EnemyGrid rowSpacing ───────────────────────────────────────────────
        if (enemyGrid != null)
            enemyGrid.rowSpacing = baseRowSpacing * scale;
    }

    // ── Gizmo ──────────────────────────────────────────────────────────────────
    private void OnDrawGizmos()
    {
        if (canvasRect == null) return;

        float w = baseWidth  * scale;
        float h = baseHeight * scale;
        Vector3 c = canvasRect.transform.position;

        Gizmos.color = new Color(0f, 1f, 1f, 0.3f);
        Gizmos.DrawWireCube(c, new Vector3(w, h, 0.01f));

#if UNITY_EDITOR
        GUIStyle style = new GUIStyle();
        style.normal.textColor = new Color(0f, 1f, 1f, 0.9f);
        style.fontSize = 10;
        Handles.Label(c + Vector3.up * (h * 0.5f + 0.3f),
            $"Canvas: {w:F1} × {h:F1}  (scale {scale:F2}x)", style);
#endif
    }
}