using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerMovement : MonoBehaviour
{
    public float speed = 10f;
    private Rigidbody2D myRigidbody;
    private Vector3 change;
    private Animator animator;
    private const float PL_ATTACK_DIST = 2f;
    private const float WEAPON_DIST = 2f;
    private const float ATTACK_TIME = 0.5f;
    public HealthBarBehaviour playerHealthBar;
    public float maxHealth;
    private float health;
    public float damage = 10f;
    private Dictionary<Vector3, int> posOfLives;
    private const float LIFE_POWER = 20.0f;
    private GameObject[] livesInstances;
    private GameObject shieldInstance;
    private GameObject swordInstance;
    private GameObject axeInstance;
    private GameObject woodenSwordInstance;
    private GameObject goldenSwordInstance;
    private GameObject bootInstance;
    private GameObject helmetInstance;
    private GameObject knifeInstance;

    private Stack<GameObject> weaponsTaken;
    private bool WearBoots = false;
    private bool WearHelmet = false;
    private int MAX_NUM_WEAPONS = 2;
    private int numWeaponsCarried;
    private int endurance =0;

    private bool spaceWasPressed = false;
    private float timeWhenLastSpaceWasPressed = 0;

    private void setHealth(float health)
    {
        this.health = health;
    }

    private void setHealthAndUpdateHealthBar(float health)
    {
        this.health = health;
        setHealthBar(health);
    }

    public void setHealthBar(float health)
    {
        //Debug.Log("Setting Health to " + health + " and maxHealth to " + maxHealth);
        playerHealthBar.setHealth(health, maxHealth);
    }

    private IEnumerator waitUntilLivesAreInstantiated()
    {
        yield return new WaitUntil(() => GameObject.FindGameObjectsWithTag("Lives") != null);
        Debug.Log("LIVES ARE INSTANTIATED");
    }

    // Start is called before the first frame update
    void Start()
    {
        animator = GetComponent<Animator>();
        myRigidbody = GetComponent<Rigidbody2D>();
        livesInstances = GameObject.Find("Grid").GetComponent<MapGenerator>().GetLivesInstances();
        shieldInstance = GameObject.Find("Grid").GetComponent<MapGenerator>().GetShieldInstance();
        swordInstance = GameObject.Find("Grid").GetComponent<MapGenerator>().GetSwordInstance();
        axeInstance = GameObject.Find("Grid").GetComponent<MapGenerator>().GetAxeInstance();
        woodenSwordInstance = GameObject.Find("Grid").GetComponent<MapGenerator>().GetWoodenSwordInstance();
        goldenSwordInstance = GameObject.Find("Grid").GetComponent<MapGenerator>().GetGoldenSwordInstance();
        bootInstance = GameObject.Find("Grid").GetComponent<MapGenerator>().GetBootInstance();
        helmetInstance = GameObject.Find("Grid").GetComponent<MapGenerator>().GetHelmetInstance();
        knifeInstance = GameObject.Find("Grid").GetComponent<MapGenerator>().GetKnifeInstance();

        weaponsTaken = new Stack<GameObject>();

        numWeaponsCarried = 0;
        setHealthAndUpdateHealthBar(maxHealth);
        posOfLives = new Dictionary<Vector3, int>();
        GameObject[] lives = GameObject.FindGameObjectsWithTag("Lives");
        if (lives == null)
        {
            StartCoroutine(waitUntilLivesAreInstantiated());
            lives = GameObject.FindGameObjectsWithTag("Lives");
        }
        GetLivePositions(lives);
    }

    // Update is called once per frame
    void Update()
    {
        // every fram, reset how much the player has changed
        change = Vector3.zero;

        //check for button press
        change.x = Input.GetAxisRaw("Horizontal");
        change.y = Input.GetAxisRaw("Vertical");

        attackIfSpaceKeyIsPressed();

        takeWeaponIfKeyXIsPressed();

        updateAnimationAndMove();

        int key = getLifeConsumedAndRemove();
        if (key != -1)
        {
            var life = GameObject.Find("Life_" + key);
            Destroy(life);
            livesInstances[key] = null;
            if (health < maxHealth - LIFE_POWER) setHealthAndUpdateHealthBar(health + LIFE_POWER);
            else if (health < maxHealth) setHealthAndUpdateHealthBar(maxHealth);
        }
    }

    private void takeWeaponIfKeyXIsPressed()
    {
        if (Input.GetKey(KeyCode.W) && numWeaponsCarried < MAX_NUM_WEAPONS)
        {
            Debug.Log("W key is pressed and recognized!");
            float swordDistance = Vector2.Distance(transform.position, swordInstance.transform.position);
            if (swordDistance < WEAPON_DIST)
            {
                Debug.Log("Take the Sword");
                swordInstance.GetComponent<SpriteRenderer>().enabled = false;
                swordInstance.transform.parent = this.gameObject.transform;
                weaponsTaken.Push(swordInstance);
                damage += 20;
                numWeaponsCarried++;
            }

            float shieldDistance = Vector2.Distance(transform.position, shieldInstance.transform.position);
            if (shieldDistance < WEAPON_DIST)
            {
                Debug.Log("Take the Shield");
                shieldInstance.GetComponent<SpriteRenderer>().enabled = false;
                shieldInstance.transform.parent = this.gameObject.transform;
                weaponsTaken.Push(shieldInstance);
                endurance += 5;
                numWeaponsCarried++;
            }

            float axeDistance = Vector2.Distance(transform.position, axeInstance.transform.position);
            if (axeDistance < WEAPON_DIST)
            {
                Debug.Log("Take the Axe");
                axeInstance.GetComponent<SpriteRenderer>().enabled = false;
                axeInstance.transform.parent = this.gameObject.transform;
                weaponsTaken.Push(axeInstance);
                damage += 10;
                numWeaponsCarried++;
            }


            float woodenSwordDistance = Vector2.Distance(transform.position, woodenSwordInstance.transform.position);
            if (woodenSwordDistance< WEAPON_DIST)
            {
                Debug.Log("Take the Wooden Sword");
                woodenSwordInstance.GetComponent<SpriteRenderer>().enabled = false;
                woodenSwordInstance.transform.parent = this.gameObject.transform;
                weaponsTaken.Push(woodenSwordInstance);
                damage += 5;
                numWeaponsCarried++;
            }

            float goldenSwordDistance = Vector2.Distance(transform.position, goldenSwordInstance.transform.position);
            if (goldenSwordDistance < WEAPON_DIST)
            {
                Debug.Log("Take the Golden Sword");
                goldenSwordInstance.GetComponent<SpriteRenderer>().enabled = false;
                goldenSwordInstance.transform.parent = this.gameObject.transform;
                weaponsTaken.Push(goldenSwordInstance);
                damage += 20;
                numWeaponsCarried++;
            }

            float knifeDistance = Vector2.Distance(transform.position, knifeInstance.transform.position);
            if (knifeDistance < WEAPON_DIST)
            {
                Debug.Log("Take the Knife");
                knifeInstance.GetComponent<SpriteRenderer>().enabled = false;
                knifeInstance.transform.parent = this.gameObject.transform;
                weaponsTaken.Push(knifeInstance);
                damage += 5;
                numWeaponsCarried++;
            }

        }
        if (Input.GetKey(KeyCode.W) && !WearBoots)
        {
            float bootDistance = Vector2.Distance(transform.position, bootInstance.transform.position);
            if (bootDistance < WEAPON_DIST)
            {
                Debug.Log("Wear the Boots");
                bootInstance.GetComponent<SpriteRenderer>().enabled = false;
                bootInstance.transform.parent = this.gameObject.transform;
                endurance += 2;
                WearBoots = true;
            }
        }

        if (Input.GetKey(KeyCode.W) && !WearHelmet)
        {
            float helmetDistance = Vector2.Distance(transform.position, helmetInstance.transform.position);
            if (helmetDistance < WEAPON_DIST)
            {
                Debug.Log("Wear the Helmet");
                helmetInstance.GetComponent<SpriteRenderer>().enabled = false;
                helmetInstance.transform.parent = this.gameObject.transform;
                endurance += 10;
                WearHelmet = true;
            }
        }

        if (Input.GetKey(KeyCode.Q) && numWeaponsCarried > 0)
        {
            Debug.Log("Leave Weapons");
            GameObject tempInstance = weaponsTaken.Pop();
            tempInstance.transform.SetParent(null, true);
            tempInstance.transform.position = transform.position;
            tempInstance.GetComponent<SpriteRenderer>().enabled = true;
            numWeaponsCarried--;
        }
    }

    private void attackIfSpaceKeyIsPressed()
    {
        
        if (timeWhenLastSpaceWasPressed + ATTACK_TIME < Time.time)
        {
            spaceWasPressed = false;
        }
        if (Input.GetKey(KeyCode.Space) && !spaceWasPressed)
        {
            spaceWasPressed = true;
            timeWhenLastSpaceWasPressed = Time.time;
            //Debug.Log("Player is Attacking!");
            GameObject[] enemies = GameObject.FindGameObjectsWithTag("Enemy");
            GameObject boss = GameObject.FindGameObjectWithTag("Boss");
            bool actionExecuted = false;
            foreach (GameObject enemy in enemies)
            {
                if (!enemy.GetComponent<Character>().getIsDead()) { 
                    if (Vector2.Distance(transform.position, enemy.transform.position) < PL_ATTACK_DIST)
                    {
                        //Debug.Log("Player hit enemy: " + enemy.name);
                        enemy.GetComponent<Character>().isHitByThePlayer(damage);
                        actionExecuted = true;
                        break;
                    }
                }

            }
            if (!actionExecuted)
            {
                if (!boss.GetComponent<Character>().getIsDead())
                { 
                    if (Vector3.Distance(transform.position, boss.transform.position) < PL_ATTACK_DIST)
                    {
                        boss.GetComponent<Character>().isHitByThePlayer(damage);
                        actionExecuted = true;
                    }
                }
            }
        }
    }



    private void GetLivePositions(GameObject[] lives)
    {
        for (int i = 0; i < lives.Length; i++)
        {
            posOfLives.Add(lives[i].transform.position, i);
        }
    }
    
    private int getLifeConsumedAndRemove()
    {
        Vector3 posToint = new Vector3((int)transform.position.x, (int)transform.position.y, 1);
        int key = -1;
        if (posOfLives != null && posOfLives.ContainsKey(posToint))
        {
            key = posOfLives[posToint];
            posOfLives.Remove(posToint);
        }
        return key;
    }

    public void inAttack(float hitStrength)
    {
        //Debug.Log("Player has health:" + health + " got hit with strength: " + hitStrength);
        setHealthAndUpdateHealthBar(health - hitStrength + endurance);

        if (health <= 0)
        {
            StartCoroutine(waitUntilToLoadtheScene());
        }
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

    private IEnumerator waitUntilToLoadtheScene()
    {
        yield return new WaitForSeconds(3);
        SceneManager.LoadScene(3); // Victory = 2, GameOver = 3
    }

}
