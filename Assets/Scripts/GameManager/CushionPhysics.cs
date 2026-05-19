using UnityEngine;

public class CushionPhysics : MonoBehaviour
{
    [Tooltip("Arraste aqui os GameObjects das tabelas (cushions) da mesa.")]
    public GameObject[] cushions;

    [Range(0f, 1f)]
    public float bounciness = 0.75f;

    private void Start()
    {
        if (cushions == null || cushions.Length == 0)
        {
            Debug.LogWarning("[CushionPhysics] Nenhuma cushion atribuída no Inspector.");
            return;
        }

        var mat = new PhysicsMaterial("CushionBounce")
        {
            bounciness      = bounciness,
            bounceCombine   = PhysicsMaterialCombine.Maximum,
            frictionCombine = PhysicsMaterialCombine.Minimum,
            staticFriction  = 0.1f,
            dynamicFriction = 0.1f
        };

        foreach (GameObject go in cushions)
        {
            if (go == null) continue;
            foreach (Collider col in go.GetComponentsInChildren<Collider>())
            {
                if (!col.isTrigger)
                    col.sharedMaterial = mat;
            }
        }
    }
}
