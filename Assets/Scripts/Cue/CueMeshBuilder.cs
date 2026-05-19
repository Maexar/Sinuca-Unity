using UnityEngine;

/// <summary>
/// Gera proceduralmente um mesh cônico (tapered) para o taco de sinuca.
/// Substitui o Cylinder primitivo por uma forma realista: ponta fina, cabo grosso.
///
/// Como usar: adicione este componente ao GameObject do taco (filho do CuePivot).
/// O mesh é gerado automaticamente no Awake — nenhuma configuração extra necessária.
/// </summary>
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CueMeshBuilder : MonoBehaviour
{
    [Header("Geometria")]
    [Tooltip("Metade do comprimento do taco em unidades Unity. Deve ser igual ao meshTipOffset no CueController.")]
    public float halfLength = 0.75f;

    [Tooltip("Raio da ponta (ferrule). Valor real ~6mm.")]
    public float tipRadius = 0.006f;

    [Tooltip("Raio do cabo (butt). Valor real ~15mm.")]
    public float buttRadius = 0.015f;

    [Tooltip("Faces laterais do cilindro. 12 é suave o suficiente para a distância de câmera.")]
    public int segments = 12;

    [Tooltip("Anéis ao longo do comprimento. Mais anéis = taper mais suave.")]
    public int rings = 24;

    [Header("Perfil do Taper")]
    [Tooltip("Controla como o raio varia do cabo (esquerda) à ponta (direita).\n" +
             "Curva padrão: shaft fino e uniforme, cabo abre rapidamente — igual a um taco real.")]
    public AnimationCurve taperCurve = new AnimationCurve(
        new Keyframe(0.00f, 1.00f, 0f,    -4f),  // cabo: raio máximo, abre rápido
        new Keyframe(0.25f, 0.35f, -2f,   -0.4f), // transição grip → shaft
        new Keyframe(0.55f, 0.08f, -0.15f, 0f),   // shaft fino começa
        new Keyframe(1.00f, 0.00f, 0f,     0f)    // ponta: raio mínimo
    );

    [Header("Cores")]
    public Color tipColor   = Color.white;                          // ferrule (ponta branca)
    public Color shaftColor = new Color(0.76f, 0.60f, 0.42f, 1f); // madeira clara
    public Color buttColor  = new Color(0.30f, 0.16f, 0.08f, 1f); // madeira escura / grip

    // ─────────────────────────────────────────────────────────────────────────

    // Raio em t (0 = cabo, 1 = ponta): curva não-linear que mantém shaft fino
    // e abre rapidamente no cabo — perfil de taco real.
    private float GetRadius(float t) =>
        Mathf.Lerp(tipRadius, buttRadius, taperCurve.Evaluate(t));

    private void Awake()
    {
        // Mesh gerada em unidades reais — sem escala
        transform.localScale = Vector3.one;

        GetComponent<MeshFilter>().mesh = BuildMesh();
        BuildMaterial();
    }

    // ── Geração do mesh cônico ────────────────────────────────────────────────

    private Mesh BuildMesh()
    {
        // Layout do eixo Y no mesh (antes da rotação 90°X aplicada pelo CueController):
        //   Y = -halfLength  →  cabo/butt (longe da bola)
        //   Y = +halfLength  →  ponta/tip  (próximo da bola)

        int totalVerts = (rings + 1) * segments + 2;
        var verts = new Vector3[totalVerts];
        var norms = new Vector3[totalVerts];
        var uvs   = new Vector2[totalVerts];

        // Vértices laterais
        for (int r = 0; r <= rings; r++)
        {
            float t      = (float)r / rings;
            float y      = Mathf.Lerp(-halfLength, halfLength, t);
            float radius = GetRadius(t);

            for (int s = 0; s < segments; s++)
            {
                float angle = (float)s / segments * Mathf.PI * 2f;
                float x = Mathf.Cos(angle) * radius;
                float z = Mathf.Sin(angle) * radius;

                int i    = r * segments + s;
                verts[i] = new Vector3(x, y, z);
                norms[i] = new Vector3(x, 0f, z).normalized;
                uvs[i]   = new Vector2((float)s / segments, t);
            }
        }

        // Centros das tampas
        int buttCap = (rings + 1) * segments;
        int tipCap  = buttCap + 1;
        verts[buttCap] = new Vector3(0f, -halfLength, 0f);
        verts[tipCap]  = new Vector3(0f,  halfLength, 0f);
        norms[buttCap] = Vector3.down;
        norms[tipCap]  = Vector3.up;
        uvs[buttCap]   = new Vector2(0.5f, 0f);
        uvs[tipCap]    = new Vector2(0.5f, 1f);

        // Triângulos
        int[] tris = new int[rings * segments * 6 + segments * 6];
        int   ti   = 0;

        // Faces laterais
        for (int r = 0; r < rings; r++)
        {
            for (int s = 0; s < segments; s++)
            {
                int a = r * segments + s;
                int b = r * segments + (s + 1) % segments;
                int c = a + segments;
                int d = b + segments;

                tris[ti++] = a; tris[ti++] = c; tris[ti++] = b;
                tris[ti++] = b; tris[ti++] = c; tris[ti++] = d;
            }
        }

        // Tampa do cabo (−Y)
        for (int s = 0; s < segments; s++)
        {
            tris[ti++] = buttCap;
            tris[ti++] = (s + 1) % segments;
            tris[ti++] = s;
        }

        // Tampa da ponta (+Y)
        for (int s = 0; s < segments; s++)
        {
            tris[ti++] = tipCap;
            tris[ti++] = rings * segments + s;
            tris[ti++] = rings * segments + (s + 1) % segments;
        }

        var mesh = new Mesh { name = "CueTapered" };
        mesh.vertices  = verts;
        mesh.normals   = norms;
        mesh.uv        = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // ── Material com gradiente de cor ─────────────────────────────────────────

    private void BuildMaterial()
    {
        // Textura 1×64: V=0 = cabo, V=1 = ponta
        const int H = 64;
        var tex = new Texture2D(1, H, TextureFormat.RGB24, false);

        for (int y = 0; y < H; y++)
        {
            float t = (float)y / (H - 1);
            Color c;

            if (t < 0.28f)
                // Cabo: madeira escura → madeira clara
                c = Color.Lerp(buttColor, shaftColor, t / 0.28f);
            else if (t < 0.92f)
                // Shaft: madeira clara uniforme
                c = shaftColor;
            else
                // Ponta: madeira → branco (ferrule)
                c = Color.Lerp(shaftColor, tipColor, (t - 0.92f) / 0.08f);

            tex.SetPixel(0, y, c);
        }

        tex.Apply();
        tex.wrapMode   = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;

        var mat = new Material(Shader.Find("Standard"));
        mat.mainTexture              = tex;
        mat.SetFloat("_Glossiness",  0.80f); // acabamento envernizado
        mat.SetFloat("_Metallic",    0.00f);

        GetComponent<MeshRenderer>().material = mat;
    }
}
