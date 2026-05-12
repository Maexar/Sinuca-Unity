using UnityEngine;
using System.Collections.Generic;

public class BallRack : MonoBehaviour
{
    public GameObject[] ballPrefabs;
    public float ballDiameter = 0.057f;
    public Transform tableSurface;

    private readonly List<GameObject> _rackBalls = new List<GameObject>();

    private void Start()
    {
        RackBalls();
    }

    public void RackBalls()
    {
        ClearRack();

        if (ballPrefabs == null || ballPrefabs.Length == 0)
        {
            Debug.LogError("BallRack: ballPrefabs não preenchido.");
            return;
        }

        if (tableSurface == null)
        {
            Debug.LogError("BallRack: tableSurface não atribuído.");
            return;
        }

        // topo real da mesa no espaço do mundo
        Renderer surfaceRenderer = tableSurface.GetComponent<Renderer>();
        float surfaceTop = surfaceRenderer != null
            ? surfaceRenderer.bounds.max.y
            : tableSurface.position.y;

        float ballY = surfaceTop + (ballDiameter * 0.5f);

        float d = ballDiameter + 0.001f;
        float rowOffset = d * Mathf.Sqrt(3f) / 2f;

        Vector3 apex = new Vector3(
            tableSurface.position.x + 0.3f,
            ballY,
            tableSurface.position.z
        );

        int ballIndex = 0;
        int[] rowCounts = { 1, 2, 3, 4, 5 };

        for (int row = 0; row < rowCounts.Length; row++)
        {
            for (int col = 0; col <= row; col++)
            {
                Vector3 pos = apex + new Vector3(
                    row * rowOffset,
                    0f,
                    (col - row / 2f) * d
                );

                if (ballIndex < ballPrefabs.Length)
                {
                    GameObject ball = Instantiate(ballPrefabs[ballIndex], pos, Quaternion.identity);
                    _rackBalls.Add(ball);
                    ballIndex++;
                }
            }
        }
    }

    public void ClearRack()
    {
        for (int i = 0; i < _rackBalls.Count; i++)
        {
            if (_rackBalls[i] != null)
                Destroy(_rackBalls[i]);
        }

        _rackBalls.Clear();
    }
}