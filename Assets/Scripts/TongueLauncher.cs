using System.Collections;
using UnityEngine;

public class TongueLauncher : MonoBehaviour
{
    [Header("Scripts Ref:")]
    [SerializeField] TongueLine tongueLine;
    [SerializeField] GrapplePointDetector grapplePointDetector;
    [SerializeField] PlayerController playerController;
    [SerializeField] PauseButtons pauseScript;

    [Header("Layers Settings:")]
    [SerializeField] private bool grappleToAll = false;

    [Header("Main Camera:")]
    [SerializeField] Camera m_camera;

    [Header("Transform Ref:")]
    [SerializeField] Transform player;
    [SerializeField] Transform gunHolder;
    [SerializeField] Transform gunPivot;
    public Transform tongueAimLineStartpoint;
    public Transform firePoint;
    public LineRenderer lr;
    public Transform tongueRangeCircle;
    public float rangeCircleOffset = 1;

    [Header("Physics Ref:")]
    [SerializeField] Rigidbody2D rb;

    [Header("Distance:")]
    public float grappleStrength = 20;
    [SerializeField] private bool hasMaxDistance = false;
    public float baseMaxDistance;
    private float maxDistance;
    private float shrunkDistance = 8;

    private enum LaunchType
    {
        Transform_Launch,
        Physics_Launch
    }

    [Header("Launching:")]
    [SerializeField] private bool launchToPoint = true;
    [SerializeField] private LaunchType launchType = LaunchType.Physics_Launch;
    [SerializeField] private float launchSpeed = 1;
    public AnimationCurve launchCurve;

    [Header("No Launch To Point")]
    [HideInInspector] public Vector2 grapplePoint;
    [HideInInspector] public Vector2 grappleDistanceVector;

    [Header("Touch")]
    public Vector3 dragStartPosition;
    public Vector3 dragEndPosition;
    Vector3 secondLinePoint;
    Touch touch;
    public bool touchEnded;

    [Header("My Variables")]
    [SerializeField] float timeSlow = 0.3f;
    [SerializeField] float aimMultiplier = 2;
    public GameObject grappleTarget;
    public bool grapplePointIdentified = false;
    private Vector2 hitPoint;
    private Collider2D hitCollider;
    [HideInInspector] public Vector2 addedForce;
    public bool aimingGrapple;
    bool mouseAllowedToDrag;

    private void Start()
    {
        tongueLine.enabled = false;
    }

    private void Update()
    {
        if (!playerController.dead && !pauseScript.pause)
        {
            GetTouch();
        }
        SwitchFromPointToTransform();
        KeepGrapplePointOnCollider();
        TransitionAimLine();
        DetatchWhenClose();

        if (playerController.dried)
        {
            maxDistance = shrunkDistance;
        }
        else
        {
            maxDistance = baseMaxDistance;
        }
        if(playerController.isGrounded || playerController.isSwimming)
        {
            aimingGrapple = false;
        }

        if(playerController.dead)
        {
            lr.positionCount = 0;
        }
    }
    private void FixedUpdate()
    {
        SetRangeCircle();
        SlowTime();
        Launch();

        if (tongueLine.isGrappling)
        {
            Grapple();
        }
    }
    void GetTouch()
    {
        //Identify touch
        if (Input.touchCount > 0)
        {
            touch = Input.GetTouch(0);

            //Get initial touch position
            if (touch.phase == TouchPhase.Began)
            {
                dragStartPosition = Camera.main.WorldToViewportPoint(touch.position);
                dragStartPosition.z = 0;
            }

            //Get current touch position
            dragEndPosition = Camera.main.WorldToViewportPoint(touch.position);
            dragEndPosition.z = 0;

            if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
            {
                Dragging();
            }

            if (touch.phase == TouchPhase.Ended && !playerController.isGrounded && !playerController.isSwimming && touchEnded)
            {
                //On first click, set grapple point then grapple
                if (touchEnded)
                {
                    SetGrapplePoint();
                }
                touchEnded = false;
            }
            if (touch.phase == TouchPhase.Ended)
            {
                lr.positionCount = 0;
            }
        }
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                dragStartPosition = Camera.main.WorldToViewportPoint(Input.mousePosition);
                dragStartPosition.z = 0;
                mouseAllowedToDrag = true;
            }

            //Get current touch position
            dragEndPosition = Camera.main.WorldToViewportPoint(Input.mousePosition);
            dragEndPosition.z = 0;

            if (Input.GetMouseButton(0) && mouseAllowedToDrag)
            {
                Dragging();
            }
            if (Input.GetMouseButtonUp(0))
            {
                mouseAllowedToDrag = false;
                // On first click, set grapple point then grapple
                if (!playerController.isGrounded && !playerController.isSwimming && touchEnded)
                {
                    SetGrapplePoint();
                }
                touchEnded = false;
            }
        }
    }
    void Dragging()
    {
        if (!playerController.isGrounded)
        {
            //Set the aim line to a clamped vector of the distance between the first click on the screen to the current dragging position
            lr.positionCount = 2;

            int maxDrag = 12;
            if (playerController.dried)
            {
                maxDrag = 4;
            }
            secondLinePoint = tongueAimLineStartpoint.position + (Vector3.ClampMagnitude((dragStartPosition - dragEndPosition) * aimMultiplier, maxDrag));

            //Create the line
            lr.SetPosition(0, tongueAimLineStartpoint.position);
            lr.SetPosition(1, secondLinePoint);
            aimingGrapple = true;
        }
    }
    void SetGrapplePoint()
    {
        //Remove the aim line when the grapple is fired
        lr.positionCount = 0;
        aimingGrapple = false;

        //Direction of the first click on the screen to when the screen is released
        Vector2 distanceVector = dragStartPosition - dragEndPosition;

        float fillPercentage = Mathf.InverseLerp(0, (12/*Max Drag*/), (secondLinePoint - transform.position).magnitude);
        if (fillPercentage > 0.1f)
        {
            if (Physics2D.Raycast(firePoint.position, distanceVector))
            {
                //Aim a raycast that starts at the player and is fired at the direction of the initial touch - the last touch
                RaycastHit2D _hit = Physics2D.Raycast(firePoint.position, distanceVector, Mathf.Infinity, playerController.layerMask);

                if (_hit.collider != null && _hit.collider.gameObject.CompareTag("Grapplable") || grappleToAll)
                {
                    //If the hit object can be grappled to, grapple to it
                    if (Vector2.Distance(_hit.point, firePoint.position) <= maxDistance || !hasMaxDistance)
                    {
                        hitPoint = _hit.point;
                        hitCollider = _hit.collider;
                        grappleTarget = _hit.collider.gameObject;

                        grappleDistanceVector = grapplePoint - (Vector2)gunPivot.position;
                        tongueLine.enabled = true;
                        grapplePointIdentified = true;
                        playerController.grappleMode = true;

                    }
                }
            }
        }
    }

    void SlowTime()
    {
        //When aiming, slow down time
        if ((touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary || (mouseAllowedToDrag && Input.GetMouseButton(0)))
            /*&& !tongueLine.isGrappling*/ && !playerController.isGrounded && !playerController.isSwimming
            && !pauseScript.pause && !playerController.dead)
        {
            touchEnded = true;
            Time.timeScale = timeSlow;
        }
        else if (!pauseScript.pause) //Resume time when tongue is fired or when the player is grounded
        {
            Time.timeScale = 1;
        }
    }
    void SwitchFromPointToTransform()
    {
        if (grapplePointIdentified && grappleTarget != null) //Switch the grapple point from a point on an object to the center of that object if it is prey
        {
            if ((grappleTarget.transform.gameObject.layer == 7 || grappleTarget.transform.gameObject.layer == 12 || grappleTarget.transform.gameObject.layer == 8))
            {
                grapplePoint = grappleTarget.transform.position;
            }
        }
    }
    void KeepGrapplePointOnCollider()
    {
        //Make the grapple point stay as the edge of the collider where the raycast hit
        if (grappleTarget != null && hitCollider != null && hitPoint != null && grapplePoint != null
            && grappleTarget.layer != 7 && grappleTarget.layer != 8 && grappleTarget.layer != 12)
        {
            grapplePoint = hitCollider.ClosestPoint(hitPoint);
            hitPoint = grapplePoint;
        }
    }
    void TransitionAimLine()
    {
        //Remove the aim line when grounded or swimming
        if ((playerController.isGrounded || playerController.isSwimming) && lr.positionCount > 0)
        {
            playerController.skipToJump = true;
            lr.positionCount = 0;
        }
    }

    private void DetatchWhenClose()
    {
        //If the tongue gets too close to its target, it detaches;
        if (grapplePointDetector.closeToGrapplePoint)
        {
            tongueLine.enabled = false;
            tongueLine.isGrappling = false;
            grapplePointIdentified = false;
            grappleTarget = null;
            rb.gravityScale = 1.2f;
            grapplePointDetector.bugHit = false;
        }
    }
    void Grapple()
    {
        rb.gravityScale = 0;

        //Set the frog's velocity towards the grapple point
        addedForce = (grapplePoint - (Vector2)transform.position).normalized * grappleStrength;
        rb.velocity = addedForce;
    }
    void SetRangeCircle()
    {
        //Sets the scale of the circle to match the range
        if (lr.positionCount > 0 && !playerController.dead)
        {
            tongueRangeCircle.transform.localScale = new Vector3(maxDistance * 5.7333f * rangeCircleOffset, maxDistance * 6.466f * rangeCircleOffset, 0);
            tongueRangeCircle.gameObject.SetActive(true);
        }
        else
        {
            tongueRangeCircle.gameObject.SetActive(false);
        }
    }
    private void Launch()
    {
        if (launchToPoint && tongueLine.isGrappling)
        {
            if (launchType == LaunchType.Transform_Launch)
            {
                Vector2 firePointDistance = firePoint.position - gunHolder.localPosition;
                Vector2 targetPos = grapplePoint - firePointDistance;
                gunHolder.position = Vector2.Lerp(gunHolder.position, targetPos, launchCurve.Evaluate(Time.fixedDeltaTime) * launchSpeed);
            }
        }
    }
}