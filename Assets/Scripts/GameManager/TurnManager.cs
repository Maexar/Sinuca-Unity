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
        foreach (var ball in BallController.AllBalls)
        {
            if (!ball.IsPocketed && !ball.IsStopped)
                return false;
        }
        return true;
    }
}

