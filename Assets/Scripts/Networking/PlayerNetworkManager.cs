using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class PlayerNetworkManager : NetworkBehaviour
{
    public static Color[] colors = { Color.red, Color.blue, Color.green, Color.black, Color.white, Color.yellow, Color.cyan, Color.magenta };

    private WorldBuilder worldBuilder;
    public NetworkIdentity ownIdentity;
    public Planet planet;

    private UIsManager uiManager;

    private int reinforcementLeftToSpawn = 0;

    private GameManager gameManager;

    public override void OnStartLocalPlayer()
    {
        worldBuilder = GameObject.FindGameObjectWithTag("WorldBuilder").GetComponent<WorldBuilder>();
        planet = GameObject.FindGameObjectWithTag("PlanetManager").GetComponent<Planet>();
        CmdRequestPlanetData(ownIdentity);

        uiManager = GameObject.FindGameObjectWithTag("UIManager").GetComponent<UIsManager>();
    }

    public override void OnStartServer()
    {
        worldBuilder = GameObject.FindGameObjectWithTag("WorldBuilder").GetComponent<WorldBuilder>();
        gameManager = GameObject.FindGameObjectWithTag("GameManager").GetComponent<GameManager>();
    }

    [Command]
    public void CmdRequestPlanetData(NetworkIdentity callerIdentity)
    {
        worldBuilder.requestData(callerIdentity.connectionToClient);
    }

    public void submitOrder(int selectedStateIndex, int orderStateIndex)
    {
        CmdSubmitOrder(ownIdentity, selectedStateIndex, orderStateIndex, Input.GetKey(KeyCode.LeftShift));
    }

    [Command]
    public void CmdSubmitOrder(NetworkIdentity id, int selectedStateIndex, int orderStateIndex, bool shiftPressed)
    {
        gameManager.treatOrder(id.connectionToClient, selectedStateIndex, orderStateIndex, shiftPressed);
    }

    [TargetRpc]
    public void TargetNotifyStateOwnerSwitch(NetworkConnection target, int stateIndex, int newPlayerIndex)
    {
        planet.changeStateOwner(stateIndex, newPlayerIndex);
    }

    [TargetRpc]
    public void TargetNotifyStateChangeTroops(NetworkConnection target, int stateIndex, int newTroops)
    {
        planet.changeTroops(stateIndex, newTroops);
    }

    [TargetRpc]
    public void TargetNotifyReiforcementLeft(NetworkConnection target, int reinforcementLeft)
    {
        reinforcementLeftToSpawn = reinforcementLeft;
        uiManager.reinforcementText.text = reinforcementLeft == 0 ? "" : reinforcementLeft.ToString();
    }

    [TargetRpc]
    public void TargetNotifyGameInfo(NetworkConnection target, string infoText)
    {
        uiManager.gameInfoText.text = infoText;
    }

    [TargetRpc]
    public void TargetSendAlert(NetworkConnection target, string alertText)
    {
        uiManager.setAlertText(alertText);
    }
}
