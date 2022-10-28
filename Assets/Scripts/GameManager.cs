using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;

public class GameManager : NetworkBehaviour
{
    public Planet planetManager;

    private List<NetworkConnection> players = new List<NetworkConnection>();
    private List<PlayerNetworkManager> playersManagers = new List<PlayerNetworkManager>();

    private int playerTurnCounter = -1;
    private GameState gameState = GameState.Regroup;

    private float startingUnitsByStates = 1.1f; //around 2.7-2.8 in real risk
    private int MAX_PLAYERS = 8;
    private int actualPlayers = -1;

    private bool firstReinforcementsSetup = false;

    private Dictionary<int, int> reinforcementLeftByPlayers = new Dictionary<int, int>();

    private BoardManager boardManager;

    public void registerNewPlayer(NetworkConnection player, PlayerNetworkManager playersManager)
    {
        players.Add(player);
        playersManagers.Add(playersManager);
    }

    public void treatOrder(NetworkConnection player, int stateIndexFrom, int stateIndexTo, bool shiftPressed)
    {
        int playerIndex = players.IndexOf(player);

        Debug.Log("Recieved order from player " + playerIndex + " to move from " + stateIndexFrom + " to " + stateIndexTo);

        bool commandWithoutSelection = stateIndexFrom == -1;

        if(firstReinforcementsSetup)
        {
            manageOrderWhileFirstReinforcement(stateIndexTo, playerIndex, shiftPressed);
            return;
        }

        if(playerIndex != playerTurnCounter)
        {
            sendAlert(playerIndex, "Wait for your turn !");
            return;
        }

        if(gameState == GameState.Reinforcements)
        {
            manageReinforcementOrder(stateIndexTo, playerIndex, shiftPressed);

            if(reinforcementLeftByPlayers[playerIndex] == 0)
            {
                advancePhase();
            }

            return;
        }

        if (commandWithoutSelection || !boardManager.assessNeighbours(stateIndexFrom, stateIndexTo))
        {
            sendAlert(playerIndex, "Move not valid.");
            return;
        }

        if (gameState == GameState.Attack)
        {
            manageAttackOrder(stateIndexFrom, stateIndexTo, playerIndex);
        }

        if(gameState == GameState.Regroup)
        {
            //Player chooses how many troops to move between states
            return;
        }
    }

    public void startGame()
    {
        actualPlayers = Mathf.Min(players.Count, MAX_PLAYERS);
        boardManager = planetManager.constructABoardManager();
        boardManager.assignStatesForPlayers(actualPlayers);

        int totalStates = boardManager.getTotalStatesNumber();
        int allTroopsPerPlayer = (int)(totalStates * startingUnitsByStates / actualPlayers);

        for (int index = 0; index < actualPlayers; ++index)
        {
            reinforcementLeftByPlayers[index] = allTroopsPerPlayer - boardManager.statesByPlayer[index].Count;
            playersManagers[index].TargetNotifyReiforcementLeft(players[index], reinforcementLeftByPlayers[index]);
        }

        foreach(int stateIndex in boardManager.getAllStateIndecies())
        {
            sendChangeOwner(stateIndex, boardManager.getStateOwner(stateIndex));
            sendChangeTroops(stateIndex, 1);
        }

        firstReinforcementsSetup = true;

        sendGamePhaseUpdate();
    }

    public void manageReinforcementOrder(int stateToReinforceIndex, int playerIndex, bool shiftPressed)
    {
        if (reinforcementLeftByPlayers[playerIndex] > 0 && (playerTurnCounter == playerIndex || firstReinforcementsSetup)) //just to be sure
        {
            //manage first reinforcement phase for all players simultenaously
            State toReinforce = boardManager.getStateByIndex(stateToReinforceIndex);
            if (boardManager.doesPlayerOwnState(toReinforce, playerIndex))
            {
                int amount = 1;
                if (shiftPressed)
                {
                    amount = Mathf.Min(reinforcementLeftByPlayers[playerIndex], TroopsHandler.HELI_VALUE);
                }
                toReinforce.troops += amount;
                reinforcementLeftByPlayers[playerIndex] -= amount;
                sendChangeTroops(stateToReinforceIndex, toReinforce.troops);
                notifyPlayerReinforcements(playerIndex);
            }
            else
            {
                sendAlert(playerIndex, "You do not own this state.");
            }
        }
        else if(!firstReinforcementsSetup)
        {
            sendAlert(playerIndex, "Not your turn.");
        }
    }

    private void manageAttackOrder(int stateIndexFrom, int stateIndexTo, int playerIndex)
    {
        State sFrom = boardManager.getStateByIndex(stateIndexFrom);
        State sTo = boardManager.getStateByIndex(stateIndexTo);

        if (sFrom.playerIndex != sTo.playerIndex && boardManager.doesPlayerOwnState(sFrom, playerIndex)) //if states have != owners and stateFrom belongs to attacker
        {
            if (sFrom.troops > 1)
            {
                int attackingStateTroops = sFrom.troops;
                int attackingTroops = Mathf.Min(sFrom.troops - 1, 3);
                int defendingTroops = Mathf.Min(sTo.troops, 2);
                Vector2Int losses = boardManager.computeBattle(sFrom, sTo, attackingTroops, defendingTroops);
                bool stateIsLost = sTo.troops == 0;

                sendAlertToAll("Losses : " + losses.x + " - " + losses.y + ". " + (stateIsLost ? "State Captured" : "Troops left : " + sFrom.troops + " - " + sTo.troops));

                if (stateIsLost)
                {
                    sendAlert(playerIndex, "State Captured");
                    sendAlert(sTo.playerIndex, "State lost !");

                    boardManager.switchStateOwner(sTo.playerIndex, playerIndex, sTo);
                    int movingTroops = attackingTroops - (attackingStateTroops - sFrom.troops);

                    sFrom.troops -= movingTroops;
                    sTo.troops += movingTroops;

                    sendChangeOwner(stateIndexTo, playerIndex);
                }

                sendChangeTroops(stateIndexFrom, sFrom.troops);
                sendChangeTroops(stateIndexTo, sTo.troops);
            }
        }
        else
        {
            sendAlert(playerIndex, "Attack not valid.");
        }

        return;
    }

    public void manageOrderWhileFirstReinforcement(int stateToReinforceIndex, int playerIndex, bool shiftPressed)
    {
        manageReinforcementOrder(stateToReinforceIndex, playerIndex, shiftPressed);

        bool allDone = true;
        foreach (int troopsLeft in reinforcementLeftByPlayers.Values)
        {
            if (troopsLeft != 0)
            {
                allDone = false;
                break;
            }
        }

        if (allDone)
        {
            firstReinforcementsSetup = false;
            Debug.Log("Init Phase Done");
            advancePhase();
        }
    }

    private void advancePhase()
    {
        Debug.Log("Advancing Game");

        bool nextPlayer = gameState == GameState.Regroup;
        gameState = (GameState)(((int)gameState + 1) % 3);
        if(nextPlayer) playerTurnCounter = (playerTurnCounter + 1) % actualPlayers;

        sendGamePhaseUpdate();

        if(gameState == GameState.Reinforcements)
        {
            reinforcementLeftByPlayers[playerTurnCounter] = boardManager.getReinforcementForPlayer(playerTurnCounter);
            notifyPlayerReinforcements(playerTurnCounter);
        }
    }

    private void notifyPlayerReinforcements(int playerIndex)
    {
        playersManagers[playerIndex].TargetNotifyReiforcementLeft(players[playerIndex], reinforcementLeftByPlayers[playerIndex]);
    }

    private void sendGamePhaseUpdate()
    {
        for (int i = 0; i < playersManagers.Count; ++i)
        {
            string text = "";
            if(firstReinforcementsSetup)
            {
                text = "First Reinforcement phase.";
            }
            else
            {
                text += playerTurnCounter == i ? "Your" : "Player " + (playerTurnCounter + 1);
                text += " " + gameState.ToString() + " phase.";
            }

            playersManagers[i].TargetNotifyGameInfo(players[i], text);
        }
    }

    private void sendAlertToAll(string text)
    {
        for (int i = 0; i < playersManagers.Count; ++i)
        {
            sendAlert(i, text);
        }
    }

    private void sendAlert(int playerIndex, string text)
    {
        playersManagers[playerIndex].TargetSendAlert(players[playerIndex], text);
    }

    private void notifyChangeOwner(int playerIndex, int stateIndex, int newOnwer)
    {
        playersManagers[playerIndex].TargetNotifyStateOwnerSwitch(players[playerIndex], stateIndex, newOnwer);
    }

    private void sendChangeOwner(int stateIndex, int newOnwer)
    {
        for (int i = 0; i < playersManagers.Count; ++i)
        {
            notifyChangeOwner(i, stateIndex, newOnwer);
        }
    }

    private void sendChangeTroops(int stateIndex, int newTroops)
    {
        for(int i = 0; i < playersManagers.Count; ++i)
        {
            playersManagers[i].TargetNotifyStateChangeTroops(players[i], stateIndex, newTroops);
        }
    }
}

public enum GameState { Reinforcements, Attack, Regroup}
