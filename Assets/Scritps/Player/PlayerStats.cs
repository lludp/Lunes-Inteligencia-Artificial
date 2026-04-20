using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerStats : MonoBehaviour
{
    public string loseSceneName = "LoseScene"; 

    public void Die()
    {
        Debug.Log("Jugador alcanzado");

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        SceneManager.LoadScene(loseSceneName);
    }
}