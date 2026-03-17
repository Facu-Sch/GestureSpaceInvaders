using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Referencias")]
    public EnemyGrid enemyGrid;
    public Player    player;

    [Header("UI")]
    public TextMeshProUGUI scoreText;
    public TextMeshProUGUI statusText;

    private int  _score;
    private bool _gameActive = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        RefreshScore();
        statusText.gameObject.SetActive(false);
    }

    private void Update()
    {
        // Reinicio por teclado — solo disponible cuando la partida terminó
        if (!_gameActive && Keyboard.current[Key.R].wasPressedThisFrame)
            RestartGame();
    }

    public bool IsGameActive() => _gameActive;

    public void AddScore(int pts)
    {
        _score += pts;
        RefreshScore();
    }

    public void OnGridCleared()          => EndGame("YOU  WIN!");
    public void OnEnemiesReachedBottom() => EndGame("GAME  OVER");

    /// <summary>
    /// Recarga la escena activa. Al volver a ejecutar Start en todos los
    /// componentes, el canvas se reposiciona frente al jugador automáticamente.
    /// Llamado desde XRGameController (SwipeDown mano izquierda) o teclado R.
    /// </summary>
    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void EndGame(string msg)
    {
        _gameActive = false;
        statusText.gameObject.SetActive(true);
        statusText.text = msg;
        if (player)    player.enabled    = false;
        if (enemyGrid) enemyGrid.enabled = false;
    }

    private void RefreshScore()
    {
        if (scoreText) scoreText.text = $"SCORE  {_score:D5}";
    }
}