using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TroopsHandler
{
    public static int TANK_VALUE = 1;
    public static int HELI_VALUE = 5;

    public GameObject tankPrefab;
    public GameObject heliPrefab;

    private StatesManager stateManager;

    public TroopsHandler(StatesManager stateManager)
    {
        this.stateManager = stateManager;

        foreach(State s in stateManager.states)
        {
            s.spawnedTroopsByValue[HELI_VALUE] = new List<UnitManager>();
            s.spawnedTroopsByValue[TANK_VALUE] = new List<UnitManager>();
        }
    }

    public void changeTroops(int stateIndex, int newTroops)
    {
        State s = stateManager.getStateByIndex(stateIndex);
        s.troops = newTroops;

        int desiredHeliCount = newTroops / HELI_VALUE;
        int desiredTankCount = newTroops - desiredHeliCount*HELI_VALUE;

        adjustSpawnedTroops(s, desiredHeliCount, HELI_VALUE);
        adjustSpawnedTroops(s, desiredTankCount, TANK_VALUE);
    }

    private void adjustSpawnedTroops(State s, int desiredTroops, int troopType)
    {
        GameObject prefab = troopType == HELI_VALUE ? heliPrefab : tankPrefab;

        while (desiredTroops != s.spawnedTroopsByValue[troopType].Count)
        {
            if (desiredTroops < s.spawnedTroopsByValue[troopType].Count)
            {
                //Remove
                UnitManager t = s.spawnedTroopsByValue[troopType][0];
                s.spawnedTroopsByValue[troopType].RemoveAt(0);
                GameObject.Destroy(t.gameObject);
            }
            else
            {
                //Spawn
                Vector3 up = getRandomPosNearCapital(s, 5);
                Vector3 forward = Vector3.Cross(Random.onUnitSphere, up);
                UnitManager h = GameObject.Instantiate(prefab, up * 10, Quaternion.LookRotation(forward, up)).GetComponent<UnitManager>();

                if(s.playerIndex >= PlayerNetworkManager.colors.Length || s.playerIndex < 0)
                {
                    Debug.Log(s.playerIndex + "/" + PlayerNetworkManager.colors.Length);
                }

                Debug.Log("Spawning troop in state " + s.stateIndex + " for player " + s.playerIndex);

                h.setColor(PlayerNetworkManager.colors[s.playerIndex]);
                s.spawnedTroopsByValue[troopType].Add(h);
            }
        }
    }

    private Vector3 getRandomPosNearCapital(State s, int tries)
    {
        float minDist = -1;
        Vector3 closest = Vector3.zero;
        Vector3 capitalNormal = s.capital.normalVector;

        for(int i = 0; i < tries; i++)
        {
            int rindex = (int)(Random.value * (s.lands.Count-1));

            if(rindex >= s.lands.Count)
            {
                Debug.Log("Random is fucked : " + rindex + "/" + s.lands.Count);
            }

            Vector3 up = s.lands[rindex].normalVector;
            float dist = Vector3.Angle(up, capitalNormal);

            if(minDist > dist || minDist == -1)
            {
                closest = up;
                minDist = dist;
            }
        }

        return closest;
    }
}
