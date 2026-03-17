using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Maneja todo el input XR del juego via microgestos.
/// - Mano derecha : SwipeLeft/Right = mover nave, ThumbTap = disparar
/// - Mano izquierda: ThumbTap     = toggle canvas lock/unlock a la mano
///                   SwipeDown    = reiniciar partida
///
/// Al iniciar el canvas y GameSpace se posicionan frente al CenterEyeAnchor.
/// Mientras el canvas está desbloqueado, el delta de la mano mueve tanto el
/// canvas (HUD) como GameSpace (enemigos + player) juntos, y rota el canvas
/// para que siempre mire al CenterEyeAnchor.
///
/// Movimiento proporcional: la distancia mano→canvas en el momento del
/// desbloqueo se usa como factor fijo de amplificación del delta.
/// </summary>
public class XRGameController : MonoBehaviour
{
    [Header("Gesture Sources")]
    [SerializeField] private OVRMicrogestureEventSource rightHandGestureSource;
    [SerializeField] private OVRMicrogestureEventSource leftHandGestureSource;

    [Header("Referencias")]
    [SerializeField] private Player        player;
    [SerializeField] private Transform     gameSpace;      // padre de EnemyGrid y Player
    [SerializeField] private RectTransform canvasRect;     // HUD (hijo de GameSpace en Unity UI)

    [Header("Canvas - Posición inicial")]
    [Tooltip("Transform del CenterEyeAnchor del OVR Camera Rig")]
    [SerializeField] private Transform centerEyeAnchor;
    [Tooltip("Distancia frente al jugador a la que aparece el canvas al inicio")]
    [SerializeField] private float spawnDistance = 1.5f;
    [Tooltip("Offset vertical respecto a la altura de los ojos (negativo = más abajo)")]
    [SerializeField] private float spawnHeightOffset = -0.2f;

    [Header("Canvas - Mano Izquierda")]
    [SerializeField] private Transform leftHandAnchor;
    [Tooltip("Offset en espacio local de la mano izquierda al punto de anclaje")]
    [SerializeField] private Vector3 canvasHandOffset = new Vector3(0f, 0.1f, 0.1f);
    [Tooltip("¿El canvas empieza bloqueado en el espacio (true) o siguiendo la mano (false)?")]
    [SerializeField] private bool startLocked = true;

    // ── Estado interno ─────────────────────────────────────────────────────────

    private bool    _canvasLocked     = true;
    private bool    _xrActive         = false;
    private Vector3 _canvasAnchorWorld;             // para Gizmos

    private float   _refDistance;                   // distancia mano→canvas al desbloquear
    private Vector3 _prevHandPos;
    private bool    _refSet = false;

    // ── Init ───────────────────────────────────────────────────────────────────

    private void Start()
    {
        if (rightHandGestureSource != null)
        {
            rightHandGestureSource.GestureRecognizedEvent.AddListener(OnRightHandGesture);
            _xrActive = true;
        }

        if (leftHandGestureSource != null)
            leftHandGestureSource.GestureRecognizedEvent.AddListener(OnLeftHandGesture);

        PlaceInFrontOfPlayer();

        _canvasLocked = startLocked;
        if (!_canvasLocked)
            CalibrateDistance();
    }

    private void OnDestroy()
    {
        if (rightHandGestureSource != null)
            rightHandGestureSource.GestureRecognizedEvent.RemoveListener(OnRightHandGesture);
        if (leftHandGestureSource != null)
            leftHandGestureSource.GestureRecognizedEvent.RemoveListener(OnLeftHandGesture);
    }

    // ── Update ─────────────────────────────────────────────────────────────────

    private void Update()
    {
        if (!GameManager.Instance.IsGameActive()) return;
        if (!_canvasLocked) FollowLeftHand();
    }

    // ── Posición inicial ───────────────────────────────────────────────────────

    /// <summary>
    /// Posiciona GameSpace y el canvas frente al jugador, mirando al CenterEyeAnchor.
    /// Se llama en Start, cubriendo inicio y reinicio por recarga de escena.
    /// </summary>
    public void PlaceInFrontOfPlayer()
    {
        Transform eye = centerEyeAnchor != null ? centerEyeAnchor : Camera.main?.transform;
        if (eye == null) return;

        // Dirección horizontal frente al jugador (sin pitch)
        Vector3 forward = eye.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
        forward.Normalize();

        Vector3 spawnPos = eye.position
                         + forward    * spawnDistance
                         + Vector3.up * spawnHeightOffset;

        // Mover GameSpace (arrastra EnemyGrid y Player como hijos)
        if (gameSpace != null)
            gameSpace.position = spawnPos;

        // Mover y rotar el canvas (HUD) al mismo punto
        if (canvasRect != null)
        {
            canvasRect.transform.position = spawnPos;
            FaceCanvas(eye);
        }
    }

    // ── Canvas follow ──────────────────────────────────────────────────────────

    private void FollowLeftHand()
    {
        if (!_refSet) return;

        Vector3 currentHandPos = GetHandAnchorWorld();
        _canvasAnchorWorld     = currentHandPos;

        Vector3 handDelta = currentHandPos - _prevHandPos;
        _prevHandPos = currentHandPos;

        if (handDelta.sqrMagnitude < 1e-8f) return;

        // Amplificar el delta por la distancia de referencia (fija)
        Vector3 scaledDelta = handDelta * _refDistance;

        // Mover GameSpace (arrastra EnemyGrid y Player)
        if (gameSpace != null)
            gameSpace.position += scaledDelta;

        // Mover el canvas por separado y rotarlo hacia el ojo
        if (canvasRect != null)
        {
            canvasRect.transform.position += scaledDelta;

            Transform eye = centerEyeAnchor != null ? centerEyeAnchor : Camera.main?.transform;
            if (eye != null) FaceCanvas(eye);
        }
    }

    // ── Calibración ────────────────────────────────────────────────────────────

    private void CalibrateDistance()
    {
        Vector3 handPos    = GetHandAnchorWorld();
        _canvasAnchorWorld = handPos;

        Vector3 refPos   = canvasRect != null
            ? canvasRect.transform.position
            : (gameSpace != null ? gameSpace.position : transform.position);

        _refDistance = Mathf.Max(Vector3.Distance(handPos, refPos), 0.01f);
        _prevHandPos = handPos;
        _refSet      = true;
    }

    // ── Helpers ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Rota el canvas para que su frente mire al ojo (billboard horizontal).
    /// Solo rota en Y para que el canvas quede siempre vertical.
    /// </summary>
    private void FaceCanvas(Transform eye)
    {
        Vector3 lookDir = canvasRect.transform.position - eye.position;
        lookDir.y = 0f;
        if (lookDir.sqrMagnitude > 0.001f)
            canvasRect.transform.rotation = Quaternion.LookRotation(lookDir);
    }

    private Vector3 GetHandAnchorWorld()
    {
        if (leftHandAnchor != null)
            return leftHandAnchor.TransformPoint(canvasHandOffset);

        return canvasRect != null
            ? canvasRect.transform.position + canvasHandOffset
            : transform.position;
    }

    // ── Gestos mano derecha ────────────────────────────────────────────────────

    private void OnRightHandGesture(OVRHand.MicrogestureType gesture)
    {
        if (!GameManager.Instance.IsGameActive()) return;

        switch (gesture)
        {
            case OVRHand.MicrogestureType.SwipeLeft:
                player?.SetXRMovement(-1f);
                break;
            case OVRHand.MicrogestureType.SwipeRight:
                player?.SetXRMovement(1f);
                break;
            case OVRHand.MicrogestureType.ThumbTap:
                player?.XRShoot();
                break;
        }
    }

    // ── Gestos mano izquierda ──────────────────────────────────────────────────

    private void OnLeftHandGesture(OVRHand.MicrogestureType gesture)
    {
        switch (gesture)
        {
            case OVRHand.MicrogestureType.ThumbTap:
                _canvasLocked = !_canvasLocked;
                if (!_canvasLocked)
                    CalibrateDistance();
                break;

            case OVRHand.MicrogestureType.SwipeBackward:
                GameManager.Instance.RestartGame();
                break;
        }
    }

    // ── Gizmos ─────────────────────────────────────────────────────────────────

    private void OnDrawGizmos()
    {
        Vector3 anchor = Application.isPlaying
            ? _canvasAnchorWorld
            : GetHandAnchorWorld();

        float s = 0.05f;
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.9f);
        Gizmos.DrawLine(anchor + Vector3.left  * s, anchor + Vector3.right   * s);
        Gizmos.DrawLine(anchor + Vector3.down  * s, anchor + Vector3.up      * s);
        Gizmos.DrawLine(anchor + Vector3.back  * s, anchor + Vector3.forward * s);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.35f);
        Gizmos.DrawSphere(anchor, 0.02f);

        if (Application.isPlaying && canvasRect != null)
        {
            Gizmos.color = _canvasLocked
                ? new Color(1f, 0.2f, 0.2f, 0.5f)
                : new Color(0.2f, 1f, 0.2f, 0.5f);
            Gizmos.DrawLine(anchor, canvasRect.transform.position);

            if (!_canvasLocked && _refSet)
            {
                Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.25f);
                Gizmos.DrawWireSphere(anchor, _refDistance);
            }
        }
    }

    // ── Acceso público ─────────────────────────────────────────────────────────

    public bool  IsXRActive   => _xrActive;
    public bool  CanvasLocked => _canvasLocked;
    public float RefDistance  => _refDistance;
}