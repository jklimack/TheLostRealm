using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PursueState : State
{
    private Transform target;

    public PursueState(Character character) : base(character)
    {
    }

    public override void OnStateEnter()
    {
        Debug.Log("Entering Pursue State...");
        target = GameObject.FindGameObjectWithTag("Player").transform;
    }

    public override void Execute()
    {

        if (!Player_in_range())
        {
            character.SetState(new PatrolState(character));
        }

    }

    public override void OnStateExit()
    {
    }

   


    public bool Player_in_range()
    {
        Vector3 playerPosition = GameObject.FindGameObjectWithTag("Player").transform.position;
        float distance = Vector3.Distance(character.transform.position, playerPosition);
        if (distance < 10.00)
            return true;
        else
            return false;
    }
}
