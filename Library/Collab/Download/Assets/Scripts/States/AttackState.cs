using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AttackState : State
{
    private float Distance;
    private const float ATTACK_DIST = 2f;
    private const float ATTACK_TIME = 1f; // how many seconds each attack takes
    private float lastAttackTime = 0;

    public AttackState(Character character) : base(character)
    {
    }

    public override void OnStateEnter()
    {
        Debug.Log("Entering Attack State...");
        character.getAnimator().SetBool("Attack", true);
        PlayerInRange();
    }

    public override void Execute()
    {
        if (Distance > ATTACK_DIST)
        {
            character.SetState(new PursueState(character));
        }
        else
        {
            character.getAnimator().SetBool("Attack", true);
            if (lastAttackTime + ATTACK_TIME < Time.time)
            {
                HitPlayer();
                lastAttackTime = Time.time;
            }

        }
        PlayerInRange();
    }

    public override void OnStateExit()
    {
        character.getAnimator().SetBool("Attack", false);
    }

    public void PlayerInRange()
    {
        Vector3 playerPosition = character.getPlayer().transform.position;
        Distance = Vector3.Distance(character.transform.position, playerPosition);
    }

    void HitPlayer()
    {
        Debug.Log("Time to hit the player");
        character.getPlayer().GetComponent<PlayerMovement>().inAttack(character.getHitStrength());
    }


}
