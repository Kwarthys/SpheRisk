using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridMapManager
{
    private int resolution;

    private GridMapNode[][] grids = new GridMapNode[6][];

    private Mesh[] meshes = new Mesh[6];
    private Vector2[][] uv3s = new Vector2[6][];
    private Texture2D[] texes = new Texture2D[6];
    private Texture2D[] altTexes = new Texture2D[6];

    private Vector2Int[] neighboursOffsets;

    private StatesManager stateManager;
    private ContinentsManager continentsManager;

    public Color oceanColor = new Color(0, 0, 1);

    public System.Random rng;

    public TroopsHandler troopsHandler;

    public GameObject heliPrefab;
    public GameObject tankPrefab;

    /**MultiThreadedFollowProcess**/
    public volatile string taskDescription = "";
    public volatile float progressBarLevel = 0;
    public volatile bool generationDone = false;
    public volatile bool updateMeshesColors = false;
    /******************************/

    public void updateProgressStatus(string description, float amount = 0)
    {
        taskDescription = description;
        progressBarLevel = amount;
    }

    public GridMapNode getNodeAt(int gridIndex, int x, int y)
    {
        return grids[gridIndex][x + y * resolution];
    }

    public void updateProgressStatus(float amount)
    {
        progressBarLevel = amount;
    }

    public void registerMesh(Mesh mesh, int mapIndex)
    {
        meshes[mapIndex] = mesh;
        uv3s[mapIndex] = mesh.uv3;
    }

    public Texture2D getTextureOfGrid(int mapIndex)
    {
        return texes[mapIndex];
    }

    public Texture2D getAltTextureOfGrid(int mapIndex)
    {
        return altTexes[mapIndex];
    }

    public void generateStates()
    {
        taskDescription = "Generating States";

        int stateIndex = 1;

        int totalSize = 6 * resolution * resolution;
        int gridSize = resolution * resolution;

        for (int grid = 0; grid < 6; ++grid)
        {
            for (int i = 0; i < resolution * resolution; ++i)
            {
                GridMapNode node = grids[grid][i];
                if (node.stateIndex == 0)
                {
                    stateManager.createANewStateAndSpread(stateIndex++, node, 100);
                }

                progressBarLevel = (grid * gridSize + i) / totalSize;
            }
        }

        updateMeshesColors = true;

        stateManager.annihilateStates();

        updateMeshesColors = true;

        stateManager.generateCapitals();

        continentsManager.generateContinents();
        updateMeshesColors = true;

        continentsManager.bridgeClosestContinents();
        continentsManager.finishMap();
        updateMeshesColors = true;

        generationDone = true;
    }

    public GridMapNode getClickedNode(Vector3 normalizedClic)
    {
        if (!generationDone) return null;

        Vector3[] directions = { Vector3.up, Vector3.forward, Vector3.right, Vector3.back, Vector3.left, Vector3.down };
        int closestDir = 0;
        float closestAngle = Vector3.Angle(directions[0], normalizedClic);

        for (int i = 1; i < 6; ++i)
        {
            float angle = Vector3.Angle(directions[i], normalizedClic);
            if (closestAngle > angle)
            {
                closestAngle = angle;
                closestDir = i;
            }
        }

        //look for closest point
        GridMapNode node = grids[closestDir][(resolution * resolution) / 2]; //start in face's middle
        closestAngle = Vector3.Angle(node.normalVector, normalizedClic);
        bool nghbCloser = true;

        while (nghbCloser)
        {
            GridMapNode closest = null;
            nghbCloser = false;
            foreach (GridMapNode nghb in getNeighbours(node))
            {
                float angle = Vector3.Angle(nghb.normalVector, normalizedClic);
                if (angle < closestAngle)
                {
                    closest = nghb;
                    closestAngle = angle;
                    nghbCloser = true;
                }
            }

            if (closest != null)
                node = closest;
        }

        return node;
    }

    public void selectStateAt(Vector3 normalizedClic)
    {
        GridMapNode node = getClickedNode(normalizedClic);

        if (node == null) return;

        if (node.stateIndex == -1)
        {
            //deselectonly
            stateManager.deselectState();
            //Debug.Log("hit Water");
        }
        else
        {
            stateManager.selectState(node.stateIndex);
            Debug.Log("Selected state " + stateManager.getStateByIndex(node.stateIndex) + " in Continent " + continentsManager.getContinentByIndex(node.continentIndex));
        }

        updateChangedStates();
    }

    public void updateChangedStates()
    {
        List<State> toUpdate = stateManager.getStatesToUpdate();

        if (toUpdate.Count > 0)
        {
            applyStateDataToMeshes(toUpdate);
        }
    }

    public bool tryGetBothStates(Vector3 clicOnSphere, out State selected, out State clicked)
    {
        clicked = null;
        selected = getSelectedStates();
        GridMapNode clickedNode = getClickedNode(clicOnSphere);

        if (clickedNode.stateIndex == -1) return false;

        clicked = stateManager.getStateByIndex(clickedNode.stateIndex);

        return clicked != null && selected != null;
    }

    public State getSelectedStates()
    {
        return stateManager.getSelectedState();
    }

    public void generateBuildingsAuthoringData(out Bridge[] bridgesData, out Vector3[] citiesData)
    {
        stateManager.generateBuildingsAuthoringData(out bridgesData, out citiesData);
    }

    public void generateNeighboursAuthoringData(out Vector2Int[] stateNeighbours, out Vector2Int[] continentsNeighbours)
    {
        stateNeighbours = stateManager.getStateNeighboursData();
        continentsNeighbours = continentsManager.getContinentsNeighboursData();
    }

    public void generateAuthoringData(out int[] stateIndecies, out int[] continentIndecies, out Vector3[] normals)
    {
        stateIndecies = new int[6 * resolution * resolution];
        continentIndecies = new int[6 * resolution * resolution];
        normals = new Vector3[6 * resolution * resolution];

        int gridSize = resolution * resolution;


        List<int> generatedIndecies = new List<int>();

        for (int gridIndex = 0; gridIndex < 6; gridIndex++)
        {
            int i = 0;
            foreach(GridMapNode n in grids[gridIndex])
            {
                stateIndecies[gridIndex * gridSize + i] = n.stateIndex;
                continentIndecies[gridIndex * gridSize + i] = n.continentIndex;
                normals[gridIndex * gridSize + i] = n.normalVector;

                //Debug.Log("gridIndex " + gridIndex + " inGridIndex " + i + " totalIndex " + (gridIndex * gridSize + i) + " stateID " + n.stateIndex);

                if(!generatedIndecies.Contains(n.stateIndex))
                {
                    generatedIndecies.Add(n.stateIndex);
                }

                ++i;
            }
        }
        string debug = "";
        foreach (int si in generatedIndecies)
        {
            debug += "s" + si + " ";
        }
        Debug.Log(debug);
    }

    public float[] generateAllAltitudeData()
    {
        int gridSize = resolution * resolution;
        float[] allAltitudes = new float[6 * gridSize];

        for(int gridIndex = 0; gridIndex < 6; ++gridIndex)
        {
            Vector2[] uv3s = meshes[gridIndex].uv3;
            for(int i = 0; i < resolution * resolution; ++i)
            {
                allAltitudes[gridIndex * gridSize + i] = uv3s[i].x;
            }
        }

        return allAltitudes;
    }

    public void generateGridMapFromPartialData(int resolution, int startingIndex, int[] stateIndecies, int[] continentIndecies, Vector3[] normalVectors)
    {
        int gridSize = resolution * resolution;


        string debug = "";

        bool s0 = false;

        for(int i = 0; i < stateIndecies.Length; ++i)
        {
            int fullIndex = startingIndex + i;
            int gridIndex = fullIndex / gridSize;
            int index = fullIndex - gridIndex * gridSize;

            int x = index % resolution;
            int y = index / resolution;

            int stateIndex = stateIndecies[i];
            int continentIndex = continentIndecies[i];

            //Debug.Log("fullIndex:" + fullIndex + " - gridIndex " + gridIndex + " indexInGrid " + index + " : " + x + "," + y + " in State s" + stateIndex);

            
            if(stateIndex == 0)
            {
                //Debug.LogError("State index is 0");
                s0 = true;
            }
            

            GridMapNode n = new GridMapNode()
            {
                ownerGrid = (GridFacesEnum)gridIndex,
                x = x,
                y = y,
                index = index,
                playerIndex = continentIndex,
                stateIndex = stateIndex,
                continentIndex = continentIndex,
                normalVector = normalVectors[i]
            };

            if(stateIndex != -1)
            {
                State s;
                if(stateManager.tryGetStateByIndex(stateIndex, out s))
                {
                    s.lands.Add(n);
                }
                else
                {
                    s = new State();
                    s.stateIndex = stateIndex;
                    s.continentIndex = continentIndex;
                    s.playerIndex = continentIndex;
                    s.lands.Add(n);
                    stateManager.addState(s);

                    debug += " s" + stateIndex;

                    Continent c;
                    if (!continentsManager.tryGetContinentByIndex(continentIndex, out c))
                    {
                        c = new Continent();
                        c.continentIndex = continentIndex;
                        continentsManager.addContinent(c);
                    }

                    if (!c.states.Contains(s))
                    {
                        c.states.Add(s);
                    }
                }
            }

            grids[gridIndex][index] = n;        
        }

        //if(debug.Length != 0)
            //Debug.Log("Created" + debug);

        if (s0)
        {
            Debug.LogError("State index is 0 in package " + startingIndex + " to " + (startingIndex + stateIndecies.Length));
        }
    }

    public void generateNeighbourDataFromArrays(Vector2Int[] stateNeighbours, Vector2Int[] continentsNeighbours)
    {
        foreach (Vector2Int nghbs in stateNeighbours)
        {
            State s1 = stateManager.getStateByIndex(nghbs.x);
            State s2 = stateManager.getStateByIndex(nghbs.y);

            if(s2 != null && s1 != null)
            {
                s1.neighbourStates.Add(s2);
                s2.neighbourStates.Add(s1);

                //Debug.Log(s1 + "--" + s2);
            }
            else
            {
                Debug.LogWarning("Received non existent State from network : " + nghbs.x + "-" + nghbs.y);
            }
        }

        foreach (Vector2Int nghbs in continentsNeighbours)
        {
            Continent c1 = continentsManager.getContinentByIndex(nghbs.x);
            Continent c2 = continentsManager.getContinentByIndex(nghbs.y);

            c1.neighbours.Add(c2);
            c2.neighbours.Add(c1);
        }

        stateManager.computeAllBorders();
        continentsManager.computeAllBorders();

        troopsHandler = new TroopsHandler(stateManager);
        troopsHandler.heliPrefab = heliPrefab;
        troopsHandler.tankPrefab = tankPrefab;

        generationDone = true;
    }

    public void generateBuildingsFromData(Bridge[] bridgesData, Vector3[] citiesData)
    {
        stateManager.registerBuildingsFromData(bridgesData, citiesData);
    }

    public void initGrids()
    {
        for (int i = 0; i < 6; ++i)
        {
            grids[i] = new GridMapNode[resolution * resolution];
            texes[i] = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false, false);
            altTexes[i] = new Texture2D(resolution, resolution, TextureFormat.ARGB32, false, false);

            uv3s[i] = new Vector2[resolution * resolution];
        }

        neighboursOffsets = new Vector2Int[4];
        neighboursOffsets[0] = new Vector2Int(0, 1);    //Top
        neighboursOffsets[1] = new Vector2Int(0, -1);   //Bot
        neighboursOffsets[2] = new Vector2Int(1, 0);    //Right
        neighboursOffsets[3] = new Vector2Int(-1, 0);   //Left
    }

    public void initialize(int resolution)
    {
        this.resolution = resolution;

        initGrids();

        stateManager = new StatesManager(grids, this);
        continentsManager = new ContinentsManager(stateManager, this);
        stateManager.continentManager = continentsManager;

        rng = new System.Random();
    }

    public int fillNode(int mapIndex, int x, int y, Vector3 normalVector, bool isWater)
    {
        GridMapNode node = new GridMapNode()
        {
            x = x,
            y = y,
            index = x + y * resolution,
            ownerGrid = (GridFacesEnum)mapIndex,
            normalVector = normalVector,
            playerIndex = isWater ? -1 : 0,
            stateIndex = isWater ? -1 : 0,
            continentIndex = -1
        };

        grids[mapIndex][node.index] = node;

        return node.playerIndex;
    }

    public GridMapNode[] getNeighbours(GridMapNode node)
    {
        GridMapNode[] ns = new GridMapNode[4];

        int addedNs = 0;
        foreach (Vector2Int offset in neighboursOffsets)
        {
            Vector2Int nsPos = new Vector2Int(offset.x + node.x, offset.y + node.y);
            if (isInGrid(nsPos))
            {
                ns[addedNs++] = grids[(int)node.ownerGrid][nsPos.x + nsPos.y * resolution];
            }
            else
            {
                //checkoffset on other grids
                ns[addedNs++] = getCorrespondIngGrid(node, offset);
            }
        }

        if (addedNs != 4)
        {
            Debug.LogError("Couldn't find 4 neighbours, should not happen");
        }

        return ns;
    }


    private void applyStateDataToMeshes(List<State> states)
    {
        foreach(State s in states)
        {
            Debug.Log("Updating " + s);
            foreach(GridMapNode node in s.lands)
            {
                int grid = (int)node.ownerGrid;
                updateNodeToAltTexture(grid, node.index, uv3s[grid]);
            }
        }

        for (int i = 0; i < 6; ++i)
        {
            meshes[i].uv3 = uv3s[i];
            altTexes[i].Apply();
        }
    }

    private void updateNodeToAltTexture(int grid, int index, Vector2[] uv3)
    {
        GridMapNode node = grids[grid][index];

        Color c = getColorFromNode(node);
        bool altColor = getAltColorFromNode(node, out Color alt);

        uv3[index] = new Vector2(uv3[index].x, altColor? 1 : 0);
        altTexes[grid].SetPixel(node.x, node.y, altColor ? alt : c);
    }


    private void applyNodeToMeshes(int grid, int index, Vector2[] uv3)
    {
        GridMapNode node = grids[grid][index];

        Color c = getColorFromNode(node);

        uv3[index] = new Vector2(uv3[index].x, 0);
        texes[grid].SetPixel(node.x, node.y, c);
        altTexes[grid].SetPixel(node.x, node.y, c);
    }

    public float getDistance(GridMapNode n1, GridMapNode n2)
    {
        return Vector3.Angle(n1.normalVector, n2.normalVector);
    }

    public void applyAllDataToMeshes()
    {
        for (int grid = 0; grid < 6; ++grid)
        {
            for(int i = 0; i < resolution * resolution; ++i)
            {
                applyNodeToMeshes(grid, i, uv3s[grid]);
            }

            texes[grid].Apply();
            altTexes[grid].Apply();
            meshes[grid].uv3 = uv3s[grid];
        }
    }

    private Color getColorFromNode(GridMapNode node)
    {
        Color c = getColorFromIndex(node.playerIndex);

        bool tooDark = (c.r + c.g * 1.7f + c.b * 0.3f) < 0.1f;

        if (node.rogueIsland)
        {
            c = new Color(1, 1, 0); //yellow
        }
        else if (node.isContinentBorder)// && !node.isWaterBorder)
        {
            c = new Color(0, 0, 0); //black
        }
        else if (node.isBorder)// && !node.isWaterBorder)
        {
            c = tooDark ? Color.Lerp(c, new Color(1, 1, 1), 0.5f) : Color.Lerp(c, new Color(0, 0, 0), 0.5f);
        }

        return c;
    }

    private bool getAltColorFromNode(GridMapNode node, out Color c)
    {
        c = getColorFromIndex(node.playerIndex);

        if(node.isBorder)
        {
            return false;
        }
        else if (node.selected)
        {
            c = new Color(1, 1, 1);
        }
        else if (node.targetable)
        {
            c = new Color(1, 0, 0);
        }
        else if (node.isFriendly)
        {
            c = new Color(0, 1, 0);
        }
        else
        {
            return false;
        }

        return true;
    }

    private Color getColorFromIndex(int i)
    {
        if(i==-1)
        {
            return oceanColor;
        }

        return new Color(((3000 * i) % 255)/255f, ((300 * i) % 255)/255f, ((30000 * i) % 255)/255f);
    }

    /*** big chonky code ***/
    //stitching all the sphere's faces together, there must be a better way to do this
    private GridMapNode getCorrespondIngGrid(GridMapNode startNode, Vector2Int offset)
    {
        GridFacesEnum startGrid = startNode.ownerGrid;

        int correspondingGrid = -1;
        int nghbY = -1;
        int nghbX = -1;

        switch(startGrid)
        {
            case GridFacesEnum.Top:
                //Debug.Log("Top");
                if (offset.Equals(new Vector2Int(0, 1)))//Top
                {
                    correspondingGrid = (int)GridFacesEnum.Back;
                    nghbY = resolution - 1 - startNode.x;
                    nghbX = 0;
                }
                else if (offset.Equals(new Vector2Int(0, -1)))//Bot
                {
                    correspondingGrid = (int)GridFacesEnum.Front;
                    nghbY = resolution - 1 - startNode.x;
                    nghbX = resolution - 1;
                }
                else if (offset.Equals(new Vector2Int(1, 0)))//Right
                {
                    correspondingGrid = (int)GridFacesEnum.Right;
                    nghbY = 0;
                    nghbX = resolution - 1 - startNode.y;
                }
                else if (offset.Equals(new Vector2Int(-1, 0)))//Left
                {
                    correspondingGrid = (int)GridFacesEnum.Left;
                    nghbY = 0;
                    nghbX = startNode.y;
                }
                break;

            case GridFacesEnum.Front:
                //Debug.Log("Front");
                if (offset.Equals(new Vector2Int(0, 1)))//Top
                {
                    correspondingGrid = (int)GridFacesEnum.Left;
                    nghbY = resolution - 1 - startNode.x;
                    nghbX = 0;
                }
                else if (offset.Equals(new Vector2Int(0, -1)))//Bot
                {
                    correspondingGrid = (int)GridFacesEnum.Right;
                    nghbY = resolution - 1 - startNode.x;
                    nghbX = resolution - 1;
                }
                else if (offset.Equals(new Vector2Int(1, 0)))//Right
                {
                    correspondingGrid = (int)GridFacesEnum.Top;
                    nghbY = 0;
                    nghbX = resolution - 1 - startNode.y;
                }
                else if (offset.Equals(new Vector2Int(-1, 0)))//Left
                {
                    correspondingGrid = (int)GridFacesEnum.Bot;
                    nghbY = 0;
                    nghbX = startNode.y;
                }
                break;

            case GridFacesEnum.Right:
                //Debug.Log("Right");
                if (offset.Equals(new Vector2Int(0, 1)))//Top
                {
                    correspondingGrid = (int)GridFacesEnum.Bot;
                    nghbY = resolution - 1 - startNode.x;
                    nghbX = 0;
                }
                else if (offset.Equals(new Vector2Int(0, -1)))//Bot
                {
                    correspondingGrid = (int)GridFacesEnum.Top;
                    nghbY = resolution - 1 - startNode.x;
                    nghbX = resolution - 1;
                }
                else if (offset.Equals(new Vector2Int(1, 0)))//Right
                {
                    correspondingGrid = (int)GridFacesEnum.Front;
                    nghbY = 0;
                    nghbX = resolution - 1 - startNode.y;
                }
                else if (offset.Equals(new Vector2Int(-1, 0)))//Left
                {
                    correspondingGrid = (int)GridFacesEnum.Back;
                    nghbY = 0;
                    nghbX = startNode.y;
                }
                break;

            case GridFacesEnum.Back:
                //Debug.Log("Back");
                if (offset.Equals(new Vector2Int(0, 1)))//Top
                {
                    correspondingGrid = (int)GridFacesEnum.Left;
                    nghbY = startNode.x;
                    nghbX = resolution - 1;
                }
                else if (offset.Equals(new Vector2Int(0, -1)))//Bot
                {
                    correspondingGrid = (int)GridFacesEnum.Right;
                    nghbY = startNode.x;
                    nghbX = 0;
                }
                else if (offset.Equals(new Vector2Int(1, 0)))//Right
                {
                    correspondingGrid = (int)GridFacesEnum.Bot;
                    nghbY = resolution - 1;
                    nghbX = startNode.y;
                }
                else if (offset.Equals(new Vector2Int(-1, 0)))//Left
                {
                    correspondingGrid = (int)GridFacesEnum.Top;
                    nghbY = resolution - 1;
                    nghbX = resolution - 1 - startNode.y;
                }
                break;

            case GridFacesEnum.Left:
                //Debug.Log("Left");
                if (offset.Equals(new Vector2Int(0, 1)))//Top
                {
                    correspondingGrid = (int)GridFacesEnum.Bot;
                    nghbY = startNode.x;
                    nghbX = resolution - 1;
                }
                else if (offset.Equals(new Vector2Int(0, -1)))//Bot
                {
                    correspondingGrid = (int)GridFacesEnum.Top;
                    nghbY = startNode.x;
                    nghbX = 0;
                }
                else if (offset.Equals(new Vector2Int(1, 0)))//Right
                {
                    correspondingGrid = (int)GridFacesEnum.Back;
                    nghbY = resolution - 1;
                    nghbX = startNode.y;
                }
                else if (offset.Equals(new Vector2Int(-1, 0)))//Left
                {
                    correspondingGrid = (int)GridFacesEnum.Front;
                    nghbY = resolution - 1;
                    nghbX = resolution - 1 - startNode.y;
                }
                break;

            case GridFacesEnum.Bot:
                //Debug.Log("Bot");
                if (offset.Equals(new Vector2Int(0, 1)))//Top
                {
                    correspondingGrid = (int)GridFacesEnum.Back;
                    nghbX = resolution - 1;
                    nghbY = startNode.x;
                }
                else if (offset.Equals(new Vector2Int(0, -1)))//Bot
                {
                    correspondingGrid = (int)GridFacesEnum.Front;
                    nghbX = 0;
                    nghbY = startNode.x;
                }
                else if (offset.Equals(new Vector2Int(1, 0)))//Right
                {
                    correspondingGrid = (int)GridFacesEnum.Left;
                    nghbX = startNode.y;
                    nghbY = resolution - 1;
                }
                else if (offset.Equals(new Vector2Int(-1, 0)))//Left
                {
                    correspondingGrid = (int)GridFacesEnum.Right;
                    nghbX = resolution - 1 - startNode.y;
                    nghbY = resolution - 1;
                }
                break;
        }

        if(correspondingGrid == -1 || nghbX == -1 || nghbY == -1)
        {
            Debug.LogWarning("Ooops in sphere stitching : " + correspondingGrid + " " + nghbX + ", " + nghbY);
        }
        else
        {
            //Debug.Log(((int)startGrid) + " " + startNode.x + ", " + startNode.y + " + " + offset + " -> " + correspondingGrid + " " + nghbX + ", " + nghbY);
        }
        
        return grids[correspondingGrid][nghbX + nghbY * resolution];
    }

    private bool isInGrid(Vector2Int point)
    {
        return point.x >= 0
            && point.y >= 0
            && point.x < resolution
            && point.y < resolution;
    }

    public List<GridMapNode> getStatesCapitals()
    {
        return stateManager.getStatesCapitals();
    }

    public List<Bridge> getBridges()
    {
        return stateManager.bridges;
    }

    public BoardManager getBoardManager()
    {
        return new BoardManager(stateManager.states, continentsManager.continents);
    }

    /**** BOARD MANAGING ****/
    public void changeTroops(int stateIndex, int newTroops)
    {
        troopsHandler.changeTroops(stateIndex, newTroops);
    }

    public void changeStateOwner(int stateIndex, int newOwnerIndex)
    {
        State s = stateManager.getStateByIndex(stateIndex);
        s.playerIndex = newOwnerIndex;

        Debug.Log("Changing state " + stateIndex + " to player " + newOwnerIndex);

        foreach (GridMapNode n in s.lands)
        {
            n.playerIndex = newOwnerIndex;
        }

        foreach(List<UnitManager> units in s.spawnedTroopsByValue.Values)
        {
            foreach(UnitManager u in units)
            {
                u.setColor(newOwnerIndex);
            }
        }

        s.dataUpdated = true;

        State lastSelected = stateManager.getSelectedState();
        if(lastSelected != null)
        {
            stateManager.selectState(lastSelected);
        }

        updateChangedStates();
    }
}

public class GridMapNode
{
    public GridFacesEnum ownerGrid;

    public int x;
    public int y;
    public int index;
    public int playerIndex;
    public int stateIndex;
    public int continentIndex;

    public Vector3 normalVector;

    public bool isBorder = false;
    public bool isContinentBorder = false;
    public bool isWaterBorder = false;
    public bool selected = false;
    public bool targetable = false;
    public bool rogueIsland = false;
    public bool isFriendly = false;
}

public enum GridFacesEnum { Top, Front, Right, Back, Left, Bot}
