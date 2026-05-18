using UnityEngine;

public class PocketDetector : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        BallController ball = other.GetComponent<BallController>();
        if (ball == null || ball.ballType == BallType.Cue)
            return;

        if (GameManager.Instance != null)
            GameManager.Instance.OnBallPocketed(ball);
        else if (!ball.IsPocketed)
            ball.Pocket();
    }
}