using UnityEngine;
using System;

public class Enemy : MonoBehaviour
{
    public int Points { get; private set; }
    private int _health;

    public event Action<Enemy> OnDied;

    public void Initialize(int health, int points)
    {
        _health = health;
        Points  = points;
    }

    public void TakeDamage(int amount = 1)
    {
        _health -= amount;
        if (_health <= 0)
        {
            AudioManager.Instance?.PlayEnemyDestroyed();
            OnDied?.Invoke(this);
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("PlayerBullet"))
        {
            var bullet = other.GetComponent<Bullet>();
            if (bullet != null) bullet.SelfDestroy();
            TakeDamage(1);
            return;
        }

        // Si un enemigo toca la nave del jugador, game over inmediato
        if (other.CompareTag("Player"))
            GameManager.Instance.OnPlayerHit();
    }
}