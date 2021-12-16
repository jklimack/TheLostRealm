using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridManager : MonoBehaviour
{
    public Tilemap tileMap;
    public Tilemap collisionMap;
    //public Tilemap roadMap;
    public TileBase roadTile;
    public Vector3Int[,] spots;
    public Astar astar;
    public BoundsInt bounds;
    public bool ready = false;
    // Start is called before the first frame update

    void Start()
    {
        Debug.Log("Astar has Initialized!");
        tileMap.CompressBounds();
        collisionMap.CompressBounds();
        //roadMap.CompressBounds();
        bounds = tileMap.cellBounds;

        CreateGrid();
        astar = new Astar(spots, bounds.size.x, bounds.size.y);

        if (astar == null)
        {
            Debug.LogError("Astar initialized with null");
        }
        //foreach (Vector3Int spot in spots) {
        //    Debug.Log("spot is " + spot);
        //}
        
        Debug.Log("Astar has Initialized!");
        ready = true;
        
    }


    public void CreateGrid()
    {
        spots = new Vector3Int[bounds.size.x, bounds.size.y];
        for (int x = bounds.xMin, i = 0; i < (bounds.size.x); x++, i++)
        {
            for (int y = bounds.yMin, j = 0; j < (bounds.size.y); y++, j++)
            {

                bool walkable = isPointValid(x, y);
                

                //if (tileMap.HasTile(new Vector3Int(x, y, 0)))
                if (walkable)
                {
                    spots[i, j] = new Vector3Int(x, y, 0);
                }
                else
                {
                    spots[i, j] = new Vector3Int(x, y, 1);
                }
            }
        }
    }

    public bool isPointValid(int x, int y)
    {
        return tileMap.HasTile(new Vector3Int(x, y, 0)) && !collisionMap.HasTile(new Vector3Int(x, y, 0));
    }
    /*
    private void DrawRoad()
    {
        for (int i = 0; i < roadPath.Count; i++)
        {
            roadMap.SetTile(new Vector3Int(roadPath[i].X, roadPath[i].Y, 0), roadTile);
        }
    }*/

    public List<Vector2Int> computeWayPoints(Vector2Int initialPos, Vector2Int targetPos) {
        //Debug.Log("Init " + initialPos.x + ", " + initialPos.y);
        List<Spot> roadPath = astar.CreatePath(spots, initialPos, new Vector2Int(targetPos.x, targetPos.y), 1000);
        List<Vector2Int> roadPathConverted = new List<Vector2Int>();
        if (roadPath != null)
        {
            foreach (Spot spot in roadPath)
            {
                roadPathConverted.Add(new Vector2Int(spot.X, spot.Y));
            }
        }
        return roadPathConverted;
    }
    /*
    void Update()
    {

        if (Input.GetMouseButton(1))
        {
            Vector3 world = camera.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int gridPos = tilemap.WorldToCell(world);
            start = new Vector2Int(gridPos.x, gridPos.y);
        }
        if (Input.GetMouseButton(2))
        {
            Vector3 world = camera.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int gridPos = tilemap.WorldToCell(world);
            roadMap.SetTile(new Vector3Int(gridPos.x, gridPos.y, 0), null);
        }
        if (Input.GetMouseButton(0))
        {
            CreateGrid();

            Vector3 world = camera.ScreenToWorldPoint(Input.mousePosition);
            Vector3Int gridPos = tilemap.WorldToCell(world);

            if (roadPath != null && roadPath.Count > 0)
                roadPath.Clear();

            roadPath = astar.CreatePath(spots, start, new Vector2Int(gridPos.x, gridPos.y), 1000);
            if (roadPath == null)
                return;

            DrawRoad();
            start = new Vector2Int(roadPath[0].X, roadPath[0].Y);
        }
    }*/
}
