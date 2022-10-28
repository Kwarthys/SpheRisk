using Mirror;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RiskNetworkManager : NetworkManager
{
    public Transform playerHolder;
    public NetworkIdentity worldBuilder;

    public GameManager gameManager;

    public override void OnServerAddPlayer(NetworkConnection conn)
    {
        GameObject player = Instantiate(playerPrefab, playerHolder);
        if(numPlayers == 0)
        {
            Debug.Log("Giving authority");
            worldBuilder.AssignClientAuthority(conn);
        }

        gameManager.registerNewPlayer(conn, player.GetComponent<PlayerNetworkManager>());

        NetworkServer.AddPlayerForConnection(conn, player);
    }
}
