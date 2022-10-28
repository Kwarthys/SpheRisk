using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StatesManager
{
    private GridMapNode[][] grids;

    public List<State> states = new List<State>();

    public const int NUMBER_OF_FINAL_STATES = 100;
    public const int MINIMAL_ISLAND_SIZE = 2;

    private List<State> selectedStates = new List<State>();
    private List<State> selectedFriendlyStates = new List<State>();
    private List<State> selectedTargetStates = new List<State>();

    private GridMapManager mapManager;
    public ContinentsManager continentManager;

    public List<Bridge> bridges = new List<Bridge>();

    public void addState(State s)
    {
        states.Add(s);
    }

    public StatesManager(GridMapNode[][] grids, GridMapManager mapManager)
    {
        this.grids = grids;
        this.mapManager = mapManager;
    }

    public List<GridMapNode> getStatesCapitals()
    {
        List<GridMapNode> normals = new List<GridMapNode>();
        int i = 0;
        foreach (State s in states)
        {
            if (s.capital == null)
            {
                ++i;
            }
            else
            {
                normals.Add(s.capital);
            }
        }

        if(i!=0)
            Debug.Log(i + "/" + states.Count + " states without capitals");

        return normals;
    }

    public List<State> getStatesToUpdate()
    {
        List<State> toUpdate = new List<State>();
        foreach (State s in states)
        {
            if (s.dataUpdated)
            {
                s.dataUpdated = false;
                toUpdate.Add(s);
            }
        }
        return toUpdate;
    }

    public void createANewStateAndSpread(int stateIndex, GridMapNode startingNode, int maxToAdd)
    {
        State state = new State();
        state.stateIndex = stateIndex;

        List<GridMapNode> nextToAdd = new List<GridMapNode>();
        nextToAdd.Add(startingNode);

        int added = 0;

        while (added < maxToAdd && nextToAdd.Count > 0)
        {
            int randomIndex;

            if(nextToAdd.Count == 1)
            {
                randomIndex = 0;
            }
            else
            {
                float randomValue = (float)mapManager.rng.NextDouble();
                randomIndex = (int)Mathf.Lerp(0, nextToAdd.Count-1, randomValue * randomValue * randomValue); //biased towards lower values
            }

            if(randomIndex < 0 || randomIndex >= nextToAdd.Count)
            {
                Debug.LogError("Random Index is fucked : " + randomIndex + "/" + nextToAdd.Count);
            }

            GridMapNode n = nextToAdd[randomIndex];
            nextToAdd.Remove(n);

            //Debug.Log("Selected " + n.ownerGrid + " " + n.x + ", " + n.y);

            n.playerIndex = stateIndex;
            n.stateIndex = stateIndex;
            state.lands.Add(n);
            added++;

            foreach (GridMapNode nghb in mapManager.getNeighbours(n))
            {
                if (!nextToAdd.Contains(nghb) && nghb.playerIndex == 0)
                {
                    nextToAdd.Add(nghb);
                }
            }
        }

        states.Add(state);
    }

    public void annihilateStates()
    {
        mapManager.updateProgressStatus("Affining states", 0);

        computeAllBordersAndNeighbours();

        int destroyed = 0;
        int toDestroy = states.Count - NUMBER_OF_FINAL_STATES;

        while (states.Count > NUMBER_OF_FINAL_STATES)
        {
            mapManager.updateProgressStatus(1 - ((toDestroy - destroyed) * 1.0f / toDestroy));

            destroyed++;
            //merge smallest to one of its neighbours
            State smallestState = null;
            int smallestSize = int.MaxValue;

            foreach (State s in states)
            {
                int size = s.lands.Count;
                if (smallestSize > size && !s.isIsland)
                {
                    smallestSize = size;
                    smallestState = s;
                }
            }

            if (smallestState == null)
            {
                break;
            }

            List<State> neighbours = smallestState.neighbourStates;

            if (neighbours.Count == 0)
            {
                //smallestState.isIsland = true;
                //Debug.Log(smallestState + " is now an island");

                if(smallestSize <= MINIMAL_ISLAND_SIZE)
                {
                    // Island too small to be a state
                    foreach(GridMapNode n in smallestState.lands)
                    {
                        n.stateIndex = -1;
                        n.playerIndex = -1;
                        n.rogueIsland = true;
                    }

                    states.Remove(smallestState);
                }
                else
                {
                    mergeIslandToClosest(smallestState);
                }

            }
            else
            {
                if ((float)mapManager.rng.NextDouble() > 0.98f)
                {
                    mergeBySize(smallestState);
                }
                else
                {
                    mergeByCommonBorder(smallestState);
                }
            }
        }

        Debug.Log("Generated " + states.Count + " states.");
    }

    public void setStateContinent(State s, int cIndex)
    {
        s.continentIndex = cIndex;
        foreach(GridMapNode n in s.lands)
        {
            n.continentIndex = cIndex;
        }
    }

    private void mergeIslandToClosest(State toMerge)
    {
        State closest = getClosestState(toMerge);
        createBridge(closest, toMerge);
        mergeStates(closest, toMerge);
    }

    private void mergeByCommonBorder(State toMerge)
    {
        List<State> neighbours = toMerge.neighbourStates;

        if (neighbours.Count <= 0) return;

        State candidate = neighbours[0];
        int biggestCommonBorder = getCommonBorderLength(toMerge, candidate);

        for (int i = 1; i < neighbours.Count; ++i)
        {
            State s = neighbours[i];
            int commonBorder = getCommonBorderLength(toMerge, s);
            if (biggestCommonBorder < commonBorder)
            {
                biggestCommonBorder = commonBorder;
                candidate = s;
            }
        }

        //Debug.Log(states.Count + " ByBorder " + toMerge + " + " + candidate);
        mergeStates(toMerge, candidate);
    }


    private void mergeBySize(State toMerge)
    {
        List<State> neighbours = toMerge.neighbourStates;

        if (neighbours.Count <= 0) return;

        State candidate = neighbours[0];
        int smallestNghbSize = candidate.lands.Count;
        foreach (State s in neighbours)
        {
            int size = s.lands.Count;
            if (smallestNghbSize > size)
            {
                smallestNghbSize = size;
                candidate = s;
            }
        }

        //Debug.Log(states.Count + " BySize " + toMerge + " + " + candidate);
        mergeStates(toMerge, candidate);
    }

    public bool tryGetStateByIndex(int index, out State state)
    {
        foreach (State s in states)
        {
            if (s.stateIndex == index)
            {
                state = s;
                return true;
            }
        }

        state = null;
        return false;
    }

    public State getStateByIndex(int index)
    {
        foreach (State s in states)
        {
            if (s.stateIndex == index)
            {
                return s;
            }
        }

        Debug.LogError("Could not find State of index " + index + " in the " + states.Count + " states.");
        return null;
    }

    private int getCommonBorderLength(State a, State b)
    {
        int borderLength = 0;

        if (a.neighbourStates.Contains(b)) //just to be sure
        {
            foreach (GridMapNode n in a.borders)
            {
                bool touching = false;
                foreach (GridMapNode nghb in mapManager.getNeighbours(n))
                {
                    if (nghb.playerIndex == b.stateIndex)
                    {
                        touching = true;
                    }
                }

                if (touching)
                {
                    borderLength++;
                }
            }
        }

        return borderLength;
    }

    private void mergeStates(State a, State b)
    {
        //Debug.Log("Merging " + a + " to " + b);
        List<State> neighboursToUpdate = new List<State>(a.neighbourStates);
        if(neighboursToUpdate.Contains(b))
        {
            neighboursToUpdate.Remove(b);
        }

        foreach (State s in b.neighbourStates)
        {
            if (!neighboursToUpdate.Contains(s))
            {
                neighboursToUpdate.Add(s);
            }
        }

        neighboursToUpdate.Add(a);

        states.Remove(b);

        foreach (GridMapNode node in b.lands)
        {
            node.stateIndex = a.stateIndex;
            node.playerIndex = a.stateIndex;
            a.lands.Add(node);
        }

        foreach (State s in neighboursToUpdate)
        {
            computeBorderAndNeighbours(s);
        }
    }

    public void computeAllBordersAndNeighbours()
    {
        foreach (State s in states)
        {
            computeBorderAndNeighbours(s);
        }
    }

    public void computeAllBorders()
    {
        foreach (State s in states)
        {
            computeStateBorder(s);
        }
    }

    private void computeBorderAndNeighbours(State s)
    {
        List<int> stateIndecies = new List<int>();

        s.borders.Clear();

        bool errorDetected = false;

        foreach (GridMapNode land in s.lands)
        {
            if (land.stateIndex != s.stateIndex && !errorDetected)
            {
                errorDetected = true;
            }

            bool isBorder = false;
            land.isBorder = false;
            land.isWaterBorder = false;

            foreach (GridMapNode nghb in mapManager.getNeighbours(land))
            {
                if (nghb.stateIndex != s.stateIndex)
                {
                    isBorder = true;
                    land.isBorder = true;
                    if (nghb.stateIndex != -1)
                    {
                        if (!stateIndecies.Contains(nghb.stateIndex))
                        {
                            stateIndecies.Add(nghb.stateIndex);
                        }
                    }
                    else
                    {
                        land.isWaterBorder = true;
                    }
                }
            }

            if (isBorder)
            {
                s.borders.Add(land);
            }
        }

        if (errorDetected)
        {
            Debug.LogError("State " + s + " had lands without its stateIndex tag");
        }

        s.neighbourStates.Clear();
        foreach (int index in stateIndecies)
        {
            State ns = getStateByIndex(index);
            if (!s.neighbourStates.Contains(ns))
            {
                s.neighbourStates.Add(ns);
            }
        }
    }

    private void computeStateBorder(State s)
    {
        s.borders.Clear();

        bool errorDetected = false;

        foreach (GridMapNode land in s.lands)
        {
            if(land.stateIndex != s.stateIndex && !errorDetected)
            {
                errorDetected = true;
            }

            bool isBorder = false;
            land.isBorder = false;
            land.isWaterBorder = false;

            foreach (GridMapNode nghb in mapManager.getNeighbours(land))
            {
                if (nghb.stateIndex != s.stateIndex)
                {
                    isBorder = true;
                    land.isBorder = true;
                    if (nghb.stateIndex == -1)
                    {
                        land.isWaterBorder = true;
                    }
                }
            }

            if (isBorder)
            {
                s.borders.Add(land);
            }
        }

        if(errorDetected)
        {
            Debug.LogError("State " + s + " had lands without its stateIndex tag");
        }
    }

    public void reIndexAllStates()
    {
        int indecies = 1;

        foreach (State s in states)
        {
            reIndexState(indecies++, s);
        }
    }

    public void reIndexState(int newIndex, State s)
    {
        s.stateIndex = newIndex;

        foreach (GridMapNode n in s.lands)
        {
            n.stateIndex = newIndex;
        }

        s.dataUpdated = true;
    }

    /*  that was hacking-tier debug
    public void selectContinent(int cIndex)
    {
        foreach(State s in states)
        {
            if(s.continentIndex == cIndex)
            {
                foreach (GridMapNode n in s.lands)
                {
                    n.selected = true;
                }

                s.dataUpdated = true;
            }
        }

        foreach(State s in continentManager.getAllNeighbouringContinentStates(cIndex))
        {
            foreach (GridMapNode n in s.lands)
            {
                n.targetable = true;
            }
            s.dataUpdated = true;
        }
    }
    */

    public State getSelectedState()
    {
        if (selectedStates.Count == 0) return null;
        return selectedStates[0];
    }

    public void deselectState()
    {
        applySelection(false);

        selectedStates.Clear();
        selectedFriendlyStates.Clear();
        selectedTargetStates.Clear();
    }

    public void selectState(int index, bool status = true)
    {
        selectState(getStateByIndex(index), status);
    }

    public void selectState(State s, bool status = true)
    {
        List<State> states = new List<State>();
        states.Add(s);
        selectState(states, status);
    }

    public void selectState(List<State> states, bool status = true)
    {
        deselectState();

        selectedStates.AddRange(states);

        List<State> nghbs = new List<State>();

        foreach(State s in states)
        {
            foreach(State nghb in s.neighbourStates)
            {
                if(!nghbs.Contains(nghb) && !selectedStates.Contains(nghb))
                {
                    nghbs.Add(nghb);
                }
            }
        }

        foreach(State nghb in nghbs)
        {
            if(nghb.playerIndex == states[0].playerIndex)
            {
                selectedFriendlyStates.Add(nghb);
            }
            else
            {
                selectedTargetStates.Add(nghb);
            }
        }

        applySelection(true);
    }

    private void applySelection(bool status)
    {
        foreach (State s in selectedStates)
        {
            s.selected = status;
            s.dataUpdated = true;

            foreach (GridMapNode n in s.lands)
            {
                n.selected = status;
            }
        }

        foreach (State s in selectedTargetStates)
        {
            s.targetable = status;
            s.dataUpdated = true;

            foreach (GridMapNode n in s.lands)
            {
                n.targetable = status;
            }
        }

        foreach (State s in selectedFriendlyStates)
        {
            s.isFriendly = status;
            s.dataUpdated = true;

            foreach (GridMapNode n in s.lands)
            {
                n.isFriendly = status;
            }
        }
    }

    public void generateCapitals()
    {
        mapManager.updateProgressStatus("Generating capitals");

        for(int si = 0; si < states.Count; ++si)
        {
            mapManager.updateProgressStatus(si * 1.0f / states.Count);

            State s = states[si];
            Vector3 meanDir = new Vector3(0,0,0);

            int size = s.lands.Count;

            foreach (GridMapNode n in s.lands)
            {
                meanDir += n.normalVector / size;
            }

            int randomChoices = 10;

            GridMapNode[] candidates = new GridMapNode[randomChoices];
            int closestToMean = -1;
            float distToMean = 0;

            for (int i = 0; i < randomChoices; ++i)
            {
                candidates[i] = s.lands[(int)(mapManager.rng.NextDouble() * s.lands.Count)];
                float dist = Vector3.Angle(candidates[i].normalVector, meanDir);

                if (closestToMean == -1 || distToMean > dist)
                {
                    distToMean = dist;
                    closestToMean = i;
                }
            }

            s.capital = candidates[closestToMean];
        }
    }

    public State getClosestState(State s)
    {
        List<GridMapNode> toSearch = new List<GridMapNode>(s.borders);
        List<GridMapNode> searched = new List<GridMapNode>();

        while (toSearch.Count > 0)
        {
            GridMapNode currentNode = toSearch[0];
            toSearch.RemoveAt(0);
            searched.Add(currentNode);

            foreach (GridMapNode neighbour in mapManager.getNeighbours(currentNode))
            {
                if (neighbour.stateIndex != -1 && neighbour.stateIndex != s.stateIndex)
                {
                    return getStateByIndex(neighbour.stateIndex);
                }
                else if (neighbour.stateIndex == -1)
                {
                    if (!searched.Contains(neighbour) && !toSearch.Contains(neighbour))
                    {
                        toSearch.Add(neighbour);
                    }
                }
            }
        }

        Debug.LogError("Could not find a closestState");
        return null;
    }

    public GridMapNode[] closestPointsOfTwoStates(State s1, State s2)
    {
        GridMapNode[] closest = new GridMapNode[2];
        float minDist = float.MaxValue;

        foreach(GridMapNode n1 in s1.borders)
        {
            if(n1.isWaterBorder)
            {
                foreach(GridMapNode n2 in s2.borders)
                {
                    if(n2.isWaterBorder)
                    {
                        float dist = mapManager.getDistance(n1, n2);
                        if(dist < minDist)
                        {
                            closest[0] = n1;
                            closest[1] = n2;
                            minDist = dist;
                        }
                    }
                }
            }
        }

        return closest;
    }

    public State addNeighbourToClosestState(State s)
    {
        State nghb = getClosestState(s);
        createBridge(s, nghb);
        return nghb;
    }

    public void createBridge(State s1, State s2)
    {
        GridMapNode[] closestPoints = closestPointsOfTwoStates(s1, s2);
        Vector3[] connection = { closestPoints[0].normalVector, closestPoints[1].normalVector };

        s1.neighbourStates.Add(s2);
        s2.neighbourStates.Add(s1);

        //Register bridge
        bridges.Add(new Bridge()
        {
            normalStart = connection[0],
            normalEnd = connection[1]
        });
    }

    public void registerBuildingsFromData(Bridge[] bridgesData, Vector3[] citiesData)
    {
        foreach(Bridge data in bridgesData)
        {
            bridges.Add(new Bridge()
            {
                normalStart = data.normalStart,
                normalEnd = data.normalEnd
            });
        }

        foreach(Vector3 pos in citiesData)
        {
            GridMapNode capital = mapManager.getNodeAt((int)pos.z, (int)pos.x, (int)pos.y);
            getStateByIndex(capital.stateIndex).capital = capital;
        }

        Debug.Log("Finished registering buildings");
    }

    public void generateBuildingsAuthoringData(out Bridge[] bridgesData, out Vector3[] citiesData)
    {
        bridgesData = bridges.ToArray();

        citiesData = new Vector3[states.Count];
        for(int i = 0; i < states.Count; ++i)
        {
            State s = states[i];
            citiesData[i] = new Vector3(s.capital.x, s.capital.y, (int)s.capital.ownerGrid);
        }
    }

    public Vector2Int[] getStateNeighboursData()
    {
        List<Vector2Int> couples = new List<Vector2Int>();
        List<int> done = new List<int>();

        foreach(State s in states)
        {
            foreach(State n in s.neighbourStates)
            {
                if(!done.Contains(n.stateIndex))
                {
                    couples.Add(new Vector2Int(s.stateIndex, n.stateIndex));
                }
            }

            done.Add(s.stateIndex);
        }

        return couples.ToArray();
    }
}

public class State
{
    public List<GridMapNode> lands = new List<GridMapNode>();
    public List<GridMapNode> borders = new List<GridMapNode>();

    public GridMapNode capital;

    public List<State> neighbourStates = new List<State>();

    public int stateIndex = -1;
    public int continentIndex = -1;
    public int playerIndex = 0;

    public bool isIsland = false;
    public bool selected = false;
    public bool targetable = false;
    public bool isFriendly = false;

    public int troops = 0;

    public bool dataUpdated = false;

    public Dictionary<int, List<UnitManager>> spawnedTroopsByValue = new Dictionary<int, List<UnitManager>>();

    public override string ToString()
    {
        return stateIndex + " (" + playerIndex + ")";
    }
}
