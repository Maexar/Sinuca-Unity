using UnityEngine;

public class PocketDetector : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        BallController ball = other.GetComponent<BallController>();
        if (ball == null)
            return;

        if (GameManager.Instance != null)
            GameManager.Instance.OnBallPocketed(ball);
        else if (!ball.IsPocketed && ball.ballType != BallType.Cue)
            ball.Pocket();
    }
}