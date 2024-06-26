using JetBrains.Annotations;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PredatorTurner : MonoBehaviour
{
    public string turnDirection;
    public bool active;
    bool turned;
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.layer == 14 || (transform.parent.name == "Heron(Clone)" && collision.gameObject.name == "CypressTree" && FindFirstObjectByType<PlayerController>().eaten) && active)
        {
            if (!turned) 
            {
                if (transform.parent.position.x < collision.transform.position.x) //Approaching from the left
                {
                    turnDirection = "Right";
                }
                if (transform.parent.position.x > collision.transform.position.x) //Approaching from the right
                {
                    turnDirection = "Left";
                }
                turned = true;
            }
        }
    }
    private void OnTriggerExit2D(Collider2D collision)
    {
        if(collision.gameObject != null && transform.parent != null)
        {
            if (collision.gameObject.layer == 14 || (transform.parent.name == "Heron(Clone)" && collision.gameObject.name == "CypressTree" && FindFirstObjectByType<PlayerController>().eaten) && turned)
            {
                turnDirection = "";
            }
        }
    }
}