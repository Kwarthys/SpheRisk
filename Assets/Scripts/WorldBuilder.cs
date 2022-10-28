using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Threading;
using Mirror;

public class WorldBuilder : NetworkBehaviour
{
    public Planet planetGenerator;
    public TextMeshProUGUI loadingText;
    public Slider progressBar;

    public GameManager gameManager;

    public GameObject worldBuilderUI;

    private List<NetworkConnection> clientWaitingForWorld = new List<NetworkConnection>();

    private Thread t;

    private volatile bool generatingDone = false;
    private bool generating = false;
    private bool generated = false;


    public void requestData(NetworkConnection user)
    {
        if(generated)
        {
            Debug.Log("Sending Data");
            planetGenerator.sendDataToClient(user);
        }
        else
        {
            Debug.Log("Registering for later send");
            clientWaitingForWorld.Add(user);
        }
    }

    public void onClicStartGame()
    {
        if (!hasAuthority) return;

        if (!generated) return;

        CmdStartGame();
    }

    [Command]
    public void CmdStartGame()
    {
        gameManager.startGame();
    }

    public override void OnStartLocalPlayer()
    {
        if(!hasAuthority)
        {
            worldBuilderUI.SetActive(false);
        }

        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
    }

    public override void OnStartServer()
    {
        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
    }

    public void onClicGenerateWorld()
    {
        if(hasAuthority)
        {
            Debug.Log("sendingCommand");
            CmdGenerateWorld();
        }
        else
        {
            Debug.Log("Can't do that");
        }
    }

    [Command]
    public void CmdGenerateWorld()
    {
        Debug.Log("Generating");
        generating = true;
        generated = false;
        stopBuildingThread();

        planetGenerator.generatePlanet();

        t = new Thread(threadedGenerateGrid);
        t.Start();
    }

    private void stopBuildingThread()
    {
        if (t != null)
        {
            if (t.IsAlive)
            {
                t.Abort();
            }
        }
    }

    public void OnApplicationQuit()
    {
        if (!isServer) return;
        stopBuildingThread();
    }

    private void Update()
    {
        if (!isServer) return;

        if(generating)
        {
            RpcUpdateLoadingProgress(planetGenerator.getProgressStatus(), planetGenerator.getProgressText());
            /*
            if (planetGenerator.mapManager.updateMeshesColors)
            {
                planetGenerator.mapManager.updateMeshesColors = false;
                planetGenerator.mapManager.applyAllDataToMeshes();
            }*/
        }

        if(generatingDone)
        {
            generatingDone = false;
            generating = false;
            generated = true;
            Debug.Log("Generation Done");

            RpcUpdateLoadingProgress(1, "World built");
            RpcHideMenu();

            //planetGenerator.instantiateBuildings();
            planetGenerator.sendDataToClients(clientWaitingForWorld);
        }
    }

    [ClientRpc]
    public void RpcHideMenu()
    {
        worldBuilderUI.SetActive(false);
    }

    [ClientRpc]
    public void RpcUpdateLoadingProgress(float value, string txt)
    {
        progressBar.value = value;
        loadingText.text = txt;
    }

    //[Server]
    private void threadedGenerateGrid()
    {
        planetGenerator.generateGridMap();
        generatingDone = true;
    }
}
