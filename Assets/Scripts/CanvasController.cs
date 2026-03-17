using UnityEngine;
using UnityEngine.InputSystem;

public class CanvasController : MonoBehaviour
{
    [Header("Tecla para bloquear/desbloquear")]
    public Key toggleKey = Key.L;

    [Header("Velocidad de seguimiento del mouse (desbloqueado)")]
    public float followSpeed = 8f;

    [Header("Referencia")]
    public Camera mainCamera;

    private bool _locked = true;
    private float _depthFromCamera;

    private void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        _depthFromCamera = Vector3.Distance(mainCamera.transform.position, transform.position);
    }

    private void Update()
    {
        if (Keyboard.current[toggleKey].wasPressedThisFrame)
            _locked = !_locked;

        if (!_locked)
            FollowMouse();
    }

    private void FollowMouse()
    {
        var mousePos = Mouse.current.position.ReadValue();
        var screenPos = new Vector3(mousePos.x, mousePos.y, _depthFromCamera);
        var worldPos = mainCamera.ScreenToWorldPoint(screenPos);

        var target = new Vector3(worldPos.x, worldPos.y, transform.position.z);
        transform.position = Vector3.Lerp(transform.position, target, followSpeed * Time.deltaTime);
    }
}