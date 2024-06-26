using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ClickOffAd : MonoBehaviour
{
    [SerializeField] DeathScript ds;

    public void Exit()
    {
        {
            FindFirstObjectByType<SFXManager>().PlaySFX("Exit Click");
            ds.dontRespawnPressed = true;
            ds.continueScreen.SetActive(false);
            Time.timeScale = 1.0f;
        }
    }

}
