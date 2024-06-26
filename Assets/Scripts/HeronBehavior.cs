using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UnityEngine.Rendering.DebugUI;

public class HeronBehavior : MonoBehaviour
{
    [SerializeField] Transform flyAwayPosition;
    [SerializeField] PredatorGrab hitbox1;
    [SerializeField] PredatorGrab hitbox2;
    [SerializeField] PredatorTurner turner;
    [SerializeField] Rigidbody2D rb;
    SFXManager sfx;
    [SerializeField] float offset;
    bool turned;
    LevelGenerator levelGenerator;
    Vector3 endPoint;
    int speed = 50;
    private Transform player;
    private bool facingLeft;
    bool tutorial;
    bool pastFrog;
    private void Start()
    {
        sfx = FindFirstObjectByType<SFXManager>();
        levelGenerator = FindAnyObjectByType<LevelGenerator>();
        if (SceneManager.GetActiveScene().name == "Tutorial")
            tutorial = true;
           
        if(tutorial)
            endPoint = new Vector3(0, -5f, 0);
        else
            endPoint = levelGenerator.playerRefEndPoint;

        player = GameObject.Find("Frog").transform;

        FaceTheRightWay();
        sfx.PlaySFX("Heron Call");
        StartCoroutine(FlappingNoise());
    }
    IEnumerator FlappingNoise()
    {
        bool inSoundRange;
        if (Vector3.Distance(new Vector3(transform.position.x, 0, 0), new Vector3(player.position.x, 0, 0)) < 75)
        {
            inSoundRange = true;
        }
        else
            inSoundRange = false;

        yield return new WaitForSecondsRealtime(0.65f);
        if ((!pastFrog || inSoundRange) && Time.timeScale != 0)
        {
            sfx.PlaySFX("Heron Flap");
        }
        StartCoroutine(FlappingNoise());
    }
    private void Update()
    {
        if (tutorial)
            endPoint = new Vector3(0, -5f, 0);
        else
            endPoint = levelGenerator.playerRefEndPoint;

        //Turn around when hitting an edge
        if (player.GetComponent<PlayerController>().eaten || (hitbox1.poisoned || hitbox2.poisoned))
        {
            turner.active = true;
            TurnAround();
        }



        if(hitbox1.dead || hitbox2.dead) //Makes the predator die and float to the surface when it gets poisoned
        {
            GetComponentInChildren<Animator>().enabled = false;
            GetComponentInChildren<PolygonCollider2D>().gameObject.tag = "Grapplable";
            GetComponentInChildren<PolygonCollider2D>().gameObject.layer = LayerMask.NameToLayer("DeadPredator");
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.mass = 30;
            rb.gravityScale = 1;
            Destroy(hitbox1.gameObject);
            Destroy(hitbox2.gameObject);
            Destroy(this);
        }
    }
    void FixedUpdate()
    {
        Swoop();
    }
    void FaceTheRightWay()
    {
        if (transform.position.x > player.position.x)
            facingLeft = true;
        else
            facingLeft = false;
        if (!facingLeft)
        {
            transform.eulerAngles = new Vector3(0, 180, 0);
        }
    }
    void Swoop()
    {
        //Move
        transform.Translate(Vector3.left * speed * Time.deltaTime);

        Vector3 lerpedPosition;
        float iSpeed = 1f;
        if (hitbox1.grabbed || hitbox2.grabbed) //When it grabs the frog it flies away
        {
            lerpedPosition = Vector3.Lerp(transform.position, transform.position + new Vector3(0,10,0), Time.deltaTime * (iSpeed));
        }
        //When the bird passes the frog, it flies away
        else if ((facingLeft && flyAwayPosition.position.x < player.position.x) || (!facingLeft && flyAwayPosition.position.x > player.position.x))
        {
            lerpedPosition = Vector3.Slerp(transform.position, new Vector3(transform.position.x, (endPoint.y + 34.585f), 0), Time.deltaTime * 0.5f);
            pastFrog = true;
        }
        else
        {
            //Makes the bird swoop down when it is within 40x from the player
            if (Vector3.Distance(new Vector3(transform.position.x,0,0), new Vector3(player.position.x,0,0)) < 50)
            {
                //Aligns the y position of the beak exactly to the frog's y position when the frog is above the bird
                if (player.position.y > transform.position.y)
                {
                    lerpedPosition = Vector3.Lerp(transform.position, player.position, Time.deltaTime * (iSpeed + 1));
                }
                //Slowly moves the y position of the beak  to the frog's y position when the frog is below the bird
                else
                {
                    lerpedPosition = Vector3.Slerp(transform.position, player.position, Time.deltaTime * iSpeed);
                }
            }
            else
            {
                lerpedPosition = transform.position;
            }
        }

        //The bird can't go under the water
        if (lerpedPosition.y < (endPoint.y + 6.085f))
            lerpedPosition.y = (endPoint.y + 6.085f);

        //Sets the birds yPosition
        transform.position = new Vector3(transform.position.x, lerpedPosition.y, 0);
    }
    void TurnAround() 
    {
        if(!turner.turnDirection.Equals(""))
        {
            if (!turned && transform.eulerAngles == new Vector3(0, 180, 0))
            {
                transform.eulerAngles = new Vector3(0, 0, 0);
                turned = true;
            }
            if (!turned && transform.eulerAngles == new Vector3(0, 0, 0))
            {
                transform.eulerAngles = new Vector3(0, 180, 0);
                turned = true;
            }
        }

        /*
        if (turner.turnDirection.Equals("Right")) //If colliding from the left, flip right
        {
            if (!turned)
            {
                transform.eulerAngles = new Vector3(0, 180, 0);
                turned = true;
            }
        }
        else if (turner.turnDirection.Equals("Left")) //If colliding from the right, flip left
        {

            if (!turned)
            {
                transform.eulerAngles = new Vector3(0, 0, 0);
                turned = true;

            }
        }*/
    }
}