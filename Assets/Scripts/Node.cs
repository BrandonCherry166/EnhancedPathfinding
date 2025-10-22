using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Node : MonoBehaviour
{
    public Vector2Int pos;
    public float gCost;
    public float hCost;
    public float fCost => gCost + hCost;
    public Node parent;

    public bool walkable;
    
    public Node(Vector2Int position, bool walk)
    {
        this.pos = position;
        this.walkable = walk;
    }
}
