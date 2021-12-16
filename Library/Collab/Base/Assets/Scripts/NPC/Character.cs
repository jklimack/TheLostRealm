using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    Animator animator;
    Vector3 pos;

    [SerializeField]
    private State currentState;

    // Start is called before the first frame update
    private void Start()
    {
        this.pos = transform.position;
        this.pos.z = -2; // The camera is upside down, and the 3d object needs to be in -2 in order to be visible
        SetState(new PatrolState(this));
        
    }

    // Update is called once per frame
    private void Update()
    {
        currentState.Execute();
    }


    public void SetState(State state)
    {
        if (currentState != null)
            currentState.OnStateExit();

        currentState = state;
        Debug.Log(gameObject.name);

        if (currentState != null)
            currentState.OnStateEnter();
    }

    //public void MoveToward(Vector3 destination)
    //{
    //    var direction = GetDirection(destination);
    //    transform.Translate(direction * Time.deltaTime * moveSpeed);
    //}

    //private Vector3 GetDirection(Vector3 destination)
    //{
    //    return (destination - transform.position).normalized;
    //}


}
