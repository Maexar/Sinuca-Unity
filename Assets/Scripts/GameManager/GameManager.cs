using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameSettings settings;
    public DifficultyData difficultyData;

    [Header("Bola Branca — Respawn")]
    [Tooltip("Arraste aqui um GameObject vazio que marca onde a bola branca reaparece após falta.")]
    public Transform cueBallSpawnPoint;

    private GameState _currentState = GameState.MainMenu;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void SetDifficulty(DifficultyData data)
    {
        difficultyData = data;
    }

    public void StartGame()
    {
        _currentState = GameState.Playing;
        SceneManager.LoadScene("Game");
    }

    public void GoToMainMenu()
    {
        _currentState = GameState.MainMenu;
        SceneManager.LoadScene("MainMenu");
    }

    public void OnBallPocketed(BallController ball)
    {
        if (ball == null || ball.IsPocketed)
            return;

        ball.Pocket();

        Debug.Log($"[GameManager] Bola encaçapada: {ball.ballNumber} ({ball.ballType})");

        if (ball.ballType == BallType.Cue)
        {
            Debug.Log("[GameManager] Falta! Iniciando respawn da bola branca em 1.5s...");
            StartCoroutine(RespawnCueBall(ball));
        }
    }

    private IEnumerator RespawnCueBall(BallController ball)
    {
        yield return new WaitForSeconds(1.5f);

        // Fallback: busca o SpawnPoint pelo nome se a referência estiver nula
        // (acontece quando o GameManager é DontDestroyOnLoad e carregou de outra cena)
        if (cueBallSpawnPoint == null)
        {
            GameObject found = GameObject.Find("CueBallSpawnPoint");
            if (found != null)
                cueBallSpawnPoint = found.transform;
        }

        if (cueBallSpawnPoint == null)
        {
            Debug.LogError("[GameManager] cueBallSpawnPoint não encontrado! " +
                           "Crie um GameObject vazio na cena chamado 'CueBallSpawnPoint' " +
                           "e arraste para o campo no Inspector do GameManager.");
            yield break;
        }

        ball.ResetBall(cueBallSpawnPoint.position);
        Debug.Log($"[GameManager] Bola branca reposicionada em {cueBallSpawnPoint.position}");
    }
}

public enum GameState
{
    MainMenu,
    Playing,
    GameOver
}