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
    [Tooltip("Texto que se muestra antes de que empiece la partida")]
    public TextMeshProUGUI startPromptText;

    private int  _score;
    private bool _gameActive  = false;
    private bool _gameStarted = false;

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

        // Mostrar cartel — el juego spawneará normalmente pero Update
        // estará congelado porque IsGameActive() devuelve false
        if (startPromptText != null)
            startPromptText.gameObject.SetActive(true);
    }

    private void Update()
    {
        // Iniciar con teclado (PC)
        if (!_gameStarted && Keyboard.current[Key.Enter].wasPressedThisFrame)
            StartGame();

        // Reiniciar — solo cuando la partida terminó
        if (_gameStarted && !_gameActive && Keyboard.current[Key.R].wasPressedThisFrame)
            RestartGame();
    }

    public bool IsGameActive()  => _gameActive;
    public bool IsGameStarted() => _gameStarted;

    /// <summary>
    /// Inicia la partida. Llamado por SwipeUp mano izquierda o Enter en PC.
    /// EnemyGrid y Player ya están spawneados — solo se desbloquea el loop.
    /// </summary>
    public void StartGame()
    {
        if (_gameStarted) return;
        _gameStarted = true;
        _gameActive  = true;

        if (startPromptText != null)
            startPromptText.gameObject.SetActive(false);

        AudioManager.Instance?.PlayMusic();
    }

    public void AddScore(int pts)
    {
        _score += pts;
        RefreshScore();
    }

    public void OnGridCleared()          => EndGame("YOU  WIN!",  isWin: true);
    public void OnEnemiesReachedBottom() => EndGame("GAME  OVER", isWin: false);
    public void OnPlayerHit()            => EndGame("GAME  OVER", isWin: false);

    public void RestartGame()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void EndGame(string msg, bool isWin)
    {
        if (!_gameActive) return;

        _gameActive = false;

        if (isWin) AudioManager.Instance?.PlayWin();
        else       AudioManager.Instance?.PlayGameOver();

        statusText.gameObject.SetActive(true);
        statusText.text = msg;
    }

    private void RefreshScore()
    {
        if (scoreText) scoreText.text = $"SCORE  {_score:D5}";
    }
}