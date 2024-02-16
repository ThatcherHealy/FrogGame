using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DestroyUnloadedObjects : MonoBehaviour
{
    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.gameObject.transform.parent != null)
        {
            if (collision.transform.parent.name != "SpringPref(Clone)")
                Destroy(collision.gameObject.transform.parent.gameObject);
        }
        if (collision.gameObject != null)
        {
            if (collision.gameObject.transform.parent != null)
            {
                if (collision.transform.parent.name != "SpringPref(Clone)")
                    Destroy(collision.gameObject);
            }
            else
                Destroy(collision.gameObject);
        }
    }
}