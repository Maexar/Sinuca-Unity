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
                // Verifica se a bola caiu no infinito (vão do mapa) - safety check
                if (ball.transform.position.y < -5f)
                {
                    ball.Pocket();
                    continue; // Se ela foi jogada pro void, conta como encaçapada/parada
                }
                
                return false;
            }
        }
        return true;
    }
}

