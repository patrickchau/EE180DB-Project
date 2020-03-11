using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using KartGame.KartSystems;

public class ItemBox : MonoBehaviour
{

    private bool m_deactivated = false;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        if (KeyboardInput.PowerUpObtained != "used" && KeyboardInput.PowerUpObtained != "noone" && !m_deactivated)
        {
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                gameObject.transform.GetChild(i).gameObject.SetActive(false);
            }
            KeyboardInput.PowerUpObtained = "noone";
            m_deactivated = true;
        }

        if (KeyboardInput.PowerUpObtained == "used")
        {
            Debug.Log("reactivating");
            for (int i = 0; i < gameObject.transform.childCount; i++)
            {
                gameObject.transform.GetChild(i).gameObject.SetActive(true);
                m_deactivated = false;
            }
            KeyboardInput.PowerUpObtained = "noone";
        }
    }
}
