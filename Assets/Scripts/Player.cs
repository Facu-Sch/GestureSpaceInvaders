using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections.Generic;

public class Player : MonoBehaviour
{
    [Header("Referencias")]
    public RectTransform canvasRect;

    [Header("Movimiento")]
    public float moveSpeed = 8f;

    [Header("Disparo")]
    public GameObject bulletPrefab;
    public float      bulletSpeed = 20f;
    public float      fireRate    = 0.3f;
    public int        maxBullets  = 3;
    public Transform  gameArea;

    // ── Estado interno ─────────────────────────────────────────────────────────
    private float _xBound;
    private float _fireCooldown;
    private bool  _initialized;

    // Seguimiento de balas activas sin usar FindGameObjectsWithTag
    private readonly List<Bullet> _activeBullets = new();

    // Input XR: -1 izquierda, 0 quieto, 1 derecha
    private float _xrMovementDir = 0f;
    private bool  _xrInputActive = false;

    // ── Setup ──────────────────────────────────────────────────────────────────

    public void InitFromGrid(RectTransform canvas, float cellSize, float xBound)
    {
        canvasRect = canvas;
        _xBound    = xBound;

        ScaleToCell(cellSize * 0.75f);

        _initialized = true;
    }

    // ── Update ─────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (!_initialized) return;
        if (!GameManager.Instance.IsGameActive()) return;

        HandleMovement();
        _fireCooldown -= Time.deltaTime;
        HandleKeyboardShooting();
    }

    // ── Movimiento ─────────────────────────────────────────────────────────────

    private void HandleMovement()
    {
        float h = 0f;

        if (_xrInputActive)
        {
            h = _xrMovementDir;
        }
        else
        {
            if (Keyboard.current[Key.LeftArrow].isPressed)  h = -1f;
            if (Keyboard.current[Key.RightArrow].isPressed) h =  1f;
        }

        // Trabajar en localPosition: el eje X local de GameSpace es siempre
        // el lateral del juego, sin importar la rotación del GameSpace.
        var localPos = transform.localPosition;
        float newLocalX = Mathf.Clamp(
            localPos.x + h * moveSpeed * Time.deltaTime,
            -_xBound,
            _xBound);

        bool hitLeft  = newLocalX <= -_xBound && h < 0f;
        bool hitRight = newLocalX >=  _xBound && h > 0f;
        if ((hitLeft || hitRight) && _xrInputActive)
            _xrMovementDir = 0f;

        localPos.x = newLocalX;
        transform.localPosition = localPos;
    }

    // ── Disparo teclado ────────────────────────────────────────────────────────

    private void HandleKeyboardShooting()
    {
        if (_xrInputActive) return;
        if (!Keyboard.current[Key.Space].isPressed) return;
        TryShoot();
    }

    // ── API XR (llamado desde XRGameController) ────────────────────────────────

    /// <summary>
    /// Setea la dirección de movimiento continua desde microgestos.
    /// dir: -1 = izquierda, 1 = derecha, 0 = detener
    /// </summary>
    public void SetXRMovement(float dir)
    {
        _xrInputActive = true;
        _xrMovementDir = dir;
    }

    /// <summary>
    /// Disparo desde microgesto ThumbTap mano derecha.
    /// </summary>
    public void XRShoot()
    {
        _xrInputActive = true;
        TryShoot();
    }

    // ── Disparo común ──────────────────────────────────────────────────────────

    private void TryShoot()
    {
        if (_fireCooldown > 0f) return;
        if (bulletPrefab == null || gameArea == null) return;

        _activeBullets.RemoveAll(b => b == null);
        if (_activeBullets.Count >= maxBullets) return;

        _fireCooldown = fireRate;
        AudioManager.Instance?.PlayShoot();

        // Leer el borde superior del canvas dinámicamente
        float canvasH = canvasRect.rect.height;
        float topBound = canvasRect.transform.TransformPoint(
            new Vector3(0f, canvasH * 0.5f, 0f)).y;

        var go = Instantiate(bulletPrefab, gameArea);
        go.transform.position = transform.position + Vector3.up * 0.8f;
        go.transform.rotation = Quaternion.identity;
        go.tag = "PlayerBullet";

        var bullet = go.GetComponent<Bullet>() ?? go.AddComponent<Bullet>();
        bullet.speed    = bulletSpeed;
        bullet.topBound = topBound;

        _activeBullets.Add(bullet);
    }

    // ── Scaling ────────────────────────────────────────────────────────────────

    private void ScaleToCell(float targetSize)
    {
        transform.localScale = Vector3.one;
        var meshFilters = GetComponentsInChildren<MeshFilter>();
        if (meshFilters.Length == 0) return;

        Bounds bounds = meshFilters[0].sharedMesh.bounds;
        foreach (var mf in meshFilters) bounds.Encapsulate(mf.sharedMesh.bounds);

        float currentSize = Mathf.Max(bounds.size.x, bounds.size.y);
        if (currentSize <= 0f) return;

        float factor = targetSize / currentSize;
        transform.localScale    = Vector3.one * factor;
        transform.localPosition = -bounds.center * factor;
    }
}