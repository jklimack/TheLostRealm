using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class NpcPlayer : MonoBehaviour
{
    public float speed;
    public GameObject characterDestination;
    public Animator animator;
    private Rigidbody2D myRigidbody;

    Vector3 change = Vector3.zero;

    bool inRange = false; // to be enabled when player is whithin range of NPC


    void Update()
    {
        // every fram, reset how much the player has changed
        change = Vector3.zero;

        //check for button press
        change.x = Input.GetAxisRaw("Horizontal");
        change.y = Input.GetAxisRaw("Vertical");
        //Debug.Log(change);
        updateAnimationAndMove();
    }

    void updateAnimationAndMove()
    {
        if (change != Vector3.zero)
        {
            MoveCharacter();
            animator.SetFloat("moveX", change.x);
            animator.SetFloat("moveY", change.y);
            animator.SetBool("moving", true);
        }
        else
        {
            animator.SetBool("moving", false);
        }
    }

    void MoveCharacter()
    {
        myRigidbody.MovePosition(
            transform.position + change * speed * Time.deltaTime
        );
    }
}
