using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PursueState : State
{
    private Transform target;
    private float Distance;
    private const float ATTACK_DIST = 2f;
    private const int MIN_DIST = 10;
    private Vector3[] wayPointsArr;
    private int wayPointsCounter;
    private Vector3 previousPosition;
    private int framesInTheSamePosition = 0;


    public PursueState(Character character) : base(character)
    {
    }

    public override void OnStateEnter()
    {
        character.getAnimator().SetInteger("AnimState", 2);
        Debug.Log("Entering Pursue State...");
        target = GameObject.FindGameObjectWithTag("Player").transform;
        wayPointsCounter = 0;
        calculateWayPoints();
    }

    public override void Execute()
    {
        PlayerInRange();

        if (Distance < ATTACK_DIST) {
            character.SetState(new AttackState(character));
        }
        else if (Distance <= MIN_DIST)
        {
            if (wayPointsCounter >= wayPointsArr.Length - 1)
            {
                //Debug.Log("Choose Next Patrol Position");
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
                    calculateWayPoints();
                    wayPointsCounter = 0;
                    framesInTheSamePosition = 0;
                }
            }


        }
        else
        {
            character.SetState(new PatrolState(character));
        }
        target = GameObject.FindGameObjectWithTag("Player").transform;
    }

    public override void OnStateExit()
    {
        character.getAnimator().SetInteger("AnimState", 0);
    }

    private void calculateWayPoints()
    {
        // move to patrol next position
        Vector2Int initPoint = new Vector2Int(Mathf.RoundToInt(character.transform.position.x), Mathf.RoundToInt(character.transform.position.y));
        Vector2Int targetPoint = new Vector2Int(Mathf.RoundToInt(target.position.x), Mathf.RoundToInt(target.position.y));

        List<Vector2Int> wayPoints = character.getGridManager().computeWayPoints(initPoint, targetPoint);
        if (wayPoints.Count >= 2)
        {
            wayPoints.Reverse();
            wayPoints = wayPoints.GetRange(1, wayPoints.Count - 2);

        }
        wayPointsArr = new Vector3[wayPoints.Count];
        int i = 0;
        foreach (Vector2Int wayPoint in wayPoints)
        {
            wayPointsArr[i] = new Vector3(wayPoint.x, wayPoint.y, character.transform.position.z);
            //Debug.Log("Going to " + wayPoint.x + ", " + wayPoint.y + ", " + character.transform.position.z);
            i++;
        }
    }


    public void followTheWayPoints(Vector3[] wayPoints, int counter)
    {
        Vector2 target = new Vector2(wayPoints[counter].x, wayPoints[counter].y);
        Vector2 now = new Vector2(character.transform.position.x, character.transform.position.y);
        character.transform.position = Vector2.MoveTowards(now, target, 1.5f*Time.deltaTime);
    }


    public void PlayerInRange()
    {
        Vector3 playerPosition = character.getPlayer().transform.position;
        Distance = Vector3.Distance(character.transform.position, playerPosition);
    }
}
