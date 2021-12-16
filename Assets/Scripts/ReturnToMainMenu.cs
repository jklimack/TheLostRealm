using UnityEngine;
using UnityEngine.SceneManagement;

public class ReturnToMainMenu : MonoBehaviour
{

    public void returnToMainMenu()
    {
        SceneManager.LoadScene(0);
    }
}
