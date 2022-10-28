using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class UIsManager : MonoBehaviour
{
    public float alertTime = 2;
    private float alertSpawn = -10;
    private bool checkAlert = false;

    public Button startGameButton;

    public TextMeshProUGUI reinforcementText;
    public TextMeshProUGUI gameInfoText;
    public TextMeshProUGUI alertText;

    public void setAlertText(string text)
    {
        checkAlert = true;
        alertText.text = text;
        alertSpawn = Time.realtimeSinceStartup;
    }

    private void Start()
    {
        reinforcementText.text = "";
        gameInfoText.text = "";
        alertText.text = "";
    }

    private void Update()
    {
        if(checkAlert)
        {
            if(Time.realtimeSinceStartup - alertSpawn > alertTime)
            {
                checkAlert = false;
                alertText.text = "";
            }
        }
    }
}
