using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ContinentsManager
{
    public StatesManager stateManager;
    public GridMapManager mapManager;

    private int minContinentSize = 4;
    private int minNumberOfNeighbours = 3;
    private float minDistForceBridge = 5;

    public List<Continent> continents = new List<Continent>();

    public ContinentsManager(StatesManager stateManager, GridMapManager mapManager)
    {
        this.stateManager = stateManager;
        this.mapManager = mapManager;
    }

    public void addContinent(Continent c)
    {
        continents.Add(c);
    }

    public void generateContinents()
    {
        mapManager.updateProgressStatus("Generating Continents");

        List<State> nonAllocatedStates = new List<State>(stateManager.states);
        int continentIndeciesCounter = 0;

        while(nonAllocatedStates.Count > 0)
        {
            mapManager.updateProgressStatus(1 - (nonAllocatedStates.Count * 1.0f / stateManager.states.Count));

            //Find nonAllocatedState with less neighbours
            State state = nonAllocatedStates[0];
            int nghbs = state.neighbourStates.Count;

            for(int i = 1; i < nonAllocatedStates.Count; ++i)
            {
                State candidate = nonAllocatedStates[i];
                int nghbsCandidate = candidate.neighbourStates.Count;
                if(nghbs > nghbsCandidate)
                {
                    nghbs = nghbsCandidate;
                    state = candidate;
                }
            }

            State lessNghbs = state;

            if(lessNghbs.neighbourStates.Count == 0)
            {
                //island
                state = stateManager.addNeighbourToClosestState(lessNghbs);
            }
            else
            {
                //Find its neighbour with most neighbours
                List<State> stateNghbs = lessNghbs.neighbourStates;

                state = stateNghbs[0];
                nghbs = state.neighbourStates.Count;

                for (int i = 1; i < stateNghbs.Count; ++i)
                {
                    State candidate = stateNghbs[i];
                    int nghbsCandidate = candidate.neighbourStates.Count;
                    if (nghbs < nghbsCandidate)
                    {
                        nghbs = nghbsCandidate;
                        state = candidate;
                    }
                }
            }            

            if(state.continentIndex == -1)
            {
                Continent newContinent = new Continent();
                int continentIndex = continentIndeciesCounter++;
                newContinent.continentIndex = continentIndex;

                newContinent.states.Add(lessNghbs);
                stateManager.setStateContinent(lessNghbs, continentIndex);

                newContinent.states.Add(state);
                stateManager.setStateContinent(state, continentIndex);

                continents.Add(newContinent);

                nonAllocatedStates.Remove(state);
            }
            else
            {
                Continent c = getContinentByIndex(state.continentIndex);
                stateManager.setStateContinent(lessNghbs, c.continentIndex);
                c.states.Add(lessNghbs);
            }            

            nonAllocatedStates.Remove(lessNghbs);
        }

        computeAllContinentsNeighbours();

        Debug.Log("Generated first wave continents");

        annihilateContinents();

        foreach (Continent c in continents)
        {
            foreach(State s in c.states)
            {
                s.playerIndex = c.continentIndex + 1;
                foreach(GridMapNode n in s.lands)
                {
                    n.playerIndex = c.continentIndex + 1;
                }
            }
        }

        Debug.Log("Generated " + continents.Count + " continents");
    }

    public void finishMap()
    {
        checkIfAllLinked();
        computeAllBorders();
        computeContinentsValue();

        Debug.Log("Continents generation done");
    }

    public List<State> getAllNeighbouringContinentStates(int cIndex)
    {
        Continent c = getContinentByIndex(cIndex);

        List<State> states = new List<State>();

        foreach(Continent nc in c.neighbours)
        {
            states.AddRange(nc.states);
        }

        return states;
    }


    private void annihilateContinents()
    {
        mapManager.updateProgressStatus("Affining Continents");

        List<Continent> tooSmalls = new List<Continent>();

        foreach(Continent c in continents)
        {
            if(c.states.Count <= minContinentSize)
            {
                tooSmalls.Add(c);
            }
        }

        while(tooSmalls.Count > 0)
        {
            mapManager.updateProgressStatus(1 - (tooSmalls.Count * 1.0f / continents.Count));

            Continent c = tooSmalls[(int)(mapManager.rng.NextDouble() * tooSmalls.Count)];

            if(c.states.Count <= minContinentSize)
            {
                //still too small
                //get smaller neighbour continent
                List<Continent> nghbs = c.neighbours;

                if(nghbs.Count > 0)
                {
                    if(mapManager.rng.NextDouble() > 0.2)
                    {
                        if (tryGetSmallerContinentIn(nghbs, out Continent smaller))
                        {
                            //Debug.Log(c + " " + smaller + " MergedBySize");
                            mergeContinents(c, smaller);

                            tooSmalls.Remove(smaller);
                        }
                    }
                    else
                    {
                        if(tryGetMostCommonBorderIn(c, nghbs, out Continent commonBorders))
                        {
                            //Debug.Log(c + " " + commonBorders + " MergedByBorder");
                            mergeContinents(c, commonBorders);

                            tooSmalls.Remove(commonBorders);
                        }
                    }
                }
                else
                {
                    //tooSmalls.Remove(c);
                    //Debug.Log("CIsland " + c);

                    //bridges ?
                    mergeIslandContinentToClosest(c);
                    tooSmalls.Remove(c);
                }
            }
            else
            {
                tooSmalls.Remove(c);
            }
        }
    }

    private bool mergeIslandContinentToClosest(Continent island)
    {
        computeContinentBorders(island);

        if (island.borders.Count == 0)
        {
            Debug.LogError("c" + island + " has 0 length borders");
            return false;
        }

        int numberOfTries = island.states.Count * 3;
        float closestDisance = float.MaxValue;
        int[] closestStatesIndecies = null;

        for(int i = 0; i < numberOfTries; ++i)
        {
            GridMapNode nodeCandidate = island.borders[(int)(island.borders.Count * mapManager.rng.NextDouble())];

            int[] candidate = getClosestContinentFrom(nodeCandidate, out float dist);

            if(candidate != null)
            {
                if(dist < closestDisance)
                {
                    closestStatesIndecies = candidate;
                    closestDisance = dist;
                }

                //Debug.Log("For c" + island.continentIndex + " found c" + stateManager.getStateByIndex(candidate[1]).continentIndex + " at " + dist + " min=" + closestDisance + " : s" + candidate[0] + " - s" + candidate[1]);
            }
        }

        if(closestStatesIndecies != null)
        {
            State stateFrom = stateManager.getStateByIndex(closestStatesIndecies[0]);
            State stateTo = stateManager.getStateByIndex(closestStatesIndecies[1]);

            stateManager.createBridge(stateFrom, stateTo);

            Continent toMerge = getContinentByIndex(stateTo.continentIndex);

            //Debug.Log("Merged continents " + toMerge + " to " + island);

            mergeContinents(toMerge, island);
        }
        else
        {
            Debug.LogWarning("Could not merge Island c" + island);
        }

        return closestStatesIndecies != null;
    }

    private int[] getClosestContinentFrom(GridMapNode from, out float distance)
    {
        int cIndex = from.continentIndex;
        distance = float.MaxValue;

        List<GridMapNode> toSearch = new List<GridMapNode>();
        toSearch.Add(from);
        List<GridMapNode> searched = new List<GridMapNode>();

        int waterFound = 0;

        while(toSearch.Count > 0)
        {
            GridMapNode current = toSearch[0];
            toSearch.RemoveAt(0);
            searched.Add(current);

            foreach(GridMapNode nghb in mapManager.getNeighbours(current))
            {
                if (nghb.continentIndex == -1)
                {
                    //Water, add to toSearch list
                    if (!toSearch.Contains(nghb) && !searched.Contains(nghb))
                    {
                        waterFound++;
                        toSearch.Add(nghb);
                    }
                }
                else if (nghb.continentIndex != cIndex)
                {
                    //Found one
                    distance = mapManager.getDistance(from, nghb);
                    int[] data = new int[2];
                    data[0] = from.stateIndex;
                    data[1] = nghb.stateIndex;
                    return data;
                }
            }

            //Debug.Log("Left to Search : " + toSearch.Count+ ". Searched : " + searched.Count);
        }

        //Debug.LogWarning("Could not find continent close to " + from.continentIndex + ". Searched " + waterFound + " waters.");

        return null;
    }

    public void bridgeClosestContinents()
    {
        mapManager.updateProgressStatus("Adding bridges ...");

        for(int ci = 0; ci < continents.Count; ++ci)
        {
            mapManager.updateProgressStatus(1.0f * ci / continents.Count);

            //Debug.Log((continents.Count-ci) + " cs left");
            Continent current = continents[ci];

            computeContinentBorders(current);

            //GetMany points from borders
            int attempts = current.states.Count * 5;

            int[][] indecies = new int[attempts][];
            float[] distances = new float[attempts];

            for (int attempti = 0; attempti < attempts; ++attempti)
            {
                int index = (int)(current.borders.Count * mapManager.rng.NextDouble());

                if (index < 0 || index >= current.borders.Count)
                {
                    Debug.Log(index + "/" + current.borders.Count);
                }

                GridMapNode n = current.borders[index];
                int[] candidateStateIndecies = getClosestContinentFrom(n, out float dist);

                indecies[attempti] = candidateStateIndecies;
                distances[attempti] = dist;
            }

            //Debug.Log("Calculated points candidate");

            bool goOn = true;

            while(goOn)
            {
                int smallestDistanceIndex = 0;
                float smallestDistance = distances[0];

                for(int i = 1; i < distances.Length; ++i)
                {
                    if(smallestDistance > distances[i])
                    {
                        smallestDistance = distances[i];
                        smallestDistanceIndex = i;
                    }
                }

                if(smallestDistance == float.MaxValue)
                {
                    goOn = false;
                }
                else
                {
                    int[] candidateStateIndecies = indecies[smallestDistanceIndex];
                    distances[smallestDistanceIndex] = float.MaxValue; //marking it to avoid rechecking it

                    State connexionFrom = stateManager.getStateByIndex(candidateStateIndecies[0]);
                    State connexionTo = stateManager.getStateByIndex(candidateStateIndecies[1]);
                    Continent connextionToContinent = getContinentByIndex(connexionTo.continentIndex);

                    bool buildBridge = false;

                    //if not already neighbours
                    if (!connexionFrom.neighbourStates.Contains(connexionTo))
                    {
                        if (smallestDistance < minDistForceBridge)
                        {
                            buildBridge = true;
                        }
                        else
                        {
                            if(current.neighbours.Count < minNumberOfNeighbours)
                            {
                                //Still under minNeighbours requierement
                                //Checks neighbours'neighour'neighbours
                                List<State> nghbs = new List<State>(connexionFrom.neighbourStates);
                                int size = nghbs.Count;
                                for (int i = 0; i < size; ++i)
                                {
                                    State nghb = nghbs[i];
                                    foreach (State nghb2 in nghb.neighbourStates)
                                    {
                                        if (!nghbs.Contains(nghb2))
                                        {
                                            nghbs.Add(nghb2);
                                        }
                                    }
                                }

                                bool found = false;
                                foreach (State nghb in nghbs)
                                {
                                    if (connexionTo.neighbourStates.Contains(nghb))
                                    {
                                        found = true;
                                        break;
                                    }
                                }

                                buildBridge = !found;
                            }
                            else
                            {
                                goOn = false;
                            }
                        }

                        if (buildBridge)
                        {
                            stateManager.createBridge(connexionFrom, connexionTo);

                            if (!current.neighbours.Contains(connextionToContinent))
                            {
                                current.neighbours.Add(connextionToContinent);
                                connextionToContinent.neighbours.Add(current);
                            }
                        }
                    }                    
                }
            }
        }
    }

    private bool tryGetMostCommonBorderIn(Continent from, List<Continent> list, out Continent mostCommonBorder)
    {
        mostCommonBorder = null;

        if(list.Count > 0)
        {
            mostCommonBorder = list[0];
            if(list.Count > 1)
            {
                int value = getCommonBorderBetween(from, mostCommonBorder);

                for(int i = 1; i < list.Count; ++i)
                {
                    Continent candidate = list[i];
                    int candidateValue = getCommonBorderBetween(from, list[i]);

                    if(value < candidateValue)
                    {
                        value = candidateValue;
                        mostCommonBorder = candidate;
                    }
                }
            }

            return true;
        }
        else
        {
            return false;
        }
    }

    private bool tryGetSmallerContinentIn(List<Continent> list, out Continent smaller)
    {
        smaller = null;

        if(list.Count > 0)
        {
            smaller = list[0];
            int size = smaller.states.Count;

            for (int i = 1; i < list.Count; ++i)
            {
                Continent candidate = list[i];
                int candidateSize = candidate.states.Count;
                if (size > candidateSize)
                {
                    size = candidateSize;
                    smaller = candidate;
                }
            }

            return true;
        }

        return false;        
    }

    public bool tryGetContinentByIndex(int index, out Continent continent)
    {
        foreach (Continent c in continents)
        {
            if (c.continentIndex == index)
            {
                continent = c;
                return true;
            }
        }

        continent = null;
        return false;
    }

    public Continent getContinentByIndex(int index)
    {
        foreach(Continent c in continents)
        {
            if(c.continentIndex == index)
            {
                return c;
            }
        }

        Debug.LogError("Could not find continent " + index);
        return null;
    }

    private int getCommonBorderBetween(Continent c1, Continent c2)
    {
        int commonBorder = 0;
        int i = c2.continentIndex;
        foreach(GridMapNode n in c1.borders)
        {
            foreach(GridMapNode nghb in mapManager.getNeighbours(n))
            {
                if(nghb.continentIndex == i)
                {
                    commonBorder++;
                }
            }
        }

        return commonBorder;
    }

    public void computeAllBorders()
    {
        foreach (Continent c in continents)
        {
            computeContinentBorders(c);
        }
    }

    private void computeContinentBorders(Continent c)
    {
        List<GridMapNode> continentActualBorders = new List<GridMapNode>();
        List<GridMapNode> continentAllBorders = new List<GridMapNode>();

        foreach(GridMapNode n in c.borders)
        {
            n.isContinentBorder = false;
        }

        foreach (State s in c.states)
        {
            continentAllBorders.AddRange(s.borders);
        }

        foreach (GridMapNode border in continentAllBorders)
        {
            bool actualCBorder = false;
            foreach (GridMapNode borderNghb in mapManager.getNeighbours(border))
            {
                if (borderNghb.continentIndex != c.continentIndex)
                {
                    actualCBorder = true;
                    break;
                }
            }

            if (actualCBorder)
            {
                continentActualBorders.Add(border);
                border.isContinentBorder = true;
            }
        }

        c.borders = continentActualBorders;
    }

    private void computeContinentNeighbours(Continent c)
    {
        List<int> otherCIndecies = new List<int>();
        foreach (State s in c.states)
        {
            foreach (State ns in s.neighbourStates)
            {
                int nIndex = ns.continentIndex;
                if (nIndex != c.continentIndex && !otherCIndecies.Contains(nIndex))
                {
                    otherCIndecies.Add(nIndex);
                }
            }
        }

        c.neighbours.Clear(); //to be sure

        foreach (int i in otherCIndecies)
        {
            c.neighbours.Add(getContinentByIndex(i));
        }
    }

    private void computeAllContinentsNeighbours()
    {
        foreach(Continent c in continents)
        {
            computeContinentNeighbours(c);
        }
    }

    private void mergeContinents(Continent a, Continent b)
    {
        foreach(State s in b.states)
        {
            a.states.Add(s);
            stateManager.setStateContinent(s, a.continentIndex);
        }

        foreach(Continent c in b.neighbours)
        {
            if(!a.neighbours.Contains(c) && c != a)
            {
                a.neighbours.Add(c);
            }
        }

        computeContinentNeighbours(a);

        foreach(Continent c in a.neighbours)
        {
            computeContinentNeighbours(c);
        }

        continents.Remove(b);
    }

    private void computeContinentsValue()
    {
        foreach(Continent c in continents)
        {
            c.pointValue = (int)((getContinentAccessPoints(c)*1.0f + c.states.Count/2f) / 2f);
        }
    }

    private int getContinentAccessPoints(Continent c)
    {
        int accessPoints = 0;

        foreach(State s in c.states)
        {
            bool isAccessPoint = false;

            foreach(State nghb in s.neighbourStates)
            {
                if(nghb.continentIndex != c.continentIndex)
                {
                    isAccessPoint = true;
                    break;
                }
            }

            if(isAccessPoint)
            {
                accessPoints++;
            }
        }

        return accessPoints;
    }

    private bool checkIfAllLinked()
    {
        List<Continent> linked = new List<Continent>();
        linked.Add(continents[0]);

        List<Continent> toSearch = new List<Continent>();

        foreach(Continent c in linked[0].neighbours)
        {
            toSearch.Add(c);
            linked.Add(c);
        }

        while(toSearch.Count > 0)
        {
            Continent current = toSearch[0];
            toSearch.RemoveAt(0);

            foreach(Continent c in current.neighbours)
            {
                if(!linked.Contains(c))
                {
                    linked.Add(c);
                    if(!toSearch.Contains(c))
                    {
                        toSearch.Add(c);
                    }
                }                
            }
        }

        string log = "";
        foreach(Continent c in linked)
        {
            log += c.continentIndex + " ";
        }
        Debug.Log("Linked : " + log);

        if (linked.Count != continents.Count)
        {
            Debug.Log(linked.Count + " vs " + continents.Count + "!");
            List<Continent> missings = new List<Continent>();

            foreach(Continent c in continents)
            {
                if(!linked.Contains(c))
                {
                    missings.Add(c);
                    Debug.Log("Missing " + c);
                }
            }
        }
        else
        {
            Debug.Log("allLinked");
        }

        return linked.Count == continents.Count;
    }

    public Vector2Int[] getContinentsNeighboursData()
    {
        List<Vector2Int> couples = new List<Vector2Int>();
        List<int> done = new List<int>();

        foreach(Continent c in continents)
        {
            foreach(Continent n in c.neighbours)
            {
                if(!done.Contains(n.continentIndex))
                {
                    couples.Add(new Vector2Int(c.continentIndex, n.continentIndex));
                }
            }
            done.Add(c.continentIndex);
        }

        return couples.ToArray();
    }
}

public class Continent
{
    public List<GridMapNode> borders = new List<GridMapNode>();
    public List<State> states = new List<State>();
    public List<Continent> neighbours = new List<Continent>();

    public int pointValue = 0;

    public int continentIndex;

    public override string ToString()
    {
        return "c" + continentIndex + "(" + states.Count + ", " + neighbours.Count + " -> " + pointValue + ")";
    }
}
