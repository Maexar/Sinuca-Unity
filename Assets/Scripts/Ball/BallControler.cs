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
    // ── Registro global ───────────────────────────────────────────────────────
    public static readonly List<BallController> AllBalls = new List<BallController>();

    // ── Identidade ────────────────────────────────────────────────────────────
    [Header("Tipo")]
    public BallType ballType;
    public int      ballNumber;

    [Header("Visual")]
    [Tooltip("Se true, ApplyVisuals() define cor/material automaticamente no Start.\n" +
             "Desative se quiser controlar o material manualmente no prefab.")]
    public bool autoApplyVisuals = true;

    // ── Física ────────────────────────────────────────────────────────────────
    [Header("Física de rolamento")]
    [Tooltip("Atrito linear — quanto maior, mais rápido a bola para.")]
    public float rollingFrictionLinear  = 0.30f;

    [Tooltip("Atrito rotacional (spin).")]
    public float rollingFrictionAngular = 0.20f;

    [Tooltip("Velocidade abaixo da qual a bola é considerada parada (m/s). Valor alto causa parada brusca.")]
    public float sleepThreshold = 0.02f;

    // ── Internos ──────────────────────────────────────────────────────────────
    private Rigidbody _rb;

    public bool IsPocketed { get; private set; }
    public bool IsStopped  { get; private set; }

    // ── Cores das bolas (padrão 8-ball) ──────────────────────────────────────
    // índice 0 = não usado; índice 1–15 = número da bola; índice 16 = cue
    private static readonly Color[] BallColors = new Color[]
    {
        Color.white,                                    // 0  — não usado
        new Color(1.00f, 0.85f, 0.00f),                // 1  — amarelo
        new Color(0.10f, 0.25f, 0.80f),                // 2  — azul
        new Color(0.85f, 0.10f, 0.10f),                // 3  — vermelho
        new Color(0.50f, 0.10f, 0.55f),                // 4  — roxo
        new Color(1.00f, 0.45f, 0.00f),                // 5  — laranja
        new Color(0.10f, 0.50f, 0.10f),                // 6  — verde
        new Color(0.55f, 0.20f, 0.05f),                // 7  — marrom
        new Color(0.05f, 0.05f, 0.05f),                // 8  — preto
        new Color(1.00f, 0.85f, 0.00f),                // 9  — amarelo listrado
        new Color(0.10f, 0.25f, 0.80f),                // 10 — azul listrado
        new Color(0.85f, 0.10f, 0.10f),                // 11 — vermelho listrado
        new Color(0.50f, 0.10f, 0.55f),                // 12 — roxo listrado
        new Color(1.00f, 0.45f, 0.00f),                // 13 — laranja listrado
        new Color(0.10f, 0.50f, 0.10f),                // 14 — verde listrado
        new Color(0.55f, 0.20f, 0.05f),                // 15 — marrom listrado
    };

    // ─────────────────────────────────────────────────────────────────────────

    private void Awake()
    {
        _rb = GetComponent<Rigidbody>();

        _rb.mass                   = 0.17f;
        _rb.linearDamping          = 0.08f;   // baixo — atrito customizado já desacelera a bola
        _rb.angularDamping         = 0.20f;
        _rb.useGravity             = true;
        _rb.interpolation          = RigidbodyInterpolation.Interpolate; // visual suave entre steps
        // ContinuousSpeculative detecta colisões bola-a-bola (dinâmica vs dinâmica).
        // CollisionDetectionMode.Continuous só funciona contra colisores ESTÁTICOS,
        // causando tunneling em colisões entre bolas a alta velocidade.
        _rb.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    private void Start()
    {
        if (autoApplyVisuals)
            ApplyVisuals();
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
        if (IsPocketed) return;
        ApplyRollingFriction();
        CheckSleep();
    }

    // ── Visual ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Aplica cor e textura procedural ao Renderer da bola de acordo com
    /// ballType e ballNumber.
    ///
    /// Bola branca     → branco puro, sem textura.
    /// Bola 8          → preto.
    /// Cheias (1–7)    → cor sólida.
    /// Listradas (9–15)→ textura procedural: faixa colorida no centro, branco nas pontas.
    /// </summary>
    public void ApplyVisuals()
    {
        Renderer rend = GetComponentInChildren<Renderer>();
        if (rend == null) return;

        // Cria material baseado no Standard shader
        Material mat = new Material(Shader.Find("Standard"));
        mat.SetFloat("_Metallic",   0f);
        mat.SetFloat("_Glossiness", 0.85f); // brilho de bola de sinuca

        switch (ballType)
        {
            case BallType.Cue:
                mat.color = Color.white;
                break;

            case BallType.EightBall:
                mat.color = BallColors[8];
                break;

            case BallType.Solid:
                if (ballNumber >= 1 && ballNumber <= 7)
                    mat.color = BallColors[ballNumber];
                break;

            case BallType.Stripe:
                if (ballNumber >= 9 && ballNumber <= 15)
                {
                    // textura procedural: branco nas extremidades, cor no centro
                    Texture2D tex = CreateStripeTexture(BallColors[ballNumber]);
                    mat.mainTexture = tex;
                    mat.color       = Color.white; // base branca para a textura
                }
                break;
        }

        rend.material = mat;
    }

    /// <summary>
    /// Gera uma textura 64×64 com faixa colorida horizontal ao centro (UV do equador).
    /// As bolas esféricas em Unity usam UV cilíndrico — o centro vertical da textura
    /// fica no equador da esfera, criando o efeito de listrada.
    /// </summary>
    private static Texture2D CreateStripeTexture(Color stripeColor, int w = 64, int h = 64)
    {
        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        Color[] pixels = new Color[w * h];

        for (int y = 0; y < h; y++)
        {
            float v          = (float)y / (h - 1);   // 0 = baixo, 1 = cima
            bool  isStripe   = v > 0.30f && v < 0.70f;
            Color pixelColor = isStripe ? stripeColor : Color.white;

            for (int x = 0; x < w; x++)
                pixels[y * w + x] = pixelColor;
        }

        tex.SetPixels(pixels);
        tex.Apply();
        return tex;
    }

    // ── Física ────────────────────────────────────────────────────────────────

    private void ApplyRollingFriction()
    {
        Vector3 v = _rb.linearVelocity;
        Vector3 w = _rb.angularVelocity;

        if (v.magnitude < sleepThreshold && w.magnitude < sleepThreshold)
        {
            _rb.linearVelocity  = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
            return;
        }

        if (v.sqrMagnitude > 0.0001f)
            _rb.AddForce(-v.normalized * rollingFrictionLinear, ForceMode.Acceleration);

        if (w.sqrMagnitude > 0.0001f)
            _rb.AddTorque(-w.normalized * rollingFrictionAngular, ForceMode.Acceleration);
    }

    private void CheckSleep()
    {
        bool stopped = _rb.linearVelocity.magnitude  < sleepThreshold &&
                       _rb.angularVelocity.magnitude < sleepThreshold;

        if (stopped)
        {
            _rb.linearVelocity  = Vector3.zero;
            _rb.angularVelocity = Vector3.zero;
        }

        IsStopped = stopped;
    }

    // ── API pública ───────────────────────────────────────────────────────────

    public void ApplyImpulse(Vector3 direction, float force)
    {
        _rb.AddForce(direction * force, ForceMode.Impulse);
        IsStopped = false;
    }

    public void Pocket()
    {
        IsPocketed          = true;
        _rb.isKinematic     = true;
        _rb.linearVelocity  = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        gameObject.SetActive(false);
    }

    public void ResetBall(Vector3 position)
    {
        IsPocketed          = false;
        IsStopped           = true;
        _rb.isKinematic     = false;
        _rb.linearVelocity  = Vector3.zero;
        _rb.angularVelocity = Vector3.zero;
        transform.position  = position;
        gameObject.SetActive(true);
    }
}

// using System.Collections.Generic;
// using UnityEngine;

// public enum BallType
// {
//     Cue,        // bola branca
//     Solid,      // cheias (1–7)
//     Stripe,     // listradas (9–15)
//     EightBall   // bola 8
// }

// [RequireComponent(typeof(Rigidbody), typeof(SphereCollider))]
// public class BallController : MonoBehaviour
// {
//     // Registro global das bolas para outras partes do jogo (turnos, regras, etc.)
//     public static readonly List<BallController> AllBalls = new List<BallController>();

//     [Header("Tipo")]
//     public BallType ballType;
//     public int ballNumber;

//     [Header("Física de rolamento")]
//     [Tooltip("Atrito linear aplicado manualmente (quanto maior, mais rápido a bola para).")]
//     public float rollingFrictionLinear = 0.25f; //o quao rapido a bola para 

//     [Tooltip("Atrito rotacional (spin) – pode ser ajustado depois que colocarmos efeitos de taco.")]
//     public float rollingFrictionAngular = 0.15f; 

//     [Tooltip("Velocidade abaixo da qual a bola é considerada parada.")]
//     public float sleepThreshold = 0.05f; //o quao rapido poem pra dormir

//     private Rigidbody _rb;

//     public bool IsPocketed { get; private set; }
//     public bool IsStopped { get; private set; }

//     private void Awake()
//     {
//         _rb = GetComponent<Rigidbody>();

//         // Configuração razoável para sinuca (pode ajustar depois)
//         _rb.mass = 0.17f;                    // ~170g [file:1]
//         _rb.linearDamping = 0.05f;           // leve damping global [web:71][web:74]
//         _rb.angularDamping = 0.05f;
//         _rb.useGravity = true;
//         _rb.collisionDetectionMode = CollisionDetectionMode.Continuous;
//     }

//     private void OnEnable()
//     {
//         if (!AllBalls.Contains(this))
//             AllBalls.Add(this);
//     }

//     private void OnDisable()
//     {
//         AllBalls.Remove(this);
//     }

//     private void FixedUpdate()
//     {
//         if (IsPocketed)
//             return;

//         ApplyRollingFriction();
//         CheckSleep();
//     }

//     private void ApplyRollingFriction()
//     {
//         Vector3 v = _rb.linearVelocity;
//         Vector3 w = _rb.angularVelocity;

//         // Se já está praticamente parada, zera de vez
//         if (v.magnitude < sleepThreshold && w.magnitude < sleepThreshold)
//         {
//             _rb.linearVelocity = Vector3.zero;
//             _rb.angularVelocity = Vector3.zero;
//             return;
//         }

//         // Atrito linear proporcional à direção do movimento
//         if (v.sqrMagnitude > 0.0001f)
//         {
//             Vector3 friction = -v.normalized * rollingFrictionLinear;
//             _rb.AddForce(friction, ForceMode.Acceleration);
//         }

//         // Atrito rotacional – desacelera o spin aos poucos
//         if (w.sqrMagnitude > 0.0001f)
//         {
//             Vector3 angularFriction = -w.normalized * rollingFrictionAngular;
//             _rb.AddTorque(angularFriction, ForceMode.Acceleration);
//         }
//     }

//     private void CheckSleep()
//     {
//         if (_rb.linearVelocity.magnitude < sleepThreshold &&
//             _rb.angularVelocity.magnitude < sleepThreshold)
//         {
//             _rb.linearVelocity = Vector3.zero;
//             _rb.angularVelocity = Vector3.zero;
//             IsStopped = true;
//         }
//         else
//         {
//             IsStopped = false;
//         }
//     }

//     public void ApplyImpulse(Vector3 direction, float force)
//     {
//         // usado pelo taco 
//         _rb.AddForce(direction * force, ForceMode.Impulse);
//         IsStopped = false;
//     }

//     public void Pocket()
//     {
//         IsPocketed = true;
//         _rb.isKinematic = true;
//         _rb.linearVelocity = Vector3.zero;
//         _rb.angularVelocity = Vector3.zero;
//         gameObject.SetActive(false);
//     }

//     public void ResetBall(Vector3 position)
//     {
//         IsPocketed = false;
//         IsStopped = true;
//         _rb.isKinematic = false;
//         _rb.linearVelocity = Vector3.zero;
//         _rb.angularVelocity = Vector3.zero;
//         transform.position = position;
//         gameObject.SetActive(true);
//     }
// }