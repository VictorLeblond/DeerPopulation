using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Chevreuil : MonoBehaviour
{
    public float hunger;
    public float reproductionUrge;
    [Space]
    public bool isMale;
    [Space]
    public bool isPregnant = false;
    public float gestationPeriod;
    float gestationTimer;
    public GameObject deerObject;
    [Space]
    public float maxSpeed = 2;
    public float steerStrenght = 2;
    public float wanderStrenght = 1;
    [Space]
    public float detectionRadius = 1;
    public float detectionAngle = 60;
    public LayerMask wallLayer;
    public LayerMask foodLayer;
    public LayerMask deerLayer;

    Transform targetedFood;

    Vector2 position;
    Vector2 velocity;
    Vector2 desiredDirection;

    SpriteRenderer sprite;

    public void Start()
    {
        position = this.transform.position;
        sprite = GetComponentInChildren<SpriteRenderer>();
        
        //visualize deer's sex
        if (!isMale)
        {
            sprite.color = Color.red;
        }
    }
    private void Update()
    {
        //hungers and urge increases over time
        hunger += Time.deltaTime;
        reproductionUrge += Time.deltaTime/3;

        //if hunger meter is full, dies
        if (hunger > 100)
        {
            Destroy(this.gameObject);
        }

        if (!isMale && isPregnant)
        {
            //reset timer, if deer gives birth twice
            gestationTimer -= Time.deltaTime;
            if (gestationTimer < 0)
            {
                GiveBirth();
            }
        }
        HandleMovement();
    }

    void HandleMovement()
    {
        //sets a random direction around deer, depending on wanderStrenght value 
        //wanderStenght determines the amplitude of the randomness in the deers movement
        //steerStrenght determines how fast it goes toward that random direction
        desiredDirection = (desiredDirection + Random.insideUnitCircle * wanderStrenght).normalized;
        
        Vector2 desiredVelocity = desiredDirection * maxSpeed;
        Vector2 desiredSteeringForce = (desiredVelocity - velocity) * steerStrenght;
        Vector2 acceleration = Vector2.ClampMagnitude(desiredSteeringForce, steerStrenght) / 1;

        //Checks if there's a wall in front of the deer
        HandleWalls();

        if (reproductionUrge > hunger)
        {
            //Checks if there's any mate 
            HandleMate();
        }
        else
        {
             //Cooldown so that the deer doesnt eat everything it sees, basically makes the deer full for a period of time after eating.
            if(hunger >= 10)
                HandleFood();
        }

        //current speed cant go over maxSpeed (Time.deltaTime is so that deer speed doesnt depend on framerate)
        velocity = Vector2.ClampMagnitude(velocity + acceleration * Time.deltaTime, maxSpeed);
        position += velocity * Time.deltaTime;

        //trigo stuff and sets position/rotation
        float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        transform.SetPositionAndRotation(position, Quaternion.Euler(0, 0, angle));
    }

    void HandleMate()
    {
        //Used multiple times, detects if there is any obect with X layer
        Collider2D[] allDeers = Physics2D.OverlapCircleAll(position, detectionRadius*2, deerLayer);

        if (allDeers.Length != 0)
        {
            for (int i = 0; i < allDeers.Length; i++)
            {
                Chevreuil mate = allDeers[i].gameObject.GetComponent<Chevreuil>();
                
                if (mate != null)
                {
                    if (isAPossibleMate(mate))
                    {
                        if (isMale)
                        {
                             //only the male will go for the female, which makes some interesting chasing scenes
                            desiredDirection = (mate.transform.position - this.transform.position).normalized;
                        }
                        
                        //graphic content
                        if (Vector2.Distance(this.transform.position, mate.transform.position) < .5f)
                        {
                            //reset
                            this.reproductionUrge = 0;
                            mate.reproductionUrge = 0;

                            if (isMale)
                            {
                                mate.StartGestation();
                            }
                            else
                            {
                                StartGestation();
                            }

                        }
                    }
                    else
                    {
                         //do nothing if no possible mate
                        continue;
                    }
                }
            }
        }
    }

    public void StartGestation()
    {
        isPregnant = true;
        gestationTimer = gestationPeriod;
    }

    void GiveBirth()
    {
        isPregnant = false;
        
        //creates the deer child object in the 2D world
        GameObject childGo = Instantiate(deerObject, this.transform.position, Quaternion.identity, GameObject.Find("Deers").transform);
        Chevreuil child = childGo.GetComponent<Chevreuil>();

        //children cant reproduce, common
        child.reproductionUrge = -60;
        child.isMale = determineChildGender();
    }

    bool determineChildGender()
    {
        //50/50
        return (Random.Range(0, 10) > 5);
    }

    bool isAPossibleMate(Chevreuil mate)
    {
        //true only if both arent the same sex and neither or pregnant/a kid
        
        return (isMale != mate.isMale && (!mate.isPregnant && !this.isPregnant) && mate.reproductionUrge > 0);
    }

    void HandleFood()
    {
        if (targetedFood == null)
        {
            //look For Food
            Collider2D[] allFood = Physics2D.OverlapCircleAll(position, detectionRadius, foodLayer);
            
            if(allFood.Length > 0)
            {
                //pick Random Food
                Transform food = allFood[Random.Range(0, allFood.Length)].transform;
                Vector2 directionToFood = (food.position - this.transform.position).normalized;

                if (Vector2.Angle(transform.forward, directionToFood) < detectionAngle / 2)
                {
                    targetedFood = food;
                }
            }
        }
        else
        {
            //sets direction toward food
            desiredDirection = (targetedFood.position - this.transform.position).normalized;
            
            //eats food if close enough
            const float pickUpRadius = .05f;
            if (Vector2.Distance(targetedFood.position, this.transform.position) < pickUpRadius)
            {
                Destroy(targetedFood.gameObject);
                Eat();
            }
        }
    }

    void Eat()
    {
        hunger = 0;
    }

    void HandleWalls()
    {
        Collider2D[] walls = Physics2D.OverlapCircleAll(position, detectionRadius, wallLayer);

        if (walls.Length != 0)
        {
            Vector2 wallsSum = Vector2.zero;
            
            //new direction is the num of all walls normals, so move the deer away from said walls
            for (int i = 0; i < walls.Length; i++)
            {
                wallsSum += (Vector2)walls[i].transform.up;
            }

            desiredDirection = wallsSum.normalized;
        }
    }

    public void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
