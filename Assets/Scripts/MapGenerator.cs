using System.Collections;
using System.Collections.Generic;
using System.IO;
using System;
//using System;
using UnityEngine;
using UnityEngine.Tilemaps;


public class MapGenerator : MonoBehaviour //SingletonBehaviour<PreviewGrid>
{

    // tilemap for grass, sand, roads, paths, etc.
    public Tilemap tileMapGround;
    // tilemap for buildings, trees, collidable objects
    public Tilemap tileMapCollision;
    // tilemap for extra objects such as a door on a building
    public Tilemap tileMapAesthetic;

    public Transform objPlayer;
    //public Transform objQueen;

    private int castleX;
    private int castleY;

    public Vector2 getCastlePos()
    {
        return new Vector2(castleX, castleY);
    }

    //No of path points
    public int randomPoints;


    private const int borderThickness = 15;

    public int gridSize = 500;
    public int numNPCs = 20;
    public int numLives = 20;

    //Prefab instances;
    private GameObject[] characterInstances;
    private GameObject[] livesInstances;
    private GameObject shieldInstance;
    private GameObject swordInstance;
    private GameObject axeInstance;
    private GameObject woodenSwordInstance;
    private GameObject goldenSwordInstance;
    private GameObject bootInstance;
    private GameObject helmetInstance;
    private GameObject knifeInstance;


    // list of available tiles
    private Tile[] tileList;

    private enum Terrain : int
    {
        WATER = 0,
        GRASS = 1,
        TREE_ON_GRASS = 2,
        CLIFF = 3,
        BRIDGE = 4,
        CASTLE_WALL = 5,
        CASTLE_FLOOR = 6,
        CASTLE_GATE_BOTTOM,
        CASTLE_GATE_BOTTOM_LEFT,
        CASTLE_GATE_BOTTOM_RIGHT,
        CASTLE_GATE_MIDDLE,
        CASTLE_GATE_MIDDLE_LEFT,
        CASTLE_GATE_MIDDLE_RIGHT,
        CASTLE_GATE_MIDDLE_UPPER,
        CASTLE_GATE_MIDDLE_UPPER_LEFT,
        CASTLE_GATE_MIDDLE_UPPER_RIGHT,
        CASTLE_WALL_BOTTOM,
        CASTLE_WALL_BOTTOM_LEFT,
        CASTLE_WALL_BOTTOM_RIGHT,
        CASTLE_WALL_MIDDLE,
        CASTLE_WALL_MIDDLE_LEFT,
        CASTLE_WALL_MIDDLE_RIGHT,
        CASTLE_WALL_MIDDLE_UPPER_LEFT,
        CASTLE_WALL_MIDDLE_UPPER_RIGHT,
        CASTLE_WALL_TOP,
        CASTLE_WALL_TOP_LEFT,
        CASTLE_WALL_TOP_RIGHT,
        CASTLE_ROOF_BOTTOM,
        CASTLE_ROOF_BOTTOM_LEFT,
        CASTLE_ROOF_BOTTOM_RIGHT,
        CASTLE_ROOF_MIDDLE,
        CASTLE_ROOF_MIDDLE_LEFT,
        CASTLE_ROOF_MIDDLE_RIGHT,
        CASTLE_ROOF_TOP,
        CASTLE_ROOF_TOP_LEFT,
        CASTLE_ROOF_TOP_RIGHT,
        TWISTED_TREE_BOTTOM,
        TWISTED_TREE_MIDDLE,
        TWISTED_TREE_TOP,
        HOUSE_BOTTOM_LEFT,
        HOUSE_BOTTOM_RIGHT,
        HOUSE_MIDDLE_LEFT,
        HOUSE_MIDDLE_RIGHT,
        HOUSE_TOP_LEFT,
        HOUSE_TOP_RIGHT
    }

    private Terrain[,] map;

    private readonly ISet<Terrain> WALKABLE = new HashSet<Terrain> { Terrain.GRASS, Terrain.BRIDGE };

    /*
    Start function is called only once at game initialization.
    */
    public void Awake()
    {
        characterInstances = new GameObject[numNPCs + 1];
        livesInstances = new GameObject[numLives];

        //Debug.Log("Start locating players....");
        //locatePlayers();
        //Debug.Log("Finish locating players....");

        Debug.Log("Start building Tile List....");
        buildTileList();
        Debug.Log("Finish building Tile List....");

        Debug.Log("Start building the map....");
        buildMap();
        Debug.Log("Finish building the map....");

        Debug.Log("Start map rendering....");
        buildTileMap();
        Debug.Log("Finish map rendering....");

    }

    public void Start() { }

    private void buildMap()
    {
        //getRandomSampleMap();
        Debug.Log("Start building Perlin Noise....");
        int[] terrainType = { 1, 0, 2 };
        double[] probabilities = { 0.4, 0.2, 0.4 }; // must sum to 1, with same size as terrainType.
        //map = getPerlinNoiseMap(terrainType, probabilities, 0);
        //getConwayMap();
        map = layeredPerlin();
        Debug.Log("Finish building Perlin Noise....");

        Debug.Log("Start connecting walkable areas....");
        connectWalkableAreas();
        Debug.Log("Finish connecting walkable areas ....");

        Debug.Log("Place Characters and Castle...");
        placeCharactersAndCastle();
        Debug.Log("Finished placing characters and castle. ");

        //Debug.Log("Start building Paths....");
        //randomPathGenerator();
        //Debug.Log("Finish building Paths....");
    }

    private void connectWalkableAreas()
    {
        // first, identify all separate regions
        int clusterNumber = 0;
        int[,] clusterMap = new int[map.GetLength(0), map.GetLength(1)];

        for (int x = 0; x < map.GetLength(0); x++)
        {
            for (int y = 0; y < map.GetLength(1); y++)
            {
                if (WALKABLE.Contains(map[x, y]))
                {
                    // use the cluster number of the neighbor
                    if (x > 0 && clusterMap[x - 1, y] > 0)
                    {
                        clusterMap[x, y] = clusterMap[x - 1, y];
                    }
                    else if (y > 0 && clusterMap[x, y - 1] > 0)
                    {
                        clusterMap[x, y] = clusterMap[x, y - 1];
                    }
                    // or assign a new number
                    else
                    {
                        clusterMap[x, y] = ++clusterNumber;
                    }
                }
            }
        }

        // DEBUG
        /*
        StreamWriter yourOSW0;
        using (yourOSW0 = new StreamWriter("C:/Users/" + Environment.UserName + "/Desktop/mapDataBefore.txt"))
        {
            // To write array to file
            for (int x = 0; x < clusterMap.GetLength(0); x++)
            {
                string line = "";
                for (int y = 0; y < clusterMap.GetLength(1); y++)
                {
                    line = line + clusterMap[x, y].ToString() + " ";

                }
                yourOSW0.WriteLine(line + Environment.NewLine);
            }
        }
        */

        // second, identify regions that have different numbers, but are in fact the same
        ISet<Tuple<int, int>> theSameRegions = new HashSet<Tuple<int, int>>();
        for (int x = 1; x < clusterMap.GetLength(0); x++)
        {
            for (int y = 1; y < clusterMap.GetLength(1); y++)
            {
                if (clusterMap[x, y] > 0)
                {
                    // check the neighbor above
                    if (clusterMap[x, y - 1] > 0 && clusterMap[x, y - 1] != clusterMap[x, y])
                    {
                        theSameRegions.Add(new Tuple<int, int>(
                            Math.Min(clusterMap[x, y - 1], clusterMap[x, y]),
                            Math.Max(clusterMap[x, y - 1], clusterMap[x, y])
                        ));
                    }
                    // check the neighbor on the left
                    if (clusterMap[x - 1, y] > 0 && clusterMap[x - 1, y] != clusterMap[x, y])
                    {
                        theSameRegions.Add(new Tuple<int, int>(
                            Math.Min(clusterMap[x - 1, y], clusterMap[x, y]),
                            Math.Max(clusterMap[x - 1, y], clusterMap[x, y])
                        ));
                    }
                }
            }
        }

        // third, calculate how the clusters should be renumbered
        int[] translatedClusterNumbers = new int[clusterNumber + 1];
        for (int i = 0; i <= clusterNumber; i++)
        {
            translatedClusterNumbers[i] = i;
        }

        foreach (Tuple<int, int> element1 in theSameRegions)
        {
            int minNumber = element1.Item1;
            bool setOfSameRegionsHasChanged;
            SortedSet<int> sameRegions = new SortedSet<int>() { element1.Item1, element1.Item2 };
            do
            {
                setOfSameRegionsHasChanged = false;
                foreach (Tuple<int, int> element2 in theSameRegions)
                {
                    if (sameRegions.Contains(element2.Item2) && !sameRegions.Contains(element2.Item1) ||
                        sameRegions.Contains(element2.Item1) && !sameRegions.Contains(element2.Item2))
                    {
                        sameRegions.Add(element2.Item1);
                        sameRegions.Add(element2.Item2);
                        setOfSameRegionsHasChanged = true;
                    }
                }
            } while (setOfSameRegionsHasChanged);

            translatedClusterNumbers[element1.Item1] = sameRegions.Min;
            translatedClusterNumbers[element1.Item2] = sameRegions.Min;
        }

        // numbers from 1 to the number of clusters
        SortedSet<int> oldClusterNumberValues = new SortedSet<int>();
        for (int i = 1; i <= clusterNumber; i++)
        {
            oldClusterNumberValues.Add(translatedClusterNumbers[i]);
        }

        int newNumber = 1;
        foreach (int oldNumber in oldClusterNumberValues)
        {
            for (int i = 1; i <= clusterNumber; i++)
            {
                if (translatedClusterNumbers[i] == oldNumber)
                {
                    translatedClusterNumbers[i] = newNumber;
                }
            }
            newNumber++;
        }

        // then, renumber the clusters
        for (int x = 0; x < clusterMap.GetLength(0); x++)
        {
            for (int y = 0; y < clusterMap.GetLength(1); y++)
            {
                clusterMap[x, y] = translatedClusterNumbers[clusterMap[x, y]];
            }
        }

        int numberOfDetectedClusters = newNumber - 1;

        // DEBUG
        /*
        StreamWriter yourOSW;
        using (yourOSW = new StreamWriter("C:/Users/" + Environment.UserName + "/Desktop/mapData.txt"))
        {
            // To write array to file
            for (int x = 0; x < clusterMap.GetLength(0); x++)
            {
                string line = "";
                for (int y = 0; y < clusterMap.GetLength(1); y++)
                {
                    line = line + clusterMap[x, y].ToString() + " ";

                }
                yourOSW.WriteLine(line + Environment.NewLine);
            }
        }*/


        if (numberOfDetectedClusters > 1)
        {
            // collect all edge points of the identified clusters
            List<Tuple<int, int>>[] edgePointsForCluster = new List<Tuple<int, int>>[numberOfDetectedClusters + 1];
            for (int x = 0; x < clusterMap.GetLength(0); x++)
            {
                for (int y = 0; y < clusterMap.GetLength(1); y++)
                {
                    if (clusterMap[x, y] > 0)
                    {
                        if (x > 0 && clusterMap[x - 1, y] == 0 ||
                            y > 0 && clusterMap[x, y - 1] == 0 ||
                            x < clusterMap.GetLength(0) - 1 && clusterMap[x + 1, y] == 0 ||
                            y > clusterMap.GetLength(1) - 1 && clusterMap[x, y + 1] == 0)
                        {
                            if (edgePointsForCluster[clusterMap[x, y]] == null)
                            {
                                edgePointsForCluster[clusterMap[x, y]] = new List<Tuple<int, int>>();
                            }
                            edgePointsForCluster[clusterMap[x, y]].Add(new Tuple<int, int>(x, y));
                        }
                    }
                }
            }

            // calculate distances between clusters
            int[,] distanceMatrix = new int[numberOfDetectedClusters + 1, numberOfDetectedClusters + 1];
            Tuple<int, int>[,] closestPointMatrix = new Tuple<int, int>[numberOfDetectedClusters + 1, numberOfDetectedClusters + 1];
            for (int i = 1; i < distanceMatrix.GetLength(0); i++)
            {
                for (int j = 1; j < i; j++)
                {
                    Tuple<int, int> pointA = edgePointsForCluster[i][0];
                    Tuple<int, int> pointB = edgePointsForCluster[j][0];
                    int minDistance = distanceBetweenPoints(pointA, pointB);

                    foreach (Tuple<int, int> a in edgePointsForCluster[i])
                    {
                        foreach (Tuple<int, int> b in edgePointsForCluster[j])
                        {
                            if (distanceBetweenPoints(a, b) < minDistance)
                            {
                                pointA = a;
                                pointB = b;
                                minDistance = distanceBetweenPoints(a, b);
                            }
                        }
                    }
                    distanceMatrix[i, j] = minDistance;
                    distanceMatrix[j, i] = minDistance;
                    closestPointMatrix[i, j] = pointA;
                    closestPointMatrix[j, i] = pointB;
                }
            }

            // DEBUG
            /*
            StreamWriter yourOSW2;
            using (yourOSW2 = new StreamWriter("C:/Users/" + Environment.UserName + "/Desktop/distance.txt"))
            {
                // To write array to file
                for (int x = 0; x < distanceMatrix.GetLength(0); x++)
                {
                    string line = "";
                    for (int y = 0; y < distanceMatrix.GetLength(1); y++)
                    {
                        line = line + distanceMatrix[x, y].ToString() + " ";

                    }
                    yourOSW2.WriteLine(line + Environment.NewLine);
                }
            }*/

            // finding the closest cluster pairs to build a connected graph
            SortedSet<int> connectedClusters = new SortedSet<int>() { 1 };
            List<Tuple<int, int>> connectionPairs = new List<Tuple<int, int>>();

            for (int clusterToConnect = 2; clusterToConnect <= numberOfDetectedClusters; clusterToConnect++)
            {
                int minDistance = -1;
                int cluster = 0;
                foreach (int clusterConnected in connectedClusters)
                {
                    if (minDistance == -1 || distanceMatrix[clusterToConnect, clusterConnected] < minDistance)
                    {
                        cluster = clusterConnected;
                        minDistance = distanceMatrix[clusterToConnect, clusterConnected];
                    }
                }
                connectedClusters.Add(clusterToConnect);
                connectionPairs.Add(new Tuple<int, int>(clusterToConnect, cluster));
                //Debug.Log("Connecting " + clusterToConnect + " with " + cluster);
            }

            // finally, connect the computed regions
            foreach (Tuple<int, int> connection in connectionPairs)
            {
                // connect the following points:
                Tuple<int, int> p1 = closestPointMatrix[connection.Item1, connection.Item2];
                Tuple<int, int> p2 = closestPointMatrix[connection.Item2, connection.Item1];
                // find factors a and b of a straight line
                bool vertical = (p1.Item1 - p2.Item1 == 0);
                double a = vertical ? 0.0 : (p1.Item2 - p2.Item2) / (p1.Item1 - p2.Item1);
                double b = p1.Item2 - a * p1.Item1;

                //Debug.Log("y = x * " + a + " + " + b + " to connect (" + p1.Item1 + "," + p1.Item2 + ") and (" + p2.Item1 + "," + p2.Item2 + ")");

                int xMin = Math.Max(Math.Min(p1.Item1, p2.Item1) - 1, 0);
                int xMax = Math.Max(p1.Item1, p2.Item1) + 1;
                if (xMax >= map.GetLength(0))
                {
                    xMax = map.GetLength(0) - 2;
                }
                int yMin = Math.Max(Math.Min(p1.Item2, p2.Item2) - 1, 0);
                int yMax = Math.Max(p1.Item2, p2.Item2) + 1;
                if (yMax >= map.GetLength(1))
                {
                    yMax = map.GetLength(1) - 2;
                }

                xMin = Math.Min(xMin, xMax - 1);
                yMin = Math.Min(yMin, yMax - 1);

                for (int x = xMin; x <= xMax; x++)
                {
                    for (int y = yMin; y <= yMax; y++)
                    {
                        if (a == 0.0)
                        {
                            makePointWalkable(x, y);
                        }
                        else if (Math.Round(Math.Abs(y - x * a - b)) <= 3 && (Math.Round(Math.Abs(x - (y - b) / a)) <= 3))
                        {
                            makePointWalkable(x, y);
                        }
                    }
                }

            }

        }

        // DEBUG
        /*
        StreamWriter yourOSWFinal;
        using (yourOSWFinal = new StreamWriter("C:/Users/" + Environment.UserName + "/Desktop/mapDataFinal.txt"))
        {
            // To write array to file
            for (int x = 0; x < map.GetLength(0); x++)
            {
                string line = "";
                for (int y = 0; y < map.GetLength(1); y++)
                {
                    line = line + map[x, y].ToString() + " ";

                }
                yourOSWFinal.WriteLine(line + Environment.NewLine);
            }
        }*/

    }

    private void makePointWalkable(int x, int y)
    {
        if (map[x, y] == Terrain.CLIFF || map[x, y] == Terrain.TREE_ON_GRASS)
        {
            map[x, y] = Terrain.GRASS;
        }
        else if (map[x, y] == Terrain.WATER)
        {
            map[x, y] = Terrain.BRIDGE;
        }
    }

    private int distanceBetweenPoints(Tuple<int, int> a, Tuple<int, int> b)
    {
        return Math.Abs(a.Item1 - b.Item1) + Math.Abs(a.Item2 - b.Item2);
    }

    //private void locatePlayers()
    //{
    //    objPlayer.position = new Vector3(0, 0, 1);
    //    objQueen.position = new Vector3(gridSize - 1, gridSize - 1, 1);
    //}

    /*
    Function getRandomSampleMap is used as a sample for generating a map.
    The elements of the map are placed 100% randomly, no PCG algorithm is
    used for tuning. The purpose is only to be a basis for the PCG.
    */
    private void getRandomSampleMap()
    {

        System.Random rand = new System.Random();
        map = new Terrain[gridSize, gridSize];

        for (int y = 0; y < map.GetLength(1); y++)
        {
            for (int x = 0; x < map.GetLength(0); x++)
            {
                map[x, y] = (Terrain)rand.Next(0, 3);
            }
        }
    }//end getMap

    private Terrain[,] getPerlinNoiseMap(int[] terrainType, double[] dist, float scale, float newNoise)
    {

        System.Random rand = new System.Random();
        Terrain[,] map = new Terrain[gridSize, gridSize];

        // Used since unity's perlin does't have a seed
        if (newNoise < 0.1)
            newNoise = UnityEngine.Random.Range(0.1f, 1000.21f);

        for (int y = 0; y < map.GetLength(1); y++)
        {
            for (int x = 0; x < map.GetLength(0); x++)
            {
                // map[x, y] = rand.Next(0, 3);



                // Scale determines the number of different formations (rivers, mountains)
                // Bigger scale corresponds to less formations
                //float scale = 0.035f;
                float xx = x * scale + newNoise;
                float yy = y * scale + newNoise;


                float perlin_value = Mathf.PerlinNoise(xx, yy);

                // Debug.Log(perlin_value);
                double val = 0;
                for (int i = 0; i < terrainType.GetLength(0); i++)
                {
                    val += dist[i];
                    if (val > perlin_value)
                    {
                        map[x, y] = (Terrain)terrainType[i];
                        break;
                    }
                }
                /*
                if      (perlin_value < 0.4) { map[x, y] = 0; }
                else if (perlin_value < 0.5) { map[x, y] = 1; }
                else if (perlin_value < 0.6) { map[x, y] = 2; }
                else                         { map[x, y] = 2; }
                */

            }
        }
        return map;
    }//end getMap

    private void getConwayMap()
    {
        System.Random rand = new System.Random();
        getRandomSampleMap();
        Terrain[,] tempMap = new Terrain[map.GetLength(0), map.GetLength(1)];
        int num_iterations = 2;
        for (int i = 0; i < num_iterations; i++)
        {
            for (int y = 0; y < map.GetLength(1); y++)
            {
                for (int x = 0; x < map.GetLength(0); x++)
                {
                    tempMap[x, y] = (Terrain)getNextConwayVal(map, x, y);
                }
            }
            map = tempMap;
        }

    }//end getConwayMap

    private int abs(int x)
    {
        if (x < 0)
            return x * (-1);
        return x;
    }

    private int getNextConwayVal(Terrain[,] map, int x, int y)
    {
        int[] counts = new int[3];

        for (int x1 = x - 1; x1 <= x + 1; x1++)
        {
            for (int y1 = y - 1; y1 <= y + 1; y1++)
            {
                if (x1 >= 0 && x1 < gridSize && x1 != x)
                {
                    if (y1 >= 0 && y1 < gridSize && y1 != y)
                    {
                        counts[(int)map[x1, y1]]++;
                    }
                }
            }
        }

        int idx = 0;
        int target = 4;
        int val = abs(counts[0] - target);
        for (int i = 0; i < counts.GetLength(0); i++)
        {
            if (abs(counts[i] - target) < val)
            {
                val = abs(counts[i] - target);
                idx = i;
            }
        }
        return idx;
    }//end method

    /*
    Function buildTileMap is used to convert the integer matrix 'map' into a
    Tilemap that can be used in unity. It clears any existing tiles on the tilemap,
    then uses tileList to get the tile data for each grid-cell.
    */
    public void buildTileMap()
    {
        clearMap();

        for (int y = 0; y < map.GetLength(1); y++)
        { // loop height-wise
            for (int x = 0; x < map.GetLength(0); x++)
            { // loop width-wise
                setTile(map[x, y], x, y, map);
            }
        }
    }//end buildTileMap

    private void setTile(Terrain mapID, int x, int y, Terrain[,] map)
    {
        switch (mapID)
        {
            case Terrain.WATER:
                tileMapCollision.SetTile(new Vector3Int(x, y, 0), tileList[1]);
                break;
            case Terrain.GRASS:
                tileMapGround.SetTile(new Vector3Int(x, y, 0), tileList[0]);
                break;
            case Terrain.TREE_ON_GRASS:
                tileMapGround.SetTile(new Vector3Int(x, y, 0), tileList[0]);
                int treeIndex = getTreeTile(map, x, y);

                tileMapCollision.SetTile(new Vector3Int(x, y, 0), tileList[treeIndex]);
                break;
            case Terrain.CLIFF:
                tileMapCollision.SetTile(new Vector3Int(x, y, 0), tileList[5]);
                break;
            case Terrain.BRIDGE:
                tileMapGround.SetTile(new Vector3Int(x, y, 0), tileList[4]);
                break;
            case Terrain.CASTLE_FLOOR:
                tileMapGround.SetTile(new Vector3Int(x, y, 0), tileList[18]);
                break;
            case Terrain.CASTLE_WALL:
                tileMapCollision.SetTile(new Vector3Int(x, y, 0), tileList[17]);
                break;

            case Terrain.CASTLE_GATE_BOTTOM:
                tileMapGround.SetTile(new Vector3Int(x, y, 0), tileList[18]);
                break;
            case Terrain.CASTLE_GATE_BOTTOM_LEFT:
                tileMapGround.SetTile(new Vector3Int(x, y, 0), tileList[18]);
                tileMapCollision.SetTile(new Vector3Int(x, y, 0), tileList[19]);
                break;
            case Terrain.CASTLE_GATE_BOTTOM_RIGHT:
                tileMapGround.SetTile(new Vector3Int(x, y, 0), tileList[18]);
                tileMapCollision.SetTile(new Vector3Int(x, y, 0), tileList[20]);
                break;
            case Terrain.CASTLE_GATE_MIDDLE_LEFT:
                tileMapGround.SetTile(new Vector3Int(x, y, 0), tileList[18]);
                tileMapCollision.SetTile(new Vector3Int(x, y, 0), tileList[21]);
                break;
            case Terrain.CASTLE_GATE_MIDDLE_RIGHT:
                tileMapGround.SetTile(new Vector3Int(x, y, 0), tileList[18]);
                tileMapCollision.SetTile(new Vector3Int(x, y, 0), tileList[22]);
                break;
            case Terrain.CASTLE_GATE_MIDDLE:
                tileMapGround.SetTile(new Vector3Int(x, y, 0), tileList[18]);
                tileMapAesthetic.SetTile(new Vector3Int(x, y, 0), tileList[23]);
                break;
            case Terrain.CASTLE_GATE_MIDDLE_UPPER_LEFT:
                tileMapAesthetic.SetTile(new Vector3Int(x, y, 0), tileList[24]);
                break;
            case Terrain.CASTLE_GATE_MIDDLE_UPPER_RIGHT:
                tileMapAesthetic.SetTile(new Vector3Int(x, y, 0), tileList[25]);
                break;
            case Terrain.CASTLE_GATE_MIDDLE_UPPER:
                tileMapAesthetic.SetTile(new Vector3Int(x, y, 0), tileList[26]);
                break;

            case Terrain.CASTLE_WALL_BOTTOM:
                tileMapCollision.SetTile(new Vector3Int(x, y, 0), tileList[27]);
                break;
            case Terrain.CASTLE_WALL_BOTTOM_LEFT:
                tileMapGround.SetTile(new Vector3Int(x, y, 0), tileList[0]);
                tileMapCollision.SetTile(new Vector3Int(x, y, 0), tileList[28]);
                break;
            case Terrain.CASTLE_WALL_BOTTOM_RIGHT:
                tileMapGround.SetTile(new Vector3Int(x, y, 0), tileList[0]);
                tileMapCollision.SetTile(new Vector3Int(x, y, 0), tileList[29]);
                break;
            case Terrain.CASTLE_WALL_MIDDLE:
                tileMapCollision.SetTile(new Vector3Int(x, y, 0), tileList[30]);
                tileMapAesthetic.SetTile(new Vector3Int(x, y, 0), tileList[30]);
                break;
            case Terrain.CASTLE_WALL_MIDDLE_LEFT:
                tileMapGround.SetTile(new Vector3Int(x, y, 0), tileList[0]);
                tileMapCollision.SetTile(new Vector3Int(x, y, 0), tileList[31]);
                tileMapAesthetic.SetTile(new Vector3Int(x, y, 0), tileList[31]);
                break;
            case Terrain.CASTLE_WALL_MIDDLE_RIGHT:
                tileMapGround.SetTile(new Vector3Int(x, y, 0), tileList[0]);
                tileMapCollision.SetTile(new Vector3Int(x, y, 0), tileList[32]);
                tileMapAesthetic.SetTile(new Vector3Int(x, y, 0), tileList[32]);
                break;
            case Terrain.CASTLE_WALL_MIDDLE_UPPER_LEFT:
                tileMapGround.SetTile(new Vector3Int(x, y, 0), tileList[0]);
                tileMapCollision.SetTile(new Vector3Int(x, y, 0), tileList[33]);
                break;
            case Terrain.CASTLE_WALL_MIDDLE_UPPER_RIGHT:
                tileMapGround.SetTile(new Vector3Int(x, y, 0), tileList[0]);
                tileMapCollision.SetTile(new Vector3Int(x, y, 0), tileList[34]);
                break;
            case Terrain.CASTLE_WALL_TOP:
                tileMapCollision.SetTile(new Vector3Int(x, y, 0), tileList[36]);
                tileMapAesthetic.SetTile(new Vector3Int(x, y, 0), tileList[36]);
                break;
            case Terrain.CASTLE_WALL_TOP_LEFT:
                tileMapGround.SetTile(new Vector3Int(x, y, 0), tileList[0]);
                tileMapCollision.SetTile(new Vector3Int(x, y, 0), tileList[35]);
                break;
            case Terrain.CASTLE_WALL_TOP_RIGHT:
                tileMapGround.SetTile(new Vector3Int(x, y, 0), tileList[0]);
                tileMapCollision.SetTile(new Vector3Int(x, y, 0), tileList[37]);
                break;

            case Terrain.CASTLE_ROOF_BOTTOM_LEFT:
                tileMapGround.SetTile(new Vector3Int(x, y, 0), tileList[0]);
                tileMapCollision.SetTile(new Vector3Int(x, y, 0), tileList[38]);
                break;
            case Terrain.CASTLE_ROOF_BOTTOM:
                tileMapCollision.SetTile(new Vector3Int(x, y, 0), tileList[39]);
                tileMapAesthetic.SetTile(new Vector3Int(x, y, 0), tileList[39]);
                break;
            case Terrain.CASTLE_ROOF_BOTTOM_RIGHT:
                tileMapGround.SetTile(new Vector3Int(x, y, 0), tileList[0]);
                tileMapCollision.SetTile(new Vector3Int(x, y, 0), tileList[40]);
                break;
            case Terrain.CASTLE_ROOF_MIDDLE_LEFT:
                tileMapGround.SetTile(new Vector3Int(x, y, 0), tileList[0]);
                tileMapCollision.SetTile(new Vector3Int(x, y, 0), tileList[41]);
                break;
            case Terrain.CASTLE_ROOF_MIDDLE:
                tileMapCollision.SetTile(new Vector3Int(x, y, 0), tileList[42]);
                break;
            case Terrain.CASTLE_ROOF_MIDDLE_RIGHT:
                tileMapGround.SetTile(new Vector3Int(x, y, 0), tileList[0]);
                tileMapCollision.SetTile(new Vector3Int(x, y, 0), tileList[43]);
                break;
            case Terrain.CASTLE_ROOF_TOP_LEFT:
                tileMapGround.SetTile(new Vector3Int(x, y, 0), tileList[0]);
                tileMapCollision.SetTile(new Vector3Int(x, y, 0), tileList[44]);
                break;
            case Terrain.CASTLE_ROOF_TOP:
                tileMapGround.SetTile(new Vector3Int(x, y, 0), tileList[0]);
                tileMapCollision.SetTile(new Vector3Int(x, y, 0), tileList[45]);
                break;
            case Terrain.CASTLE_ROOF_TOP_RIGHT:
                tileMapGround.SetTile(new Vector3Int(x, y, 0), tileList[0]);
                tileMapCollision.SetTile(new Vector3Int(x, y, 0), tileList[46]);
                break;
            case Terrain.TWISTED_TREE_BOTTOM:
                tileMapGround.SetTile(new Vector3Int(x, y, 0), tileList[0]);
                tileMapCollision.SetTile(new Vector3Int(x, y, 0), tileList[47]);
                break;
            case Terrain.TWISTED_TREE_MIDDLE:
                tileMapGround.SetTile(new Vector3Int(x, y, 0), tileList[0]);
                tileMapCollision.SetTile(new Vector3Int(x, y, 0), tileList[48]);
                break;
            case Terrain.TWISTED_TREE_TOP:
                tileMapGround.SetTile(new Vector3Int(x, y, 0), tileList[0]);
                tileMapCollision.SetTile(new Vector3Int(x, y, 0), tileList[49]);
                break;
            case Terrain.HOUSE_BOTTOM_LEFT:
                tileMapGround.SetTile(new Vector3Int(x, y, 0), tileList[0]);
                tileMapCollision.SetTile(new Vector3Int(x, y, 0), tileList[50]);
                break;
            case Terrain.HOUSE_BOTTOM_RIGHT:
                tileMapGround.SetTile(new Vector3Int(x, y, 0), tileList[0]);
                tileMapCollision.SetTile(new Vector3Int(x, y, 0), tileList[51]);
                break;
            case Terrain.HOUSE_MIDDLE_LEFT:
                tileMapGround.SetTile(new Vector3Int(x, y, 0), tileList[0]);
                tileMapCollision.SetTile(new Vector3Int(x, y, 0), tileList[52]);
                break;
            case Terrain.HOUSE_MIDDLE_RIGHT:
                tileMapGround.SetTile(new Vector3Int(x, y, 0), tileList[0]);
                tileMapCollision.SetTile(new Vector3Int(x, y, 0), tileList[53]);
                break;
            case Terrain.HOUSE_TOP_LEFT:
                tileMapGround.SetTile(new Vector3Int(x, y, 0), tileList[0]);
                tileMapCollision.SetTile(new Vector3Int(x, y, 0), tileList[54]);
                break;
            case Terrain.HOUSE_TOP_RIGHT:
                tileMapGround.SetTile(new Vector3Int(x, y, 0), tileList[0]);
                tileMapCollision.SetTile(new Vector3Int(x, y, 0), tileList[55]);
                break;

        }
    }//end setTile

    /*
    Function buildTileList: populate the list tileList with Tiles from the
    loaded sprite sheet. The indices of the items in the list are used for
    the map generation for each type of object to be placed. For example,
    the index 0 represents grass. When 0 is used in a cell in the map, the
    Tile within tileList at index 0 will be a grass-type tile.
    */
    private void buildTileList()
    {
        tileList = new Tile[56];

        tileList[0] = Resources.Load<Tile>("TilePalette/Hyptosis/hyptosis_tile-art-batch-1_12"); //grass
        tileList[1] = Resources.Load<Tile>("TilePalette/Hyptosis/Castle2_182"); //water
        tileList[2] = Resources.Load<Tile>("TilePalette/Hyptosis/hyptosis_tile-art-batch-1_494"); //tree
        tileList[3] = Resources.Load<Tile>("TilePalette/Hyptosis/Castle2_175"); // cobblestone
        tileList[4] = Resources.Load<Tile>("TilePalette/Overworld_422"); //bridge
        tileList[5] = Resources.Load<Tile>("TilePalette/Hyptosis/hyptosis_tile-art-batch-1_164"); //cliff
        tileList[6] = Resources.Load<Tile>("TilePalette/Hyptosis/hyptosis_tile-art-batch-1_652"); //tree-stump
        tileList[7] = Resources.Load<Tile>("TilePalette/Hyptosis/hyptosis_tile-art-batch-1_471"); //tree-Top
        tileList[8] = Resources.Load<Tile>("TilePalette/Hyptosis/hyptosis_tile-art-batch-1_569"); //tree-Bottom
        tileList[9] = Resources.Load<Tile>("TilePalette/Hyptosis/hyptosis_tile-art-batch-1_654"); //tree-Left
        tileList[10] = Resources.Load<Tile>("TilePalette/Hyptosis/hyptosis_tile-art-batch-1_521"); //tree-Right
        tileList[11] = Resources.Load<Tile>("TilePalette/Hyptosis/hyptosis_tile-art-batch-1_469"); //tree-o-top-left
        tileList[12] = Resources.Load<Tile>("TilePalette/Hyptosis/hyptosis_tile-art-batch-1_472"); //tree-o-top-right
        tileList[13] = Resources.Load<Tile>("TilePalette/Hyptosis/hyptosis_tile-art-batch-1_568"); //tree-o-bottom-left
        tileList[14] = Resources.Load<Tile>("TilePalette/Hyptosis/hyptosis_tile-art-batch-1_571"); //tree-o-bottom-right
        tileList[15] = Resources.Load<Tile>("TilePalette/Hyptosis/hyptosis_tile-art-batch-1_494"); //tree-i-top-right
        tileList[16] = Resources.Load<Tile>("TilePalette/Hyptosis/hyptosis_tile-art-batch-1_682"); //tree-i-top-left
        tileList[17] = Resources.Load<Tile>("TilePalette/Hyptosis/Castle2_63"); //castle wall_middle
        tileList[18] = Resources.Load<Tile>("TilePalette/Hyptosis/Castle2_173"); //castle floor_middle

        tileList[19] = Resources.Load<Tile>("TilePalette/Hyptosis/Castle2_81"); //castle gate bottom left
        tileList[20] = Resources.Load<Tile>("TilePalette/Hyptosis/Castle2_82"); //castle gate bottom right
        tileList[21] = Resources.Load<Tile>("TilePalette/Hyptosis/Castle2_65"); //castle gate middle left
        tileList[22] = Resources.Load<Tile>("TilePalette/Hyptosis/Castle2_67"); //castle gate middle right
        tileList[23] = Resources.Load<Tile>("TilePalette/Hyptosis/Castle2_66"); //castle gate middle
        tileList[24] = Resources.Load<Tile>("TilePalette/Hyptosis/Castle2_49"); //castle gate middle upper left
        tileList[25] = Resources.Load<Tile>("TilePalette/Hyptosis/Castle2_51"); //castle gate middle upper right
        tileList[26] = Resources.Load<Tile>("TilePalette/Hyptosis/Castle2_50"); //castle gate middle upper

        tileList[27] = Resources.Load<Tile>("TilePalette/Hyptosis/Castle2_99"); //castle wall bottom
        tileList[28] = Resources.Load<Tile>("TilePalette/Hyptosis/Castle2_98"); //castle wall bottom left
        tileList[29] = Resources.Load<Tile>("TilePalette/Hyptosis/Castle2_101"); //castle wall bottom right
        tileList[30] = Resources.Load<Tile>("TilePalette/Hyptosis/Castle2_84"); //castle wall middle
        tileList[31] = Resources.Load<Tile>("TilePalette/Hyptosis/Castle2_83"); //castle wall middle left
        tileList[32] = Resources.Load<Tile>("TilePalette/Hyptosis/Castle2_86"); //castle wall middle right
        tileList[33] = Resources.Load<Tile>("TilePalette/Hyptosis/Castle2_68"); //castle wall middle upper left
        tileList[34] = Resources.Load<Tile>("TilePalette/Hyptosis/Castle2_71"); //castle wall middle upper right
        tileList[35] = Resources.Load<Tile>("TilePalette/Hyptosis/Castle2_52"); //castle wall top left
        tileList[36] = Resources.Load<Tile>("TilePalette/Hyptosis/Castle2_53"); //castle wall top
        tileList[37] = Resources.Load<Tile>("TilePalette/Hyptosis/Castle2_55"); //castle wall top right

        tileList[38] = Resources.Load<Tile>("TilePalette/Hyptosis/Castle2_36"); //castle roof bottom left
        tileList[39] = Resources.Load<Tile>("TilePalette/Hyptosis/Castle2_37"); //castle roof bottom
        tileList[40] = Resources.Load<Tile>("TilePalette/Hyptosis/Castle2_39"); //castle roof bottom right
        tileList[41] = Resources.Load<Tile>("TilePalette/Hyptosis/Castle2_20"); //castle roof middle left
        tileList[42] = Resources.Load<Tile>("TilePalette/Hyptosis/Castle2_21"); //castle roof middle
        tileList[43] = Resources.Load<Tile>("TilePalette/Hyptosis/Castle2_23"); //castle roof middle right
        tileList[44] = Resources.Load<Tile>("TilePalette/Hyptosis/Castle2_5"); //castle roof top left
        tileList[45] = Resources.Load<Tile>("TilePalette/Hyptosis/Castle2_6"); //castle roof top
        tileList[46] = Resources.Load<Tile>("TilePalette/Hyptosis/Castle2_8"); //castle roof top right

        tileList[47] = Resources.Load<Tile>("TilePalette/Hyptosis/Castle2_210"); //twisted tree bottom
        tileList[48] = Resources.Load<Tile>("TilePalette/Hyptosis/Castle2_196"); //twisted tree middle
        tileList[49] = Resources.Load<Tile>("TilePalette/Hyptosis/Castle2_180"); //twisted tree top

        tileList[50] = Resources.Load<Tile>("TilePalette/Overworld_286"); //house bottom left
        tileList[51] = Resources.Load<Tile>("TilePalette/Overworld_287"); //house bottom right
        tileList[52] = Resources.Load<Tile>("TilePalette/Overworld_246"); //house middle left
        tileList[53] = Resources.Load<Tile>("TilePalette/Overworld_247"); //house middle right
        tileList[54] = Resources.Load<Tile>("TilePalette/Overworld_207"); //house top left
        tileList[55] = Resources.Load<Tile>("TilePalette/Overworld_208"); //house top right


    }//end buildTileList

    private int getTreeTile(Terrain[,] map, int x, int y)
    {
        bool top = false;
        bool bottom = false;
        bool left = false;
        bool right = false;

        if (y > 0 && map[x, y - 1] != Terrain.TREE_ON_GRASS)
            bottom = true;
        if (y < gridSize - 1 && map[x, y + 1] != Terrain.TREE_ON_GRASS)
            top = true;
        if (x > 0 && map[x - 1, y] != Terrain.TREE_ON_GRASS)
            left = true;
        if (x < gridSize - 1 && map[x + 1, y] != Terrain.TREE_ON_GRASS)
            right = true;
        if (top)
        {
            if (left)
                return 11;
            if (right)
                return 12;
            return 7;
        }
        if (bottom)
        {
            if (left)
                return 13;
            if (right)
                return 14;
            return 8;
        }
        if (right)
            return 10;
        if (left)
            return 9;

        return 2;
    }//end method

    public void clearMap()
    {
        tileMapGround.ClearAllTiles();
        tileMapCollision.ClearAllTiles();

    }//end clearMap

    //public void randomPathGenerator()
    //{
    //    Vector2 endVec = new Vector2(objQueen.position.x, objQueen.position.y);
    //    randomPath(endVec);
    //    endVec = new Vector2((int)UnityEngine.Random.Range(0f, (float)gridSize / 2), gridSize - 1);
    //    randomPath(endVec);
    //    endVec = new Vector2(gridSize - 1, (int)UnityEngine.Random.Range(0f, (float)gridSize / 2));
    //    randomPath(endVec);
    //}



    public void randomPath(Vector2 end)
    {
        if (randomPoints == 0) randomPoints = 1;
        Vector2 start = new Vector2(objPlayer.position.x, objPlayer.position.y);

        if (start.x < 0) { start = new Vector2(0, start.y); }
        if (start.y < 0) { start = new Vector2(start.x, 0); }
        if (end.x < 0) { end = new Vector2(0, end.y); }
        if (end.y < 0) { end = new Vector2(end.x, 0); }

        List<Vector2> randomPoint = new List<Vector2>();

        randomPoint.Add(start);

        for (int i = 0; i < randomPoints; i++)
        {
            randomPoint.Add(new Vector2(
                (int)RandomGaussian(0f, (float)gridSize - 1),
                (int)((gridSize - 1) / 2 + UnityEngine.Random.Range(0, (gridSize - 1) / 2))
               )
            );
        }
        randomPoint.Add(end);
        for (int i = 0; i < randomPoint.Count - 1; i++)
        {
            //Debug.Log(randomPoint[i].x + " " + randomPoint[i].y);
            //Debug.Log(randomPoint[i + 1].x + " " + randomPoint[i + 1].y);

            plotLine(
                (int)randomPoint[i].x,
                (int)randomPoint[i].y,
                (int)randomPoint[i + 1].x,
                (int)randomPoint[i + 1].y
            );
        }

    }


    /*
     Create random distributed points to expand the path road, encouraging exploration
     */
    public static float RandomGaussian(float minValue = 0.0f, float maxValue = 1.0f)
    {
        float u, v, S;

        do
        {
            u = 2.0f * UnityEngine.Random.value - 1.0f;
            v = 2.0f * UnityEngine.Random.value - 1.0f;
            S = u * u + v * v;
        }
        while (S >= 1.0f);

        // Standard Normal Distribution
        float std = u * Mathf.Sqrt(-2.0f * Mathf.Log(S) / S);

        // Normal Distribution centered between the min and max value
        // and clamped following the "three-sigma rule"
        float mean = (minValue + maxValue) / 2.0f;
        float sigma = (maxValue - mean) / 3.0f;
        return Mathf.Clamp(std * sigma + mean, minValue, maxValue);
    }

    /* Bresenham's line algorithm.
        Inital Algorithm
        Try with Anit-aliased quadratic Bézier curve
        http://members.chello.at/easyfilter/bresenham.html
     */
    public void plotLine(int x0, int y0, int x1, int y1)
    {
        int dx = Mathf.Abs(x1 - x0), sx = x0 < x1 ? 1 : -1;
        int dy = -Mathf.Abs(y1 - y0), sy = y0 < y1 ? 1 : -1;
        int err = dx + dy, e2; /* error value e_xy */
        for (; ; )
        {  /* loop */
            if (map[x0, y0] == Terrain.GRASS)
            {
                if (x0 > 0 && x0 < gridSize && y0 > 0 && y0 < gridSize) map[x0, y0] = Terrain.CLIFF;
            }
            else
            {
                if (x0 > 0 && x0 < gridSize && y0 > 0 && y0 < gridSize) map[x0, y0] = Terrain.BRIDGE;
            }

            if (x0 == x1 && y0 == y1) break;
            e2 = 2 * err;
            if (e2 >= dy) { err += dy; x0 += sx; } /* e_xy+e_x > 0 */
            if (e2 <= dx) { err += dx; y0 += sy; } /* e_xy+e_y < 0 */
        }
    }

    private void applyElevation(Terrain[,] map)
    {
        int[] vals = { 0, 1 };
        double[] p = { 0.20, 0.80 };
        Terrain[,] elevation = getPerlinNoiseMap(vals, p, 0.035f, 0); //300f
        for (int y = 0; y < map.GetLength(1); y++)
        {
            for (int x = 0; x < map.GetLength(0); x++)
            {
                //if(!hasDifferentNeighbours(elevation, x, y))
                if (elevation[x, y] == 0)
                    map[x, y] = Terrain.CLIFF;
            }
        }
    }//end elevation

    private bool hasDifferentNeighbours(int[,] map, int x, int y)
    {
        int val = map[x, y];
        for (int x1 = x - 1; x1 <= x + 1; x1++)
        {
            for (int y1 = y - 1; y1 <= y + 1; y1++)
            {
                if (x1 >= 0 && x1 < gridSize && x1 != x)
                {
                    if (y1 >= 0 && y1 < gridSize && y1 != y)
                    {
                        if (map[x1, y1] != val)
                            return false;
                    }
                }
            }
        }
        return true;
    }// end hasDifferentNeighbours

    private Terrain[,] layeredPerlin()
    {
        //place trees and grass
        int[] t1 = { 1, 2 };
        double[] p1 = { 0.5, 0.5 };
        Terrain[,] newMap = getPerlinNoiseMap(t1, p1, 0.035f, 0); //100f);

        // place mountains
        applyElevation(newMap);

        // place water
        int[] t2 = { 1, 0, 1 };
        double[] p2 = { 0.47, 0.05, 0.9 };
        Terrain[,] water = getPerlinNoiseMap(t2, p2, 0.015f, 0);// 150f);
        for (int y = 0; y < newMap.GetLength(1); y++)
        {
            for (int x = 0; x < newMap.GetLength(0); x++)
            {
                if (water[x, y] == 0)
                    newMap[x, y] = Terrain.WATER;
            }
        }

        // build exterior wall of trees
        for (int i = 0; i < gridSize; i++)
        {
            for (int j = 0; j < borderThickness; j++)
            {
                List<Tuple<int, int>> positionsToChange = new List<Tuple<int, int>>() {
                    new Tuple<int,int>(i, j),
                    new Tuple<int,int>(i, gridSize - 1 - j),
                    new Tuple<int,int>(j, i),
                    new Tuple<int,int>(gridSize - 1 - j, i),
                };
                foreach (Tuple<int, int> position in positionsToChange)
                {
                    if (WALKABLE.Contains(newMap[position.Item1, position.Item2]))
                    {
                        newMap[position.Item1, position.Item2] = Terrain.TREE_ON_GRASS;
                    }
                }
            }
        }


        return newMap;
    }



    private void placeCharactersAndCastle()
    {
        System.Random rand = new System.Random();

        //place castle
        int castleSize = 11;
        castleX = 0;
        castleY = 0;
        // rendomly select the location closest to the right corner (try 20 times)
        for (int i = 0; i < 20; i++)
        {
            int cx;
            int cy;
            bool isAGoodPlaceForTheCastle;
            do
            {
                isAGoodPlaceForTheCastle = true;
                cx = rand.Next(borderThickness + castleSize / 2, gridSize - 1 - castleSize / 2 - borderThickness);
                cy = rand.Next(borderThickness + castleSize / 2, gridSize - 1 - castleSize / 2 - borderThickness);

                // checking if there is enough space below the castle
                for (int offsetX = -castleSize / 2 - 3; offsetX < castleSize / 2 + 3 && isAGoodPlaceForTheCastle; offsetX++)
                {
                    for (int offsetY = -castleSize / 2 - 3; offsetY < 0 && isAGoodPlaceForTheCastle; offsetY++)
                    {
                        if (!WALKABLE.Contains(map[cx + offsetX, cy + offsetY]))
                        {
                            isAGoodPlaceForTheCastle = false;
                        }
                    }
                }

                // checking if there is enough space around the castle (left, right and above)
                for (int offsetY = 0; offsetY < 8 && isAGoodPlaceForTheCastle; offsetY++)
                {
                    // left
                    if (!WALKABLE.Contains(map[cx - castleSize / 2 - 1, cy + offsetY]))
                    {
                        isAGoodPlaceForTheCastle = false;
                    }

                    // right
                    if (!WALKABLE.Contains(map[cx + castleSize / 2 + 1, cy + offsetY]))
                    {
                        isAGoodPlaceForTheCastle = false;
                    }

                    // top
                    if (offsetY >= castleSize)
                    {
                        for (int offsetX = -castleSize / 2 - 1; offsetX <= castleSize / 2 + 1 && isAGoodPlaceForTheCastle; offsetX++)
                        {
                            if (!WALKABLE.Contains(map[cx + offsetX, cy + offsetY]))
                            {
                                isAGoodPlaceForTheCastle = false;
                            }
                        }
                    }
                }

            } while (!isAGoodPlaceForTheCastle);

            if (cx + cy > castleX + castleY)
            {
                castleX = cx;
                castleY = cy;
            }
        }
        buildCastle(castleX, castleY, castleSize);

        // locate the boss
        GameObject unitPrefabBoss = (GameObject)Resources.Load("Prefabs/Bigboss", typeof(GameObject));
        characterInstances[0] = Instantiate(unitPrefabBoss, new Vector3(castleX, castleY - 1, 1), Quaternion.identity);
        characterInstances[0].name = "Boss";

        // locating the twisted trees
        int numberOfTwistedTrees = 20;
        for (int i = 0; i < numberOfTwistedTrees; i++) {
            bool goodLocationForTheTwistedTree;
            int tx;
            int ty;
            do
            {
                goodLocationForTheTwistedTree = true;
                tx = rand.Next(borderThickness + 2, gridSize - 2 - borderThickness);
                ty = rand.Next(borderThickness + 2, gridSize - 2 - borderThickness);

                for (int h = 0; h < 5 && goodLocationForTheTwistedTree; h++)
                {
                    for (int w = 0; w < 3 && goodLocationForTheTwistedTree; w++)
                    {
                        if (map[tx + w, ty + h] != Terrain.GRASS)
                        {
                            goodLocationForTheTwistedTree = false;
                            break;
                        }
                    }
                }
            } while (!goodLocationForTheTwistedTree);

            // build the twisted tree
            map[tx + 1, ty + 1] = Terrain.TWISTED_TREE_BOTTOM;
            map[tx + 1, ty + 2] = Terrain.TWISTED_TREE_MIDDLE;
            map[tx + 1, ty + 3] = Terrain.TWISTED_TREE_TOP;
        }

        // locating the houses
        int numberOfHouses = 10;
        for (int i = 0; i < numberOfHouses; i++)
        {
            bool goodLocationForTheHouse;
            int tx;
            int ty;
            do
            {
                goodLocationForTheHouse = true;
                tx = rand.Next(borderThickness + 2, gridSize - 2 - borderThickness);
                ty = rand.Next(borderThickness + 2, gridSize - 2 - borderThickness);

                for (int h = 0; h < 5 && goodLocationForTheHouse; h++)
                {
                    for (int w = 0; w < 3 && goodLocationForTheHouse; w++)
                    {
                        if (map[tx + w, ty + h] != Terrain.GRASS)
                        {
                            goodLocationForTheHouse = false;
                            break;
                        }
                    }
                }
            } while (!goodLocationForTheHouse);

            // build the twisted tree
            map[tx + 1, ty + 1] = Terrain.HOUSE_BOTTOM_LEFT;
            map[tx + 2, ty + 1] = Terrain.HOUSE_BOTTOM_RIGHT;
            map[tx + 1, ty + 2] = Terrain.HOUSE_MIDDLE_LEFT;
            map[tx + 2, ty + 2] = Terrain.HOUSE_MIDDLE_RIGHT;
            map[tx + 1, ty + 3] = Terrain.HOUSE_TOP_LEFT;
            map[tx + 2, ty + 3] = Terrain.HOUSE_TOP_RIGHT;
        }

        // position the hearts
        GameObject unitPrefab;
        int nx;
        int ny;
        HashSet<(int, int)> checkDuplicatePositions = new HashSet<(int, int)>();
        for (int i = 0; i < numLives; i++)
        {
            do
            {
                nx = rand.Next(borderThickness, gridSize - 1 - borderThickness);
                ny = rand.Next(borderThickness, gridSize - 1 - borderThickness);
            } while (!checkWalkablePosition(nx, ny) || checkDuplicatePositions.Contains((nx, ny)));
            checkDuplicatePositions.Add((nx, ny));
            unitPrefab = (GameObject)Resources.Load("Prefabs/Lives", typeof(GameObject));
            livesInstances[i] = Instantiate(unitPrefab, new Vector3(nx, ny, 1), Quaternion.identity);
            livesInstances[i].name = "Life_" + i;
        }

        //Place Weapons
        // 1. Sword
        do
        {
            nx = rand.Next(borderThickness, gridSize - 1 - borderThickness);
            ny = rand.Next(borderThickness, gridSize - 1 - borderThickness);
        } while (!checkWalkablePosition(nx, ny) || checkDuplicatePositions.Contains((nx, ny)));
        unitPrefab = (GameObject)Resources.Load("Prefabs/Sword", typeof(GameObject));
        swordInstance = Instantiate(unitPrefab, new Vector3(nx, ny, 1), Quaternion.identity);
        swordInstance.name = "Sword";

        // 2. Shield
        do
        {
            nx = rand.Next(borderThickness, gridSize - 1 - borderThickness);
            ny = rand.Next(borderThickness, gridSize - 1 - borderThickness);
        } while (!checkWalkablePosition(nx, ny) || checkDuplicatePositions.Contains((nx, ny)));
        unitPrefab = (GameObject)Resources.Load("Prefabs/Shield", typeof(GameObject));
        shieldInstance = Instantiate(unitPrefab, new Vector3(nx, ny, 1), Quaternion.identity);
        shieldInstance.name = "Shield";


        // 3. Axe
        do
        {
            nx = rand.Next(borderThickness, gridSize - 1 - borderThickness);
            ny = rand.Next(borderThickness, gridSize - 1 - borderThickness);
        } while (!checkWalkablePosition(nx, ny) || checkDuplicatePositions.Contains((nx, ny)));
        unitPrefab = (GameObject)Resources.Load("Prefabs/Axe", typeof(GameObject));
        axeInstance = Instantiate(unitPrefab, new Vector3(nx, ny, 1), Quaternion.identity);
        axeInstance.name = "Axe";

        // 4. Wooden Sword
        do
        {
            nx = rand.Next(borderThickness, gridSize - 1 - borderThickness);
            ny = rand.Next(borderThickness, gridSize - 1 - borderThickness);
        } while (!checkWalkablePosition(nx, ny) || checkDuplicatePositions.Contains((nx, ny)));
        unitPrefab = (GameObject)Resources.Load("Prefabs/WoodenSword", typeof(GameObject));
        woodenSwordInstance = Instantiate(unitPrefab, new Vector3(nx, ny, 1), Quaternion.identity);
        woodenSwordInstance.name = "WoodenSword";

        // 5. Golden Sword
        do
        {
            nx = rand.Next(borderThickness, gridSize - 1 - borderThickness);
            ny = rand.Next(borderThickness, gridSize - 1 - borderThickness);
        } while (!checkWalkablePosition(nx, ny) || checkDuplicatePositions.Contains((nx, ny)));
        unitPrefab = (GameObject)Resources.Load("Prefabs/GoldenSword", typeof(GameObject));
        goldenSwordInstance = Instantiate(unitPrefab, new Vector3(nx, ny, 1), Quaternion.identity);
        goldenSwordInstance.name = "GoldenSword";
        // 6. Boot
        do
        {
            nx = rand.Next(borderThickness, gridSize - 1 - borderThickness);
            ny = rand.Next(borderThickness, gridSize - 1 - borderThickness);
        } while (!checkWalkablePosition(nx, ny) || checkDuplicatePositions.Contains((nx, ny)));
        unitPrefab = (GameObject)Resources.Load("Prefabs/Boot", typeof(GameObject));
        bootInstance = Instantiate(unitPrefab, new Vector3(nx, ny, 1), Quaternion.identity);
        bootInstance.name = "Boot";

        // 7. Helmet
        do
        {
            nx = rand.Next(borderThickness, gridSize - 1 - borderThickness);
            ny = rand.Next(borderThickness, gridSize - 1 - borderThickness);
        } while (!checkWalkablePosition(nx, ny) || checkDuplicatePositions.Contains((nx, ny)));
        unitPrefab = (GameObject)Resources.Load("Prefabs/Helmet", typeof(GameObject));
        helmetInstance = Instantiate(unitPrefab, new Vector3(nx, ny, 1), Quaternion.identity);
        helmetInstance.name = "Helmet";

        // 8. Knife
        do
        {
            nx = rand.Next(borderThickness, gridSize - 1 - borderThickness);
            ny = rand.Next(borderThickness, gridSize - 1 - borderThickness);
        } while (!checkWalkablePosition(nx, ny) || checkDuplicatePositions.Contains((nx, ny))) ;
        unitPrefab = (GameObject)Resources.Load("Prefabs/Knife", typeof(GameObject));
        knifeInstance = Instantiate(unitPrefab, new Vector3(nx, ny, 1), Quaternion.identity);
        knifeInstance.name = "Knife";


        // place player
        int finalPx = gridSize;
        int finalPy = gridSize;
        for (int i = 0; i < 20; i++)
        {
            int px;
            int py;
            do
            {
                px = rand.Next(borderThickness, gridSize - 1 - borderThickness);
                py = rand.Next(borderThickness, gridSize - 1 - borderThickness);
            } while (!checkWalkablePosition(px, py));

            if (px + py < finalPx + finalPy)
            {
                finalPx = px;
                finalPy = py;
            }
        }
        objPlayer.position = new Vector3(finalPx, finalPy, 1);

        //position the NPCs
        for (int i = 1; i <= numNPCs; i=i+4)
        {
            do
            {
                nx = rand.Next(borderThickness, gridSize - 1 - borderThickness);
                ny = rand.Next(borderThickness, gridSize - 1 - borderThickness);
            } while (!checkWalkablePosition(nx, ny));
            //create NPC character...
            // ...
            unitPrefab = (GameObject)Resources.Load("Prefabs/Bandit", typeof(GameObject));
            characterInstances[i] = Instantiate(unitPrefab, new Vector3(nx, ny, 1), Quaternion.identity);
            characterInstances[i].name = "Bandit_" + i;
            if (i +1 <= numNPCs)
            {
                characterInstances[i + 1] = Instantiate(unitPrefab, new Vector3(nx, ny, 1), Quaternion.identity);
                characterInstances[i + 1].name = "Bandit_" + (i + 1);
            }
            if (i + 2 <= numNPCs)
            {
                characterInstances[i + 2] = Instantiate(unitPrefab, new Vector3(nx, ny, 1), Quaternion.identity);
                characterInstances[i + 2].name = "Bandit_" + (i + 2);
            }
            if (i + 3 <= numNPCs)
            {
                characterInstances[i + 3] = Instantiate(unitPrefab, new Vector3(nx, ny, 1), Quaternion.identity);
                characterInstances[i + 3].name = "Bandit_" + (i + 3);
            }
        }


    }//end method


    private bool checkWalkablePosition(int px, int py)
    {
        for (int x = px - 2; x < px + 2; x++)
        {
            for (int y = py - 2; y < py + 2; y++)
            {
                if (!WALKABLE.Contains(map[x, y]))
                {
                    return false;
                }
            }
        }
        return true;
    }


    private void buildCastle(int cx, int cy, int castleSize)
    {

        // build the left wall
        map[cx - castleSize / 2, cy] = Terrain.CASTLE_WALL_BOTTOM_LEFT;
        map[cx - castleSize / 2, cy + 1] = Terrain.CASTLE_WALL_MIDDLE_LEFT;
        map[cx - castleSize / 2, cy + 2] = Terrain.CASTLE_WALL_MIDDLE_UPPER_LEFT;
        map[cx - castleSize / 2, cy + 3] = Terrain.CASTLE_WALL_TOP_LEFT;
        map[cx - castleSize / 2, cy + 4] = Terrain.CASTLE_ROOF_BOTTOM_LEFT;
        map[cx - castleSize / 2, cy + 5] = Terrain.CASTLE_ROOF_MIDDLE_LEFT;
        map[cx - castleSize / 2, cy + 6] = Terrain.CASTLE_ROOF_TOP_LEFT;

        // build the right wall
        map[cx + castleSize / 2, cy] = Terrain.CASTLE_WALL_BOTTOM_RIGHT;
        map[cx + castleSize / 2, cy + 1] = Terrain.CASTLE_WALL_MIDDLE_RIGHT;
        map[cx + castleSize / 2, cy + 2] = Terrain.CASTLE_WALL_MIDDLE_UPPER_RIGHT;
        map[cx + castleSize / 2, cy + 3] = Terrain.CASTLE_WALL_TOP_RIGHT;
        map[cx + castleSize / 2, cy + 4] = Terrain.CASTLE_ROOF_BOTTOM_RIGHT;
        map[cx + castleSize / 2, cy + 5] = Terrain.CASTLE_ROOF_MIDDLE_RIGHT;
        map[cx + castleSize / 2, cy + 6] = Terrain.CASTLE_ROOF_TOP_RIGHT;

        // build the gate
        map[cx - 1, cy] = Terrain.CASTLE_GATE_BOTTOM_LEFT;
        map[cx - 1, cy + 1] = Terrain.CASTLE_GATE_MIDDLE_LEFT;
        map[cx - 1, cy + 2] = Terrain.CASTLE_GATE_MIDDLE_UPPER_LEFT;
        map[cx - 1, cy + 3] = Terrain.CASTLE_WALL_TOP;
        map[cx - 1, cy + 4] = Terrain.CASTLE_ROOF_BOTTOM;
        map[cx - 1, cy + 5] = Terrain.CASTLE_ROOF_MIDDLE;
        map[cx - 1, cy + 6] = Terrain.CASTLE_ROOF_TOP;
        map[cx, cy] = Terrain.CASTLE_GATE_BOTTOM;
        map[cx, cy + 1] = Terrain.CASTLE_GATE_MIDDLE;
        map[cx, cy + 2] = Terrain.CASTLE_GATE_MIDDLE_UPPER;
        map[cx, cy + 3] = Terrain.CASTLE_WALL_TOP;
        map[cx, cy + 4] = Terrain.CASTLE_ROOF_BOTTOM;
        map[cx, cy + 5] = Terrain.CASTLE_ROOF_MIDDLE;
        map[cx, cy + 6] = Terrain.CASTLE_ROOF_TOP;
        map[cx + 1, cy] = Terrain.CASTLE_GATE_BOTTOM_RIGHT;
        map[cx + 1, cy + 1] = Terrain.CASTLE_GATE_MIDDLE_RIGHT;
        map[cx + 1, cy + 2] = Terrain.CASTLE_GATE_MIDDLE_UPPER_RIGHT;
        map[cx + 1, cy + 3] = Terrain.CASTLE_WALL_TOP;
        map[cx + 1, cy + 4] = Terrain.CASTLE_ROOF_BOTTOM;
        map[cx + 1, cy + 5] = Terrain.CASTLE_ROOF_MIDDLE;
        map[cx + 1, cy + 6] = Terrain.CASTLE_ROOF_TOP;

        // build the walls between
        for (int xi = cx - castleSize / 2 + 1; xi < cx + castleSize / 2; xi++)
        {
            if (xi < cx - 1 || xi > cx + 1)
            {
                map[xi, cy] = Terrain.CASTLE_WALL_BOTTOM;
                map[xi, cy + 1] = Terrain.CASTLE_WALL_MIDDLE;
                map[xi, cy + 2] = Terrain.CASTLE_WALL_MIDDLE;
                map[xi, cy + 3] = Terrain.CASTLE_WALL_TOP;
                map[xi, cy + 4] = Terrain.CASTLE_ROOF_BOTTOM;
                map[xi, cy + 5] = Terrain.CASTLE_ROOF_MIDDLE;
                map[xi, cy + 6] = Terrain.CASTLE_ROOF_TOP;
            }
        }
    }

    // Getters
    public GameObject[] GetLivesInstances() {
        return livesInstances;
    }

    public GameObject[] GetCharacterInstances()
    {
        return characterInstances;
    }

    public GameObject GetShieldInstance()
    {
        return shieldInstance;
    }

    public GameObject GetSwordInstance()
    {
        return swordInstance;
    }

    public GameObject GetAxeInstance()
    {
        return axeInstance;
    }

    public GameObject GetWoodenSwordInstance()
    {
        return woodenSwordInstance;
    }

    public GameObject GetGoldenSwordInstance()
    {
        return goldenSwordInstance;
    }

    public GameObject GetBootInstance()
    {
        return bootInstance;
    }

    public GameObject GetHelmetInstance()
    {
        return helmetInstance;
    }

    public GameObject GetKnifeInstance()
    {
        return knifeInstance;
    }

}//end class
