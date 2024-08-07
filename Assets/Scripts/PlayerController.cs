using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.SocialPlatforms;
using UnityEngine.UI;

public class PlayerController : MonoBehaviour
{
    public LayerMask layerMask;
    [SerializeField] Color sparrowScoreColor;
    [SerializeField] CircleCollider2D normalCol;
    [SerializeField] CircleCollider2D bullfrogCol;
    [SerializeField] Transform grapplePointDetector;
    CircleCollider2D col;
    [SerializeField] Sprite[] initialSprites = new Sprite[5];
    SFXManager sfx;
    [SerializeField] SpriteRenderer spriteRenderer;
    [SerializeField] private TongueLauncher tongueLauncher;
    [SerializeField] private TongueLine tongueLine;
    [SerializeField] private Rigidbody2D rb;
    [SerializeField] private LineRenderer jumpLr;
    [SerializeField] Transform jumpLrStartpoint;
    [SerializeField] private LineRenderer swimLr;
    [SerializeField] Transform swimLrStartpoint;
    [SerializeField] private ScoreController scoreController;
    [SerializeField] private PauseButtons pauseScript;
    [SerializeField] private LevelGenerator levelGenerator;
    [SerializeField] private OxygenAndMoistureController oxygenAndMoistureController;
    [SerializeField] private LayerMask ground;
    [SerializeField] private LayerMask slide;
    [SerializeField] GameObject tongueRangeCircle;

    [SerializeField] GameObject cattailParticles;
    [SerializeField] GameObject mudParticles;
    [SerializeField] GameObject splashParticles;
    [SerializeField] GameObject eatParticles;
    [SerializeField] GameObject sparrowEatParticles;
    [SerializeField] GameObject dartFrogPoisonParticles;
    GameObject activeDartFrogPoisonParticles;
    public string biomeIn;
    public bool transitioningBiome;
    public bool eatenByFalcon;
    float initialMass;
    bool mouseAllowedToDrag;

    [Header("States")]
    public bool isGrounded;
    public bool isSliding;
    public bool jump;
    public bool isSwimming;
    public bool saturated;
    public bool wet;
    bool wasWet;
    public bool dead;
    public bool eaten;
    public string killer;
    public PredatorGrab killerGrab;
    public bool drowned;
    public bool dried;
    public bool poisoned;
    public bool invulnerable;
    [HideInInspector] public bool grabbedByPoisonedFalcon;
    [HideInInspector] public bool grabbedByPoisonedPredator;
    public enum Species { Default, Treefrog, Froglet, BullFrog, PoisonDartFrog };
    [HideInInspector] public bool poisonAvailable;

    [Header("Settings")]
    public float coyoteTime = 0.2f;
    private float coyoteTimeCounter;
    public float jumpBuffer = 0.2f;
    private float jumpBufferCounter;
    [SerializeField] float aimMultiplier = 5;
    public static Species species;
    public bool conserveMomentum;
    [SerializeField] bool aimingJumpStopsMomentum;

    public bool skipToJump;
    [HideInInspector] public bool changeBiome;
    [HideInInspector] public bool transitionCamera;
    float defaultGravityScale;
    private float power;
    private float jumpingPower;
    private float swimmingPower;
    private float maxJumpAimLineLength, initialMaxJumpAimLineLength, driedMaxJumpAimLineLength = 5;
    private float maxSwimAimLineLength, initialMaxSwimAimLineLength, driedMaxSwimAimLineLength = 5;
    private bool draggingStarted = false;
    private bool cantSwim;
    Vector3 secondLinePoint;
    Vector3 draggingPos;
    Vector3 dragStartPos;
    Vector3 dragReleasePos;
    Touch touch;
    bool splashParticleCooldown;
    public bool killerFinalized;
    GameObject currentPlant;

    [Header("Animation")]
    bool jumpAnimationPlaying;
    [SerializeField] Transform sprite;
    [SerializeField] Animator animator;
    string currentState;
    Vector3 initialSpriteScale;
    Vector3 initialSpriteOffset;
    bool grappleRotationSet;
    bool wasSwimming;
    bool wasAlive;
    bool jumpTransferCoroutineStarted;
    bool exitWaterSFXAllowed;
    bool facingRight = true;
    public bool grappleMode;
    string SLIDE = "FrogSlide";
    string IDLE = "FrogIdle";
    string JUMP = "FrogJump";
    string MIDAIR = "FrogMidair";
    string GRAPPLE = "FrogGrapple";
    string READY_SWIM = "FrogReadySwim";
    string SWIM = "FrogSwim";
    string MIDSWIM = "FrogMidswim";
    string STRAIGHT_JUMP = "FrogStraightJump";
    string STRAIGHT_GRAPPLE = "FrogStraightGrapple";

    private void Awake()
    {
        //Application.targetFrameRate = 60;
    }

    private void Start()
    {
        sfx = FindFirstObjectByType<SFXManager>();
        col = normalCol;

        initialMass = rb.mass;
        Time.timeScale = 1.0f;
        SetSpecies();
        ConfigureSpecies();
        initialMaxJumpAimLineLength = maxJumpAimLineLength;
        initialMaxSwimAimLineLength = maxSwimAimLineLength;

        defaultGravityScale = rb.gravityScale;
        rb.freezeRotation = true;
        if(levelGenerator != null) 
        {
            biomeIn = levelGenerator.biomeSpawning.ToString();
        }

        initialSpriteScale = sprite.localScale;
        initialSpriteOffset = sprite.localPosition;
        ChangeAnimationState(SLIDE);
        StartCoroutine(RibbitRandomly());

        if(SceneManager.GetActiveScene().name == "GameScene")
            rb.AddForce(new Vector2(40, -40), ForceMode2D.Impulse);
    }

    void Update()
    {
        //Coyote Time
        if(isGrounded || isSliding)
        {
            coyoteTimeCounter = coyoteTime;
        }
        else
        {
            coyoteTimeCounter -= Time.deltaTime;
        }

        if (!dead && !pauseScript.pause) 
        {
            DetectInputs();
            Jump();
        }

        //Cancel lines when you die
        if (dead)
        {
            swimLr.positionCount = 0;
            jumpLr.positionCount = 0;
        }

        //Pause the frog when it drowns
        if (drowned)
        {
            rb.velocity = Vector3.zero;
        }

        //Play eaten sfx everytime the player dies
        if(dead && wasAlive)
        {
            sfx.PlaySFX("Eaten");
            wasAlive = false;
        }
        if(!dead)
        {
            wasAlive = true;
        }
        else
        {
            wasAlive = false;
        }

        //Cancel all swimming and jumping when you pause
        if(pauseScript.pause)
        {
            draggingStarted = false;
            skipToJump = false;
            swimLr.positionCount = 0;
            jumpLr.positionCount = 0;
        }
    }
    private void FixedUpdate()
    {
        GroundCheck();
        Swimming();
        AnimateFrog();
        SetDirection();
    }
    //////////////////////////////////////////////////////////////////////////////////////////////// MOVEMENT //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////


    void GroundCheck()
    {
        float extraDistance = 0.35f;
        if (species == Species.BullFrog) extraDistance = 0.49f;
        RaycastHit2D raycastHitGround = Physics2D.BoxCast(col.bounds.center,
        new Vector2(col.bounds.size.x * 0.4f, col.bounds.size.y * 0.7f), 0f, Vector2.down, extraDistance, ground);

        RaycastHit2D raycastHitSlide = Physics2D.BoxCast(col.bounds.center,
        new Vector2(col.bounds.size.x * 0.4f, col.bounds.size.y * 0.7f), 0f, Vector2.down, extraDistance, slide);

        if (raycastHitGround.collider != null || raycastHitSlide.collider != null)
        {
            grappleMode = false;
            isGrounded = true;
            if (raycastHitSlide.collider != null)
            {
                isSliding = true;
            }
        }
        else
        {
            isGrounded = false;
        }

        if (raycastHitSlide.collider == null)
        {
            isSliding = false;
        }


        if (isGrounded)
        {
            isSwimming = false;
            tongueLine.isGrappling = false;

            if (!conserveMomentum && !jump)
            {
                rb.velocity = Vector2.zero;
            }
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
            if (!tongueLine.isGrappling)
            {
                float slowingFactor = 1f;
                rb.drag = slowingFactor;

                rb.gravityScale = defaultGravityScale / 2;

                tongueLine.isGrappling = false;
            }
        }
        else //Remove the swim line and resume time when out of the water
        {
            if (!tongueLine.isGrappling)
                rb.gravityScale = defaultGravityScale;

            swimLr.positionCount = 0;
            rb.drag = 0;
        }
    }
    void DetectInputs()
    {
        bool inputEnded = false;
        bool inputBegan = false;

        if (Input.touchCount > 0)
        {
            touch = Input.GetTouch(0);
            
            if (touch.phase == TouchPhase.Began)
            {
                draggingStarted = true;
                inputBegan = true;  
                DragStart();
            }

            if (((touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary) && draggingStarted) || skipToJump)
            {
                Dragging();
            }

            if ((touch.phase == TouchPhase.Ended && draggingStarted) || (touch.phase == TouchPhase.Ended && skipToJump))
            {
                jumpBufferCounter = jumpBuffer;
                DragRelease();
                draggingStarted = false;
                inputEnded = true;
            }

        }
        else if (Input.GetMouseButtonDown(0))
        {
            // Handle mouse input start
            mouseAllowedToDrag = true;
            draggingStarted = true;
            inputBegan = true;
            DragStart();
        }
        else if (Input.GetMouseButton(0) && ((mouseAllowedToDrag && draggingStarted) || skipToJump))
        {
            // Handle mouse dragging
            Dragging();

        }
        else if (Input.GetMouseButtonUp(0))
        {
            if (draggingStarted || skipToJump)
            {
                jumpBufferCounter = jumpBuffer;
                DragRelease();
                draggingStarted = false;
                inputEnded = true;
            }

            jumpBufferCounter = jumpBuffer;
            mouseAllowedToDrag = false;
        }

        if (inputEnded)
        {
            jumpBufferCounter = jumpBuffer;
        }
        else if (!inputBegan)
        {
            jumpBufferCounter -= Time.deltaTime;
        }
    }
    
    void DragStart() 
    {
        if(Input.touchCount > 0)
            dragStartPos = Camera.main.WorldToViewportPoint(touch.position); 
        else
            dragStartPos = Camera.main.WorldToViewportPoint(Input.mousePosition);

        dragStartPos.z = 0;
        if (isSwimming) 
        {
            swimLr.positionCount = 1;
            swimLr.SetPosition(0, swimLrStartpoint.position);
        }
        else
        {
            jumpLr.positionCount = 1;
            jumpLr.SetPosition(0, jumpLrStartpoint.position);
        }
    }
    void Dragging() 
    {
        if (Input.touchCount > 0)
            draggingPos = Camera.main.WorldToViewportPoint(touch.position);
        else
            draggingPos = Camera.main.WorldToViewportPoint(Input.mousePosition);
        draggingPos.z = 0;

        if (skipToJump)
            dragStartPos = tongueLauncher.dragStartPosition;

        if (dried)
        {
            maxJumpAimLineLength = driedMaxJumpAimLineLength;
            maxSwimAimLineLength = driedMaxSwimAimLineLength;
        }        
        else
        {
            maxJumpAimLineLength = initialMaxJumpAimLineLength;
            maxSwimAimLineLength = initialMaxSwimAimLineLength;
        }


        if (isSwimming)
        {
            secondLinePoint = swimLrStartpoint.position + Vector3.ClampMagnitude(((dragStartPos - draggingPos) * aimMultiplier), maxSwimAimLineLength);
            jumpLr.positionCount = 0;
            swimLr.positionCount = 2;
            swimLr.SetPosition(0, swimLrStartpoint.position);
            swimLr.SetPosition(1, secondLinePoint);

            float fillPercentage = Mathf.InverseLerp(0, (maxSwimAimLineLength), (secondLinePoint - transform.position).magnitude);
            if(fillPercentage > 0.1f)
            {
                ChangeAnimationState(READY_SWIM);
            }
        }
        else if (isGrounded || (coyoteTimeCounter > 0 && !tongueLine.isGrappling))
        {

            secondLinePoint = jumpLrStartpoint.position + Vector3.ClampMagnitude(((dragStartPos - draggingPos) * aimMultiplier), maxJumpAimLineLength);
            if (!isSliding) 
            {
                if(isGrounded)
                    ChangeAnimationState(IDLE);

                if (aimingJumpStopsMomentum && isGrounded)
                {
                    rb.velocity = new Vector2(0, rb.velocity.y);
                    rb.mass = 0.1f;
                }
            }

            if(isGrounded)
            {
                swimLr.positionCount = 0;
                jumpLr.positionCount = 2;
                jumpLr.SetPosition(0, jumpLrStartpoint.position);
                jumpLr.SetPosition(1, secondLinePoint);
            }
        }
    }
    void DragRelease() 
    {
        skipToJump = false;
        tongueLauncher.touchEnded = false;
        jumpLr.positionCount = 0;
        swimLr.positionCount = 0;
        dragReleasePos = draggingPos;
        dragReleasePos.z = 0;
        rb.mass = initialMass;

        if((coyoteTimeCounter > 0 || isGrounded || isSwimming) || jumpBufferCounter > 0)
        {
            jump = true;
        }
    }
    void Jump()
    {
        if (jump && !pauseScript.pause && (coyoteTimeCounter > 0 || isSwimming) && jumpBufferCounter > 0)
        {
            Vector3 force = dragStartPos - dragReleasePos;
            float fillPercentage;

            if (isSwimming) //Swim
            {
                fillPercentage = Mathf.InverseLerp(0, (maxSwimAimLineLength), (secondLinePoint - transform.position).magnitude);

                if (fillPercentage > 0.1f) //Don't do anything unless the drag was significant
                {
                    rb.velocity *= 0.3f;
                    power = swimmingPower;
                    if(wasSwimming)
                        ChangeAnimationState(SWIM);
                    sfx.PlaySFX("Swim");
                }
            }
            else //Jump
            {
                fillPercentage = Mathf.InverseLerp(0, (maxJumpAimLineLength), (secondLinePoint - transform.position).magnitude);

                if (fillPercentage > 0.1f) //Don't do anything unless the drag was significant
                {
                    //rb.velocity = Vector2.zero;
                    rb.velocity *= 0.3f;
                    power = jumpingPower;
                    ChangeAnimationState(JUMP);

                    if (Time.timeScale != 0f)
                        sfx.PlaySFX("Jump");

                    StartCoroutine(JumpAnimationTimer());
                }
            }

            if (dried)
                power = 5;

            if (fillPercentage > 0.1f) //Don't do anything unless the drag was significant
            {
                Vector3 clampedForce = (force.normalized) * fillPercentage * (power) * rb.mass;
                rb.AddForce(clampedForce, ForceMode2D.Impulse);

                if (isSwimming)
                {
                    if(species != Species.Froglet)
                        SetSpriteRotation(secondLinePoint - transform.position, 0);
                    else
                    {
                        if(rb.velocity.x > 0)
                            SetSpriteRotation(secondLinePoint - transform.position, -18.4f);
                        else
                            SetSpriteRotation(secondLinePoint - transform.position, 18.4f);
                    }
                }
            }

            tongueLauncher.lr.positionCount = 0;
            coyoteTimeCounter = 0;
            jumpBufferCounter = 0;

            jump = false;
        }
    }
    //////////////////////////////////////////////////////////////////////////////////////////////// SPECIES CONFIGURATION //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

    void SetSpecies() 
    {
        if (PlayerPrefs.GetString("Species") == "Default")
            species = Species.Default;
        else if (PlayerPrefs.GetString("Species") == "Tree Frog")
            species = Species.Treefrog;
        else if (PlayerPrefs.GetString("Species") == "Froglet")
            species = Species.Froglet;
        else if (PlayerPrefs.GetString("Species") == "Bullfrog")
            species = Species.BullFrog;
        else if (PlayerPrefs.GetString("Species") == "Poison Dart Frog")
            species = Species.PoisonDartFrog;
        else
        {
            PlayerPrefs.SetString("Species", "Default");
            species = Species.Default;
        }
    }

    void ConfigureSpecies()
    {
        switch (species) 
        {
            case Species.Default:
                jumpingPower = 36;
                swimmingPower = 37f;
                maxJumpAimLineLength = 10;
                maxSwimAimLineLength = 8;
                oxygenAndMoistureController.oxygenLossRate = 0.07f;
                oxygenAndMoistureController.moistureLossRate = 0.08f;
                tongueLauncher.baseMaxDistance = 25;

                spriteRenderer.sprite = initialSprites[0];
                break;
            case Species.Treefrog:
                jumpingPower = 44f;
                swimmingPower = 29;
                maxJumpAimLineLength = 12;
                maxSwimAimLineLength = 7;
                oxygenAndMoistureController.oxygenLossRate = 0.1f;
                oxygenAndMoistureController.moistureLossRate = 0.07f;
                tongueLauncher.baseMaxDistance = 33;
                tongueLauncher.grappleStrength = 25;

                sprite.localScale = new Vector3(0.4f, 0.4f, 0.4f);
                spriteRenderer.sprite = initialSprites[1];
                ConfigureSpeciesAnimations("Tree", false);
                break;
            case Species.Froglet:
                jumpingPower = 27;
                swimmingPower = 45f;
                maxJumpAimLineLength = 6;
                maxSwimAimLineLength = 13;
                oxygenAndMoistureController.oxygenLossRate = 0f;
                oxygenAndMoistureController.moistureLossRate = 0.15f;
                tongueLauncher.baseMaxDistance = 18;

                sprite.localScale = new Vector3(0.45f, 0.45f, 0.45f);
                spriteRenderer.sprite = initialSprites[2];
                ConfigureSpeciesAnimations("", true);
                break;
            case Species.BullFrog:
                jumpingPower = 38;
                swimmingPower = 40;
                maxJumpAimLineLength = 10;
                maxSwimAimLineLength = 10;
                oxygenAndMoistureController.oxygenLossRate = 0.06f;
                oxygenAndMoistureController.moistureLossRate = 0.06f;
                tongueLauncher.baseMaxDistance = 30;
                tongueLauncher.grappleStrength = 21;

                normalCol.enabled = false;
                bullfrogCol.enabled = true;
                col = bullfrogCol;
                grapplePointDetector.localScale = new Vector3(1.9f, 1.9f, 1.9f);

                sprite.localScale = new Vector3(1f, 1f, 1f);
                spriteRenderer.sprite = initialSprites[3];
                ConfigureSpeciesAnimations("Bull", false);
                break;
            case Species.PoisonDartFrog:
                jumpingPower = 40;
                swimmingPower = 35;
                maxJumpAimLineLength = 10;
                maxSwimAimLineLength = 10;
                oxygenAndMoistureController.oxygenLossRate = 0.08f;
                oxygenAndMoistureController.moistureLossRate = 0.09f;
                tongueLauncher.baseMaxDistance = 25;
                tongueLauncher.grappleStrength = 23;

                poisonAvailable = true;
                activeDartFrogPoisonParticles = Instantiate(dartFrogPoisonParticles, transform.position, Quaternion.identity, transform);

                sprite.localScale = new Vector3(0.8f, 0.8f, 0.8f);
                spriteRenderer.sprite = initialSprites[4];
                ConfigureSpeciesAnimations("PoisonDart", false);
                break;
        }
        tongueLauncher.rangeCircleOffset *= 1 / sprite.localScale.x;
    }
    void ConfigureSpeciesAnimations(string modifier, bool froglet)
    {
        SLIDE = modifier + SLIDE; 
        IDLE = modifier + IDLE;
        JUMP = modifier + JUMP;
        MIDAIR = modifier + MIDAIR;
        GRAPPLE = modifier + GRAPPLE;
        READY_SWIM = modifier + READY_SWIM;
        SWIM = modifier + SWIM; 
        MIDSWIM = modifier + MIDSWIM;
        STRAIGHT_JUMP = modifier + STRAIGHT_JUMP;
        STRAIGHT_GRAPPLE = modifier + STRAIGHT_GRAPPLE;

        if(froglet)
        {
            SLIDE = "FrogletSlide";
            IDLE = "FrogletIdle";
            JUMP = "FrogletJump";
            MIDAIR = "FrogletMidair";
            GRAPPLE = "FrogletGrapple";
            READY_SWIM = "FrogletReadySwim";
            SWIM = "FrogletSwim";
            MIDSWIM = "FrogletMidswim";
            STRAIGHT_JUMP = "FrogletStraightJump";
            STRAIGHT_GRAPPLE = "FrogletStraightGrapple";
        }
    }
    IEnumerator RibbitRandomly()
    {
        float delayTime = Random.Range(15, 30);
        float chanceOfRibbiting = 3;
        yield return new WaitForSeconds(delayTime);
        if(Random.Range(0, 10) <= chanceOfRibbiting) 
        {
            sfx.PlayRibbit();
        }
        StartCoroutine(RibbitRandomly());
    }
    //////////////////////////////////////////////////////////////////////////////////////////////// ANIMATION //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    void AnimateFrog()
    {
        if (eaten || grabbedByPoisonedPredator)
        {
            ChangeAnimationState(GRAPPLE);
        }
        else
        {
            //When the player is grounded, they are idle when their velocity is less than 1 and sliding otherwise
            if (isGrounded && !draggingStarted && !jumpAnimationPlaying)
            {
                if (Mathf.Abs(rb.velocity.x) <= 1f && !isSliding)
                {
                    ChangeAnimationState(IDLE);
                    
                }
                else
                {
                    ChangeAnimationState(SLIDE);
                }
            }

            //Make the player always slide on slides
            if (isSliding && !jumpAnimationPlaying)
            {
            
                ChangeAnimationState(SLIDE);
            }

            //Open mouth while aiming
            if (tongueLauncher.aimingGrapple && !isGrounded && !isSwimming)
            {
                if (currentState == MIDAIR)
                    ChangeAnimationState(GRAPPLE);

                if (currentState == STRAIGHT_JUMP)
                    ChangeAnimationState(STRAIGHT_GRAPPLE);
            }

            //Open mouth while grappling, but only set rotation at the moment the frog begins grappling
            if (tongueLine.isGrappling)
            {
                ChangeAnimationState(STRAIGHT_GRAPPLE);

                if (!grappleRotationSet)
                {
                    SetSpriteRotation((Vector3)tongueLauncher.grapplePoint - tongueLauncher.tongueAimLineStartpoint.position, 0);
                    grappleRotationSet = true;
                }
            }
            else //When the grapple is done, resume midair animation
            {
                grappleRotationSet = false;
                if ((currentState == GRAPPLE || currentState == STRAIGHT_GRAPPLE) && !tongueLauncher.aimingGrapple)
                {
                    ChangeAnimationState(STRAIGHT_JUMP);
                }
            }

            //If you jump into the water, start MIDSWIM animation
            if (isSwimming && currentState != SWIM && currentState != READY_SWIM && jumpBufferCounter <= -0.1f)
            {
                ChangeAnimationState(MIDSWIM);
            }

            //If you jump out of the water, start MIDJUMP animation
            if (!tongueLauncher.aimingGrapple && !isSwimming && !isSliding && (currentState == READY_SWIM || currentState == SWIM || currentState == MIDSWIM))
            {
                ChangeAnimationState(STRAIGHT_JUMP);

            }
            //If you jump out of the water while aiming grapple, start GRAPPLE animation
            if (tongueLauncher.aimingGrapple && !isSwimming && !isSliding && (currentState == READY_SWIM || currentState == SWIM || currentState == MIDSWIM))
            {
                ChangeAnimationState(GRAPPLE);
            }

            //Scrapped change that made slide go to straight jump when not grounded
            if(currentState == IDLE && !isGrounded)
            {
                if (tongueLine.isGrappling)
                    ChangeAnimationState(STRAIGHT_GRAPPLE);
                else
                    ChangeAnimationState(STRAIGHT_JUMP);
            } 

            //While in a straight animation state, rotate to match velocity
            if (currentState == STRAIGHT_JUMP || currentState == STRAIGHT_GRAPPLE)
            {
                int offset = 15;
                if (species == Species.BullFrog) offset = 0;
                if(rb.velocity.magnitude >= 1)
                {
                    if (sprite.localScale.x > 0)
                    {
                        if (!tongueLine.isGrappling)
                        {
                            SetSpriteRotation(((Vector3)rb.velocity) - tongueLauncher.tongueAimLineStartpoint.localPosition, offset);
                        }
                        else
                        {
                            SetSpriteRotation(((Vector3)tongueLauncher.grapplePoint) - tongueLauncher.tongueAimLineStartpoint.position, offset);
                        }
                    }
                    else
                    {
                        if (!tongueLine.isGrappling)
                        {
                            SetSpriteRotation(((Vector3)rb.velocity) - tongueLauncher.tongueAimLineStartpoint.localPosition, -offset);
                        }
                        else
                        {
                            SetSpriteRotation(((Vector3)tongueLauncher.grapplePoint) - tongueLauncher.tongueAimLineStartpoint.position, -offset);
                        }
                    }
                    
                }
            }   
            if(currentState == IDLE)
            {
                sprite.rotation = Quaternion.identity;
            }

            //When you jump or go midjump, wait an interval, then if you are still in that state, switch to straight 
            if(!jumpTransferCoroutineStarted && (currentState == JUMP || currentState == MIDAIR || (currentState == SLIDE && !isGrounded)))
            {
                StartCoroutine(SwitchFromJumpToStraight(currentState));
            }

            //
            if(currentState == SLIDE && coyoteTimeCounter <= 0 && !isGrounded && !isSliding)
            {
                ChangeAnimationState(STRAIGHT_JUMP);
            }

            //Resets the rotation after you leave the water
            if (wasWet && !wet)
            {
                if(!isGrounded && exitWaterSFXAllowed)
                    sfx.PlaySFX("Exit Water");
            }
            if(wet && !wasWet)
            {
                StartCoroutine(AllowExitSFXDelay());
                if(!isGrounded)
                {
                    if (rb.velocity.magnitude < 40)
                        sfx.PlaySFX("Splash");
                    else
                        sfx.PlaySFX("Big Splash");
                }
            }
        }
        wasSwimming = isSwimming;
        wasWet = wet;
    }
    IEnumerator SwitchFromJumpToStraight(string state)
    {
        jumpTransferCoroutineStarted = true;
        yield return new WaitForSeconds(1.5f);
        if ((currentState == state && state != SLIDE) || (state == SLIDE && currentState == SLIDE && !isGrounded))
        {
            ChangeAnimationState(STRAIGHT_JUMP);
        }
        jumpTransferCoroutineStarted = false;
    }
    IEnumerator AllowExitSFXDelay()
    {
        exitWaterSFXAllowed = false;
        yield return new WaitForSeconds(0.2f);
        exitWaterSFXAllowed = true;
    }
    void SetDirection()
    {
        
        if (rb.velocity.x < -3f && facingRight)
        {
            // Player was moving in negative direction
            facingRight = false;
        }
        if (rb.velocity.x > 3f && !facingRight)
        {
            // Player was moving in positive direction
            facingRight = true;
        }

        if(facingRight)
        {
            sprite.localScale = new Vector3(-Mathf.Abs(initialSpriteScale.x), initialSpriteScale.y, initialSpriteScale.z);
        }
        else
            sprite.localScale = new Vector3(Mathf.Abs(initialSpriteScale.x), initialSpriteScale.y, initialSpriteScale.z);
    }
    void ChangeAnimationState(string newState) 
    {
        //Stop the same animation from interrupting itself
        if (currentState == newState) return;
        if (species == Species.Froglet && currentState == SWIM && newState == READY_SWIM) return;

        //Play the new animation
        animator.Play(newState);
        currentState = newState;

        //Fix the offset so the frog sits on the ground correctly
        if(currentState == IDLE) 
        {
            if (IDLE == "FrogIdle")
                sprite.transform.localPosition = new Vector3(initialSpriteOffset.x, initialSpriteOffset.y + 0.4f, 0);
            else if (IDLE == "TreeFrogIdle")
                sprite.transform.localPosition = new Vector3(initialSpriteOffset.x, initialSpriteOffset.y + 0.25f, 0);
            else if (IDLE == "FrogletIdle")
                sprite.transform.localPosition = new Vector3(initialSpriteOffset.x, initialSpriteOffset.y + 0.15f, 0);
            else if (IDLE == "BullFrogIdle")
                sprite.transform.localPosition = new Vector3(initialSpriteOffset.x, initialSpriteOffset.y + 0.0f, 0);
            else if (IDLE == "PoisonDartFrogIdle")
                sprite.transform.localPosition = new Vector3(initialSpriteOffset.x, initialSpriteOffset.y + 0.3f, 0);
            sprite.transform.localScale = new Vector3(initialSpriteScale.x, Mathf.Abs(initialSpriteScale.y), 1);
        }
        else if (currentState == SLIDE) 
        {
            if(SLIDE == "FrogSlide")
                sprite.transform.localPosition = new Vector3(initialSpriteOffset.x, initialSpriteOffset.y - 0.2f, 0);
            else if (SLIDE == "TreeFrogSlide")
                sprite.transform.localPosition = new Vector3(initialSpriteOffset.x, initialSpriteOffset.y - 0.25f, 0);
            else if (SLIDE == "FrogletSlide")
                sprite.transform.localPosition = new Vector3(initialSpriteOffset.x, initialSpriteOffset.y - 0.25f, 0);
            else if (SLIDE == "BullFrogSlide")
                sprite.transform.localPosition = new Vector3(initialSpriteOffset.x, initialSpriteOffset.y - 0.25f, 0);
            else if (SLIDE == "PoisonDartFrogSlide")
                sprite.transform.localPosition = new Vector3(initialSpriteOffset.x, initialSpriteOffset.y - 0.2f, 0);

            sprite.transform.localScale = new Vector3(initialSpriteScale.x, Mathf.Abs(initialSpriteScale.y), 1);
        }
        else
        {
            sprite.transform.localPosition = new Vector3(initialSpriteOffset.x, initialSpriteOffset.y, 0);
        }
    }

    void SetSpriteRotation(Vector3 target, float offset) 
    {
        float angle;
        angle = Mathf.Atan2(target.y, target.x) * Mathf.Rad2Deg;
        if (rb.velocity.x < 0f)
        {
            angle -= 180;
        }
        Quaternion targetRotation = Quaternion.Euler(0f, 0f, angle + offset);
        sprite.transform.rotation = targetRotation;

        if (sprite.transform.eulerAngles.z > 90 && sprite.transform.eulerAngles.z < 270)
        {
            sprite.transform.localScale = new Vector3(Mathf.Abs(initialSpriteScale.x), -Mathf.Abs(initialSpriteScale.y), 1); // Flip the sprite
        }
        else
        {
            sprite.transform.localScale = new Vector3(Mathf.Abs(initialSpriteScale.x), Mathf.Abs(initialSpriteScale.y), 1);  // Reset the sprite scale
        }
        SetDirection();
    }

    IEnumerator JumpAnimationTimer() 
    {
        jumpAnimationPlaying = true;
        yield return new WaitForSeconds(animator.GetCurrentAnimatorStateInfo(0).length * 2);
        jumpAnimationPlaying = false;
    }
    //////////////////////////////////////////////////////////////////////////////////////////////// COLLISIONS //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
    private void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.layer == 6 || collision.gameObject.layer == 13 || collision.gameObject.layer == 9) //ground, slugpath, or riverbed decoration
        {
            if(collision.gameObject.transform.parent != null && collision.gameObject.transform.parent.CompareTag("Lilypad") || collision.gameObject.CompareTag("Lilypad"))
            {
                sfx.PlaySFX("Lilypad Land");
            }
            if ((collision.gameObject.transform.parent != null && collision.gameObject.transform.parent.CompareTag("Log")) || collision.gameObject.CompareTag("Log"))
            {
                sfx.PlaySFX("Log Land");
            }
            if (collision.gameObject.GetComponent<CypressTag>() != null)
            {
                sfx.PlaySFX("Cypress Land");
            }
            if (collision.gameObject.transform.parent != null && collision.gameObject.transform.parent.CompareTag("Rock") || collision.gameObject.CompareTag("Rock"))
            {
                sfx.PlaySFX("Rock");
            }
        }
        if (collision.gameObject.layer == 14 || collision.gameObject.layer == 3) //mud
        {
            sfx.PlaySFX("Mud Land");
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if ((collision.gameObject.layer == 7 || collision.gameObject.layer == 12) 
            || (species == Species.BullFrog && collision.gameObject.CompareTag("BPrey"))
            || (species == Species.PoisonDartFrog && collision.gameObject.CompareTag("Poisonous"))) //Prey
        {
            AddPreyScore(collision);

            //When grappling to prey continue momentum and destroy prey
            if (tongueLine.isGrappling && tongueLauncher.grappleTarget != null && collision.transform.parent == tongueLauncher.grappleTarget.transform)
            {
                tongueLauncher.grapplePointIdentified = false;
                rb.gravityScale = defaultGravityScale;

                power = 7;
                if (collision.gameObject.layer == 12)
                    power = 2;

                rb.AddForce(power * rb.mass * tongueLauncher.addedForce.normalized, ForceMode2D.Impulse);

                //Remove the aim line when the frog eats prey
                tongueLauncher.lr.positionCount = 0;

                //Cancel the grapple
                tongueLine.enabled = false;
                tongueLine.isGrappling = false;
                tongueLauncher.grapplePointIdentified = false;
                tongueLauncher.grappleTarget = null;
            }

            //When you eat a spider, don't destroy its web
            if (collision.gameObject.transform.parent.name == "Spider(Clone)" || collision.gameObject.transform.parent.name == "Spider")
            {
                Destroy(collision.gameObject);
                Destroy(collision.transform.parent.gameObject.GetComponentInChildren<BoxCollider2D>());
            }
            else
            {
                Destroy(collision.transform.parent.gameObject);
            }
        }

        //Make a noise when the player bounces off a cichlid
        if (collision.gameObject.transform.parent != null)
        {
            if (collision.gameObject.transform.parent.name == "Cichlid" || collision.gameObject.transform.parent.name == "Cichlid(Clone)")
            {
                if (species != Species.BullFrog)
                    sfx.PlaySFX("Cichlid Bounce");
            }
        }

        if (collision.gameObject.layer == 11) //Cattail
        {
            if (tongueLauncher.grappleTarget != null && collision.transform == tongueLauncher.grappleTarget.transform)
            {
                tongueLauncher.grapplePointIdentified = false;
                rb.gravityScale = defaultGravityScale;
                power = 25;

                sfx.PlaySFX("Cattail");

                //Spawn Cattail Particles
                float angle = Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg;
                GameObject particles = Instantiate(cattailParticles, collision.ClosestPoint(transform.position), Quaternion.Euler(-angle, 90, 0));
                Destroy(particles, 3);

                // Launch the player up and to the right if they are coming from the left
                if (tongueLauncher.grappleTarget != null && (tongueLauncher.grappleTarget.transform.position.x - transform.position.x >= 0))
                    rb.AddForce((Vector2.one - new Vector2(0,0.4f)) * power * rb.mass, ForceMode2D.Impulse); // approx. 60 degree angle

                else //Launch the player up and to the left if they are coming from the right
                    rb.AddForce(new Vector2(-Vector2.one.x, new Vector2(0, 0.7f).y) * power * rb.mass, ForceMode2D.Impulse); // approx. -60 degree angle

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
        if (collision.gameObject.CompareTag("NoSwim"))
            cantSwim = true;

        //When the player gets hit by a predator, they die
        if (collision.gameObject.CompareTag("Predator"))
        {
            bool eatenByAlligator = false;
            if ((collision.transform.parent.transform.parent != null))
            {
                if (collision.transform.parent.transform.parent.gameObject.name == "ALLIGATOR")
                {
                    
                    eatenByAlligator = true;
                }
            }
            if (!drowned && (!poisonAvailable) && !invulnerable 
                || eatenByAlligator)
            {
                killerGrab = collision.gameObject.GetComponent<PredatorGrab>();

                if (!killerFinalized)
                {
                    if (collision.transform.parent.transform.parent != null)
                    {
                        killer = collision.transform.parent.transform.parent.gameObject.name;
                    }
                    else if (collision.transform.parent != null)
                    {
                        killer = collision.transform.parent.gameObject.name;
                    }
                    else
                    {
                        killer = collision.gameObject.name;
                    }
                }
                

                eaten = true;
                dead = true;
            }
            else if (poisonAvailable) 
            {
                poisonAvailable = false;
                collision.gameObject.tag = "Untagged";
                invulnerable = true;
                StartCoroutine(DartFrogPoison(collision));
            }
        }

        if (collision.gameObject.CompareTag("Water"))
        {
            wet = true;
            if (!splashParticleCooldown && !isSwimming)
            {
                GameObject splash = Instantiate(splashParticles, (Vector3)collision.ClosestPoint(transform.position), Quaternion.Euler(-90, 0, 0));
                ParticleSystem.MainModule ps = splash.GetComponent<ParticleSystem>().main;
                ps.startSpeedMultiplier = Mathf.Abs((rb.velocity.y));

                Destroy(splash, 1);
                StartCoroutine(SplashCooldown());
            }
        }

        if (collision.gameObject.CompareTag("BiomeSwapper"))
        {
            changeBiome = true;
            Destroy(collision.gameObject);
        }

        if (collision.gameObject.CompareTag("CameraTransition"))
        {
            collision.transform.parent.GetComponentInChildren<ReturnBlocker>().GetComponent<PolygonCollider2D>().enabled = true;  
            biomeIn = levelGenerator.biomeSpawning.ToString();
            transitionCamera = true;
            transitioningBiome = true;
            Destroy(collision.gameObject);
        }
        if (collision.gameObject.CompareTag("EndBiomeTransition"))
        {
            transitioningBiome = false;
        }

        if (collision.gameObject.layer == 14) //When the player is on mud, it doesnt lose moisture
        {
            saturated = true;
        }

        //Die to poison spiders
        if (collision.gameObject.CompareTag("Poisonous") && species != Species.PoisonDartFrog)
        {
            sfx.PlaySFX("Poison");
            dead = true;
            poisoned = true;
        }

        //Plant SFX
        if ((collision.gameObject.transform.parent != null && collision.gameObject.transform.parent.CompareTag("Plant")) || collision.gameObject.CompareTag("Plant"))
        {
            if(currentPlant == null)
            {
                sfx.PlaySFX("Plant");
                currentPlant = collision.gameObject.transform.parent.gameObject;
            }
            if(collision.gameObject.transform.parent.gameObject != currentPlant)
            {
                sfx.PlaySFX("Plant");
                currentPlant = collision.gameObject.transform.parent.gameObject;
            }

        }
    }
    IEnumerator SplashCooldown()
    {
        splashParticleCooldown = true;
        yield return new WaitForSeconds(0.2f);
        splashParticleCooldown = false;
    }
    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.gameObject.layer == 11) //Cattail
        {
            if (tongueLauncher.grappleTarget != null && collision.transform == tongueLauncher.grappleTarget.transform)
            {
                tongueLauncher.grapplePointIdentified = false;
                rb.gravityScale = defaultGravityScale;
                power = 25;

                sfx.PlaySFX("Cattail");

                //Spawn Cattail Particles
                float angle = Mathf.Atan2(rb.velocity.y, rb.velocity.x) * Mathf.Rad2Deg;
                GameObject particles = Instantiate(cattailParticles, collision.ClosestPoint(transform.position), Quaternion.Euler(-angle, 90, 0));
                Destroy(particles, 3);

                // Launch the player up and to the right if they are coming from the left
                if (tongueLauncher.grappleTarget != null && (tongueLauncher.grappleTarget.transform.position.x - transform.position.x >= 0))
                    rb.AddForce((Vector2.one - new Vector2(0, 0.4f)) * power * rb.mass, ForceMode2D.Impulse); // approx. 60 degree angle

                else //Launch the player up and to the left if they are coming from the right
                    rb.AddForce(new Vector2(-Vector2.one.x, new Vector2(0, 0.7f).y) * power * rb.mass, ForceMode2D.Impulse); // approx. -60 degree angle

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

        //When sliding, match the sprite rotation to the ground
        if ((collision.gameObject.layer == 14 || collision.gameObject.layer == 6) && currentState == SLIDE)
        {
            Vector2 normal = (Vector2)transform.position - collision.ClosestPoint(transform.position);
            float angle = Mathf.Atan2(normal.y, normal.x) * Mathf.Rad2Deg;

            if(transform.position.y > collision.ClosestPoint(transform.position).y)
            {
                if(species != Species.Froglet)
                    sprite.transform.rotation = Quaternion.Euler(0, 0, angle - 90);
                else
                {
                    if(facingRight)
                        sprite.transform.rotation = Quaternion.Euler(0, 0, angle - 108.4f);
                    else
                        sprite.transform.rotation = Quaternion.Euler(0, 0, angle - 72.6f);
                }

            }
        }

        //The player swims when they are in water, not grounded, and not in a no-swim-zone
        if (collision.gameObject.tag == "Water")
        {
            wet = true;
            if(!isGrounded && !cantSwim)
            {
                isSwimming = true;
            }
        }
        if (collision.gameObject.layer == 14 && isSliding) //When the player is on mud, it doesnt lose moisture
        {
            MudParticles(collision);
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        //When the player leaves a no-swim-zone, they can swim
        if (collision.gameObject.CompareTag("NoSwim") && cantSwim)
            cantSwim = false;
        if (collision.gameObject.CompareTag("Water"))
        {
            isSwimming = false;
            wet = false;
        }
        if (collision.gameObject.layer == 14) //When the player is on mud, it doesnt lose moisture
        {
            saturated = false;
        }
    }

    void AddPreyScore(Collider2D collision)
    {
        if (collision.transform.parent != null)
        {
            sfx.PlaySFX("Eat");
            //if beetle, add 5
            if (collision.gameObject.transform.parent.name == "Beetle(Clone)")
            {
                scoreController.SpawnFloatingText(5, transform.position, Color.white);
                scoreController.Score(5);

                if (PlayerPrefs.GetInt("treeFrogUnlocked", 0) == 1)
                {
                    PlayerPrefs.SetInt("InsectsEaten", PlayerPrefs.GetInt("InsectsEaten", 0) + 1);
                }
            }
            //if fly or waterstrider, add 10
            if (collision.gameObject.transform.parent.name == "Fly(Clone)" || collision.gameObject.transform.parent.name == "WaterStrider(Clone)")
            {
                scoreController.SpawnFloatingText(10, transform.position, Color.white);
                scoreController.Score(10);

                if (PlayerPrefs.GetInt("treeFrogUnlocked", 0) == 1)
                {
                    PlayerPrefs.SetInt("InsectsEaten", PlayerPrefs.GetInt("InsectsEaten", 0) + 1);
                }
            }
            //if slug or snail, add 15
            else if (collision.gameObject.transform.parent.name == "Slug(Clone)" || collision.gameObject.transform.parent.name == "Snail(Clone)")
            {
                scoreController.SpawnFloatingText(15, transform.position, Color.white);
                scoreController.Score(15);
            }

            //if minnow, add 15
            else if (collision.gameObject.transform.parent.name == "BogMinnow(Clone)" || collision.gameObject.transform.parent.name == "AmazonMinnow(Clone)")
            {
                if (PlayerPrefs.GetInt("frogletUnlocked") == 1)
                {
                    PlayerPrefs.SetInt("FishEaten", PlayerPrefs.GetInt("FishEaten", 0) + 1);
                }

                scoreController.SpawnFloatingText(15, transform.position, Color.white);
                scoreController.Score(15);
            }

            //If spider, add 20
            else if ((collision.gameObject.transform.parent.name == "Spider" || collision.gameObject.transform.parent.name == "Spider(Clone)"))
            {
                if (!collision.gameObject.transform.parent.gameObject.GetComponent<SpiderBehavior>().poisonous)
                {
                    scoreController.SpawnFloatingText(20, transform.position, Color.white);
                    scoreController.Score(20);
                }
                else //If poison dart frog eats a poisonous spider, add green 50
                {
                    if (species == Species.PoisonDartFrog)
                    {
                        scoreController.SpawnFloatingText(50, transform.position, Color.green);
                        scoreController.Score(50);
                    }
                }
            }

            //if dragonfly, add 25
            else if (collision.gameObject.transform.parent.name == "Dragonfly(Clone)")
            {
                scoreController.SpawnFloatingText(25, transform.position, Color.white);
                scoreController.Score(25);

                if (PlayerPrefs.GetInt("treeFrogUnlocked", 0) == 1)
                {
                    PlayerPrefs.SetInt("InsectsEaten", PlayerPrefs.GetInt("InsectsEaten", 0) + 1);
                }
            }
            //if cichlid, add blue 50
            else if (collision.gameObject.transform.parent.name == "Cichlid(Clone)")
            {
                scoreController.SpawnFloatingText(50, transform.position, Color.blue);
                scoreController.Score(50);
            }
            //if bird, add brown 50
            else if (collision.gameObject.transform.parent.name == "Sparrow(Clone)")
            {
                scoreController.SpawnFloatingText(50, transform.position, sparrowScoreColor);
                scoreController.Score(50);
            }
            //if goldfish, add yellow 100
            else if (collision.gameObject.transform.parent.name == "Goldfish(Clone)")
            {
                sfx.PlaySFX("Eat2");
                if (PlayerPrefs.GetInt("frogletUnlocked", 0) == 1)
                {
                    PlayerPrefs.SetInt("FishEaten", PlayerPrefs.GetInt("FishEaten", 0) + 1);
                }

                scoreController.SpawnFloatingText(100, transform.position, Color.yellow);
                scoreController.Score(100);
            }
            EatParticles(collision);
        }
    }
    void MudParticles(Collider2D col)
    {
        Vector2 normal = (Vector2)transform.position - col.ClosestPoint(transform.position);
        float angle = Mathf.Atan2(normal.y,normal.x) * Mathf.Rad2Deg;
        if (rb.velocity.x > 0)
        {
            GameObject mp = Instantiate(mudParticles, col.ClosestPoint(transform.position), Quaternion.Euler(-angle - 90, 90, 0));
            ParticleSystem.MainModule ps = mp.GetComponent<ParticleSystem>().main;
            ps.startSpeedMultiplier = rb.velocity.magnitude;
            Destroy(mp, 1);
        }
        else if (rb.velocity.x < 0)
        {
            GameObject mp = Instantiate(mudParticles, col.ClosestPoint(transform.position), Quaternion.Euler(-angle + 90, 90, 0));
            ParticleSystem.MainModule ps = mp.GetComponent<ParticleSystem>().main;
            ps.startSpeedMultiplier = rb.velocity.magnitude;
            Destroy(mp, 1);
        }
    }
    void EatParticles(Collider2D col)
    {
        GameObject ep;
        if (col.gameObject.transform.parent.name == "Sparrow(Clone)")
            ep = Instantiate(sparrowEatParticles, /*col.*/transform.position, Quaternion.identity);
        else
            ep = Instantiate(eatParticles, col.transform.position, Quaternion.identity);

        Destroy(ep, 1);
    }
    IEnumerator DartFrogPoison(Collider2D col) 
    {
        activeDartFrogPoisonParticles.transform.SetParent(col.transform.parent);

        if (col.transform.parent.name == "Fish" || col.transform.parent.name == "Fish (1)" || col.transform.parent.name == "Fish (2)" 
            || col.transform.parent.name == "Fish (3)" || col.transform.parent.name == "Fish (4)" || col.transform.parent.name == "Fish (5)")
        {
            activeDartFrogPoisonParticles.transform.localPosition = new Vector3(-1.7f, 1.1f, 0);
            activeDartFrogPoisonParticles.transform.localScale = new Vector3(8.5f, 4, 4);
        }
        else if (col.transform.parent.name == "Heron(Clone)" || col.transform.parent.name == "Heron")
        {
            activeDartFrogPoisonParticles.transform.localPosition = new Vector3(18.2f, -0.9f, 0);
            activeDartFrogPoisonParticles.transform.localScale = new Vector3(6, 6, 6);
            activeDartFrogPoisonParticles.GetComponent<ParticleSystemRenderer>().sortingOrder = 11;
        }
        else if (col.transform.parent.name == "Gar(Clone)" || col.transform.parent.name == "Gar")
        {
            activeDartFrogPoisonParticles.transform.localPosition = Vector3.zero;
            activeDartFrogPoisonParticles.transform.localScale = new Vector3(10, 3, 8);
            activeDartFrogPoisonParticles.GetComponent<ParticleSystemRenderer>().sortingOrder = 7;
        }
        else if (col.transform.parent.name == "Arapaima(Clone)" || col.transform.parent.name == "Arapaima")
        {
            activeDartFrogPoisonParticles.transform.localPosition = new Vector3(3, 1.47f, 0);
            activeDartFrogPoisonParticles.transform.localScale = new Vector3(18.75f, 4.625f, 6);
            activeDartFrogPoisonParticles.GetComponent<ParticleSystemRenderer>().sortingOrder = 11;
        }
        else if (col.transform.parent.name == "Piranha(Clone)" || col.transform.parent.name == "Piranha")
        {
            activeDartFrogPoisonParticles.transform.localPosition = new Vector3(2, 0.54f, 0);
            activeDartFrogPoisonParticles.transform.localScale = new Vector3(3, 3, 3);
            activeDartFrogPoisonParticles.GetComponent<ParticleSystemRenderer>().sortingOrder = 2;
        }
        else if (col.transform.parent.name == "Falcon(Clone)" || col.transform.parent.name == "Falcon" || col.transform.parent.name == "Sprite")
        {
            eatenByFalcon = true;
            activeDartFrogPoisonParticles.transform.localPosition = new Vector3(0.58f, -0.28f, 0);
            activeDartFrogPoisonParticles.transform.localScale = new Vector3(3, 6, 1.5f);
            activeDartFrogPoisonParticles.transform.localRotation = Quaternion.Euler(0,0,140);
            activeDartFrogPoisonParticles.GetComponent<ParticleSystemRenderer>().sortingOrder = 12;
        }


        col.gameObject.GetComponent<PredatorGrab>().poisoned = true;
        yield return new WaitForSeconds(10);
        invulnerable = false;
    }
}