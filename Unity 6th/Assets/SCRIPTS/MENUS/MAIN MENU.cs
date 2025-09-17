using UnityEngine;
using UnityEngine.SceneManagement;

public class MAINMENU : MonoBehaviour
{
    public void GAMESTARTED()
    {
        Time.timeScale = 1f;
        Debug.Log("Entering Level Selection");
        SceneManager.LoadScene("LEVEL SELECTION");
    }

    public void STORE()
    {
        Time.timeScale = 1f;
        Debug.Log("Entering Store");
        SceneManager.LoadScene("STORE");
    }
    public void EXIT()
    {
        Time.timeScale = 1f;
        Debug.Log("Entering Credits");
        SceneManager.LoadScene("CREDITS");
    }
    public void ARTGALLERY()
    {
        Time.timeScale = 1f;
        Debug.Log("Entering Art Gallery");
        SceneManager.LoadScene("ART GALLERY");
    }
    public void MENU()
    {
        Time.timeScale = 1f;
        Debug.Log("Entering Main Menu");
        SceneManager.LoadScene("MAIN MENU");
    }
    public void EXITS()
    {
        Application.Quit();
    }
}
