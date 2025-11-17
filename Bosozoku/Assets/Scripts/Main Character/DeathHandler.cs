using UnityEngine;
using UnityEngine.SceneManagement;

public class DeathHandler : MonoBehaviour
{


    private void Start()
    {

    }

    public void HandleDeath()
    {
        SceneManager.LoadSceneAsync("MainMenu");
        Time.timeScale = 0;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

}
