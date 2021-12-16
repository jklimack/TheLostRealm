using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuScript : MonoBehaviour
{
    public Button difficultyEasy;
    public Button difficultyNormal;
    public Button difficultyHard;

    public static float difficultyFactor = 1.0f;

    void Start()
    {
        difficultyEasy.Select();
    }

    public void choiceEasy()
    {
        difficultyFactor = 1.0f;
        difficultyEasy.gameObject.SetActive(true);
        difficultyNormal.gameObject.SetActive(false);
        difficultyHard.gameObject.SetActive(false);
    }

    public void choiceNormal()
    {
        difficultyFactor = 1.2f;
        difficultyEasy.gameObject.SetActive(false);
        difficultyNormal.gameObject.SetActive(true);
        difficultyHard.gameObject.SetActive(false);
    }

    public void choiceHard()
    {
        difficultyFactor = 1.4f;
        difficultyEasy.gameObject.SetActive(false);
        difficultyNormal.gameObject.SetActive(false);
        difficultyHard.gameObject.SetActive(true);
    }

    public void playGame(){
        SceneManager.LoadScene(1);

    }
}
