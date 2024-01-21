using System.Collections;
using System.Collections.Generic;
using System.Net.Mail;
using TMPro;
using Unity.VisualScripting;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private TongueLauncher tongueLauncher;
    [SerializeField] private TongueLine tongueLine;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private LineRenderer jumpLr;
    [SerializeField] private LineRenderer swimLr;
    [SerializeField] private CircleCollider2D circleCollider;
    [SerializeField] private ScoreController scoreController;
    [SerializeField] private LayerMask ground;
    [SerializeField] GameObject tongueRangeCircle;

    private float power = 5;
    private float maxDrag = 5;
    public bool skipToJump;
    private bool draggingStarted = false;
    private bool cantSwim;
    Vector3 secondLinePoint;
    Vector3 draggingPos;
    Vector3 dragStartPos;
    Vector3 dragReleasePos;
    Touch touch;

    [Header("States")]
    public bool isGrounded;
    public bool jump;
    public bool isSwimming;
    public bool dead = false;


    [Header("Settings")]
    public bool conserveMomentum;
    public bool aimingJumpStopsMomentum;


    private void Start()
    {
        rb.freezeRotation = true;
    }

    void Update()
    {
        if (!dead) 
        {
            DetectInputs();
        }
    }
    private void FixedUpdate()
    {
        Jump();
        GroundCheck();
        Swimming();
    }

    void GroundCheck()
    {
        float extraDistance = 0.35f;
        RaycastHit2D raycastHit = Physics2D.BoxCast(circleCollider.bounds.center,
        new Vector2(circleCollider.bounds.size.x * 0.4f, circleCollider.bounds.size.y * 0.7f), 0f, Vector2.down, extraDistance, ground);
        if (raycastHit.collider != null)
            isGrounded = true;
        else
            isGrounded = false; 

        if (isGrounded)
        {
            isSwimming = false;
            tongueLine.isGrappling = false;

            if (!conserveMomentum)
                rb.velocity = Vector2.zero;
        }
        else
        {
            jumpLr.positionCount = 0;
        }
    }
    void Swimming()
    {
        if (isSwimming)
        {
            float slowingFactor = 0.7f;
            rb.drag = slowingFactor;
            tongueLine.isGrappling = false;
        }
        else //Remove the swim line and resume time when out of the water
        {
            swimLr.positionCount = 0;
            rb.drag = 0;
        }
    }
    void DetectInputs()
    {
        if (Input.touchCount > 0 && (isGrounded || isSwimming))
        {
            touch = Input.GetTouch(0);
            if (touch.phase == TouchPhase.Began)
            {
                draggingStarted = true;
                DragStart();
            }
            if (((touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary) && draggingStarted) || skipToJump)
            {
                Dragging();
            }
            if ((touch.phase == TouchPhase.Ended && draggingStarted) || (touch.phase == TouchPhase.Ended && skipToJump))
            {
                DragRelease();
                draggingStarted = false;
            }
        }
    }
    void Jump() 
    {
        if (jump) 
        {
            if (isSwimming)
            {
                rb.velocity *= 0.3f;
                power = 4.5f;
            }
            else if (!isSwimming && !tongueLine.isGrappling)
            {
                rb.velocity = Vector2.zero;
                power = 5f;
            }

            Vector3 force = dragStartPos - dragReleasePos;
            Vector3 clampedForce = Vector3.ClampMagnitude(force, maxDrag) * power * rb.mass;
            rb.AddForce(clampedForce, ForceMode2D.Impulse);

            jump = false;
        }
    }
    void DragStart() 
    {
        dragStartPos = Camera.main.WorldToViewportPoint(touch.position); 
        dragStartPos.z = 0;
        if (isSwimming) 
        {
            swimLr.positionCount = 1;
            swimLr.SetPosition(0, transform.position);
        }
        else
        {
            jumpLr.positionCount = 1;
            jumpLr.SetPosition(0, transform.position);
        }
    }
    void Dragging() 
    {            
       draggingPos = Camera.main.WorldToViewportPoint(touch.position);
       draggingPos.z = 0;

        if (skipToJump)
            dragStartPos = tongueLauncher.dragStartPosition;
        
        secondLinePoint = transform.position + (Vector3.ClampMagnitude((dragStartPos - draggingPos), maxDrag + 5));
        
        if (isSwimming)
        {
            jumpLr.positionCount = 0;
            swimLr.positionCount = 2;
            swimLr.SetPosition(0, transform.position);
            swimLr.SetPosition(1, secondLinePoint);
        }
        else
        {
            if (aimingJumpStopsMomentum) 
            {
                rb.velocity = new Vector2(0, rb.velocity.y);
            }

            swimLr.positionCount = 0;
            jumpLr.positionCount = 2;
            jumpLr.SetPosition(0, transform.position);
            jumpLr.SetPosition(1, secondLinePoint);
        }
    }
    void DragRelease () 
    {
        skipToJump = false;
        tongueLauncher.touchEnded = false;
        jumpLr.positionCount = 0;
        swimLr.positionCount = 0;
        dragReleasePos = draggingPos;
        dragReleasePos.z = 0;

        jump = true;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == 7 || collision.gameObject.layer == 12)
        {
            AddPreyScores(collision);

            //When grappling to prey, continue momentum and destroy prey
            if (tongueLine.isGrappling && tongueLauncher.grappleTarget != null && collision.transform.parent == tongueLauncher.grappleTarget.transform)
            {
                tongueLauncher.grapplePointIdentified = false;
                rb.gravityScale = 1.2f;

                power = 7;
                if (collision.gameObject.layer == 12)
                    power = 2;

                rb.AddForce(tongueLauncher.addedForce.normalized * power * rb.mass, ForceMode2D.Impulse);

                //Remove the aim line when the frog eats prey
                tongueLauncher.lr.positionCount = 0;

                //Cancel the grapple
                tongueLine.enabled = false;
                tongueLine.isGrappling = false;
                tongueLauncher.grapplePointIdentified = false;
                tongueLauncher.grappleTarget = null;

                Destroy(collision.transform.parent.gameObject);
            }
            else
            {
                Destroy(collision.transform.parent.gameObject);
            }
        }
        else if (collision.gameObject.layer == 11)
        {
            //When grappling to prey, continue momentum and destroy prey
            if (tongueLauncher.grappleTarget != null && collision.transform == tongueLauncher.grappleTarget.transform)
            {
                tongueLauncher.grapplePointIdentified = false;
                rb.gravityScale = 1.2f;
                power = 20;

                // Launch the player up and to the right if they are coming from the left
                if (tongueLauncher.grappleTarget != null && (tongueLauncher.grappleTarget.transform.position.x - transform.position.x >= 0))
                    rb.AddForce((Vector2.one - new Vector2(0,0.3f)) * power * rb.mass, ForceMode2D.Impulse); // approx. 70 degree angle

                else //Launch the player up and to the left if they are coming from the right
                    rb.AddForce(new Vector2(-(Vector2.one - new Vector2(0, 0.3f)).x, (Vector2.one - new Vector2(0, 0.3f)).y) * power * rb.mass, ForceMode2D.Impulse); // approx. 110 degree angle

                //Remove the aim line when the frog eats prey
                tongueLauncher.lr.positionCount = 0;

                //Cancel the grapple
                tongueLine.enabled = false;
                tongueLine.isGrappling = false;
                tongueLauncher.grapplePointIdentified = false;
                tongueLauncher.grappleTarget = null;

                //Destroy the cattail
                Destroy(collision.transform.gameObject);
            }
        }

        //When the player enters a no-swim-zone, they can't swim until they leave
        if (collision.gameObject.tag == "NoSwim")
            cantSwim = true;

        //When the player gets hit by a predator, they die
        if (collision.gameObject.tag == "Predator")
            dead = true;
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        //When the player leaves a no-swim-zone, they can swim
        if (collision.gameObject.tag == "NoSwim" && cantSwim)
            cantSwim = false;
        if (collision.gameObject.tag == "Water")
            isSwimming = false;
    }
    private void OnTriggerStay2D(Collider2D collision)
    {
        //The player swims when they are in water, not grounded, and not in a no-swim-zone
        if (collision.gameObject.tag == "Water" && !isGrounded && !cantSwim)
            isSwimming = true;
    }

    void AddPreyScores(Collider2D collision)
    {
        //if fly or strider, add 10
        if (collision.transform.parent != null &&
            collision.gameObject.transform.parent.name == "Fly(Clone)" || collision.gameObject.transform.parent.name == "WaterStrider(Clone)")
        {
            scoreController.SpawnFloatingText(10, transform.position);
            scoreController.Score(10);
        }

        //if slug, add 20
        else if (collision.gameObject.transform.parent.name == "Slug(Clone)")
        {
            scoreController.SpawnFloatingText(20, transform.position);
            scoreController.Score(20);
        }

        //if dragonfly, add 25
        else if (collision.gameObject.transform.parent.name == "Dragonfly(Clone)")
        {
            scoreController.SpawnFloatingText(25, transform.position);
            scoreController.Score(25);
        }
    }
}