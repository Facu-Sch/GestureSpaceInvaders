using UnityEngine;
using System;

public class Bullet : MonoBehaviour
{
    [HideInInspector] public float  speed      = 20f;
    [HideInInspector] public float  topBound   = 0f;   // world Y del borde superior del canvas

    /// <summary>
    /// Callback invocado al destruirse la bala (por borde o por impacto).
    /// Player lo usa para decrementar el contador de balas activas.
    /// </summary>
    [HideInInspector] public Action OnDestroyed;

    private void Start()
    {
        if (!TryGetComponent<BoxCollider>(out _))
        {
            var col   = gameObject.AddComponent<BoxCollider>();
            col.isTrigger = true;
            col.size      = new Vector3(0.3f, 0.6f, 0.5f);
        }

        if (!TryGetComponent<Rigidbody>(out var rb))
        {
            rb             = gameObject.AddComponent<Rigidbody>();
            rb.isKinematic = true;
            rb.useGravity  = false;
        }
    }

    private void Update()
    {
        transform.position += Vector3.up * speed * Time.deltaTime;

        if (transform.position.y >= topBound)
            SelfDestroy();
    }

    /// <summary>
    /// Punto único de destrucción: siempre notifica antes de destruir el GO.
    /// </summary>
    public void SelfDestroy()
    {
        OnDestroyed?.Invoke();
        Destroy(gameObject);
    }
}