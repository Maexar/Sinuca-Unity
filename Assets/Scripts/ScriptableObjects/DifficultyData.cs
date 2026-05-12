using UnityEngine;

[CreateAssetMenu(fileName = "DifficultyData", menuName = "Sinuca/DifficultyData")]
public class DifficultyData : ScriptableObject
{
    [Header("Precisão da IA (0 = ruim, 1 = perfeita)")]
    [Range(0f, 1f)] public float aimAccuracy = 0.5f;

    [Header("Assistência ao Jogador")]
    public bool showAimGuide = true;
    public bool showBallPrediction = false;
    public bool showPocketIndicator = false;

    [Header("Comportamento da IA")]
    public float thinkingTime = 2f;
    public bool useStrategicPlay = false;
}