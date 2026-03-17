using UnityEngine;

/// <summary>
/// Utilidades estáticas compartidas entre sistemas del juego.
/// </summary>
public static class GameUtils
{
    /// <summary>
    /// Escala un GameObject para que su mayor dimensión (X o Y) ocupe targetSize.
    /// </summary>
    /// <param name="go">Objeto a escalar.</param>
    /// <param name="targetSize">Tamaño objetivo en unidades Unity.</param>
    /// <param name="correctPivot">
    /// Si true, corrige localPosition para centrar el mesh en el pivote.
    /// Útil cuando el pivot del modelo no coincide con su centro visual (ej: Player).
    /// </param>
    public static void ScaleToCell(GameObject go, float targetSize, bool correctPivot = false)
    {
        go.transform.localScale = Vector3.one;

        var meshFilters = go.GetComponentsInChildren<MeshFilter>();
        if (meshFilters.Length == 0) return;

        Bounds bounds = meshFilters[0].sharedMesh.bounds;
        foreach (var mf in meshFilters)
            bounds.Encapsulate(mf.sharedMesh.bounds);

        float currentSize = Mathf.Max(bounds.size.x, bounds.size.y);
        if (currentSize <= 0f) return;

        float factor = targetSize / currentSize;
        go.transform.localScale = Vector3.one * factor;

        if (correctPivot)
            go.transform.localPosition = -bounds.center * factor;
    }
}