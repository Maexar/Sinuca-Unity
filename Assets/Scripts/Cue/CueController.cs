using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem; // Input System novo

public class CueController : MonoBehaviour
{
    [Header("Referências")]
    public Transform cueBall;        // White_Ball
    public Transform cuePivot;       // vazio no centro da bola
    public Transform cueMesh;        // modelo do taco (filho do pivot)
    public Camera mainCamera;        // câmera principal
    public LineRenderer aimLine;     // linha de mira

    [Header("Força da tacada")]
    public float minPower = 3f;
    public float maxPower = 20f;
    public float chargeSpeed = 12f;

    [Header("Visual do taco")]
    public float restDistance = 1.0f;
    public float maxPullback = 1.6f;
    public float aimLineLength = 3.0f;

    [Header("Turno")]
    public bool requireAllBallsStopped = true;

    private Vector3 _aimDirection = Vector3.forward;
    private float _currentPower;
    private bool _isCharging;
    private bool _canShoot = true;

    private void Awake()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;
    }

    private void Update()
    {
        if (cueBall == null || cuePivot == null || cueMesh == null || mainCamera == null)
            return;

        if (!cueBall.gameObject.activeInHierarchy)
            return;

        if (!_canShoot)
            return;

        Mouse mouse = Mouse.current;
        if (mouse == null)
            return;

        UpdateAimDirection(mouse);
        UpdateCueTransform();
        UpdateAimLine();
        HandleInput(mouse);
    }

    private void UpdateAimDirection(Mouse mouse)
    {
        Ray ray = mainCamera.ScreenPointToRay(mouse.position.ReadValue());

        // Raycast SEM layer mask por enquanto: pega qualquer coisa da mesa
        if (Physics.Raycast(ray, out RaycastHit hit, 200f, ~0, QueryTriggerInteraction.Ignore))
        {
            // alvo na mesma altura da bola
            Vector3 target = hit.point;
            target.y = cueBall.position.y;

            Vector3 dir = target - cueBall.position;
            dir.y = 0f;

            if (dir.sqrMagnitude > 0.0001f)
            {
                _aimDirection = dir.normalized;
            }
        }

        // debug para você ver a direção calculada
        Debug.DrawRay(cueBall.position, _aimDirection, Color.yellow);
    }

    private void UpdateCueTransform()
    {
        // pivot grudado na bola branca
        cuePivot.position = cueBall.position;

        if (_aimDirection.sqrMagnitude > 0.0001f)
        {
            // pivot olha para a direção de tacada (do centro da bola para o alvo)
            cuePivot.rotation = Quaternion.LookRotation(_aimDirection, Vector3.up);
        }

        if (!_isCharging)
        {
            cueMesh.localPosition = new Vector3(0f, 0f, -restDistance);
        }
    }

    private void UpdateAimLine()
    {
        if (aimLine == null)
            return;

        Vector3 start = cueBall.position + Vector3.up * 0.01f;
        Vector3 end = start + _aimDirection * aimLineLength;

        if (Physics.Raycast(start, _aimDirection, out RaycastHit hit, aimLineLength, ~0, QueryTriggerInteraction.Ignore))
        {
            end = hit.point + Vector3.up * 0.01f;
        }

        aimLine.positionCount = 2;
        aimLine.SetPosition(0, start);
        aimLine.SetPosition(1, end);
    }

    private void HandleInput(Mouse mouse)
    {
        if (mouse.leftButton.wasPressedThisFrame)
        {
            if (!requireAllBallsStopped || TurnManager.AllBallsStopped())
            {
                _isCharging = true;
                _currentPower = minPower;
            }
        }

        if (_isCharging && mouse.leftButton.isPressed)
        {
            _currentPower += chargeSpeed * Time.deltaTime;
            _currentPower = Mathf.Clamp(_currentPower, minPower, maxPower);
            UpdateCuePullback();
        }

        if (_isCharging && mouse.leftButton.wasReleasedThisFrame)
        {
            Shoot();
        }
    }

    private void UpdateCuePullback()
    {
        float t = Mathf.InverseLerp(minPower, maxPower, _currentPower);
        float distance = Mathf.Lerp(restDistance, maxPullback, t);
        cueMesh.localPosition = new Vector3(0f, 0f, -distance);
    }

    private void Shoot()
    {
        _isCharging = false;
        _canShoot = false;

        BallController ball = cueBall.GetComponent<BallController>();
        if (ball != null)
        {
            ball.ApplyImpulse(_aimDirection, _currentPower);
        }

        cueMesh.localPosition = new Vector3(0f, 0f, -restDistance);

        if (requireAllBallsStopped)
            StartCoroutine(WaitForBallsToStop());
        else
            _canShoot = true;
    }

    private IEnumerator WaitForBallsToStop()
    {
        while (!TurnManager.AllBallsStopped())
            yield return null;

        _canShoot = true;
    }
}