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
    }

    public void choiceNormal()
    {
        difficultyFactor = 1.2f;
    }

    public void choiceHard()
    {
        difficultyFactor = 1.4f;
    }

    public void playGame(){
        SceneManager.LoadScene(1);

    }
}
