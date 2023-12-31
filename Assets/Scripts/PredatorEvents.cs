using System.Collections;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public class PredatorEvents : MonoBehaviour
{
    public PlayerController pc;
    public ScoreController sc;
    public GameObject fishSwarmPrefab;
    public GameObject warningPrefab;
    public Transform player;

    private int lowerScoreLimit = 0;
    private int checkInterval = 6;
    private bool cooldown = false;
    private int warningTime = 4;
    private bool warningActive = false;

    private Vector3 warningSpawnPosition;
    private GameObject warning;
    private Rect cameraRect;
    private Rect shrunkCameraRect;
    private Vector3 predatorSpawnPosition;
    private bool fishEvent;
    private int fishDirectionChance;

    void Start()
    {
        StartCoroutine(DetermineSpawnTime());
    }

    IEnumerator DetermineSpawnTime()
    {
        yield return new WaitForSeconds(checkInterval);
        if (sc.score > lowerScoreLimit && !cooldown)
        {
            int chance = UnityEngine.Random.Range(1, 9);
            if (chance >= 5) //12% chance
            {
                yield return new WaitForSeconds(warningTime);
                int eventChosen = UnityEngine.Random.Range(1, 2);
                if (eventChosen == 1)
                {
                    StartCoroutine(FishEvent());
                    Debug.Log("Fish Spawned");
                }
                else if (eventChosen == 2)
                    BirdEvent();
            }
        }
        //Loop
        StartCoroutine(DetermineSpawnTime());
    }

    private IEnumerator FishEvent()
    {
        fishEvent = true;
        int fishCooldownTime = 12;
        Cooldown(fishCooldownTime);

        fishDirectionChance = UnityEngine.Random.Range(1, 3);

        Warning();
        yield return new WaitForSeconds(warningTime);

        GameObject fishSwarm = Instantiate(fishSwarmPrefab, predatorSpawnPosition, Quaternion.identity);

        //Destroy the fish after 15 seconds
        Destroy(fishSwarm, 15);
    }
    private void Warning()
    {
        warning = Instantiate(warningPrefab, warningSpawnPosition, Quaternion.identity);
        StartCoroutine(WarningDuration());

        Destroy (warning, warningTime);
    }
    void Update()
    {
        if (fishEvent)
        {
            if (fishDirectionChance == 1)
                predatorSpawnPosition = new Vector2(player.position.x + 150, -20);
            else
                predatorSpawnPosition = new Vector2(player.position.x - 150, -20);
        }

        if (warningActive && warning != null)
        {
            //First, create a rectangle that matches the camera view
            Vector3 cameraCenter = Camera.main.transform.position;
            Vector3 bottomLeft = Camera.main.ScreenToWorldPoint(Vector3.zero);
            Vector3 topRight = Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth, Camera.main.pixelHeight));
            cameraRect = new(bottomLeft.x, bottomLeft.y, (topRight.x - bottomLeft.x), (topRight.y - bottomLeft.y));

            //Then, create a new rectangle that is shrunk on the x and y axis
            float xShrink = 0.85f, yShrink = 0.7f;
            float shrunkWidth = cameraRect.width * xShrink;
            float shrunkHeight = cameraRect.height * yShrink;
            shrunkCameraRect = new Rect(cameraCenter.x - shrunkWidth / 2, cameraCenter.y - shrunkHeight / 2, shrunkWidth, shrunkHeight);

            //Finally, set the warning position to be where the predator is going to spawn from, clamped within the shrunk circle
            warningSpawnPosition = new Vector3
           (Mathf.Clamp(predatorSpawnPosition.x, shrunkCameraRect.xMin, shrunkCameraRect.xMax),
            Mathf.Clamp(predatorSpawnPosition.y, shrunkCameraRect.yMin, shrunkCameraRect.yMax), 0);

            warning.transform.position = warningSpawnPosition;
        }
    }
    IEnumerator WarningDuration()
    {
        warningActive = true;
        yield return new WaitForSeconds(warningTime);
        warningActive = false;
    }

    private void BirdEvent()
    {
        int birdCooldownTime = 12;
        Cooldown(birdCooldownTime);
    }

    IEnumerator Cooldown(int cooldownTime)
    {
        cooldown = true;
        yield return new WaitForSeconds (cooldownTime);
        fishEvent = false;
        cooldown = false;
    }
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(new Vector3(shrunkCameraRect.center.x, cameraRect.center.y, 0), new Vector3(cameraRect.size.x, cameraRect.size.y, 0));
    }
}