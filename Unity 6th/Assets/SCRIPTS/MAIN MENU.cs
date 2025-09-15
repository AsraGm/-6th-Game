using UnityEngine;
using UnityEngine.SceneManagement;

public class MAINMENU : MonoBehaviour
{
    public void GameStarted()
    {
        Time.timeScale = 1f;
        Debug.Log("Entering Level Selection");
        SceneManager.LoadScene("LEVEL SELECTION");
    }

    public void Store()
    {
        Time.timeScale = 1f;
        Debug.Log("Entering Store");
        SceneManager.LoadScene("STORE");
    }
    public void Exit()
    {
        Time.timeScale = 1f;
        Debug.Log("Entering Credits");
        SceneManager.LoadScene("CREDITS");
    }
    public void ArtGallery()
    {
        Time.timeScale = 1f;
        Debug.Log("Entering Art Gallery");
        SceneManager.LoadScene("ART GALLERY");
    }
}
