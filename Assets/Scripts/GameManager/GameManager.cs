using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public GameSettings settings;
    public DifficultyData difficultyData;

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

        Debug.Log($"Bola encaçapada: {ball.ballNumber} ({ball.ballType})");

        if (ball.ballType == BallType.Cue)
        {
            Debug.Log("Falta: bola branca encaçapada.");
        }
        else if (TurnManager.Instance != null)
        {
            TurnManager.Instance.AddScore(TurnManager.Instance.CurrentPlayer);
        }
    }
}

public enum GameState
{
    MainMenu,
    Playing,
    GameOver
}