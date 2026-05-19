using UnityEngine;
using UnityEngine.SceneManagement;
 
public class MenuManager : MonoBehaviour
{
    public void OnClickJogar()
    {
        Debug.Log("[MenuManager] OnClickJogar chamado");
        SceneManager.LoadScene(1);
    }
 
    public void OnClickSair()
    {
        Application.Quit();
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}