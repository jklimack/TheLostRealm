using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Character : MonoBehaviour
{
    Animator animator;
    public float speed = 0.5f;
    public float rotateSpeed = 1.0f;
    Rigidbody2D myRigidbody;
    GridManager gridManager;
    public HealthBarBehaviour healthBar;
    public float maxHealth;
    private float health;
    public float hitStrength = 10.0f;
    public string animName;
    GameObject player;
    private string characterName;
    private bool isDead = false;


    [SerializeField]
    private State currentState;

    private IEnumerator waitUntilGridManagerIsReadyAndStart(GridManager gridManager)
    {
        yield return new WaitUntil(() => gridManager.ready);
        //Debug.Log("GRID MANAGER IS READY");
        SetState(new PatrolState(this));
    }


    // Start is called before the first frame update
    void Start()
    {
        //Debug.Log("Character Initialization");
        gridManager = GameObject.Find("Grid").GetComponent<GridManager>();
        myRigidbody = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        player = GameObject.FindGameObjectWithTag("Player");
        health = maxHealth;
        healthBar.setHealth(health, maxHealth);
        StartCoroutine(waitUntilGridManagerIsReadyAndStart(gridManager));
        characterName = this.name;

    }

    // Update is called once per frame
    void Update()
    {
        if (isDead) SetState(new DeadState(this));
        else if (currentState != null)
        {
            currentState.Execute();
        }
    }


    public void SetState(State state)
    {

        if (currentState != null)
            currentState.OnStateExit();

        currentState = state;
        //Debug.Log(gameObject.name);

        if (currentState != null)
            currentState.OnStateEnter();
    }

    public void isHitByThePlayer(float playerDamage)
    {
        if (!isDead) { 
            //Debug.Log("Character " + this.getCharacterName() + " got hit");
            //Debug.Log(this.getCharacterName() + " current life is " + this.getHealth());
            this.health -= playerDamage;
            //Debug.Log(this.getCharacterName() + " after the attack life is " + this.getHealth());
            this.updateHealthBar(this.health);
            if (this.getHealth() <= 0.0f)
            {
                isDead = true;
            }
        }

    }



    public GridManager getGridManager()
    {
        return gridManager;
    }

    public Animator getAnimator()
    {
        return animator;
    }

    public float getSpeed()
    {
        return speed;
    }


    public float getHitStrength()
    {
        return hitStrength;
    }

    public float getRotateSpeed()
    {
        return rotateSpeed;
    }

    public Rigidbody2D getMyRigidbody()
    {
        return myRigidbody;
    }

    public float getHealth()
    {
        return health;
    }

    public void setHealth(float health)
    {
        this.health = health;
    }

    public float getHealthBar()
    {
        return health;
    }

    public void updateHealthBar(float health)
    {
        healthBar.setHealth(health, maxHealth);
    }

    public GameObject getPlayer()
    {
        return player;
    }

    public string getanimName()
    {
        return animName;
    }

    public string getCharacterName()
    {
        return characterName;
    }

    public bool getIsDead()
    {
        return isDead;
    }
}
