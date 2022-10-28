using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardManager
{
    private List<State> states;
    private List<Continent> continents;
    public Dictionary<int, List<State>> statesByPlayer;

    public BoardManager(List<State> states, List<Continent> continents)
    {
        this.states = states;
        this.continents = continents;
        statesByPlayer = new Dictionary<int, List<State>>();
    }

    public bool assessNeighbours(int s1Index, int s2Index)
    {
        State s1 = getStateByIndex(s1Index);
        State s2 = getStateByIndex(s2Index);
        return s1.neighbourStates.Contains(s2);
    }

    public void assignStatesForPlayers(int players)
    {
        List<State> allStates = new List<State>(states);
        int playerToGive = 0;

        while(allStates.Count > 0)
        {
            State given = allStates[(int)(allStates.Count * Random.value)];
            allStates.Remove(given);

            associatePlayerToState(playerToGive, given);

            if(!statesByPlayer.ContainsKey(playerToGive))
            {
                statesByPlayer[playerToGive] = new List<State>();
            }

            statesByPlayer[playerToGive].Add(given);

            //Debug.Log("Giving to p" + playerToGive);

            given.troops = 1;

            playerToGive = (playerToGive + 1) % players;
        }
    }

    public Vector2Int computeBattle(State attacker, State defender, int attackSize, int defenseSize)
    {
        wageBattle(attackSize, defenseSize, out int attackLoss, out int defenseLoss);

        attacker.troops -= attackLoss;
        defender.troops -= defenseLoss;

        return new Vector2Int(attackLoss, defenseLoss);
    }

    public void wageBattle(int numberAttack, int numberDefense, out int attackLoss, out int defenseLoss)
    {
        int[] attackDice = throwSortedD6(numberAttack);
        int[] defenseDice = throwSortedD6(numberDefense);

        attackLoss = 0;
        defenseLoss = 0;

        for(int i = 0; i < defenseDice.Length; ++i)
        {
            if(attackDice[i] > defenseDice[i])
            {
                defenseLoss++;
            }
            else
            {
                attackLoss++;
            }
            Debug.Log(attackDice[i] + " vs " + defenseDice[i] + " losses a/d:" + attackLoss +"/" + defenseLoss);
        }

        Debug.Log("results " + attackLoss + "/" + defenseLoss);
    }

    public int[] throwSortedD6(int dice)
    {
        List<int> throws = new List<int>();

        for (int i = 0; i < dice; i++)
        {
            int die = (int)(Random.value * 6) + 1;

            if (die == 7) die = 6;

            bool inserted = false;
            for (int li = 0; li < throws.Count && !inserted; ++li)
            {
                if(die > throws[li])
                {
                    throws.Insert(li, die);
                    inserted = true;
                }
            }

            if(!inserted)
            {
                throws.Add(die);
            }
        }

        return throws.ToArray();
    }

    public void associatePlayerToState(int playerIndex, State s)
    {
        s.playerIndex = playerIndex;
        foreach(GridMapNode n in s.lands)
        {
            n.playerIndex = playerIndex;
        }
    }

    public void switchStateOwner(int playerFrom, int playerTo, State s)
    {
        statesByPlayer[playerFrom].Remove(s);
        statesByPlayer[playerTo].Add(s);
        associatePlayerToState(playerTo, s);
    }

    public int getReinforcementForPlayer(int playerIndex)
    {
        int value = 0;

        value += statesByPlayer[playerIndex].Count / 3;

        foreach(Continent c in continents)
        {
            if(playerHaveWholeContinent(playerIndex, c))
            {
                value += c.pointValue;
            }
        }

        return Mathf.Max(3, value);
    }

    private bool playerHaveWholeContinent(int playerIndex, Continent c)
    {
        foreach(State s in c.states)
        {
            if(s.playerIndex != playerIndex)
            {
                return false;
            }
        }

        return true;
    }

    public bool doesPlayerOwnState(int stateIndex, int playerIndex)
    {
        State s = getStateByIndex(stateIndex);
        return doesPlayerOwnState(s, playerIndex);
    }

    public bool doesPlayerOwnState(State s, int playerIndex)
    {
        if (statesByPlayer.ContainsKey(playerIndex))
        {
            return statesByPlayer[playerIndex].Contains(s);
        }

        return false;
    }

    public int[] getAllStateIndecies()
    {
        int[] indecies = new int[states.Count];

        for(int i = 0; i < states.Count; ++i)
        {
            indecies[i] = states[i].stateIndex;
        }

        return indecies;
    }

    public int getStateOwner(int stateIndex)
    {
        return getStateByIndex(stateIndex).playerIndex;
    }

    public int getTotalStatesNumber()
    {
        return states.Count;
    }

    public State getStateByIndex(int index)
    {
        foreach (State s in states)
        {
            if (index == s.stateIndex)
            {
                return s;
            }
        }
        return null;
    }

    public Continent getContinentByIndex(int index)
    {
        foreach (Continent c in continents)
        {
            if (index == c.continentIndex)
            {
                return c;
            }
        }
        return null;
    }
}
