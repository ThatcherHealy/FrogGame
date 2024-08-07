using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class ScoreController : MonoBehaviour
{
    public int score;

    [SerializeField] bool tutorial;
    [SerializeField] TextMeshPro scoreText;
    [SerializeField] Transform playerPosition;
    [SerializeField] TextMeshPro floatingText;
    [SerializeField] GameObject floatingTextParent;
    [SerializeField] Canvas canvas;
    [SerializeField] PlayerController playerController;
    [SerializeField] LevelGenerator lg;
    [SerializeField] DeathScript ds;
    [SerializeField] TextMeshPro finalScoreText;
    [SerializeField] TextMeshPro highscoreText;
    [SerializeField] TextMeshPro drownedFinalScoreText;
    [SerializeField] TextMeshPro drownedHighscoreText;
    [SerializeField] TextMeshPro poisonedFinalScoreText;
    [SerializeField] TextMeshPro poisonedHighscoreText;
    [SerializeField] TextMeshPro[] highscoreWordTexts;
    [SerializeField] Transform startPoint;
    [SerializeField] Color hardModeHighscoreColor;
    public bool hasHighscore;

    private float initialPosition;
    private float xDistance;
    private float farthestDistance;
    private float totalDistanceTravelled;
    bool highscoreSoundPlayed;


    private void Start()
    {
        hasHighscore = false;
        farthestDistance = startPoint.position.x;
        scoreText.enabled = false;

        if (lg != null)
            initialPosition = lg.endPoint.x;
        else
            initialPosition = startPoint.transform.position.x;

        //Transfer tutorial score to game
        if(TutorialEnd.scoreTransfer != 0)
        {
            score += TutorialEnd.scoreTransfer;
            TutorialEnd.scoreTransfer = 0;
        }
    }
    private void Update()
    {
        //Starts displaying score after the player gets a point
        if (score > 0)
        {
            scoreText.enabled = true;
        }

        if (PlayerPrefs.GetInt("HardMode", 0) == 1)
            scoreText.color = Color.red;

        //Change the text color to yellow when you have a highscore
        HasHighscore();
        if (hasHighscore && !tutorial)
        {
            if (PlayerPrefs.GetInt("HardMode", 0) == 0)
                scoreText.color = Color.yellow;
            else
            {
                scoreText.color = hardModeHighscoreColor;
            }

            if (!highscoreSoundPlayed) 
            {
                FindFirstObjectByType<SFXManager>().PlaySFX("Highscore");
                highscoreSoundPlayed = true;
            }
        }

        //Set scoreText equal to score
        scoreText.text = score.ToString();

        //Adds a point whenever the player goes 4 units farther in the x axis than their previous location
        if (playerPosition.position.x > initialPosition)
            xDistance = Vector3.Distance(new Vector3(initialPosition, 0, 0), new Vector3(playerPosition.position.x, 0, 0));
        int scoreThreshold = 3;

        if (xDistance > farthestDistance + scoreThreshold)
        {
            farthestDistance = xDistance;
            Score(1);
            totalDistanceTravelled += 1;
        }

        //When the player dies, display their final score
        if (playerController.dead && (ds.dontRespawnPressed || ds.respawnedOnce) && !tutorial)
        {
            if (playerController.drowned)
            {
                finalScoreText = drownedFinalScoreText;
                highscoreText = drownedHighscoreText;
            }
            if (playerController.poisoned)
            {
                finalScoreText = poisonedFinalScoreText;
                highscoreText = poisonedHighscoreText;
            }
            scoreText.text = new string(" ");
            finalScoreText.text = score.ToString();

            CheckHighscore(score);
            highscoreText.text = Highscore().ToString();

            if (hasHighscore && !tutorial)
            {
                    finalScoreText.color = new Color(1, 0.92f, 0.016f, finalScoreText.color.a); //Yellow
                    highscoreText.color = new Color(1, 0.92f, 0.016f, finalScoreText.color.a); //Yellow
                    foreach (TextMeshPro highscoreWordText in highscoreWordTexts)
                    {
                        highscoreWordText.color = new Color(1, 0.92f, 0.016f, finalScoreText.color.a); //Yellow
                    }
            }

        }
    }

    public void Score(int scoreChange)
    {
        //Updates the score by the amount inputted
        if (!playerController.dead)
        {
            score += scoreChange;
        }
    }

    public void SpawnFloatingText(int value, Vector3 position, Color color)
    {
        if (!playerController.dead)
        {
            //Spawns a score of the value inputted at the location inputted and then destroys it  
            floatingText.text = value.ToString();
            Vector3 offset = new Vector3(0, 2, -0.5f);

            GameObject parent = Instantiate(floatingTextParent, position + offset, Quaternion.identity);
            TextMeshPro spawnedScore = Instantiate(floatingText, position, Quaternion.identity, parent.transform);
            spawnedScore.color = color;

            Destroy(spawnedScore.gameObject, 2);
            Destroy(parent, 2);
        }
    }
    public void HasHighscore()
    {
        if (PlayerController.species == PlayerController.Species.Default)
        {
            if (score > PlayerPrefs.GetInt("DefaultHighscore", 0))
            {
                hasHighscore = true;
            }
        }
        if (PlayerController.species == PlayerController.Species.Treefrog)
        {
            if (score > PlayerPrefs.GetInt("TreeFrogHighscore", 0))
            {
                hasHighscore = true;
            }
        }
        if (PlayerController.species == PlayerController.Species.Froglet)
        {
            if (score > PlayerPrefs.GetInt("FrogletHighscore", 0))
            {
                hasHighscore = true;
            }
        }
        if (PlayerController.species == PlayerController.Species.BullFrog)
        {
            if (score > PlayerPrefs.GetInt("BullfrogHighscore", 0))
            {
                hasHighscore = true;
            }
        }
        if (PlayerController.species == PlayerController.Species.PoisonDartFrog)
        {
            if (score > PlayerPrefs.GetInt("PoisonDartFrogHighscore", 0))
            {
                hasHighscore = true;
            }
        }
    }
    public void CheckHighscore(int finalScore)
    {
        //Set the main highscore
        if (finalScore > PlayerPrefs.GetInt("Highscore", 0))
        {
            PlayerPrefs.SetInt("Highscore", finalScore);
        }

        //Set individual species highscore 
        if ((PlayerPrefs.GetInt("HardMode", 0) == 0))
        {
            if (PlayerController.species == PlayerController.Species.Default)
            {
                if (finalScore > PlayerPrefs.GetInt("DefaultHighscore", 0))
                {
                    PlayerPrefs.SetInt("DefaultHighscore", finalScore);
                }
            }
            if (PlayerController.species == PlayerController.Species.Treefrog)
            {
                if (finalScore > PlayerPrefs.GetInt("TreeFrogHighscore", 0))
                {
                    PlayerPrefs.SetInt("TreeFrogHighscore", finalScore);
                }
            }
            if (PlayerController.species == PlayerController.Species.Froglet)
            {
                if (finalScore > PlayerPrefs.GetInt("FrogletHighscore", 0))
                {
                    PlayerPrefs.SetInt("FrogletHighscore", finalScore);
                }
            }
            if (PlayerController.species == PlayerController.Species.BullFrog)
            {
                if (finalScore > PlayerPrefs.GetInt("BullfrogHighscore", 0))
                {
                    PlayerPrefs.SetInt("BullfrogHighscore", finalScore);
                }
            }
            if (PlayerController.species == PlayerController.Species.PoisonDartFrog)
            {
                if (finalScore > PlayerPrefs.GetInt("PoisonDartFrogHighscore", 0))
                {
                    PlayerPrefs.SetInt("PoisonDartFrogHighscore", finalScore);
                }
            }
        }
        else //Hard Mode
        {
            if (PlayerController.species == PlayerController.Species.Default)
            {
                if (finalScore > PlayerPrefs.GetInt("DefaultHardModeHighscore", 0))
                {
                    PlayerPrefs.SetInt("DefaultHardModeHighscore", finalScore);
                }
            }
            if (PlayerController.species == PlayerController.Species.Treefrog)
            {
                if (finalScore > PlayerPrefs.GetInt("TreeFrogHardModeHighscore", 0))
                {
                    PlayerPrefs.SetInt("TreeFrogHardModeHighscore", finalScore);
                }
            }
            if (PlayerController.species == PlayerController.Species.Froglet)
            {
                if (finalScore > PlayerPrefs.GetInt("FrogletHardModeHighscore", 0))
                {
                    PlayerPrefs.SetInt("FrogletHardModeHighscore", finalScore);
                }
            }
            if (PlayerController.species == PlayerController.Species.BullFrog)
            {
                if (finalScore > PlayerPrefs.GetInt("BullfrogHardModeHighscore", 0))
                {
                    PlayerPrefs.SetInt("BullfrogHardModeHighscore", finalScore);
                }
            }
            if (PlayerController.species == PlayerController.Species.PoisonDartFrog)
            {
                if (finalScore > PlayerPrefs.GetInt("PoisonDartFrogHardModeHighscore", 0))
                {
                    PlayerPrefs.SetInt("PoisonDartFrogHardModeHighscore", finalScore);
                }
            }
        }
    }
    public int Highscore()
    {
        if (PlayerController.species == PlayerController.Species.Default)
        {
            return PlayerPrefs.GetInt("DefaultHighscore", 0);
        }
        else if (PlayerController.species == PlayerController.Species.Treefrog)
        {
            return PlayerPrefs.GetInt("TreeFrogHighscore", 0);
        }
        else if (PlayerController.species == PlayerController.Species.Froglet)
        {
            return PlayerPrefs.GetInt("FrogletHighscore", 0);
        }
        else if (PlayerController.species == PlayerController.Species.BullFrog)
        {
            return PlayerPrefs.GetInt("BullfrogHighscore", 0);
        }
        else if (PlayerController.species == PlayerController.Species.PoisonDartFrog)
        {
            return PlayerPrefs.GetInt("PoisonDartFrogHighscore", 0);
        }
        else
        {
            return 0;
        }
    }
}