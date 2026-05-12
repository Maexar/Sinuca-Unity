using UnityEngine;

public class PocketDetector : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Ball"))
            return;

        BallController ball = other.GetComponent<BallController>();
        if (ball == null)
            return;

        if (GameManager.Instance == null)
        {
            Debug.LogWarning("GameManager não encontrado na cena.");
            return;
        }

        GameManager.Instance.OnBallPocketed(ball);
    }
}