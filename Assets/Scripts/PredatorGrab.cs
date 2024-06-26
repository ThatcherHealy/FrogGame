using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;

public class PredatorGrab : MonoBehaviour
{
    PlayerController pc;
    Transform frog;
    [SerializeField] Transform grabArea;
    public bool grabbed;
    public bool poisoned;
    bool poisonedOnce;
    public bool dead;
    bool alligator;
    private void Start()
    {
        pc = FindFirstObjectByType<PlayerController>();
        if ((transform.parent.transform.parent != null))
        {
            if (transform.parent.transform.parent.gameObject.name == "ALLIGATOR")
            {
                alligator = true;
            }
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (pc == null)
            pc = FindFirstObjectByType<PlayerController>();
        if (pc != null) 
        {
            if (!pc.eaten || alligator) //If the player hasn't been eaten already, they get eaten
            {
                if (collision != null)
                {

                    if (collision.CompareTag("Player"))
                    {
                        if (collision.name == "Frog")
                            frog = collision.gameObject.transform;
                        else
                            frog = collision.gameObject.transform.parent;

                        if (!poisoned && !frog.GetComponent<PlayerController>().invulnerable)
                        {
                            grabbed = true;
                            frog.gameObject.GetComponent<Rigidbody2D>().mass = 0;
                        }
                    }
                }
            }
        }
    }
    private void Update()
    {
        //Starts the timer to release the grab in two seconds when the predator is poisoned
        if (poisoned && !poisonedOnce)
        {
            poisonedOnce = true;
            StartCoroutine(CancelGrab(2, frog, false));
        }
        if(grabbed && poisoned)
        {
            pc.grabbedByPoisonedPredator = true;
        }
        if(grabbed && transform.parent != null && transform.parent.parent != null && transform.parent.parent.name == "Falcon(Clone)" && poisoned)
        {
            pc.grabbedByPoisonedFalcon = true;
        }
    }
    private void FixedUpdate()
    {
        if (grabbed && !dead) //Keeps the player grabbed when the predator isn't dead
        {
            frog.position = grabArea.position;
        }
        if (dead) //Returns the player's mass to normal when they escape
        {
            frog.gameObject.GetComponent<Rigidbody2D>().mass = 3;
        }
    }
    public IEnumerator CancelGrab(float cancelDelay, Transform frog, bool tutorial)
    {
        //Releases the player after 2 seconds
        yield return new WaitForSeconds(cancelDelay);
        grabbed = false;
        pc.grabbedByPoisonedPredator = false;
        pc.grabbedByPoisonedFalcon = false;

        if(poisoned)
            dead = true;
        else if(!tutorial)
        {
            Destroy(GetComponent<Collider2D>());
            if (transform.parent.gameObject.GetComponentsInChildren<PredatorGrab>() != null) //Destroy all predator grab boxes in a predator
            {
                PredatorGrab[] grabs = transform.parent.gameObject.GetComponentsInChildren<PredatorGrab>();
                foreach (PredatorGrab grab in grabs)
                {
                    Destroy(grab.gameObject.GetComponent<Collider2D>());
                }
            }
            if (transform.parent.gameObject.GetComponentInChildren<PredatorVision>() != null)
                transform.parent.gameObject.GetComponentInChildren<PredatorVision>().GetComponent<Collider2D>().enabled = false;
            if (transform.parent.gameObject.GetComponentInChildren<ChaseRange>() != null)
                transform.parent.gameObject.GetComponentInChildren<ChaseRange>().GetComponent<Collider2D>().enabled = false;

            if(transform.parent != null)
            {
                if(transform.parent.parent != null)
                {
                    if(transform.parent.parent.name == "Salmon" || transform.parent.parent.name == "Salmon(Clone)") //Destroys all salmon swarm grabs
                    {
                        PredatorGrab[] grabs = transform.parent.parent.GetComponentsInChildren<PredatorGrab>();
                        foreach (PredatorGrab grab in grabs)
                        {
                            Destroy(grab);
                        }

                        Transform[] objects = transform.parent.parent.GetComponentsInChildren<Transform>();
                        foreach(Transform obj in objects)
                        {
                            if (obj.CompareTag("Predator"))
                                obj.tag = "Untagged";
                        }
                    }
                }
            }

        }


        frog.gameObject.GetComponent<Rigidbody2D>().mass = 3;
        if (!tutorial)
        {
            frog.gameObject.GetComponent<Rigidbody2D>().AddForce((frog.position - transform.position).normalized * 15, ForceMode2D.Impulse);
        }
        else
        {
            frog.gameObject.GetComponent<Rigidbody2D>().velocity = Vector3.zero;
        }
    }
}
