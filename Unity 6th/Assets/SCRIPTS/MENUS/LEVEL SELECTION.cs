using UnityEngine;
using UnityEngine.SceneManagement;

public class LEVELSELECTION: MonoBehaviour
{
    public void L1()
    {
        Time.timeScale = 1f;
        Debug.Log("Entering Level Selection");
        SceneManager.LoadScene("LEVEL 1");
    }
    public void L2()
    {
        Time.timeScale = 1f;
        Debug.Log("Entering Level Selection");
        SceneManager.LoadScene("LEVEL 2");
    }
    public void L3()
    {
        Time.timeScale = 1f;
        Debug.Log("Entering Level Selection");
        SceneManager.LoadScene("LEVEL 3");
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
