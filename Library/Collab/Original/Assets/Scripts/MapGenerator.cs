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
    //public Tilemap tileMapAesthetic;

    public Transform objPlayer;
    public Transform objQueen;

    //No of path points
    public int randomPoints;

    public int gridSize = 50;
    private int borderThickness = 15;
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
        CASTLE_FLOOR = 6
    }

    private Terrain[,] map;

    private readonly ISet<Terrain> WALKABLE = new HashSet<Terrain> { Terrain.GRASS, Terrain.BRIDGE };

    /*
    Start function is called only once at game initialization. 
    */
    public void Start()
    {
        Debug.Log("Start locating players....");
        locatePlayers();
        Debug.Log("Finish locating players....");

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

        Debug.Log("Place Characters and Castle...");
        placeCharactersAndCastle(10);
        Debug.Log("Finished placing characters and castle. ");

        Debug.Log("Start connecting walkable areas....");
        connectWalkableAreas();
        Debug.Log("Finish connecting walkable areas ....");

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
                    if (x > 0 && clusterMap[x - 1, y] > 0) {
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
                if (clusterMap[x, y] > 0) {
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
        for(int i = 0; i <= clusterNumber; i++)
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
            foreach(Tuple<int, int> connection in connectionPairs)
            {
                // connect the following points:
                Tuple<int, int> p1 = closestPointMatrix[connection.Item1, connection.Item2];
                Tuple<int, int> p2 = closestPointMatrix[connection.Item2, connection.Item1];
                // find factors a and b of a straight line
                double a = (p1.Item1 - p2.Item1) == 0 ? 0.0 : (p1.Item2 - p2.Item2) / (p1.Item1 - p2.Item1);
                double b = p1.Item2 - a * p1.Item1;

                //Debug.Log("y = x * " + a + " + " + b + " to connect (" + p1.Item1 + "," + p1.Item2 + ") and (" + p2.Item1 + "," + p2.Item2 + ")");

                int xMin = Math.Min(p1.Item1, p2.Item1) - 1;
                if (xMin < 0)
                {
                    xMin = 0;
                }
                int xMax = Math.Max(p1.Item1, p2.Item1) + 1;
                if (xMax >= map.GetLength(0))
                {
                    xMax = map.GetLength(0) - 1;
                }
                int yMin = Math.Min(p1.Item2, p2.Item2) - 1;
                if (yMin < 0)
                {
                    yMin = 0;
                }
                int yMax = Math.Max(p1.Item2, p2.Item2) + 1;
                if (yMax >= map.GetLength(1))
                {
                    yMax = map.GetLength(1) - 1;
                }

                for (int x = xMin; x <= xMax; x++)
                {
                    for (int y = yMin; y <= yMax; y++)
                    {
                        if (a == 0.0)
                        {
                            makePointWalkable(x, y);
                        }
                        else if (Math.Abs(y - Math.Round(x * a + b)) <= 2 && (Math.Abs(x - Math.Round((y - b) / a)) <= 2))
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

    private void locatePlayers()
    {
        objPlayer.position = new Vector3(0, 0, 1);
        objQueen.position = new Vector3(gridSize - 1, gridSize - 1, 1);
    }

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
                map[x, y] = (Terrain) rand.Next(0, 3);
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
                        map[x, y] = (Terrain) terrainType[i];
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
                    tempMap[x, y] = (Terrain) getNextConwayVal(map, x, y);
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
                        counts[(int) map[x1, y1]]++;
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
        tileList = new Tile[19];

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

    public void randomPathGenerator()
    {
        Vector2 endVec = new Vector2(objQueen.position.x, objQueen.position.y);
        randomPath(endVec);
        endVec = new Vector2((int) UnityEngine.Random.Range(0f, (float) gridSize / 2), gridSize - 1);
        randomPath(endVec);
        endVec = new Vector2(gridSize - 1, (int) UnityEngine.Random.Range(0f, (float) gridSize / 2));
        randomPath(endVec);
    }



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
                (int)RandomGaussian(0f, (float) gridSize - 1),
                (int)((gridSize - 1) / 2 + UnityEngine.Random.Range(0, (gridSize - 1) / 2))
               )
            );
        }
        randomPoint.Add(end);
        for (int i = 0; i < randomPoint.Count - 1; i++)
        {
            Debug.Log(randomPoint[i].x + " " + randomPoint[i].y);
            Debug.Log(randomPoint[i + 1].x + " " + randomPoint[i + 1].y);

            plotLine(
                (int) randomPoint[i].x,
                (int) randomPoint[i].y,
                (int) randomPoint[i + 1].x,
                (int) randomPoint[i + 1].y
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
        for(int i=0;i<gridSize; i++){
            for(int j=0;j<borderThickness;j++){
                newMap[i, j] = Terrain.TREE_ON_GRASS;
                newMap[i, gridSize-1-j] = Terrain.TREE_ON_GRASS;
                newMap[j, i] = Terrain.TREE_ON_GRASS;
                newMap[gridSize-1-j, i] = Terrain.TREE_ON_GRASS;
            }
        }

        


        return newMap;
    }//end method

    private void placeCharactersAndCastle(int numNPCs){
        System.Random rand = new System.Random();

        //place castle
        int castleSize = 20;
        int cx = rand.Next(borderThickness+castleSize/2, gridSize-1-castleSize/2-borderThickness);
        int cy = rand.Next(borderThickness+castleSize/2, gridSize-1-castleSize/2-borderThickness);
        buildCastle(cx, cy, castleSize);

        // place player
        
        int px;
        int py;
        do{
            px = rand.Next(borderThickness, gridSize-1-borderThickness);
            py = rand.Next(borderThickness, gridSize-1-borderThickness);
        }while(!createCharacterPosition(px, py));
        objPlayer.position = new Vector3(px, py, 1);
        
        //position the NPCs
        for(int i=0;i<numNPCs;i++){
            do{
                px = rand.Next(borderThickness, gridSize-1-borderThickness);
                py = rand.Next(borderThickness, gridSize-1-borderThickness);
            }while(!createCharacterPosition(px, py));
            //create NPC character...
            // ...
        }
    }//end method

    private bool createCharacterPosition(int px, int py){
        if (map[px,py] == Terrain.WATER || map[px,py] == Terrain.CASTLE_WALL || map[px, py] == Terrain.CASTLE_FLOOR){
            return false;
        }
        for(int x=px-2;x<px+2;x++){
            for(int y=py-2;y<py+2;y++){
                if (map[x,y] == Terrain.WATER || map[x,y] == Terrain.CASTLE_WALL || map[x, y] == Terrain.CASTLE_FLOOR){

                }
                else{
                    map[x,y] = Terrain.GRASS;
                }
            }
        }
        return true;
    }

    private void buildCastle(int cx, int cy, int castleSize){
        int wallWidth = 3;
        for(int i=0-castleSize/2;i<castleSize/2;i++){
            for(int j=0-castleSize/2;j<castleSize/2;j++){
                if(i<0-castleSize/2+wallWidth)
                    map[cx+i,cy+j] = Terrain.CASTLE_WALL;
                else if(i>castleSize/2-wallWidth-1)
                    map[cx+i,cy+j] = Terrain.CASTLE_WALL;
                else if(j<0-castleSize/2+wallWidth)
                    map[cx+i,cy+j] = Terrain.CASTLE_WALL;
                else if(j>castleSize/2-wallWidth-1)
                    map[cx+i,cy+j] = Terrain.CASTLE_WALL;
                else
                    map[cx+i,cy+j] = Terrain.CASTLE_FLOOR;
            }
        }
    }
}//end class
