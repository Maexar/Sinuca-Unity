using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Controla o taco: mira via raycast, carga de força e tacada
/// 
/// FIX — Pivot do taco:
///   O FBX do taco tem o pivot no centro do modelo. Para que a PONTA
///   fique no centro da bola branca (origem do cuePivot), precisamos
///   deslocar o mesh ao longo de -Z pela metade do comprimento do modelo.
///   Ajuste "Mesh Tip Offset" no Inspector até que a ponta toque a bola.
/// </summary>
public class CueController : MonoBehaviour
{
    [Header("Referências")]
    public Transform cueBall;       // White_Ball (transform da bola branca)
    public Transform cuePivot;      // Empty no centro da bola — eixo de rotação
    public Transform cueMesh;       // Modelo FBX do taco (filho do cuePivot)
    public Camera    mainCamera;
    public LineRenderer aimLine;

    [Header("Força da tacada")]
    public float minPower   = 1.5f;
    public float maxPower   = 10f;
    public float chargeSpeed = 6f;

    [Header("Visual do taco")]
    public float restDistance = 0.15f;   // distância da PONTA à bola em repouso
    public float maxPullback  = 0.55f;   // recuo máximo ao carregar força
    public float aimLineLength = 3.0f;

    // ── FIX: pivot ────────────────────────────────────────────────────────────
    [Header("Offset do Modelo (FIX de pivot)")]
    [Tooltip(
        "Metade do comprimento do modelo FBX em unidades Unity.\n" +
        "Ex.: se o taco mede ~1.5 m na cena → 0.75.\n" +
        "Aumente até a PONTA do taco ficar sobre a bola branca.")]
    public float meshTipOffset = 0.75f;
    // ─────────────────────────────────────────────────────────────────────────

    [Header("Turno")]
    public bool requireAllBallsStopped = true;

    private Vector3 _aimDirection = Vector3.forward;
    private float   _currentPower;
    private bool    _isCharging;
    private bool    _canShoot = true;

    // Rotação fixa do cilindro: 90° em X gira o eixo Y (altura do cilindro) para o eixo Z (direção de mira)
    private static readonly Quaternion CueMeshRot = Quaternion.Euler(90f, 0f, 0f);
    private Vector3 _restLocalPos;

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        // Auto-atribuir o cueMesh se ele não for o correto da hierarquia do jogo
        if (cuePivot != null && cuePivot.childCount > 0)
        {
            // Pega o primeiro objeto que estiver dentro do CuePivot
            cueMesh = cuePivot.GetChild(0);
        }
    }

    private void Start()
    {
        if (aimLine != null)
        {
            aimLine.gameObject.SetActive(true);
            aimLine.useWorldSpace = true;
        }

        if (cueMesh != null)
        {
            cueMesh.gameObject.SetActive(true);
            _restLocalPos = new Vector3(0f, 0f, -(meshTipOffset + restDistance));
            cueMesh.localPosition = _restLocalPos;
            cueMesh.localRotation = CueMeshRot;

            // Taco controlado por script — física não deve mover nem inclinar o mesh
            Rigidbody cueRb = cueMesh.GetComponentInChildren<Rigidbody>();
            if (cueRb != null)
                cueRb.isKinematic = true;

            // Remove colisores do taco para não empurrar bolas numeradas
            foreach (Collider col in cueMesh.GetComponentsInChildren<Collider>(true))
                col.enabled = false;
        }
    }

    private void Update()
    {
        if (cueBall == null || cuePivot == null || cueMesh == null || mainCamera == null)
            return;

        if (!cueBall.gameObject.activeInHierarchy)
            return;

        // Controle ABSOLUTO de visibilidade do taco atrelado ao _canShoot
        if (_canShoot)
        {
            if (!cuePivot.gameObject.activeSelf) cuePivot.gameObject.SetActive(true);
            if (!cueMesh.gameObject.activeSelf) cueMesh.gameObject.SetActive(true);
            if (aimLine != null && !aimLine.gameObject.activeSelf) aimLine.gameObject.SetActive(true);
        }
        else
        {
            // Força a ficar invisível enquanto as bolas estiverem rolando
            if (cuePivot.gameObject.activeSelf) cuePivot.gameObject.SetActive(false);
            if (cueMesh.gameObject.activeSelf) cueMesh.gameObject.SetActive(false);
            if (aimLine != null && aimLine.gameObject.activeSelf) aimLine.gameObject.SetActive(false);
            return;
        }

        Mouse mouse = Mouse.current;
        if (mouse == null)
            return;

        UpdateAimDirection(mouse);
        UpdateCueTransform();
        UpdateAimLine();
        HandleInput(mouse);
    }

    // ── Mira ─────────────────────────────────────────────────────────────────

    private void UpdateAimDirection(Mouse mouse)
    {
        Ray ray = mainCamera.ScreenPointToRay(mouse.position.ReadValue());

        if (Physics.Raycast(ray, out RaycastHit hit, 200f, ~0, QueryTriggerInteraction.Ignore))
        {
            Vector3 target = hit.point;
            target.y = cueBall.position.y;

            Vector3 dir = target - cueBall.position;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.0001f)
                _aimDirection = dir.normalized;
        }

        Debug.DrawRay(cueBall.position, _aimDirection * 2f, Color.yellow);
    }

    // ── Posição do taco ───────────────────────────────────────────────────────

    private void UpdateCueTransform()
    {
        cuePivot.position = cueBall.position;

        // Garante que o pivot nunca tenha componente Y na direção (sem inclinação)
        Vector3 flatDir = _aimDirection;
        flatDir.y = 0f;
        if (flatDir.sqrMagnitude > 0.0001f)
            cuePivot.rotation = Quaternion.LookRotation(flatDir.normalized, Vector3.up);

        if (!_isCharging)
        {
            cueMesh.localPosition = _restLocalPos;
            cueMesh.localRotation = CueMeshRot;
        }
    }

    // ── Linha de mira ─────────────────────────────────────────────────────────

    private void UpdateAimLine()
    {
        if (aimLine == null)
            return;

        Vector3 start = cueBall.position + Vector3.up * 0.01f;
        Vector3 end   = start + _aimDirection * aimLineLength;

        if (Physics.Raycast(start, _aimDirection, out RaycastHit hit, aimLineLength,
                            ~0, QueryTriggerInteraction.Ignore))
        {
            end = hit.point + Vector3.up * 0.01f;
        }

        aimLine.positionCount = 2;
        aimLine.SetPosition(0, start);
        aimLine.SetPosition(1, end);
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    private void HandleInput(Mouse mouse)
    {
        if (mouse.leftButton.wasPressedThisFrame)
        {
            if (!requireAllBallsStopped || TurnManager.AllBallsStopped())
            {
                _isCharging    = true;
                _currentPower  = minPower;
            }
        }

        if (_isCharging && mouse.leftButton.isPressed)
        {
            _currentPower += chargeSpeed * Time.deltaTime;
            _currentPower  = Mathf.Clamp(_currentPower, minPower, maxPower);
            UpdateCuePullback();
        }

        if (_isCharging && mouse.leftButton.wasReleasedThisFrame)
            Shoot();
    }

    private void UpdateCuePullback()
    {
        float t        = Mathf.InverseLerp(minPower, maxPower, _currentPower);
        float pullback = Mathf.Lerp(0f, maxPullback, t);

        // Adiciona a distância do pullback no eixo Z a partir da posição inicial que você arrumou!
        cueMesh.localPosition = _restLocalPos + new Vector3(0f, 0f, -pullback);
        cueMesh.localRotation = CueMeshRot;
    }

    private void Shoot()
    {
        _isCharging = false;
        _canShoot   = false;  // O Update() detectará que é false e esconderá tudo imediatamente

        BallController ball = cueBall.GetComponent<BallController>();
        if (ball != null)
            ball.ApplyImpulse(_aimDirection, _currentPower);

        cueMesh.localPosition = _restLocalPos;

        if (requireAllBallsStopped)
            StartCoroutine(WaitForBallsToStop());
        else
            _canShoot = true; // Se não for pra esperar, já volta a ativar no próximo frame
    }

    private IEnumerator WaitForBallsToStop()
    {
        // Aguarda um pequeno instante (0.2s) para a física (FixedUpdate) começar a mover as bolas
        // antes de verificar se elas já estão paradas.
        yield return new WaitForSeconds(0.2f);

        while (!TurnManager.AllBallsStopped())
            yield return null;

        if (TurnManager.Instance != null) TurnManager.Instance.OnTurnEnd();

        _canShoot = true;
        // Mostrar o taco e a mira novamente após as bolas pararem
        if (aimLine != null) aimLine.gameObject.SetActive(true);
        if (cueMesh != null) cueMesh.gameObject.SetActive(true);
    }

    // ── API pública (para o TurnManager controlar permissão de tacada) ────────

    /// <summary>Permite ou bloqueia o controle do taco externamente.</summary>
    public void SetCanShoot(bool value) => _canShoot = value;

    public bool CanShoot => _canShoot;
}

// using System.Collections;
// using UnityEngine;
// using UnityEngine.InputSystem; // Input System novo

// public class CueController : MonoBehaviour
// {
//     [Header("Referências")]
//     public Transform cueBall;        // White_Ball
//     public Transform cuePivot;       // vazio no centro da bola
//     public Transform cueMesh;        // modelo do taco (filho do pivot)
//     public Camera mainCamera;        // câmera principal
//     public LineRenderer aimLine;     // linha de mira

//     [Header("Força da tacada")]
//     public float minPower = 3f;
//     public float maxPower = 20f;
//     public float chargeSpeed = 12f;

//     [Header("Visual do taco")]
//     public float restDistance = 1.0f;
//     public float maxPullback = 1.6f;
//     public float aimLineLength = 3.0f;

//     [Header("Turno")]
//     public bool requireAllBallsStopped = true;

//     private Vector3 _aimDirection = Vector3.forward;
//     private float _currentPower;
//     private bool _isCharging;
//     private bool _canShoot = true;

//     private void Awake()
//     {
//         if (mainCamera == null)
//             mainCamera = Camera.main;
//     }

//     private void Update()
//     {
//         if (cueBall == null || cuePivot == null || cueMesh == null || mainCamera == null)
//             return;

//         if (!cueBall.gameObject.activeInHierarchy)
//             return;

//         if (!_canShoot)
//             return;

//         Mouse mouse = Mouse.current;
//         if (mouse == null)
//             return;

//         UpdateAimDirection(mouse);
//         UpdateCueTransform();
//         UpdateAimLine();
//         HandleInput(mouse);
//     }

//     private void UpdateAimDirection(Mouse mouse)
//     {
//         Ray ray = mainCamera.ScreenPointToRay(mouse.position.ReadValue());

//         // Raycast SEM layer mask por enquanto: pega qualquer coisa da mesa
//         if (Physics.Raycast(ray, out RaycastHit hit, 200f, ~0, QueryTriggerInteraction.Ignore))
//         {
//             // alvo na mesma altura da bola
//             Vector3 target = hit.point;
//             target.y = cueBall.position.y;

//             Vector3 dir = target - cueBall.position;
//             dir.y = 0f;

//             if (dir.sqrMagnitude > 0.0001f)
//             {
//                 _aimDirection = dir.normalized;
//             }
//         }

//         // debug para você ver a direção calculada
//         Debug.DrawRay(cueBall.position, _aimDirection, Color.yellow);
//     }

//     private void UpdateCueTransform()
//     {
//         // pivot grudado na bola branca
//         cuePivot.position = cueBall.position;

//         if (_aimDirection.sqrMagnitude > 0.0001f)
//         {
//             // pivot olha para a direção de tacada (do centro da bola para o alvo)
//             cuePivot.rotation = Quaternion.LookRotation(_aimDirection, Vector3.up);
//         }

//         if (!_isCharging)
//         {
//             cueMesh.localPosition = new Vector3(0f, 0f, -restDistance);
//         }
//     }

//     private void UpdateAimLine()
//     {
//         if (aimLine == null)
//             return;

//         Vector3 start = cueBall.position + Vector3.up * 0.01f;
//         Vector3 end = start + _aimDirection * aimLineLength;

//         if (Physics.Raycast(start, _aimDirection, out RaycastHit hit, aimLineLength, ~0, QueryTriggerInteraction.Ignore))
//         {
//             end = hit.point + Vector3.up * 0.01f;
//         }

//         aimLine.positionCount = 2;
//         aimLine.SetPosition(0, start);
//         aimLine.SetPosition(1, end);
//     }

//     private void HandleInput(Mouse mouse)
//     {
//         if (mouse.leftButton.wasPressedThisFrame)
//         {
//             if (!requireAllBallsStopped || TurnManager.AllBallsStopped())
//             {
//                 _isCharging = true;
//                 _currentPower = minPower;
//             }
//         }

//         if (_isCharging && mouse.leftButton.isPressed)
//         {
//             _currentPower += chargeSpeed * Time.deltaTime;
//             _currentPower = Mathf.Clamp(_currentPower, minPower, maxPower);
//             UpdateCuePullback();
//         }

//         if (_isCharging && mouse.leftButton.wasReleasedThisFrame)
//         {
//             Shoot();
//         }
//     }

//     private void UpdateCuePullback()
//     {
//         float t = Mathf.InverseLerp(minPower, maxPower, _currentPower);
//         float distance = Mathf.Lerp(restDistance, maxPullback, t);
//         cueMesh.localPosition = new Vector3(0f, 0f, -distance);
//     }

//     private void Shoot()
//     {
//         _isCharging = false;
//         _canShoot = false;

//         BallController ball = cueBall.GetComponent<BallController>();
//         if (ball != null)
//         {
//             ball.ApplyImpulse(_aimDirection, _currentPower);
//         }

//         cueMesh.localPosition = new Vector3(0f, 0f, -restDistance);

//         if (requireAllBallsStopped)
//             StartCoroutine(WaitForBallsToStop());
//         else
//             _canShoot = true;
//     }

//     private IEnumerator WaitForBallsToStop()
//     {
//         while (!TurnManager.AllBallsStopped())
//             yield return null;

//         _canShoot = true;
//     }
// }(0f, 0f, -restDistance);

//         if (requireAllBallsStopped)
//             StartCoroutine(WaitForBallsToStop());
//         else
//             _canShoot = true;
//     }

//     private IEnumerator WaitForBallsToStop()
//     {
//         while (!TurnManager.AllBallsStopped())
//             yield return null;

//         _canShoot = true;
//     }
// }(0f, 0f, -restDistance);

//         if (requireAllBallsStopped)
//             StartCoroutine(WaitForBallsToStop());
//         else
//             _canShoot = true;
//     }

//     private IEnumerator WaitForBallsToStop()
//     {
//         while (!TurnManager.AllBallsStopped())
//             yield return null;

//         _canShoot = true;
//     }
// }