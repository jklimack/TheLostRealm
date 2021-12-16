using System.Collections;
using System.Collections.Generic;
using UnityEngine;



public class PatrolState : State
{
    private Vector3 nextPosition;

    private Animator animator;
    private string animName = "Walk";
    private float Distance;
    private const int PATROL_RADIUS = 50;
    private const int MIN_DIST = 10;
    private Vector3[] wayPointsArr;
    private int wayPointsCounter;
    float step;
    private Vector3 previousPosition;
    private int framesInTheSamePosition = 0;


    public PatrolState(Character character) : base(character){
    }


    // Enter in the state
    public override void OnStateEnter()
    {
        Debug.Log("Entering Patrol State...");
        step = character.speed * Time.deltaTime;
        if (character.getCharacterName() != "Boss")
        {
            ChooseNextPosition();
            calculateWayPoints();
            wayPointsCounter = 0;
            character.getAnimator().SetInteger("AnimState", 2); // Run
        }
        else
        {
            character.getAnimator().SetInteger("AnimState", 0); // Combate Idle
        }
    
    }

    // Update is called once per frame
    public override void Execute()
    {
        PlayerInRange();
        if (Distance < MIN_DIST)
        {
            // If player in range --> change state
            character.SetState(new PursueState(character));
        }
        if (character.getCharacterName() != "Boss")
        {
        // NPC reached the random patrol point --> choose a new one       
            if (wayPointsCounter > wayPointsArr.Length - 1)
            {
                ChooseNextPosition();
                calculateWayPoints();
                wayPointsCounter = 0;
                previousPosition = new Vector3(character.transform.position.x, character.transform.position.y, character.transform.position.z);
                framesInTheSamePosition = 0;
            }
            else
            {
                followTheWayPoints(wayPointsArr, wayPointsCounter);
                bool reachedNextPoint = Mathf.Abs(character.transform.position.x - wayPointsArr[wayPointsCounter].x) < 0.5 &&
                    Mathf.Abs(character.transform.position.y - wayPointsArr[wayPointsCounter].y) < 0.5;

                if (previousPosition.Equals(new Vector3(character.transform.position.x, character.transform.position.y, character.transform.position.z)))
                {
                    framesInTheSamePosition++;
                }

                if (reachedNextPoint)
                {
                    wayPointsCounter++;

                }
                else if (framesInTheSamePosition > 3)
                {
                    //Debug.LogError("Stuck in collision");
                    ChooseNextPosition();
                    calculateWayPoints();
                    wayPointsCounter = 0;
                    framesInTheSamePosition = 0;
                }
            }
        }
        
    }

    private void calculateWayPoints()
    {
        Vector2Int initPoint = new Vector2Int(Mathf.RoundToInt(character.transform.position.x), Mathf.RoundToInt(character.transform.position.y));
        Vector2Int targetPoint = new Vector2Int(Mathf.RoundToInt(nextPosition.x), Mathf.RoundToInt(nextPosition.y));

        List<Vector2Int> wayPoints = character.getGridManager().computeWayPoints(initPoint, targetPoint);
        if (wayPoints.Count >= 2) {
            wayPoints.Reverse();
            wayPoints = wayPoints.GetRange(1, wayPoints.Count - 2);
            
        }
        wayPointsArr = new Vector3[wayPoints.Count];
        int i = 0;
        foreach (Vector2Int wayPoint in wayPoints)
        {
            wayPointsArr[i] = new Vector3(wayPoint.x, wayPoint.y, character.transform.position.z);
            i++;
        }

    }

    public override void OnStateExit()
    {
        character.getAnimator().SetInteger("AnimState", 0);
    }


    private void ChooseNextPosition() {
        Vector2 finalPos;
        bool newPositionValid;
        do
        {
            //newPositionValid = true;
            Vector2 randPos = new Vector2(Random.Range(-5f, 5f), Random.Range(-5f, 5f));
            // for random positive- negative: (Random.Range(0, 2) * 2 - 1)
            finalPos = character.transform.position + new Vector3(randPos.x, randPos.y, 0);
            /*List<Vector2> positionsToCheck = new List<Vector2>() {
                new Vector2((int)finalPos.x - 1, (int)finalPos.y - 1),
                new Vector2((int)finalPos.x - 1, (int)finalPos.y),
                new Vector2((int)finalPos.x, (int)finalPos.y - 1),
                new Vector2((int)finalPos.x, (int)finalPos.y),
                new Vector2((int)finalPos.x + 1, (int)finalPos.y),
                new Vector2((int)finalPos.x, (int)finalPos.y + 1),
                new Vector2((int)finalPos.x + 1, (int)finalPos.y + 1)

            };
            foreach(Vector2 position in positionsToCheck)
            {
                if (!character.getGridManager().isPointValid((int)position.x, (int)position.y))
                {
                    newPositionValid = false;
                    break;
                }
            }*/
            //newPositionValid = false;

            newPositionValid = character.getGridManager().isPointValid((int)finalPos.x, (int)finalPos.y);

            } while (!newPositionValid);

        nextPosition = finalPos;

    }

    public void PlayerInRange()
    {
        Vector3 playerPosition = character.getPlayer().transform.position;
        Distance = Vector3.Distance(character.transform.position, playerPosition);
    }

    public void followTheWayPoints(Vector3[] wayPoints, int counter)
    {
        Vector2 target = new Vector2(wayPoints[counter].x, wayPoints[counter].y);
        Vector2 now = new Vector2(character.transform.position.x, character.transform.position.y);
        character.transform.position = Vector2.MoveTowards(now, target, 1.5f*Time.deltaTime);
    }

}
