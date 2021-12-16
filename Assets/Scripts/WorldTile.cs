using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WorldTile : MonoBehaviour
{
    public int gCost;
    public int hCost;
    public int gridX, gridY;
    public bool walkable = true;
    public List<WorldTile> myNeighbours;
    public WorldTile parent;

    public int fCost
    {
        get
        {
            return gCost + hCost;
        }
    }
}
