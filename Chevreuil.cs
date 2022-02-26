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
        //Time.timeScale = 2;
        position = this.transform.position;
        sprite = GetComponentInChildren<SpriteRenderer>();

        if (!isMale)
        {
            sprite.color = Color.red;
        }
    }
    private void Update()
    {
        hunger += Time.deltaTime;
        reproductionUrge += Time.deltaTime/3;

        if (hunger > 100)
        {
            Destroy(this.gameObject);
        }

        if (!isMale && isPregnant)
        {
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
        desiredDirection = (desiredDirection + Random.insideUnitCircle * wanderStrenght).normalized;


        Vector2 desiredVelocity = desiredDirection * maxSpeed;
        Vector2 desiredSteeringForce = (desiredVelocity - velocity) * steerStrenght;
        Vector2 acceleration = Vector2.ClampMagnitude(desiredSteeringForce, steerStrenght) / 1;

        HandleWalls();

        if (reproductionUrge > hunger)
        {
            HandleMate();
        }
        else
        {
            if(hunger >= 10)
                HandleFood();
        }

        velocity = Vector2.ClampMagnitude(velocity + acceleration * Time.deltaTime, maxSpeed);
        position += velocity * Time.deltaTime;

        float angle = Mathf.Atan2(velocity.y, velocity.x) * Mathf.Rad2Deg;
        transform.SetPositionAndRotation(position, Quaternion.Euler(0, 0, angle));
    }

    void HandleMate()
    {
        Collider2D[] allDeers = Physics2D.OverlapCircleAll(position, detectionRadius*2, deerLayer);

        if (allDeers.Length != 0)
        {
            for (int i = 0; i < allDeers.Length; i++)
            {
                Chevreuil mate = allDeers[i].gameObject.GetComponent<Chevreuil>();
                print(allDeers[i].gameObject.name);
                if (mate != null)
                {
                    if (isAPossibleMate(mate))
                    {
                        print("found: " + mate);
                        if (isMale)
                        {
                            desiredDirection = (mate.transform.position - this.transform.position).normalized;
                        }

                        if (Vector2.Distance(this.transform.position, mate.transform.position) < .5f)
                        {
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

        GameObject childGo = Instantiate(deerObject, this.transform.position, Quaternion.identity, GameObject.Find("Deers").transform);
        Chevreuil child = childGo.GetComponent<Chevreuil>();

        child.reproductionUrge = -60;
        child.isMale = determineChildGender();
    }

    bool determineChildGender()
    {
        return (Random.Range(0, 10) > 5);
    }

    bool isAPossibleMate(Chevreuil mate)
    {
        return (isMale != mate.isMale && (!mate.isPregnant && !this.isPregnant) && mate.reproductionUrge > 0);
    }

    void HandleFood()
    {
        if (targetedFood == null)
        {
            //lookForFood
            Collider2D[] allFood = Physics2D.OverlapCircleAll(position, detectionRadius, foodLayer);
            if(allFood.Length > 0)
            {
                //pickRandomFood
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
            desiredDirection = (targetedFood.position - this.transform.position).normalized;

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
