using UnityEngine;

/// <summary>
/// Singleton que centraliza todo el audio del juego.
/// Cada efecto tiene su propio AudioSource para permitir
/// control de volumen independiente y reproducción simultánea.
///
/// Uso desde cualquier script:
///     AudioManager.Instance.PlayShoot();
///     AudioManager.Instance.PlayEnemyDestroyed();
///     etc.
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [System.Serializable]
    public class SoundConfig
    {
        public AudioClip clip;
        [Range(0f, 1f)] public float volume = 1f;
    }

    [Header("Música")]
    [SerializeField] private SoundConfig ambientMusic;

    [Header("Efectos")]
    [SerializeField] private SoundConfig shoot;
    [SerializeField] private SoundConfig enemyDestroyed;
    [SerializeField] private SoundConfig enemyMoveLateral;
    [SerializeField] private SoundConfig enemyMoveVertical;
    [SerializeField] private SoundConfig gameOver;
    [SerializeField] private SoundConfig win;

    // AudioSources individuales — uno por sonido para permitir
    // reproducción simultánea y control de volumen independiente
    private AudioSource _musicSource;
    private AudioSource _shootSource;
    private AudioSource _enemyDestroyedSource;
    private AudioSource _enemyMoveLateralSource;
    private AudioSource _enemyMoveVerticalSource;
    private AudioSource _gameOverSource;
    private AudioSource _winSource;

    // ── Init ───────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        BuildSources();
    }

    private void BuildSources()
    {
        _musicSource             = BuildSource(ambientMusic,      loop: true);
        _shootSource             = BuildSource(shoot,             loop: false);
        _enemyDestroyedSource    = BuildSource(enemyDestroyed,    loop: false);
        _enemyMoveLateralSource  = BuildSource(enemyMoveLateral,  loop: false);
        _enemyMoveVerticalSource = BuildSource(enemyMoveVertical, loop: false);
        _gameOverSource          = BuildSource(gameOver,          loop: false);
        _winSource               = BuildSource(win,               loop: false);
    }

    private AudioSource BuildSource(SoundConfig config, bool loop)
    {
        var src        = gameObject.AddComponent<AudioSource>();
        src.clip       = config?.clip;
        src.volume     = config?.volume ?? 1f;
        src.loop       = loop;
        src.playOnAwake= false;
        src.spatialBlend = 0f; // 2D — audio de UI/juego, no posicional
        return src;
    }

    private void Start()
    {
        PlayMusic();
    }

    // ── API pública ────────────────────────────────────────────────────────────

    public void PlayShoot()
    {
        PlayOneShot(_shootSource, shoot);
    }

    public void PlayEnemyDestroyed()
    {
        PlayOneShot(_enemyDestroyedSource, enemyDestroyed);
    }

    public void PlayEnemyMoveLateral()
    {
        PlayOneShot(_enemyMoveLateralSource, enemyMoveLateral);
    }

    public void PlayEnemyMoveVertical()
    {
        PlayOneShot(_enemyMoveVerticalSource, enemyMoveVertical);
    }

    public void PlayGameOver()
    {
        StopMusic();
        PlayOneShot(_gameOverSource, gameOver);
    }

    public void PlayWin()
    {
        StopMusic();
        PlayOneShot(_winSource, win);
    }

    public void PlayMusic()
    {
        if (_musicSource == null || _musicSource.clip == null) return;
        _musicSource.volume = ambientMusic?.volume ?? 1f;
        _musicSource.Play();
    }

    public void StopMusic()
    {
        _musicSource?.Stop();
    }

    // ── Volumen en runtime ─────────────────────────────────────────────────────

    public void SetMusicVolume(float volume)
    {
        if (ambientMusic != null) ambientMusic.volume = volume;
        if (_musicSource  != null) _musicSource.volume  = volume;
    }

    public void SetShootVolume(float volume)
    {
        if (shoot        != null) shoot.volume        = volume;
        if (_shootSource != null) _shootSource.volume = volume;
    }

    public void SetEnemyDestroyedVolume(float volume)
    {
        if (enemyDestroyed        != null) enemyDestroyed.volume        = volume;
        if (_enemyDestroyedSource != null) _enemyDestroyedSource.volume = volume;
    }

    public void SetEnemyMoveLateralVolume(float volume)
    {
        if (enemyMoveLateral        != null) enemyMoveLateral.volume        = volume;
        if (_enemyMoveLateralSource != null) _enemyMoveLateralSource.volume = volume;
    }

    public void SetEnemyMoveVerticalVolume(float volume)
    {
        if (enemyMoveVertical        != null) enemyMoveVertical.volume        = volume;
        if (_enemyMoveVerticalSource != null) _enemyMoveVerticalSource.volume = volume;
    }

    public void SetGameOverVolume(float volume)
    {
        if (gameOver        != null) gameOver.volume        = volume;
        if (_gameOverSource != null) _gameOverSource.volume = volume;
    }

    public void SetWinVolume(float volume)
    {
        if (win        != null) win.volume        = volume;
        if (_winSource != null) _winSource.volume = volume;
    }

    // ── Helper ─────────────────────────────────────────────────────────────────

    private void PlayOneShot(AudioSource src, SoundConfig config)
    {
        if (src == null || config?.clip == null) return;
        src.volume = config.volume;
        src.Play();
    }
}