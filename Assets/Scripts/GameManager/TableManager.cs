using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Constrói a mesa de sinuca proceduralmente.
///
/// FIX — Caçapas sem trigger:
///   Cada caçapa agora recebe automaticamente:
///     • SphereCollider com isTrigger = true (zona de detecção)
///     • PocketDetector (script que avisa o GameManager)
///   Isso garante que o OnTriggerEnter funcione sem precisar
///   configurar nada manualmente no Editor.
///
/// Hierarquia gerada:
///   Table
///    ├── TableSurface
///    ├── CushionTop / Bottom / Left / Right
///    ├── pocket
///    │    ├── Pocket_TL  → HoleDisk, HoleTube, HoleBottom, Rim, JawZ, JawX
///    │    │               + SphereCollider(trigger) + PocketDetector
///    │    ├── Pocket_TR  → ...
///    │    ├── Pocket_BL  → ...
///    │    ├── Pocket_BR  → ...
///    │    ├── Pocket_TM  → ...
///    │    └── Pocket_BM  → ...
///    └── Leg_1..4
/// </summary>
public class SnookerTableBuilder : MonoBehaviour
{
    // ─── Mesa ─────────────────────────────────────────────────────────────────
    [Header("Dimensões da Mesa (metros)")]
    public float tableWidth     = 2.74f;
    public float tableDepth     = 1.37f;
    public float tableThickness = 0.10f;
    public float legHeight      = 0.78f;
    public float legSize        = 0.08f;

    // ─── Tabelas ──────────────────────────────────────────────────────────────
    [Header("Tabelas (Cushions)")]
    public float cushionWidth  = 0.07f;
    public float cushionHeight = 0.04f;

    // ─── Caçapas ──────────────────────────────────────────────────────────────
    [Header("Caçapas (Pockets)")]
    [Tooltip("Raio interno do buraco")]
    public float pocketRadius = 0.055f;
    [Tooltip("Profundidade do tubo")]
    public float pocketDepth  = 0.14f;
    [Tooltip("Largura extra do aro metálico além do raio")]
    public float rimExtra     = 0.012f;
    [Tooltip("Comprimento de cada jaw")]
    public float jawLength    = 0.09f;
    [Tooltip("Espessura de cada jaw")]
    public float jawThick     = 0.048f;

    // ─── Trigger da Caçapa ────────────────────────────────────────────────────
    [Header("Trigger da Caçapa (FIX)")]
    [Tooltip(
        "Multiplicador do raio do trigger em relação a pocketRadius.\n" +
        "1.3 significa 30% maior que o buraco visual — ajuste se as bolas\n" +
        "passarem sem ser detectadas ou se bolas fora do buraco forem detectadas.")]
    public float pocketTriggerRadiusMultiplier = 1.3f;
    [Tooltip(
        "Centro Y do trigger relativo ao pivot da caçapa (negativo = abaixo do tampo).\n" +
        "Manter entre -0.03 e 0 funciona bem para a maioria dos casos.")]
    public float pocketTriggerCenterY = -0.02f;

    // ─── Materiais ────────────────────────────────────────────────────────────
    [Header("Materiais (opcional — cores automáticas como fallback)")]
    public Material feltMaterial;
    public Material cushionMaterial;
    public Material woodMaterial;
    public Material pocketMaterial;
    public Material metalMaterial;

    // ─── Física das Tabelas ───────────────────────────────────────────────────
    [Header("Física das Tabelas (Cushions)")]
    [Tooltip("Elasticidade das tabelas. 0 = sem ricochete, 1 = ricochete total. ~0.75 é realista para sinuca.")]
    [Range(0f, 1f)]
    public float cushionBounciness = 0.75f;

    // ── Cores padrão ──────────────────────────────────────────────────────────
    static readonly Color C_Felt    = new Color(0.10f, 0.45f, 0.15f);
    static readonly Color C_Cushion = new Color(0.07f, 0.36f, 0.09f);
    static readonly Color C_Wood    = new Color(0.38f, 0.21f, 0.04f);
    static readonly Color C_Pocket  = new Color(0.04f, 0.04f, 0.04f);
    static readonly Color C_Metal   = new Color(0.60f, 0.60f, 0.65f);

    // ─────────────────────────────────────────────────────────────────────────

    private void Start()
    {
        ApplyCushionPhysics();
    }

    private void ApplyCushionPhysics()
    {
        var mat = new PhysicsMaterial("CushionBounce")
        {
            bounciness      = cushionBounciness,
            bounceCombine   = PhysicsMaterialCombine.Maximum,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            staticFriction  = 0.1f,
            dynamicFriction = 0.1f
        };

        foreach (Collider col in GetComponentsInChildren<Collider>(true))
        {
            if (col.isTrigger) continue;
            string n = col.gameObject.name;
            if (n.StartsWith("Cushion") || n.StartsWith("Jaw"))
                col.sharedMaterial = mat;
        }
    }

    [ContextMenu("Build Table")]
    public void BuildTable()
    {
        // Remove TODOS os filhos que sejam mesas — evita duplicatas "Table (1)", "Table (2)"...
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);
            if (child.name == "Table" || child.name.StartsWith("Table ("))
                DestroyImmediate(child.gameObject);
        }

        GameObject table = new GameObject("Table");
        table.transform.SetParent(transform, false);
        table.transform.localPosition = new Vector3(0f, legHeight, 0f);

        BuildTableSurface(table.transform);
        BuildCushion(table.transform, "CushionTop");
        BuildCushion(table.transform, "CushionBottom");
        BuildCushion(table.transform, "CushionLeft");
        BuildCushion(table.transform, "CushionRight");
        BuildPockets(table.transform);
        BuildLegs(table.transform);

        Debug.Log("[SnookerTableBuilder] Mesa criada com sucesso.");
    }

    // =========================================================================
    // TAMPO
    // =========================================================================
    void BuildTableSurface(Transform parent)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = "TableSurface";
        go.transform.SetParent(parent, false);
        go.transform.localPosition = new Vector3(0f, -tableThickness * 0.5f, 0f);
        go.transform.localScale    = new Vector3(tableWidth, tableThickness, tableDepth);
        ApplyMat(go, feltMaterial, C_Felt);
    }

    // =========================================================================
    // TABELAS (CUSHIONS)
    // =========================================================================
    void BuildCushion(Transform parent, string name)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.SetParent(parent, false);

        float hw = tableWidth  * 0.5f;
        float hd = tableDepth  * 0.5f;
        float yc = cushionHeight * 0.5f;

        switch (name)
        {
            case "CushionTop":
                go.transform.localPosition = new Vector3(0f, yc,  hd + cushionWidth * 0.5f);
                go.transform.localScale    = new Vector3(tableWidth, cushionHeight, cushionWidth);
                break;
            case "CushionBottom":
                go.transform.localPosition = new Vector3(0f, yc, -hd - cushionWidth * 0.5f);
                go.transform.localScale    = new Vector3(tableWidth, cushionHeight, cushionWidth);
                break;
            case "CushionLeft":
                go.transform.localPosition = new Vector3(-hw - cushionWidth * 0.5f, yc, 0f);
                go.transform.localScale    = new Vector3(cushionWidth, cushionHeight, tableDepth);
                break;
            case "CushionRight":
                go.transform.localPosition = new Vector3( hw + cushionWidth * 0.5f, yc, 0f);
                go.transform.localScale    = new Vector3(cushionWidth, cushionHeight, tableDepth);
                break;
        }
        ApplyMat(go, cushionMaterial, C_Cushion);
    }

    // =========================================================================
    // CAÇAPAS
    // =========================================================================
    void BuildPockets(Transform parent)
    {
        GameObject root = new GameObject("pocket");
        root.transform.SetParent(parent, false);
        root.transform.localPosition = Vector3.zero;

        float hw = tableWidth  * 0.5f;
        float hd = tableDepth  * 0.5f;

        CreateCornerPocket(root.transform, "Pocket_TL", new Vector3(-hw, 0f,  hd), -1f,  1f);
        CreateCornerPocket(root.transform, "Pocket_TR", new Vector3( hw, 0f,  hd),  1f,  1f);
        CreateCornerPocket(root.transform, "Pocket_BL", new Vector3(-hw, 0f, -hd), -1f, -1f);
        CreateCornerPocket(root.transform, "Pocket_BR", new Vector3( hw, 0f, -hd),  1f, -1f);
        CreateMiddlePocket(root.transform, "Pocket_TM", new Vector3(0f,  0f,  hd),  1f);
        CreateMiddlePocket(root.transform, "Pocket_BM", new Vector3(0f,  0f, -hd), -1f);
    }

    // ── Canto ─────────────────────────────────────────────────────────────────
    void CreateCornerPocket(Transform parent, string pName,
                            Vector3 pos, float signX, float signZ)
    {
        GameObject go = new GameObject(pName);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;

        // ── FIX: trigger + detector ──────────────────────────────────────────
        AddPocketTrigger(go);
        // ────────────────────────────────────────────────────────────────────

        CreateHole(go.transform);
        CreateRim(go.transform);

        float jH  = cushionHeight;
        float jY  = jH * 0.5f;
        float off = pocketRadius * 0.85f;

        CreateJaw(go.transform, "JawZ",
            new Vector3(signX * off, jY, 0f),
            new Vector3(jawThick, jH, jawLength),
            -signX * signZ * 45f);

        CreateJaw(go.transform, "JawX",
            new Vector3(0f, jY, signZ * off),
            new Vector3(jawLength, jH, jawThick),
             signX * signZ * 45f);
    }

    // ── Central ───────────────────────────────────────────────────────────────
    void CreateMiddlePocket(Transform parent, string pName,
                            Vector3 pos, float signZ)
    {
        GameObject go = new GameObject(pName);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;

        // ── FIX: trigger + detector ──────────────────────────────────────────
        AddPocketTrigger(go);
        // ────────────────────────────────────────────────────────────────────

        CreateHole(go.transform);
        CreateRim(go.transform);

        float jH  = cushionHeight;
        float jY  = jH * 0.5f;
        float off = pocketRadius * 0.9f;
        float ang = 30f;

        CreateJaw(go.transform, "JawL",
            new Vector3(-off, jY, 0f),
            new Vector3(jawThick, jH, jawLength),
             signZ * ang);

        CreateJaw(go.transform, "JawR",
            new Vector3( off, jY, 0f),
            new Vector3(jawThick, jH, jawLength),
            -signZ * ang);
    }

    // ── FIX: adiciona trigger zone e PocketDetector ───────────────────────────
    /// <summary>
    /// Adiciona ao GameObject da caçapa:
    ///   • SphereCollider isTrigger=true  → detecta a entrada das bolas
    ///   • PocketDetector                 → notifica o GameManager
    ///
    /// O collider não-trigger dos filhos (HoleDisk, HoleBottom) permanece
    /// para a física real das bolas.
    /// </summary>
    void AddPocketTrigger(GameObject pocketRoot)
    {
        // Garante que o GameObject tem a tag "Pocket" (opcional mas útil para debug)
        // pocketRoot.tag = "Pocket";  // descomente se criar essa tag no projeto

        SphereCollider trigger = pocketRoot.AddComponent<SphereCollider>();
        trigger.isTrigger = true;
        trigger.radius    = pocketRadius * pocketTriggerRadiusMultiplier;
        trigger.center    = new Vector3(0f, pocketTriggerCenterY, 0f);

        pocketRoot.AddComponent<PocketDetector>();
    }

    // ── Buraco: disco + tubo aberto + fundo ───────────────────────────────────
    void CreateHole(Transform parent)
    {
        // 1. Disco escuro — cobre o feltro e simula a abertura visual
        GameObject disk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        disk.name = "HoleDisk";
        disk.transform.SetParent(parent, false);
        disk.transform.localPosition = new Vector3(0f, 0.001f, 0f);
        disk.transform.localScale    = new Vector3(pocketRadius * 2f, 0.001f, pocketRadius * 2f);
        ApplyMat(disk, pocketMaterial, C_Pocket);

        // Remove o collider do disco — a detecção é feita pelo trigger no pai
        Collider diskCol = disk.GetComponent<Collider>();
        if (diskCol) Destroy(diskCol);

        // 2. Tubo aberto (mesh procedural, sem tampas) — dá profundidade ao buraco
        GameObject tube = new GameObject("HoleTube");
        tube.transform.SetParent(parent, false);
        tube.transform.localPosition = Vector3.zero;
        MeshFilter   mf = tube.AddComponent<MeshFilter>();
        MeshRenderer mr = tube.AddComponent<MeshRenderer>();
        mf.mesh     = BuildOpenCylinder(pocketRadius, pocketDepth, 32);
        mr.material = MakeDoubleSided(pocketMaterial, C_Pocket);

        // 3. Fundo do tubo
        GameObject bot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bot.name = "HoleBottom";
        bot.transform.SetParent(parent, false);
        bot.transform.localPosition = new Vector3(0f, -pocketDepth, 0f);
        bot.transform.localScale    = new Vector3(pocketRadius * 2f, 0.001f, pocketRadius * 2f);
        ApplyMat(bot, pocketMaterial, C_Pocket);

        // Remove o collider do fundo também
        Collider botCol = bot.GetComponent<Collider>();
        if (botCol) Destroy(botCol);
    }

    // ── Aro metálico ──────────────────────────────────────────────────────────
    void CreateRim(Transform parent)
    {
        GameObject rim = new GameObject("Rim");
        rim.transform.SetParent(parent, false);
        rim.transform.localPosition = new Vector3(0f, 0.002f, 0f);
        MeshFilter   mf = rim.AddComponent<MeshFilter>();
        MeshRenderer mr = rim.AddComponent<MeshRenderer>();
        mf.mesh     = BuildRing(pocketRadius, pocketRadius + rimExtra, 0.004f, 32);
        mr.material = metalMaterial != null ? metalMaterial : MakeMetal(C_Metal);
    }

    // ── Jaw angulado ──────────────────────────────────────────────────────────
    void CreateJaw(Transform parent, string jawName,
                   Vector3 localPos, Vector3 scale, float rotY)
    {
        GameObject jaw = GameObject.CreatePrimitive(PrimitiveType.Cube);
        jaw.name = jawName;
        jaw.transform.SetParent(parent, false);
        jaw.transform.localPosition    = localPos;
        jaw.transform.localScale       = scale;
        jaw.transform.localEulerAngles = new Vector3(0f, rotY, 0f);
        ApplyMat(jaw, cushionMaterial, C_Cushion);
    }

    // =========================================================================
    // PERNAS
    // =========================================================================
    void BuildLegs(Transform parent)
    {
        float hw = tableWidth  * 0.5f - legSize;
        float hd = tableDepth  * 0.5f - legSize;
        float yc = -tableThickness - legHeight * 0.5f;

        Vector3[] positions = {
            new Vector3(-hw, yc,  hd), new Vector3( hw, yc,  hd),
            new Vector3(-hw, yc, -hd), new Vector3( hw, yc, -hd),
        };

        for (int i = 0; i < positions.Length; i++)
        {
            GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
            leg.name = "Leg_" + (i + 1);
            leg.transform.SetParent(parent, false);
            leg.transform.localPosition = positions[i];
            leg.transform.localScale    = new Vector3(legSize, legHeight, legSize);
            ApplyMat(leg, woodMaterial, C_Wood);
        }
    }

    // =========================================================================
    // MESHES PROCEDURAIS
    // =========================================================================

    static Mesh BuildOpenCylinder(float radius, float height, int seg)
    {
        Mesh mesh = new Mesh { name = "OpenCylinder" };
        var verts = new Vector3[seg * 2];
        var uvs   = new Vector2[seg * 2];
        var tris  = new int[seg * 6];

        for (int i = 0; i < seg; i++)
        {
            float t   = (float)i / seg;
            float ang = t * Mathf.PI * 2f;
            float c   = Mathf.Cos(ang);
            float s   = Mathf.Sin(ang);

            verts[i]       = new Vector3(c * radius, 0f,      s * radius);
            verts[i + seg] = new Vector3(c * radius, -height, s * radius);
            uvs[i]         = new Vector2(t, 1f);
            uvs[i + seg]   = new Vector2(t, 0f);

            int b    = i * 6;
            int next = (i + 1) % seg;
            tris[b]     = i;
            tris[b + 1] = i + seg;
            tris[b + 2] = next + seg;
            tris[b + 3] = i;
            tris[b + 4] = next + seg;
            tris[b + 5] = next;
        }

        mesh.vertices  = verts;
        mesh.uv        = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    static Mesh BuildRing(float innerR, float outerR, float height, int seg)
    {
        Mesh mesh  = new Mesh { name = "Ring" };
        var verts  = new Vector3[seg * 4];
        var uvs    = new Vector2[seg * 4];

        for (int i = 0; i < seg; i++)
        {
            float t   = (float)i / seg;
            float ang = t * Mathf.PI * 2f;
            float c   = Mathf.Cos(ang);
            float s   = Mathf.Sin(ang);

            verts[i]           = new Vector3(c * innerR, height, s * innerR);
            verts[i + seg]     = new Vector3(c * outerR, height, s * outerR);
            verts[i + seg * 2] = new Vector3(c * innerR, 0f,     s * innerR);
            verts[i + seg * 3] = new Vector3(c * outerR, 0f,     s * outerR);
            uvs[i] = uvs[i + seg]         = new Vector2(t, 1f);
            uvs[i + seg * 2] = uvs[i + seg * 3] = new Vector2(t, 0f);
        }

        var tris = new System.Collections.Generic.List<int>(seg * 24);
        for (int i = 0; i < seg; i++)
        {
            int n = (i + 1) % seg;
            tris.Add(i);           tris.Add(i + seg);       tris.Add(n);
            tris.Add(n);           tris.Add(i + seg);       tris.Add(n + seg);
            tris.Add(i + seg);     tris.Add(i + seg * 3);   tris.Add(n + seg);
            tris.Add(n + seg);     tris.Add(i + seg * 3);   tris.Add(n + seg * 3);
            tris.Add(i + seg * 3); tris.Add(i + seg * 2);   tris.Add(n + seg * 3);
            tris.Add(n + seg * 3); tris.Add(i + seg * 2);   tris.Add(n + seg * 2);
        }

        mesh.vertices  = verts;
        mesh.uv        = uvs;
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        return mesh;
    }

    // =========================================================================
    // UTILITÁRIOS DE MATERIAL
    // =========================================================================
    void ApplyMat(GameObject go, Material mat, Color fallback)
    {
        Renderer r = go.GetComponent<Renderer>();
        if (r) r.material = mat != null ? mat : MakeColor(fallback);
    }

    static Material MakeColor(Color c)
    {
        var m = new Material(Shader.Find("Standard")) { color = c };
        m.SetFloat("_Metallic",   0f);
        m.SetFloat("_Glossiness", 0.25f);
        return m;
    }

    static Material MakeMetal(Color c)
    {
        var m = new Material(Shader.Find("Standard")) { color = c };
        m.SetFloat("_Metallic",   0.85f);
        m.SetFloat("_Glossiness", 0.75f);
        return m;
    }

    static Material MakeDoubleSided(Material src, Color fallback)
    {
        Material m = new Material(src != null ? src : MakeColor(fallback));
        m.SetInt("_Cull", (int)CullMode.Off);
        return m;
    }
}

// using UnityEngine;
// using UnityEngine.Rendering;

// /// <summary>
// /// Constrói uma mesa de sinuca com caçapas realistas:
// ///  - Disco escuro no tampo (simula a abertura)
// ///  - Tubo aberto sem tampa superior (buraco 3D visível)
// ///  - Aro metálico ao redor
// ///  - Dois "jaws" angulados em cada caçapa (criam dificuldade para entrar)
// ///
// /// Hierarquia:
// ///   Table
// ///    ├── TableSurface
// ///    ├── CushionTop / Bottom / Left / Right
// ///    ├── pocket
// ///    │    ├── Pocket_TL  → HoleDisk, HoleTube, HoleBottom, Rim, JawZ, JawX
// ///    │    ├── Pocket_TR  → ...
// ///    │    ├── Pocket_BL  → ...
// ///    │    ├── Pocket_BR  → ...
// ///    │    ├── Pocket_TM  → HoleDisk, HoleTube, HoleBottom, Rim, JawL, JawR
// ///    │    └── Pocket_BM  → ...
// ///    └── Leg_1..4
// /// </summary>
// public class SnookerTableBuilder : MonoBehaviour
// {
//     // ─── Mesa ─────────────────────────────────────────────────────────────────
//     [Header("Dimensões da Mesa (metros)")]
//     public float tableWidth     = 2.74f;
//     public float tableDepth     = 1.37f;
//     public float tableThickness = 0.10f;
//     public float legHeight      = 0.78f;
//     public float legSize        = 0.08f;

//     // ─── Tabelas ──────────────────────────────────────────────────────────────
//     [Header("Tabelas (Cushions)")]
//     public float cushionWidth  = 0.07f;
//     public float cushionHeight = 0.04f;

//     // ─── Caçapas ──────────────────────────────────────────────────────────────
//     [Header("Caçapas (Pockets)")]
//     [Tooltip("Raio interno do buraco")]
//     public float pocketRadius = 0.055f;
//     [Tooltip("Profundidade do tubo")]
//     public float pocketDepth  = 0.14f;
//     [Tooltip("Largura extra do aro metálico além do raio")]
//     public float rimExtra     = 0.012f;
//     [Tooltip("Comprimento de cada jaw")]
//     public float jawLength    = 0.09f;
//     [Tooltip("Espessura de cada jaw")]
//     public float jawThick     = 0.048f;

//     // ─── Materiais ────────────────────────────────────────────────────────────
//     [Header("Materiais (opcional — cores automáticas como fallback)")]
//     public Material feltMaterial;
//     public Material cushionMaterial;
//     public Material woodMaterial;
//     public Material pocketMaterial;
//     public Material metalMaterial;

//     // ── Cores padrão ──────────────────────────────────────────────────────────
//     static readonly Color C_Felt    = new Color(0.10f, 0.45f, 0.15f);
//     static readonly Color C_Cushion = new Color(0.07f, 0.36f, 0.09f);
//     static readonly Color C_Wood    = new Color(0.38f, 0.21f, 0.04f);
//     static readonly Color C_Pocket  = new Color(0.04f, 0.04f, 0.04f);
//     static readonly Color C_Metal   = new Color(0.60f, 0.60f, 0.65f);

//     // ─────────────────────────────────────────────────────────────────────────

//     void Start() => BuildTable();

//     [ContextMenu("Build Table")]
//     public void BuildTable()
//     {
//         Transform old = transform.Find("Table");
//         if (old) DestroyImmediate(old.gameObject);

//         GameObject table = new GameObject("Table");
//         table.transform.SetParent(transform, false);
//         // Pivot da mesa no nível do chão + altura das pernas
//         table.transform.localPosition = new Vector3(0f, legHeight, 0f);

//         BuildTableSurface(table.transform);
//         BuildCushion(table.transform, "CushionTop");
//         BuildCushion(table.transform, "CushionBottom");
//         BuildCushion(table.transform, "CushionLeft");
//         BuildCushion(table.transform, "CushionRight");
//         BuildPockets(table.transform);
//         BuildLegs(table.transform);

//         Debug.Log("[SnookerTableBuilder] Mesa criada com sucesso.");
//     }

//     // =========================================================================
//     // TAMPO
//     // =========================================================================
//     void BuildTableSurface(Transform parent)
//     {
//         GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
//         go.name = "TableSurface";
//         go.transform.SetParent(parent, false);
//         go.transform.localPosition = new Vector3(0f, -tableThickness * 0.5f, 0f);
//         go.transform.localScale    = new Vector3(tableWidth, tableThickness, tableDepth);
//         ApplyMat(go, feltMaterial, C_Felt);
//     }

//     // =========================================================================
//     // TABELAS
//     // =========================================================================
//     void BuildCushion(Transform parent, string name)
//     {
//         GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
//         go.name = name;
//         go.transform.SetParent(parent, false);
//         float hw = tableWidth  * 0.5f;
//         float hd = tableDepth  * 0.5f;
//         float yc = cushionHeight * 0.5f;

//         switch (name)
//         {
//             case "CushionTop":
//                 go.transform.localPosition = new Vector3(0f,  yc,  hd + cushionWidth * 0.5f);
//                 go.transform.localScale    = new Vector3(tableWidth, cushionHeight, cushionWidth);
//                 break;
//             case "CushionBottom":
//                 go.transform.localPosition = new Vector3(0f,  yc, -hd - cushionWidth * 0.5f);
//                 go.transform.localScale    = new Vector3(tableWidth, cushionHeight, cushionWidth);
//                 break;
//             case "CushionLeft":
//                 go.transform.localPosition = new Vector3(-hw - cushionWidth * 0.5f, yc, 0f);
//                 go.transform.localScale    = new Vector3(cushionWidth, cushionHeight, tableDepth);
//                 break;
//             case "CushionRight":
//                 go.transform.localPosition = new Vector3( hw + cushionWidth * 0.5f, yc, 0f);
//                 go.transform.localScale    = new Vector3(cushionWidth, cushionHeight, tableDepth);
//                 break;
//         }
//         ApplyMat(go, cushionMaterial, C_Cushion);
//     }

//     // =========================================================================
//     // CAÇAPAS
//     // =========================================================================
//     void BuildPockets(Transform parent)
//     {
//         GameObject root = new GameObject("pocket");
//         root.transform.SetParent(parent, false);
//         root.transform.localPosition = Vector3.zero;

//         float hw = tableWidth  * 0.5f;
//         float hd = tableDepth  * 0.5f;

//         // Cantos: signX e signZ definem qual canto (cada um é +1 ou -1)
//         CreateCornerPocket(root.transform, "Pocket_TL", new Vector3(-hw, 0f,  hd), -1f,  1f);
//         CreateCornerPocket(root.transform, "Pocket_TR", new Vector3( hw, 0f,  hd),  1f,  1f);
//         CreateCornerPocket(root.transform, "Pocket_BL", new Vector3(-hw, 0f, -hd), -1f, -1f);
//         CreateCornerPocket(root.transform, "Pocket_BR", new Vector3( hw, 0f, -hd),  1f, -1f);

//         // Centrais: signZ = +1 (topo) ou -1 (base)
//         CreateMiddlePocket(root.transform, "Pocket_TM", new Vector3(0f, 0f,  hd),  1f);
//         CreateMiddlePocket(root.transform, "Pocket_BM", new Vector3(0f, 0f, -hd), -1f);
//     }

//     // ── Canto ─────────────────────────────────────────────────────────────────
//     void CreateCornerPocket(Transform parent, string pName,
//                             Vector3 pos, float signX, float signZ)
//     {
//         GameObject go = new GameObject(pName);
//         go.transform.SetParent(parent, false);
//         go.transform.localPosition = pos;

//         CreateHole(go.transform);
//         CreateRim(go.transform);

//         // Os ângulos dos jaws são derivados da geometria do canto:
//         //   Jaw no eixo-Z (borda X)  →  rotY = -signX*signZ * 45°
//         //   Jaw no eixo-X (borda Z)  →  rotY =  signX*signZ * 45°
//         float jH  = cushionHeight;
//         float jY  = jH * 0.5f;
//         float off = pocketRadius * 0.85f;

//         CreateJaw(go.transform, "JawZ",
//             new Vector3(signX * off, jY, 0f),
//             new Vector3(jawThick, jH, jawLength),
//             -signX * signZ * 45f);

//         CreateJaw(go.transform, "JawX",
//             new Vector3(0f, jY, signZ * off),
//             new Vector3(jawLength, jH, jawThick),
//              signX * signZ * 45f);
//     }

//     // ── Central (lateral) ─────────────────────────────────────────────────────
//     void CreateMiddlePocket(Transform parent, string pName,
//                             Vector3 pos, float signZ)
//     {
//         GameObject go = new GameObject(pName);
//         go.transform.SetParent(parent, false);
//         go.transform.localPosition = pos;

//         CreateHole(go.transform);
//         CreateRim(go.transform);

//         float jH  = cushionHeight;
//         float jY  = jH * 0.5f;
//         float off = pocketRadius * 0.9f;
//         float ang = 30f; // abertura levemente maior que nos cantos

//         CreateJaw(go.transform, "JawL",
//             new Vector3(-off, jY, 0f),
//             new Vector3(jawThick, jH, jawLength),
//              signZ * ang);

//         CreateJaw(go.transform, "JawR",
//             new Vector3( off, jY, 0f),
//             new Vector3(jawThick, jH, jawLength),
//             -signZ * ang);
//     }

//     // ── Buraco: disco + tubo aberto + fundo ───────────────────────────────────
//     void CreateHole(Transform parent)
//     {
//         // 1. Disco escuro no nível do tampo — cobre o feltro e simula a abertura
//         GameObject disk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
//         disk.name = "HoleDisk";
//         disk.transform.SetParent(parent, false);
//         disk.transform.localPosition = new Vector3(0f, 0.001f, 0f); // ligeiramente acima do tampo
//         disk.transform.localScale    = new Vector3(pocketRadius * 2f, 0.001f, pocketRadius * 2f);
//         ApplyMat(disk, pocketMaterial, C_Pocket);

//         // 2. Tubo aberto (mesh procedural, sem tampas) — dá profundidade ao buraco
//         GameObject tube = new GameObject("HoleTube");
//         tube.transform.SetParent(parent, false);
//         tube.transform.localPosition = Vector3.zero;

//         MeshFilter   mf = tube.AddComponent<MeshFilter>();
//         MeshRenderer mr = tube.AddComponent<MeshRenderer>();
//         mf.mesh = BuildOpenCylinder(pocketRadius, pocketDepth, 32);

//         // Material dupla-face: a parede interna fica visível de cima
//         mr.material = MakeDoubleSided(pocketMaterial, C_Pocket);

//         // 3. Fundo do tubo
//         GameObject bot = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
//         bot.name = "HoleBottom";
//         bot.transform.SetParent(parent, false);
//         bot.transform.localPosition = new Vector3(0f, -pocketDepth, 0f);
//         bot.transform.localScale    = new Vector3(pocketRadius * 2f, 0.001f, pocketRadius * 2f);
//         ApplyMat(bot, pocketMaterial, C_Pocket);
//     }

//     // ── Aro metálico ──────────────────────────────────────────────────────────
//     void CreateRim(Transform parent)
//     {
//         GameObject rim = new GameObject("Rim");
//         rim.transform.SetParent(parent, false);
//         rim.transform.localPosition = new Vector3(0f, 0.002f, 0f);

//         MeshFilter   mf = rim.AddComponent<MeshFilter>();
//         MeshRenderer mr = rim.AddComponent<MeshRenderer>();
//         mf.mesh = BuildRing(pocketRadius, pocketRadius + rimExtra, 0.004f, 32);

//         Material m = metalMaterial != null ? metalMaterial : MakeMetal(C_Metal);
//         mr.material = m;
//     }

//     // ── Jaw angulado ──────────────────────────────────────────────────────────
//     void CreateJaw(Transform parent, string jawName,
//                    Vector3 localPos, Vector3 scale, float rotY)
//     {
//         GameObject jaw = GameObject.CreatePrimitive(PrimitiveType.Cube);
//         jaw.name = jawName;
//         jaw.transform.SetParent(parent, false);
//         jaw.transform.localPosition    = localPos;
//         jaw.transform.localScale       = scale;
//         jaw.transform.localEulerAngles = new Vector3(0f, rotY, 0f);
//         ApplyMat(jaw, cushionMaterial, C_Cushion);
//     }

//     // =========================================================================
//     // PERNAS
//     // =========================================================================
//     void BuildLegs(Transform parent)
//     {
//         float hw = tableWidth  * 0.5f - legSize;
//         float hd = tableDepth  * 0.5f - legSize;
//         float yc = -tableThickness - legHeight * 0.5f;

//         Vector3[] positions = {
//             new Vector3(-hw, yc,  hd), new Vector3( hw, yc,  hd),
//             new Vector3(-hw, yc, -hd), new Vector3( hw, yc, -hd),
//         };

//         for (int i = 0; i < positions.Length; i++)
//         {
//             GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Cube);
//             leg.name = "Leg_" + (i + 1);
//             leg.transform.SetParent(parent, false);
//             leg.transform.localPosition = positions[i];
//             leg.transform.localScale    = new Vector3(legSize, legHeight, legSize);
//             ApplyMat(leg, woodMaterial, C_Wood);
//         }
//     }

//     // =========================================================================
//     // MESHES PROCEDURAIS
//     // =========================================================================

//     /// <summary>
//     /// Cilindro sem tampas (apenas as paredes laterais).
//     /// O winding aponta para DENTRO — face interior visível de cima.
//     /// </summary>
//     static Mesh BuildOpenCylinder(float radius, float height, int seg)
//     {
//         Mesh mesh = new Mesh { name = "OpenCylinder" };

//         var verts = new Vector3[seg * 2];
//         var uvs   = new Vector2[seg * 2];
//         var tris  = new int[seg * 6];

//         for (int i = 0; i < seg; i++)
//         {
//             float t   = (float)i / seg;
//             float ang = t * Mathf.PI * 2f;
//             float c   = Mathf.Cos(ang);
//             float s   = Mathf.Sin(ang);

//             // Anel superior (Y = 0) e anel inferior (Y = -height)
//             verts[i]       = new Vector3(c * radius, 0f,      s * radius);
//             verts[i + seg] = new Vector3(c * radius, -height, s * radius);
//             uvs[i]         = new Vector2(t, 1f);
//             uvs[i + seg]   = new Vector2(t, 0f);

//             int b    = i * 6;
//             int next = (i + 1) % seg;

//             // Winding inverso → face interna do tubo visível
//             tris[b]     = i;
//             tris[b + 1] = i + seg;
//             tris[b + 2] = next + seg;
//             tris[b + 3] = i;
//             tris[b + 4] = next + seg;
//             tris[b + 5] = next;
//         }

//         mesh.vertices  = verts;
//         mesh.uv        = uvs;
//         mesh.triangles = tris;
//         mesh.RecalculateNormals();
//         mesh.RecalculateBounds();
//         return mesh;
//     }

//     /// <summary>
//     /// Anel plano (annulus) com face superior, inferior e laterais interna/externa.
//     /// </summary>
//     static Mesh BuildRing(float innerR, float outerR, float height, int seg)
//     {
//         Mesh mesh = new Mesh { name = "Ring" };

//         // 4 aros de vértices: inner-top(0), outer-top(seg), inner-bot(2*seg), outer-bot(3*seg)
//         var verts = new Vector3[seg * 4];
//         var uvs   = new Vector2[seg * 4];

//         for (int i = 0; i < seg; i++)
//         {
//             float t   = (float)i / seg;
//             float ang = t * Mathf.PI * 2f;
//             float c   = Mathf.Cos(ang);
//             float s   = Mathf.Sin(ang);

//             verts[i]           = new Vector3(c * innerR, height, s * innerR);
//             verts[i + seg]     = new Vector3(c * outerR, height, s * outerR);
//             verts[i + seg * 2] = new Vector3(c * innerR, 0f,     s * innerR);
//             verts[i + seg * 3] = new Vector3(c * outerR, 0f,     s * outerR);
//             uvs[i]             = uvs[i + seg] = new Vector2(t, 1f);
//             uvs[i + seg * 2]   = uvs[i + seg * 3] = new Vector2(t, 0f);
//         }

//         var tris = new System.Collections.Generic.List<int>(seg * 24);
//         for (int i = 0; i < seg; i++)
//         {
//             int n = (i + 1) % seg;
//             // Face superior (anel visível de cima)
//             tris.Add(i);         tris.Add(i + seg);     tris.Add(n);
//             tris.Add(n);         tris.Add(i + seg);     tris.Add(n + seg);
//             // Lateral externa
//             tris.Add(i + seg);   tris.Add(i + seg * 3); tris.Add(n + seg);
//             tris.Add(n + seg);   tris.Add(i + seg * 3); tris.Add(n + seg * 3);
//             // Face inferior
//             tris.Add(i + seg * 3); tris.Add(i + seg * 2); tris.Add(n + seg * 3);
//             tris.Add(n + seg * 3); tris.Add(i + seg * 2); tris.Add(n + seg * 2);
//         }

//         mesh.vertices  = verts;
//         mesh.uv        = uvs;
//         mesh.triangles = tris.ToArray();
//         mesh.RecalculateNormals();
//         mesh.RecalculateBounds();
//         return mesh;
//     }

//     // =========================================================================
//     // UTILITÁRIOS DE MATERIAL
//     // =========================================================================
//     void ApplyMat(GameObject go, Material mat, Color fallback)
//     {
//         Renderer r = go.GetComponent<Renderer>();
//         if (r) r.material = mat != null ? mat : MakeColor(fallback);
//     }

//     static Material MakeColor(Color c)
//     {
//         var m = new Material(Shader.Find("Standard")) { color = c };
//         m.SetFloat("_Metallic",   0f);
//         m.SetFloat("_Glossiness", 0.25f);
//         return m;
//     }

//     static Material MakeMetal(Color c)
//     {
//         var m = new Material(Shader.Find("Standard")) { color = c };
//         m.SetFloat("_Metallic",   0.85f);
//         m.SetFloat("_Glossiness", 0.75f);
//         return m;
//     }

//     /// <summary>
//     /// Copia um material e desativa o face-culling (Cull Off),
//     /// tornando ambas as faces do triângulo visíveis.
//     /// Isso garante que a parede interna do tubo seja renderizada.
//     /// </summary>
//     static Material MakeDoubleSided(Material src, Color fallback)
//     {
//         Material m = new Material(src != null ? src : MakeColor(fallback));
//         m.SetInt("_Cull", (int)CullMode.Off);
//         return m;
//     }
// }