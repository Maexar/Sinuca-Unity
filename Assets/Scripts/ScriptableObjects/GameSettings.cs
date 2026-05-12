using UnityEngine;

[CreateAssetMenu(fileName = "GameSettings", menuName = "Sinuca/GameSettings")]
public class GameSettings : ScriptableObject
{
    [Header("Dificuldade")]
    public DifficultyLevel difficulty = DifficultyLevel.Medium;

    [Header("Física das Bolas")]
    public float ballMass = 0.17f;
    public float ballDrag = 0.3f;
    public float ballAngularDrag = 0.5f;
    public float rollingFriction = 0.02f;
    public float restitution = 0.7f;

    [Header("Taco")]
    public float maxForce = 20f;
    public float minForce = 1f;
}

public enum DifficultyLevel
{
    Easy,
    Medium,
    Hard
}