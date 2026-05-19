using UnityEngine;

public class TurnManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
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

