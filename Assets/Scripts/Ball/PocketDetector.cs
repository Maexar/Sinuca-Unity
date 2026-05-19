using UnityEngine;

public class PocketDetector : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        BallController ball = other.GetComponent<BallController>();
        if (ball == null)
            return;

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("[PocketDetector] GameManager.Instance é null — bola não encaçapada.");
            return;
        }

        Debug.Log($"[PocketDetector] Bola detectada na caçapa: {ball.ballNumber} ({ball.ballType})");
        GameManager.Instance.OnBallPocketed(ball);
    }
}