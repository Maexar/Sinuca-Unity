using System.Collections.Generic;
using UnityEngine;

public enum BallType
{
    Cue,        // bola branca
    Solid,      // cheias (1–7)
    Stripe,     // listradas (9–15)
    EightBall   // bola 8
}

[RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
public class BallController : MonoBehaviour
{
    // Registro global das bolas para outras partes do jogo (turnos, regras, etc.)
    public static readonly List<BallController> AllBalls = new List<BallController>();

    [Header("Tipo")]
    public BallType ballType;
    public int ballNumber;

    [Header("Física de rolamento")]
    [Tooltip("Atrito linear aplicado manualmente (quanto maior, mais rápido a bola para).")]
    public float rollingFrictionLinear = 0.25f; //o quao rapido a bola para 

    [Tooltip("Atrito rotacional (spin) – pode ser ajustado depois que colocarmos efeitos de taco.")]
    public float rollingFrictionAngular = 0.15f; 

    [Tooltip("Velocidade abaixo da qual a bola é considerada parada.")]
    public float sleepThreshold = 0.05f; //o quao rapido poem pra dormir

    private Rigidbody _rb;

    public bool IsPocketed { get; private set; }
    public bool IsStopped { get; private set; }

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        // Configuração razoável para sinuca (pode ajustar depois)
        _rb.mass = 0.17f;                    // ~170g [file:1]
        _rb.linearDamping = 0.05f;           // leve damping global [web:71][web:74]
        _rb.angularDamping = 0.05f;
        _rb.useGravity = true;
        _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
    }

    private void OnEnable()
    {
        if (!AllBalls.Contains(this))
            AllBalls.Add(this);
    }

    private void OnDisable()
    {
        AllBalls.Remove(this);
    }

    private void FixedUpdate()
    {
        if (IsPocketed)
            return;

        ApplyRollingFriction();
        CheckSleep();
    }

    private void ApplyRollingFriction()
    {
        Vector3 v = _rb.linearVelocity;
        Vector3 w = _rb.angularVelocity;

        // Se já está praticamente parada, zera de vez
        if (v.magnitude < sleepThreshold && w.magnitude < sleepThreshold)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            return;
        }

        // Atrito linear proporcional à direção do movimento
        if (v.sqrMagnitude > 0.0001f)
        {
            Vector3 friction = -v.normalized * rollingFrictionLinear;
            _rb.AddForce(friction, ForceMode.Acceleration);
        }

        // Atrito rotacional – desacelera o spin aos poucos
        if (w.sqrMagnitude > 0.0001f)
        {
            Vector3 angularFriction = -w.normalized * rollingFrictionAngular;
            _rb.AddTorque(angularFriction, ForceMode.Acceleration);
        }
    }

    private void CheckSleep()
    {
        if (_rb.linearVelocity.magnitude < sleepThreshold &&
            _rb.angularVelocity.magnitude < sleepThreshold)
        {
            _rb.linearVelocity = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            IsStopped = true;
        }
        else
        {
            IsStopped = false;
        }
    }

    public void ApplyImpulse(Vector3 direction, float force)
    {
        // usado pelo taco 
        _rb.AddForce(direction * force, ForceMode.Impulse);
        IsStopped = false;
    }

    public void Pocket()
    {
        IsPocketed = true;
        _rb.isKinematic = true;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        gameObject.SetActive(false);
    }

    public void ResetBall(Vector3 position)
    {
        IsPocketed = false;
        IsStopped = true;
        _rb.isKinematic = false;
        _rb.linearVelocity = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        transform.position = position;
        gameObject.SetActive(true);
    }
}