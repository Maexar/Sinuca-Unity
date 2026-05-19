using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class WinnerScreen : MonoBehaviour
{
    [Header("Referências")]
    public GameObject panel;
    public TextMeshProUGUI winnerText;

    private void OnEnable()  => TurnManager.OnGameOver += Show;
    private void OnDisable() => TurnManager.OnGameOver -= Show;

    private void Show(int winner)
    {
        Time.timeScale = 0f;
        panel.SetActive(true);

        winnerText.text = winner switch
        {
            0  => "Jogador 1 venceu!",
            1  => "Jogador 2 venceu!",
            _  => "Empate!"
        };
    }

    public void OnClickJogarNovamente()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void OnClickMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}
