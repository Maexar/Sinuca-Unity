using TMPro;
using UnityEngine;

public class TurnManagerUI : MonoBehaviour
{
    public TextMeshProUGUI jogador1Text;
    public TextMeshProUGUI jogador2Text;

    public Color corAtivo   = Color.white;
    public Color corInativo = new Color(1f, 1f, 1f, 0.4f);

    private void OnEnable()  => TurnManager.OnTurnChanged += Refresh;
    private void OnDisable() => TurnManager.OnTurnChanged -= Refresh;
    private void Start()     => Refresh();

    private void Refresh()
    {
        if (TurnManager.Instance == null) return;

        int current = TurnManager.Instance.CurrentPlayer;
        int[] scores = TurnManager.Instance.Scores;

        if (jogador1Text != null)
        {
            jogador1Text.text  = $"Jogador 1: {scores[0]}";
            jogador1Text.color = current == 0 ? corAtivo : corInativo;
        }

        if (jogador2Text != null)
        {
            jogador2Text.text  = $"Jogador 2: {scores[1]}";
            jogador2Text.color = current == 1 ? corAtivo : corInativo;
        }
    }
}
