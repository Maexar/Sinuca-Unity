using System;
using UnityEngine;

public class TurnManager : MonoBehaviour
{
    public static TurnManager Instance { get; private set; }

    public static event Action OnTurnChanged;

    public int CurrentPlayer { get; private set; } = 0;
    public int[] Scores { get; private set; } = new int[2];

    private int _ballsPocketedThisTurn = 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void OnTurnEnd()
    {
        if (_ballsPocketedThisTurn == 0)
            CurrentPlayer = 1 - CurrentPlayer;

        _ballsPocketedThisTurn = 0;
        OnTurnChanged?.Invoke();
    }

    public void AddScore(int playerIndex, int points = 1)
    {
        Scores[playerIndex] += points;
        _ballsPocketedThisTurn++;
        OnTurnChanged?.Invoke();
    }

    public static bool AllBallsStopped()
    {
        for (int i = BallController.AllBalls.Count - 1; i >= 0; i--)
        {
            var ball = BallController.AllBalls[i];
            if (ball == null)
            {
                BallController.AllBalls.RemoveAt(i);
                continue;
            }

            if (!ball.IsPocketed && !ball.IsStopped)
            {
                // Bola caiu fora do mapa — rota pelo GameManager para acionar respawn da branca
                if (ball.transform.position.y < -5f)
                {
                    Debug.Log($"[TurnManager] Bola {ball.ballNumber} caiu fora do mapa, encaçapando via GameManager.");
                    if (GameManager.Instance != null)
                        GameManager.Instance.OnBallPocketed(ball);
                    else
                        ball.Pocket();
                    continue;
                }

                return false;
            }
        }
        return true;
    }
}
