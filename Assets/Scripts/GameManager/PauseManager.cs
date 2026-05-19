using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

public class PauseManager : MonoBehaviour
{
    [Header("Referência")]
    [Tooltip("O painel de pause (Canvas > PausePanel)")]
    public GameObject pausePanel;

    private bool _isPaused = false;

    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (_isPaused)
                Continuar();
            else
                Pausar();
        }
    }

    public void Pausar()
    {
        _isPaused = true;
        Time.timeScale = 0f;        // congela física e animações
        pausePanel.SetActive(true);
    }

    public void Continuar()
    {
        _isPaused = false;
        Time.timeScale = 1f;
        pausePanel.SetActive(false);
    }

    public void VoltarAoMenu()
    {
        Time.timeScale = 1f;        // restaura antes de trocar de cena
        SceneManager.LoadScene("MainMenu");
    }
}