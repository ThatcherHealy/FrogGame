using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UIElements;

public class SpiderBehavior : MonoBehaviour
{
    [SerializeField] LineRenderer web;
    [SerializeField] LineRenderer strand;
    [SerializeField] Transform[] webEnds;
    [SerializeField] GameObject poisonGlow;
    Vector3 webMidpoint;

    Rigidbody2D rb;
    [SerializeField] GameObject sprite;
    [SerializeField] Sprite basicSprite;
    [SerializeField] Sprite poisonSprite;
    private SpriteRenderer sr;

    [SerializeField] GameObject poisonParticlesPrefab;
    GameObject poisonParticles;
    bool particlesSpawned;

    [SerializeField] PolygonCollider2D polygonCollider;
    private Vector3[] linePositions = new Vector3[2];

    [SerializeField] Animator animator;
    string CLIMB;

    [Header("Settings")]
    [SerializeField] int chanceToExist = 2;
    [SerializeField] float offsetBelow = 2;
    [SerializeField] float bobSpacing = 2;
    public bool poisonous;

    // Start is called before the first frame update
    void Start()
    {
        sr = sprite.GetComponent<SpriteRenderer>();
        ChanceToExist();

        rb = GetComponent<Rigidbody2D>();
        DetermineIfPoisonous();

        SetUpWeb();
        transform.position = new Vector3(webMidpoint.x, webMidpoint.y - offsetBelow, 0);

        StartCoroutine(Bob());
    }

    IEnumerator Bob()
    {
        //Down
        if(animator != null)
        {
            animator.SetFloat("Speed", 0);
        }

        rb.velocity = new Vector3(0, -bobSpacing, 0);
        yield return new WaitForSeconds(2.5f);

        //Up
        if (animator != null)
        {
            animator.Play(CLIMB);
            animator.SetFloat("Speed", 1);
        }

        rb.velocity = new Vector3(0, bobSpacing, 0);
        yield return new WaitForSeconds(2.5f);

        //Repeat
        StartCoroutine(Bob());
    }

    void Update()
    {
        AttatchStrand();
        SetEdgeCollider();

        if (poisonous) 
        {
            PoisonEffects();
        }
        else
        {
            if (sprite != null)
                sr.sprite = basicSprite;
        }

        //Destroy particles when the sprite is destroyed
        if (sprite == null)
        {
            if (poisonParticles != null) 
                poisonParticles.SetActive(false);
            if (poisonGlow != null)
                poisonGlow.SetActive(false);
        }
    }

    void SetUpWeb() 
    {
        web.positionCount = 2;
        strand.positionCount = 2;
        web.SetPosition(0, webEnds[0].position);
        web.SetPosition(1, webEnds[1].position);

        webMidpoint = new Vector3((webEnds[0].position.x + webEnds[1].position.x) / 2, (webEnds[0].position.y + webEnds[1].position.y) / 2, 0);
        strand.SetPosition(0, webMidpoint);
    }
    void AttatchStrand() 
    {
        if (sprite != null) 
        {
            strand.SetPosition(1, transform.position);
        }
        else //Pause the strand when the frog is eaten
        {
            strand.SetPosition(1, strand.GetPosition(1));
        }
    }
    private void SetEdgeCollider()
    {
        web.GetPositions(linePositions);
        Vector3[] colliderPoints = new Vector3[web.positionCount * 2];

        Vector2 width = new Vector2(0, 0.1f);
        bool swap = false;

        for (int i = 0; i < web.positionCount * 2; i += 2)
        {
            Vector2 localLRPos = web.transform.InverseTransformPoint(web.GetPosition(i / 2)); //The position of the lr points converted to local space

            //Spawns two points per lr point, one a little to the left and one a little to the right.

            //bool swap is used to spawn the points like: 
            // o --> o
            //       |
            // o <-- o

            if (!swap)
            {
                colliderPoints[i] = localLRPos - width;
                colliderPoints[i + 1] = localLRPos + width;
                swap = true;
            }
            else
            {
                colliderPoints[i] = localLRPos + width;
                colliderPoints[i + 1] = localLRPos - width;
                swap = false;
            }
        }

        polygonCollider.points = ToVector2Array(colliderPoints);
    }
    void ChanceToExist()
    {
        int chance = Random.Range(1, 100);
        if (chance >=  chanceToExist)
        { 
            Destroy(gameObject);
        }
    }
    void DetermineIfPoisonous() 
    {
        float chance = Random.Range(1, 6);
        if (chance == 5)
        {
            poisonous = true;
            CLIMB = "PoisonousSpider";
        }
        else
        {
            CLIMB = "Spider";
        }
    }
    void PoisonEffects()
    {
        if (sprite != null)
        {
            sprite.tag = "Poisonous";
            sr.sprite = poisonSprite;
            PoisonParticles();
            poisonGlow.SetActive(true);
        }
    }
    private Vector2[] ToVector2Array(Vector3[] v3)
    {
        return System.Array.ConvertAll<Vector3, Vector2>(v3, getV3fromV2);
    }
    private Vector2 getV3fromV2(Vector3 v3)
    {
        return new Vector2(v3.x, v3.y);
    }
    void PoisonParticles() 
    {
        if (!particlesSpawned) 
        {
            poisonParticles = Instantiate(poisonParticlesPrefab, sprite.transform.position, Quaternion.identity);
            particlesSpawned = true;
        }
        if (poisonParticles != null) 
        {
            poisonParticles.transform.position = sprite.transform.position;
        }
    }
}
