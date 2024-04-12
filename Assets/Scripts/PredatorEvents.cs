using System.Collections;
using System.Collections.Generic;
using System.Runtime.ExceptionServices;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;

public class Predator
{
    public enum PredatorType { FishSwarm, Arapaima, Heron, Falcon }
    private PredatorType type;
    private GameObject linkedWarning;
    private GameObject prefab;
    private GameObject activeObject;
    private int direction;

    public void SetType(PredatorType setType, GameObject setPrefab, GameObject warning, GameObject obj)
    {
        type = setType;
        prefab = setPrefab;
        linkedWarning = warning;
        activeObject = obj;
    }
    PredatorType Type()
    {
        return type;
    }
    GameObject LinkedWarning() 
    {
        return linkedWarning;
    }
    GameObject GetPredator()
    {
        return activeObject;
    }
    void SetDirection(int directionChance)
    {
        direction = directionChance;
    }

    Vector3 SetSpawnPoint(LevelGenerator lg, Transform player)
    {
        if(type == PredatorType.FishSwarm || type == PredatorType.Arapaima)
        {
            if (lg.biomeSpawning != lg.playerBiome)
                direction = 2;

            if (direction == 1)
                return new Vector2(player.position.x + 150, lg.playerRefEndPoint.y - 16.415f);
            else
                return new Vector2(player.position.x - 150, lg.playerRefEndPoint.y - 16.415f);
        }
        else if (type == PredatorType.Falcon)
        {
            return new Vector2(player.position.x, lg.playerRefEndPoint.y + 209.585f);
        }
        else //Heron
        {
            if (direction == 1)
                return new Vector2(player.position.x + 100, lg.playerRefEndPoint.y + 9.585f);
            else
                return new Vector2(player.position.x - 100, lg.playerRefEndPoint.y + 9.585f);
        }
    }
}
public class PredatorEvents : MonoBehaviour
{
    [SerializeField] PlayerController pc;
    [SerializeField] ScoreController sc;
    [SerializeField] LevelGenerator lg;
    [SerializeField] GameObject fishSwarmPrefab;
    [SerializeField] GameObject arapaimaPrefab;
    [SerializeField] GameObject heronPrefab;
    [SerializeField] GameObject falconPrefab;
    [SerializeField] GameObject warningPrefab;
    [SerializeField] Transform player;


    private Vector3 predatorSpawnPosition;
    private bool fishEvent;
    private int directionChance;
    private bool birdEvent;
    public int lowerScoreLimit = 50;
    private float checkInterval = 5;
    int max, startingValue = 4;
    private bool cooldown = false;
    bool spawned;
    bool falcon;

    private int warningTime = 3;
    private bool warningActive = false;
    private Dictionary<GameObject, GameObject> warningToPredatorMap = new Dictionary<GameObject, GameObject>();
    private GameObject currentPredator;
    private GameObject warning;
    private List<GameObject> warnings = new List<GameObject>();
    private List<Predator> predators = new List<Predator>();
    private Rect cameraRect;
    private Rect shrunkCameraRect;

    void Start()
    {
        StartCoroutine(DetermineSpawnTime());
        max = startingValue;
    }

    IEnumerator DetermineSpawnTime()
    {
        //Decrease checkInterval by 0.2 every 250 points, capped out at 2 seconds
        float multiplier = Mathf.Round(sc.score / 250);
        for (int i = 0; i < multiplier; i++)
        {
            checkInterval -= 0.2f;
        }

        if (checkInterval < 1)
            checkInterval = 1;

        yield return new WaitForSeconds(checkInterval);


        if (sc.score > lowerScoreLimit && !cooldown && !pc.dead)
        {
            int chance = UnityEngine.Random.Range(1, max);
            if (chance == 1) //33% chance
            {
                max = startingValue;
                //Spawns a fish when you're in the water and a bird when you're out of the water
                //Never spawns a bird when you're in cypress
                if (pc.isSwimming && lg.playerBiome != LevelGenerator.Biome.Cypress)
                {
                    StartCoroutine(FishEvent());
                }
                else
                {
                    int falconChance = UnityEngine.Random.Range(1, 4);
                    if (falconChance == 1)
                    {
                        falcon = true;
                    } 
                    StartCoroutine(BirdEvent());
                }
            }
            else
            {
                max--;
            }
        }
        //Loop
        StartCoroutine(DetermineSpawnTime());
    }

    private IEnumerator FishEvent()
    {
        fishEvent = true;
        int fishCooldownTime = 20;
        StartCoroutine(Cooldown(fishCooldownTime));

        directionChance = UnityEngine.Random.Range(1, 3);
        SetSpawnPosition();

        Warning();
        yield return new WaitForSeconds(warningTime);

        if (lg.playerBiome != LevelGenerator.Biome.Amazon) 
        {
            //Sets currentPredator to be the closest fish in the swarm to the player
            currentPredator = Instantiate(fishSwarmPrefab, predatorSpawnPosition, Quaternion.identity);
            FishBehavior[] fishes = currentPredator.GetComponentsInChildren<FishBehavior>();
            Transform closestFish = currentPredator.GetComponentInChildren<FishBehavior>().transform; 
            foreach (FishBehavior fish in fishes)
            {
                if (math.distance(fish.transform.position.x, player.position.x) < math.distance(closestFish.transform.position.x, player.position.x))
                {
                    closestFish = fish.transform;
                }
            }
            currentPredator = closestFish.gameObject;
        }
        else
        {
            currentPredator = Instantiate(arapaimaPrefab, predatorSpawnPosition, Quaternion.identity);
        }
        spawned = true;

        //Destroy the fish after 15 seconds
        Destroy(currentPredator, 15);
    }
    private IEnumerator BirdEvent()
    {
        birdEvent = true;
        int birdCooldownTime = 20;
        StartCoroutine(Cooldown(birdCooldownTime));

        directionChance = UnityEngine.Random.Range(1, 3);
        SetSpawnPosition();

        Warning();
        yield return new WaitForSeconds(warningTime);

        if(falcon)
        {
            currentPredator = Instantiate(falconPrefab, predatorSpawnPosition, Quaternion.identity);
        }
        else
            currentPredator = Instantiate(heronPrefab, predatorSpawnPosition, Quaternion.identity);
        spawned = true;

        //Destroy the fish after 15 seconds
        Destroy(currentPredator, 15);

    }
    private void Warning()
    {
        warning = Instantiate(warningPrefab, predatorSpawnPosition, Quaternion.identity);
        warningActive = true;
    }

    void FixedUpdate() 
    {
        SetSpawnPosition();
        SetWarningPosition();

        if(pc.drowned || pc.poisoned) //Deactivate warning and predator when player drowns
        {
            if (warning != null)
                warning.SetActive(false);

            if (currentPredator != null)
                currentPredator.SetActive(false);
        }
    }

    void SetSpawnPosition() 
    {
        if (fishEvent)
        {
            //Always spawn fish to the left when you're at the end of a biome
            if (lg.biomeSpawning != lg.playerBiome)
                directionChance = 2;

            if (directionChance == 1)
                predatorSpawnPosition = new Vector2(player.position.x + 150, lg.playerRefEndPoint.y - 16.415f);
            else
                predatorSpawnPosition = new Vector2(player.position.x - 150, lg.playerRefEndPoint.y - 16.415f);
        }
        else if (birdEvent)
        {
            if (!falcon)
            {
                if (directionChance == 1)
                    predatorSpawnPosition = new Vector2(player.position.x + 100, lg.playerRefEndPoint.y + 9.585f);
                else
                    predatorSpawnPosition = new Vector2(player.position.x - 100, lg.playerRefEndPoint.y + 9.585f);
            }
            else
            {
                predatorSpawnPosition = new Vector2(player.position.x, lg.playerRefEndPoint.y + 209.585f);
            }

        }
    }
    void SetWarningPosition() 
    {
        if (warningActive && warning != null)
        {
            //Stop the warning when the predator is destroyed
            if(spawned && currentPredator == null)
            {
                spawned = false;
                Destroy(warning);
                return;
            }
            //Stop the warning when the falcon stops diving
            if (falcon && spawned && !currentPredator.GetComponent<FalconBehavior>().diving)
            {
                spawned = false;
                Destroy(warning);
                return;
            }

            //First, smooth the centerpoint of the camera
            Vector3 cameraCenter = Camera.main.transform.position;
            Vector3 smoothedCenter = cameraCenter;
            int smoothSpeed = 5;
            smoothedCenter = Vector3.Lerp(smoothedCenter, cameraCenter, smoothSpeed * Time.deltaTime);

            //Then, create a rectangle that matches the smoothed camera view
            Vector3 bottomLeft = Camera.main.ScreenToWorldPoint(Vector3.zero);
            Vector3 topRight = Camera.main.ScreenToWorldPoint(new Vector3(Camera.main.pixelWidth, Camera.main.pixelHeight));
            cameraRect = new(bottomLeft.x, bottomLeft.y, (topRight.x - bottomLeft.x), (topRight.y - bottomLeft.y));

            //Then, create a new rectangle that is shrunk on the x and y axis
            float xShrink = 0.85f, yShrink = 0.7f;
            if (falcon)
                yShrink = 0.8f;

            float shrunkWidth = cameraRect.width * xShrink;
            float shrunkHeight = cameraRect.height * yShrink;
            shrunkCameraRect = new Rect(smoothedCenter.x - shrunkWidth / 2, smoothedCenter.y - shrunkHeight / 2, shrunkWidth, shrunkHeight);

            //Finally, set the warning position to be where the predator is going to spawn from (before it's spawned), and then to where the predator is. All clamped within the shrunk rectangle
            Vector3 warningTarget;
            if (spawned)
            {
                    warningTarget = new Vector3(
                    Mathf.Clamp(currentPredator.transform.position.x, shrunkCameraRect.xMin, shrunkCameraRect.xMax),
                    Mathf.Clamp(currentPredator.transform.position.y, shrunkCameraRect.yMin, shrunkCameraRect.yMax), 0);
            }
            else
            {
              warningTarget = new Vector3(
              Mathf.Clamp(predatorSpawnPosition.x, shrunkCameraRect.xMin, shrunkCameraRect.xMax),
              Mathf.Clamp(predatorSpawnPosition.y, shrunkCameraRect.yMin, shrunkCameraRect.yMax), 0);
            }

            //Set Position
            warning.transform.position = Vector3.Lerp(warning.transform.position, warningTarget, 100 * Time.deltaTime);

            //Set Scale
            float scale; float distance;
            if (spawned)
            {
                distance = Mathf.Abs(Vector3.Distance(player.position, currentPredator.transform.position));
            }
            else
            {
                distance = Mathf.Abs(Vector3.Distance(player.position, predatorSpawnPosition));
            }
            float normalizedDistance = Mathf.Clamp01(distance / 150f);
            if(falcon)
                normalizedDistance = Mathf.Clamp01(distance / 200f);

            // Use Mathf.Lerp to interpolate between 0.5 and 1 based on the normalized distance
            scale = Mathf.Lerp(0.5f, 1f, 1 - normalizedDistance);
            scale *= 1.5f;
            warning.transform.localScale = new Vector3(scale,scale,1);

            //When the predator enters the cameraview, stop the warning
            if ((!falcon && (spawned && currentPredator.transform.position.x < (topRight.x + 5) && currentPredator.transform.position.x > (bottomLeft.x - 5)))
                || (falcon && (spawned && currentPredator.transform.position.y < (topRight.y + 5))))
            {
                warningActive = false;
                Destroy(warning);
            }
        }
    }
    IEnumerator Cooldown(float cooldownTime)
    {
        cooldown = true;
        checkInterval = 5;

        //Decrease cooldownTime by 1 every 250 points, capped out at 5 seconds
        float multiplier = Mathf.Round(sc.score / 250);
        for (int i = 0; i < multiplier; i++)
        {
            cooldownTime -= 0.5f;
        }

        if(cooldownTime < 5)
            cooldownTime = 5;

        yield return new WaitForSeconds(cooldownTime);
        spawned = false;
        fishEvent = false;
        birdEvent = false;
        falcon = false;
        cooldown = false;
    }
}