using System;
using System.Collections.Generic;
using UnityEngine;
using Mirror;


[System.Serializable]
public class NoiseSettings
{
    public float radius = 1;
    public float speed = 1;
    public float strength = .1f;
    public float amplitudePersistence = 0.5f;
    public float speedIncrease = 1.5f;
    [Range(1, 10)]
    public int layers = 1;
    public Vector3 offset = new Vector3(0, 0, 0);
    public float timeFactor = 0.1f;

    public AnimationCurve heightRepartition;
}

public class Planet : NetworkBehaviour
{
    [Range(2,256)]
    public int resolution = 20;

    [SerializeField, HideInInspector]
    private MeshFilter[] landFilters;
    private MeshRenderer[] landRenderers;
    private PlanetFace[] landFaces;

    private TerrainGenerator generator;

    public Material landMaterial;

    public NoiseSettings landSettings;

    [Range(2, 64)]
    public int texResolution = 8;
    public Gradient elevationColors;
    public Color oceanShallowColor;
    public Color oceanDeepColor;
    public Color atmosphereColor;
    public Texture2D tex;

    public bool generate = false;
    public bool generateAndChangeSeed = false;

    public AtmosphereBehaviour atmosphere;

    [Range(0, 2)]
    public float seaLevel = 1.2f;

    public bool generated = false;

    private int seed = 0;

    public Transform landHolder;

    public GridMapManager mapManager = new GridMapManager();

    public GameObject cityPrefab;
    public Transform citiesHolder;

    public BridgeBuilder bridgeBuilder;

    private Dictionary<int, UnitManager> citiesByStates = new Dictionary<int, UnitManager>();

    private float[] allAltitudes;

    public GameObject tankPrefab;
    public GameObject heliPrefab;

    public float getProgressStatus()
    {
        return mapManager.progressBarLevel;
    }

    public string getProgressText()
    {
        return mapManager.taskDescription;
    }

    private void Start()
    {
        seed = System.DateTime.Now.Millisecond;

        mapManager.oceanColor = oceanShallowColor;
        mapManager.initialize(resolution);
        mapManager.tankPrefab = tankPrefab;
        mapManager.heliPrefab = heliPrefab;

        generated = true;
    }

    /*
    private void Update()
    {
        if(generate || generateAndChangeSeed)
        {
            if (generateAndChangeSeed) seed = System.DateTime.Now.Millisecond;

             generated = false;

            generatePlanet();
            generate = false;
            generateAndChangeSeed = false;

            generated = true;
        }

        //landMaterial.SetFloat("_SeaLevel", seaLevel);
    }
    */

    public void sendDataToClients(List<NetworkConnection> users)
    {
        foreach(NetworkConnection user in users)
        {
            sendDataToClient(user);
        }
    }

    public void sendDataToClient(NetworkConnection user)
    {
        float[] allAltitudes = mapManager.generateAllAltitudeData();

        int[] stateIndecies;
        int[] continentIndecies;
        Vector3[] normals;
        mapManager.generateAuthoringData(out stateIndecies, out continentIndecies, out normals);

        Vector2Int[] stateNeighbours;
        Vector2Int[] continentsNeighbours;
        mapManager.generateNeighboursAuthoringData(out stateNeighbours, out continentsNeighbours);

        Bridge[] bridgesData;
        Vector3[] citiesData;
        mapManager.generateBuildingsAuthoringData(out bridgesData, out citiesData);

        TargetInitializeGrids(user, resolution);

        int dataSize = 1000;
        int numberOfPacks = (stateIndecies.Length / dataSize) + 1;
        for(int i = 0; i < numberOfPacks; ++i)
        {
            int startingIndex = i * dataSize;
            int[] partialStateIndecies = MyUtils.getPartialArray(startingIndex, dataSize, stateIndecies);
            int[] partialContinentIndecies = MyUtils.getPartialArray(startingIndex, dataSize, continentIndecies);
            Vector3[] partialNormals = MyUtils.getPartialArray(startingIndex, dataSize, normals);
            TargetFillMapWithPartialData(user, resolution, startingIndex, partialStateIndecies, partialContinentIndecies, partialNormals);

            float[] partialAltitudes = MyUtils.getPartialArray(startingIndex, dataSize, allAltitudes);
            TargetFillAltitudesWithPartialData(user, startingIndex, partialAltitudes);
        }

        TargetGeneratePlanetFromData(user, resolution, stateNeighbours, continentsNeighbours, bridgesData, citiesData);
    }

    [TargetRpc]
    public void TargetInitializeGrids(NetworkConnection user, int resolution)
    {
        this.resolution = resolution;
        init();
        mapManager.initialize(resolution);
        mapManager.heliPrefab = heliPrefab;
        mapManager.tankPrefab = tankPrefab;
    }

    [TargetRpc]
    public void TargetFillMapWithPartialData(NetworkConnection user, int resolution, int startingIndex, int[] stateIndecies, int[] continentIndecies, Vector3[] normals)
    {
        //Debug.Log("Generating grid from " + startingIndex + " to " + (startingIndex + stateIndecies.Length));
        mapManager.generateGridMapFromPartialData(resolution, startingIndex, stateIndecies, continentIndecies, normals);
    }

    [TargetRpc]
    public void TargetFillAltitudesWithPartialData(NetworkConnection user, int startingIndex, float[] altitudes)
    {
        //Debug.Log("Altitude chunk " + startingIndex + " to " + (startingIndex + altitudes.Length));
        registerAltitudesFromData(startingIndex, altitudes);
    }

    [TargetRpc]
    public void TargetGeneratePlanetFromData(NetworkConnection user, int resolution, Vector2Int[] stateNeighbours, Vector2Int[] continentsNeighbours, Bridge[] bridgesData, Vector3[] citiesData)
    {
        Debug.Log("Exploiting Data");
        generateMeshesFromData(this.allAltitudes);
        generateColors();
        mapManager.generateNeighbourDataFromArrays(stateNeighbours, continentsNeighbours);
        mapManager.generateBuildingsFromData(bridgesData, citiesData);
        mapManager.applyAllDataToMeshes();
        instantiateBuildings();
        Debug.Log("Finished creating world rpc");
    }

    private void registerAltitudesFromData(int startingIndex, float[] altitudes)
    {
        if(this.allAltitudes == null)
        {
            this.allAltitudes = new float[resolution * resolution * 6];
            //Debug.Log("Creating array");
        }

        for (int i = 0; i < altitudes.Length; ++i)
        {
            this.allAltitudes[startingIndex + i] = altitudes[i];
            //Debug.Log(i + "/" + (startingIndex + i) + "a:" + altitudes[i]);
        }
    }

    void generateMeshesFromData(float[] allAltitudes)
    {
        for (int i = 0; i < landFaces.Length; ++i)
        {
            Mesh m = landFaces[i].constructMeshFromArray(allAltitudes, i * resolution * resolution);
            mapManager.registerMesh(m, i);
            landRenderers[i].material.SetFloat("_SeaLevel", seaLevel);
            landRenderers[i].material.SetTexture("_StatesTexture", mapManager.getTextureOfGrid(i));
            landRenderers[i].material.SetTexture("_AltStatesTexture", mapManager.getAltTextureOfGrid(i));
            landRenderers[i].material.SetVector("_ElevationMinMax", new Vector4(generator.elevationMinMax.min, generator.elevationMinMax.max));
        }

        //landMaterial.SetVector("_ElevationMinMax", new Vector4(generator.elevationMinMax.min, generator.elevationMinMax.max));
        //landMaterial.SetVector("_SteepnessMinMax", new Vector4(minMaxSteepness.min, minMaxSteepness.max));
    }

    public void generatePlanet()
    {
        init();
        mapManager.initialize(resolution);
        generateMeshes();
        generateColors();
        //generateGridMap();
        //instantiateBuildings();

        //generate = false;
    }

    public void generateGridMap()
    {
        mapManager.generateStates();
    }

    //private void OnValidate()
    //{
    //   generatePlanet();
    //}

    void init()
    {        
        generator = new TerrainGenerator(landSettings, seed);//radius, layers, speed, strength, amplitudePersistence, speedIncrease, min, offset);

        //seaLevel = Mathf.Lerp(0.98f, 1.02f, Random.value);

        if (landFilters == null || landFilters.Length != 6)
            landFilters = new MeshFilter[6];
        if (landRenderers == null || landRenderers.Length != 6)
            landRenderers = new MeshRenderer[6];
        if (landFaces == null || landFaces.Length != 6)
            landFaces = new PlanetFace[6];

        Vector3[] directions = { Vector3.up, Vector3.forward, Vector3.right, Vector3.back, Vector3.left, Vector3.down };

        for(int i = 0; i < 6; ++i)
        {
            if(landFilters[i] == null)
            {
                GameObject meshObject = new GameObject("landMesh");
                meshObject.transform.parent = landHolder;

                MeshRenderer mr = meshObject.AddComponent<MeshRenderer>();
                landFilters[i] = meshObject.AddComponent<MeshFilter>();
                landFilters[i].mesh = new Mesh();
                landRenderers[i] = mr;
                mr.material = landMaterial;
            }

            landFaces[i] = new PlanetFace(generator, resolution, landFilters[i].mesh, directions[i], seaLevel);
        }
    }

    void generateMeshes()
    {
        MinMax minMaxSteepness = new MinMax();

        for(int i = 0; i < landFaces.Length; ++i)
        {
            float[] elevations = landFaces[i].constructMesh(minMaxSteepness, mapManager, i);
            landRenderers[i].material.SetFloat("_SeaLevel", seaLevel);
            landRenderers[i].material.SetTexture("_StatesTexture", mapManager.getTextureOfGrid(i));
            landRenderers[i].material.SetVector("_ElevationMinMax", new Vector4(generator.elevationMinMax.min, generator.elevationMinMax.max));
            landRenderers[i].material.SetVector("_SteepnessMinMax", new Vector4(minMaxSteepness.min, minMaxSteepness.max));
        }

        //landMaterial.SetVector("_ElevationMinMax", new Vector4(generator.elevationMinMax.min, generator.elevationMinMax.max));
        //landMaterial.SetVector("_SteepnessMinMax", new Vector4(minMaxSteepness.min, minMaxSteepness.max));
    }


    void generateColors()
    {
        if (tex == null)
        {
            tex = new Texture2D(texResolution, 2, TextureFormat.ARGB32, false);
        }

        Color[] colors = new Color[texResolution];
        Color[] waterColors = new Color[texResolution];

        Gradient selectedOcean = new Gradient();
        GradientColorKey[] oceanKeys = new GradientColorKey[2];
        GradientAlphaKey[] alphaKeys = new GradientAlphaKey[2];

        oceanKeys[0].color = oceanDeepColor;//Color.Lerp(oceanBaseColor, Color.blue, 0.8f);
        oceanKeys[0].time = 0;
        alphaKeys[0].alpha = 1;
        alphaKeys[0].time = 0;        

        oceanKeys[1].color = oceanShallowColor;
        oceanKeys[1].time = 1f;
        alphaKeys[1].alpha = 1;
        alphaKeys[1].time = 1f;

        selectedOcean.SetKeys(oceanKeys, alphaKeys);

        for (int i = 0; i < texResolution; ++i)
        {
            colors[i] = elevationColors.Evaluate(i / (texResolution - 1f));
            waterColors[i] = selectedOcean.Evaluate(i / (texResolution - 1f));
        }
        tex.SetPixels(0, 0, texResolution, 1, colors);
        tex.SetPixels(0, 1, texResolution, 1, waterColors);
        tex.Apply();

        for(int i = 0; i < 6; ++i)
        {
            landRenderers[i].material.SetTexture("_PlanetTexture", tex);
            landRenderers[i].material.SetFloat("_SeaLevel", seaLevel);
        }
        //landMaterial.SetTexture("_PlanetTexture", tex);
        //landMaterial.SetFloat("_SeaLevel", seaLevel);

        //oceanMaterial.SetTexture("_PlanetTexture", tex);

        //oceanMaterial.SetFloat("_SeaLevel", seaLevel);

        atmosphere.setColorAndRandomize(atmosphereColor);
    }

    public Vector3[] getRandomPoints()
    {
        Vector3[] ps = new Vector3[landFaces.Length];

        for(int i = 0; i < landFaces.Length; ++i)
        {
            ps[i] = landFaces[i].getRandomOnLandPoint();

            //Debug.Log(ps[i]);
        }

        return ps;
    }

    public bool tryGetBothStates(Vector3 clicOnSphere, out State selected, out State clicked)
    {
        return mapManager.tryGetBothStates(clicOnSphere, out selected, out clicked);
    }

    public void registerClic(Vector3 normalizedClic)
    {
        mapManager.selectStateAt(normalizedClic);
    }

    public void instantiateBuildings()
    {
        //Cities
        List<GridMapNode> citiesNormals = mapManager.getStatesCapitals();

        foreach(GridMapNode capital in citiesNormals)
        {
            UnitManager city = Instantiate(cityPrefab, capital.normalVector * 10, Quaternion.LookRotation(capital.normalVector), citiesHolder).GetComponent<UnitManager>();
            citiesByStates[capital.stateIndex] = city;
        }

        Debug.Log("spawned " + citiesNormals.Count + " cities");

        //Bridges
        List<Bridge> bridges = mapManager.getBridges();

        foreach (Bridge b in bridges)
        {
            bridgeBuilder.buildAndSpawnBridge(b);
        }

        Debug.Log("spawned " + bridges.Count + " bridges");
    }
    
    public BoardManager constructABoardManager()
    {
        return mapManager.getBoardManager();
    }

    /**** BOARD MANAGING ****/
    public void changeTroops(int stateIndex, int newTroops)
    {
        mapManager.changeTroops(stateIndex, newTroops);
    }

    public void changeStateOwner(int stateIndex, int newOwnerIndex)
    {
        mapManager.changeStateOwner(stateIndex, newOwnerIndex);
        citiesByStates[stateIndex].setColor(newOwnerIndex);
    }
}
