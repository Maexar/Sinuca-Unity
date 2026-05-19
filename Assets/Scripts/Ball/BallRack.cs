using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Instancia as 15 bolas numeradas em formação triangular (ordem oficial 8-ball)
/// e a bola branca na posição de saque.
///
/// Como usar:
///   • ballPrefabs[0]  = prefab da bola 1
///   • ballPrefabs[1]  = prefab da bola 2
///   • ...
///   • ballPrefabs[14] = prefab da bola 15
///   • cueBallPrefab   = prefab da bola branca
///
/// Cada prefab deve ter BallController com ballNumber e ballType já configurados.
/// ApplyVisuals() é chamado após o Instantiate para aplicar cor/textura procedural
/// caso autoApplyVisuals esteja ativo no BallController do prefab.
/// </summary>
public class BallRack : MonoBehaviour
{
    [Header("Prefabs (índice 0 = bola 1, índice 14 = bola 15)")]
    public GameObject[] ballPrefabs;   // tamanho esperado: 15
    public GameObject   cueBallPrefab; // bola branca

    [Header("Geometria")]
    public float     ballDiameter = 0.057f;
    public Transform tableSurface;

    [Header("Posição da bola branca")]
    [Tooltip("Offset em X a partir do centro da mesa. Negativo = lado do jogador.")]
    public float cueBallOffsetX = -0.7f;

    // ── Ordem oficial do 8-ball no triângulo ──────────────────────────────────
    // Posição 0 = apex; posição 14 = último da base.
    // Regra obrigatória: bola 8 no centro (linha 2, col 1).
    private static readonly int[] BallOrder =
    {
         1,               // linha 0 — apex
         2,  9,           // linha 1
         3,  8, 10,       // linha 2  (8 no centro)
         4, 14, 11,  5,   // linha 3
         6, 13, 12, 15, 7 // linha 4 — base
    };

    private readonly List<GameObject> _rackBalls = new List<GameObject>();
    private          GameObject        _cueBallInst;

    // ─────────────────────────────────────────────────────────────────────────

    private void Start() => RackBalls();

    public void RackBalls()
    {
        ClearRack();

        if (!ValidateSetup()) return;

        // Y do topo real da mesa
        Renderer sr       = tableSurface.GetComponent<Renderer>();
        float    surfaceY = sr != null ? sr.bounds.max.y : tableSurface.position.y;
        float    ballY    = surfaceY + ballDiameter * 0.5f;

        float d         = ballDiameter + 0.001f;
        float rowOffset = d * Mathf.Sqrt(3f) / 2f;

        Vector3 apex = new Vector3(
            tableSurface.position.x + 0.3f,
            ballY,
            tableSurface.position.z
        );

        // ── Triângulo ─────────────────────────────────────────────────────────
        int   slot      = 0;
        int[] rowCounts = { 1, 2, 3, 4, 5 };

        for (int row = 0; row < rowCounts.Length; row++)
        {
            for (int col = 0; col < rowCounts[row]; col++)
            {
                int ballNumber = BallOrder[slot++];
                int prefabIdx  = ballNumber - 1; // índice 0-based no array

                if (prefabIdx < 0 || prefabIdx >= ballPrefabs.Length || ballPrefabs[prefabIdx] == null)
                {
                    Debug.LogWarning($"BallRack: prefab da bola {ballNumber} não encontrado (índice {prefabIdx}).");
                    continue;
                }

                Vector3 pos = apex + new Vector3(
                    row  * rowOffset,
                    0f,
                    (col - row / 2f) * d
                );

                GameObject go = Instantiate(ballPrefabs[prefabIdx], pos, Quaternion.identity);
                go.name = "Ball_" + ballNumber;
                _rackBalls.Add(go);

                // Aplica cor/textura procedural se autoApplyVisuals estiver ativo
                BallController bc = go.GetComponent<BallController>();
                if (bc != null)
                    bc.ApplyVisuals();
            }
        }

        // ── Bola branca ───────────────────────────────────────────────────────
        if (cueBallPrefab != null)
        {
            Vector3 cuePos = new Vector3(
                tableSurface.position.x + cueBallOffsetX,
                ballY,
                tableSurface.position.z
            );

            _cueBallInst      = Instantiate(cueBallPrefab, cuePos, Quaternion.identity);
            _cueBallInst.name = "CueBall";

            BallController bc = _cueBallInst.GetComponent<BallController>();
            if (bc != null)
                bc.ApplyVisuals();
        }
    }

    public void ClearRack()
    {
        foreach (var go in _rackBalls)
            if (go != null) Destroy(go);

        _rackBalls.Clear();

        if (_cueBallInst != null)
        {
            Destroy(_cueBallInst);
            _cueBallInst = null;
        }
    }

    /// <summary>Referência à bola branca instanciada (útil para o CueController).</summary>
    public GameObject CueBallInstance => _cueBallInst;

    // ─────────────────────────────────────────────────────────────────────────

    private bool ValidateSetup()
    {
        if (ballPrefabs == null || ballPrefabs.Length == 0)
        {
            Debug.LogError("BallRack: ballPrefabs está vazio.");
            return false;
        }
        if (ballPrefabs.Length < 15)
            Debug.LogWarning($"BallRack: ballPrefabs tem {ballPrefabs.Length} entradas; esperado 15.");

        if (cueBallPrefab == null)
            Debug.LogWarning("BallRack: cueBallPrefab não atribuído — bola branca não será gerada.");

        if (tableSurface == null)
        {
            Debug.LogError("BallRack: tableSurface não atribuído.");
            return false;
        }

        return true;
    }
}

