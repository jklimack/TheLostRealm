using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DeadState :State
{
    private GameObject[] characterInstances;
    private Vector2 winningPosition;

    public DeadState(Character character)  : base(character)
    {
    }

    public override void OnStateEnter()
    {
        Debug.Log("Entering Dead State...");
        character.getAnimator().SetBool("Death", true);
        if (character.getCharacterName() == "Boss")
        {
            winningPosition = GameObject.Find("Grid").GetComponent<MapGenerator>().getCastlePos();
            character.StartCoroutine(waitUntilAnimationIsFinishedAndEndGame());

        }
        else
        {
            characterInstances = GameObject.Find("Grid").GetComponent<MapGenerator>().GetCharacterInstances();
            int index = character.getCharacterName().IndexOf("_");
            int key = int.Parse(character.getCharacterName().Substring(index + 1));
            //Debug.Log("For character " + character.getCharacterName() + " the key is: " + key);
            GameObject game = GameObject.Find("Bandit_" + key);
            character.StartCoroutine(waitUntilAnimationIsFinished(game, key));
        }

        
    }

    public override void Execute()
    {
        if (character.getCharacterName() == "Boss")
            character.StartCoroutine(waitUntilAnimationIsFinishedAndEndGame());
    
    }

    public override void OnStateExit()
    {
    }

    private IEnumerator waitUntilAnimationIsFinished(GameObject game, int key = 0)
    { 
        yield return new WaitForSeconds(3);
        UnityEngine.Object.Destroy(game);

        if (key != 0) characterInstances[key] = null;
    }

    private IEnumerator waitUntilAnimationIsFinishedAndEndGame()
    {
        yield return new WaitForSeconds(1.5f);
        {
            Vector3 playerPosition = GameObject.FindGameObjectWithTag("Player").transform.position;
            float Distance = Vector2.Distance(winningPosition, new Vector2(playerPosition.x, playerPosition.y));
            if (Distance < 1.5f)
                //UnityEngine.Object.Destroy(game);
                //yield return new WaitForSeconds(3);
                SceneManager.LoadScene(2); // Victory = 2, GameOver = 3
        }

    }
}
