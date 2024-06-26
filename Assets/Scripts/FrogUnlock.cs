using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class FrogUnlock : MonoBehaviour
{
    [SerializeField] bool unlockAll;
    [SerializeField] Button treeFrogButton;
    [SerializeField] Button frogletButton;
    [SerializeField] Button bullfrogButton;
    [SerializeField] Button poisonDartFrogButton;

    [SerializeField] GameObject treeFrogProgress;
    [SerializeField] GameObject treeFrogStats;
    [SerializeField] TextMeshProUGUI treeFrogX;
    [SerializeField] Slider treeFrogSlider;

    [SerializeField] GameObject frogletProgress;
    [SerializeField] GameObject frogletStats;
    [SerializeField] TextMeshProUGUI frogletX;
    [SerializeField] Slider frogletSlider;
    [SerializeField] GameObject frogletLocked;
    [SerializeField] TextMeshProUGUI frogletText;


    [SerializeField] GameObject bullfrogProgress;
    [SerializeField] GameObject bullfrogStats;
    [SerializeField] TextMeshProUGUI bullfrogX;
    [SerializeField] Slider bullfrogSlider;
    [SerializeField] GameObject bullfrogLocked;
    [SerializeField] TextMeshProUGUI bullfrogText;


    [SerializeField] GameObject poisonDartFrogProgress;
    [SerializeField] GameObject poisonDartFrogStats;
    [SerializeField] TextMeshProUGUI poisonDartFrogX;
    [SerializeField] Slider poisonDartFrogSlider;
    [SerializeField] GameObject poisonDartFrogLocked;
    [SerializeField] TextMeshProUGUI poisonDartFrogText;

    [SerializeField] GameObject alertPrefab;
    public GameObject treeFrogAlert;
    public GameObject frogletAlert;
    public GameObject bullfrogAlert;
    public GameObject poisonDartFrogAlert;

    [SerializeField] Transform treeFrogAlertPosition;
    [SerializeField] Transform frogletAlertPosition;
    [SerializeField] Transform bullfrogAlertPosition;
    [SerializeField] Transform poisonDartFrogAlertPosition;

    [SerializeField] GameObject[] treeFrogIcons;
    [SerializeField] GameObject[] frogletIcons;
    [SerializeField] GameObject[] bullfrogIcons;
    [SerializeField] GameObject[] poisonDartFrogIcons;

    private void Awake()
    {
        InitializeProgress();
        InitializeButtonStates();
        InitializeAlerts();
        InitializeIcons();
    }
    private void InitializeProgress()
    {
        //Tree Frog

        treeFrogX.text = PlayerPrefs.GetInt("Highscore",0).ToString();
        float treeFrogSliderProgress = (float)PlayerPrefs.GetInt("Highscore", 0) / 1500;
        treeFrogSlider.value = treeFrogSliderProgress;

        //Avoid slider looking weird
        if (treeFrogSlider.value > 0.0001 && treeFrogSlider.value < 0.01)
        {
            treeFrogSlider.value = 0.01f;
        }

        //Unlock tree frog when slider is max
        if (treeFrogSliderProgress >= 1)
        {
            PlayerPrefs.SetInt("treeFrogUnlocked", 1);
        }



        //Froglet

        frogletX.text = PlayerPrefs.GetInt("InsectsEaten", 0).ToString();
        float frogletSliderProgress = (float)PlayerPrefs.GetInt("InsectsEaten", 0) / 250;
        frogletSlider.value = frogletSliderProgress;

        //Avoid slider looking weird
        if (frogletSlider.value > 0.0001 && frogletSlider.value < 0.01)
        {
            frogletSlider.value = 0.01f;
        }

        if (frogletSliderProgress >= 1)
        {
            PlayerPrefs.SetInt("frogletUnlocked", 1);
        }



        //Bullfrog

        bullfrogX.text = PlayerPrefs.GetInt("FishEaten", 0).ToString();
        float bullfrogSliderProgress = (float)PlayerPrefs.GetInt("FishEaten", 0) / 100;
        bullfrogSlider.value = bullfrogSliderProgress;

        //Avoid slider looking weird
        if (bullfrogSlider.value > 0.0001 && bullfrogSlider.value < 0.01)
        {
            bullfrogSlider.value = 0.01f;
        }

        if (bullfrogSliderProgress >= 1)
        {
            PlayerPrefs.SetInt("bullfrogUnlocked", 1);
        }



        //Poison Dart Frog

        poisonDartFrogX.text = PlayerPrefs.GetInt("Highscore", 0).ToString();
        float poisonDartFrogSliderProgress = (float)PlayerPrefs.GetInt("Highscore", 0) / 5000;
        poisonDartFrogSlider.value = poisonDartFrogSliderProgress;

        //Avoid slider looking weird
        if (poisonDartFrogSlider.value > 0.0001 && poisonDartFrogSlider.value < 0.01)
        {
            poisonDartFrogSlider.value = 0.01f;
        }

        if (poisonDartFrogSliderProgress >= 1 && PlayerPrefs.GetInt("bullfrogUnlocked", 0) == 1)
        {
            PlayerPrefs.SetInt("poisonDartFrogUnlocked", 1);
        }
    }
    void InitializeButtonStates() 
    {
        if (PlayerPrefs.GetInt("treeFrogUnlocked", 0) == 1 || unlockAll)
        {
            treeFrogButton.interactable = true;
            treeFrogProgress.SetActive(false);
            treeFrogStats.SetActive(true);
        }
        else
        {
            treeFrogButton.interactable = false;
            treeFrogProgress.SetActive(true);
            treeFrogStats.SetActive(false);
        }

        if (PlayerPrefs.GetInt("treeFrogUnlocked", 0) != 1)
        {
            frogletLocked.SetActive(true);
            frogletText.text = "???";
            frogletProgress.SetActive(false);
            frogletStats.SetActive(false);

            frogletButton.interactable = false;
        }
        else
        {
            if (PlayerPrefs.GetInt("frogletUnlocked", 0) == 1 || unlockAll)
            {
                frogletButton.interactable = true;
                frogletProgress.SetActive(false);
                frogletStats.SetActive(true);
            }
            else
            {
                frogletButton.interactable = false;
                frogletProgress.SetActive(true);
                frogletStats.SetActive(false);
            }
        }

        if (PlayerPrefs.GetInt("frogletUnlocked", 0) != 1)
        {
            bullfrogLocked.SetActive(true);
            bullfrogProgress.SetActive(false);
            bullfrogStats.SetActive(false);
            bullfrogText.text = "???";

            bullfrogButton.interactable = false;
        }
        else
        {
            if (PlayerPrefs.GetInt("bullfrogUnlocked", 0) == 1 || unlockAll)
            {
                bullfrogButton.interactable = true;
                bullfrogProgress.SetActive(false);
                bullfrogStats.SetActive(true);
            }
            else
            {
                bullfrogButton.interactable = false;
                bullfrogProgress.SetActive(true);
                bullfrogStats.SetActive(false);
            }
        }

        if (PlayerPrefs.GetInt("bullfrogUnlocked", 0) != 1)
        {
            poisonDartFrogLocked.SetActive(true);
            poisonDartFrogProgress.SetActive(false);
            poisonDartFrogStats.SetActive(false);
            poisonDartFrogText.text = "???";

            poisonDartFrogButton.interactable = false;
        }
        else
        {
            if (PlayerPrefs.GetInt("poisonDartFrogUnlocked", 0) == 1 || unlockAll)
            {
                poisonDartFrogButton.interactable = true;
                poisonDartFrogProgress.SetActive(false);
                poisonDartFrogStats.SetActive(true);
            }
            else
            {
                poisonDartFrogButton.interactable = false;
                poisonDartFrogProgress.SetActive(true);
                poisonDartFrogStats.SetActive(false);
            }
        }
    }
    void InitializeAlerts() 
    {
        if (PlayerPrefs.GetInt("treeFrogUnlocked") == 1 && PlayerPrefs.GetInt("TreeFrogClaimed") != 1) 
        {
            treeFrogAlert = Instantiate(alertPrefab, treeFrogAlertPosition.position, Quaternion.identity, treeFrogAlertPosition);
        }
        if (PlayerPrefs.GetInt("frogletUnlocked") == 1 && PlayerPrefs.GetInt("FrogletClaimed") != 1)
        {
            frogletAlert = Instantiate(alertPrefab, frogletAlertPosition.position, Quaternion.identity, frogletAlertPosition);
        }
        if (PlayerPrefs.GetInt("bullfrogUnlocked") == 1 && PlayerPrefs.GetInt("BullfrogClaimed") != 1)
        {
            bullfrogAlert = Instantiate(alertPrefab, bullfrogAlertPosition.position, Quaternion.identity, bullfrogAlertPosition);
        }
        if (PlayerPrefs.GetInt("poisonDartFrogUnlocked") == 1 && PlayerPrefs.GetInt("PoisonDartFrogClaimed") != 1)
        {
            poisonDartFrogAlert = Instantiate(alertPrefab, poisonDartFrogAlertPosition.position, Quaternion.identity, poisonDartFrogAlertPosition);
        }
    }
    void InitializeIcons() 
    {
        if (PlayerPrefs.GetInt("treeFrogUnlocked") == 1)
        {
            foreach(GameObject treeFrogIcon in treeFrogIcons)
            {
                treeFrogIcon.SetActive(true);
            }
        }
        if (PlayerPrefs.GetInt("frogletUnlocked") == 1 && frogletIcons[0] != null)
        {
            foreach (GameObject frogletIcon in frogletIcons)
            {
                frogletIcon.SetActive(true);
            }
        }
        if (PlayerPrefs.GetInt("bullfrogUnlocked") == 1 && bullfrogIcons[0] != null)
        {
            foreach (GameObject bullfrogIcon in bullfrogIcons)
            {
                bullfrogIcon.SetActive(true);
            }
        }
        if (PlayerPrefs.GetInt("poisonDartFrogUnlocked") == 1)
        {
            foreach (GameObject poisonDartFrogIcon in poisonDartFrogIcons)
            {
                poisonDartFrogIcon.SetActive(true);
            }
            PlayerPrefs.SetInt("HardModeUnlocked", 1);
        }
    }
}