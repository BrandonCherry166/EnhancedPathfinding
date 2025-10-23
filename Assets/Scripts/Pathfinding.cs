using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting.Dependencies.Sqlite;
using UnityEngine;

public class Pathfinding : MonoBehaviour
{
   public List <Vector2Int> FindPath(Vector2Int source, Vector2Int target, HashSet<Vector2Int> occupied)
    {
        List<Node> openSet = new List<Node>();
        HashSet<Vector2Int> closedSet = new HashSet<Vector2Int>();

        Node startNode = new Node(source, true);
        Node targetNode = new Node(target, true);
        
        openSet.Add(startNode);

        Dictionary<Vector2Int, Node> allNodes = new Dictionary<Vector2Int, Node>();
        allNodes[source] = startNode;

        while (openSet.Count > 0)
        {
            openSet.Sort((a,b) => a.fCost.CompareTo(b.fCost));
            Node current = openSet[0];

            if (current.pos == target)
            {
                return ReconstructPath(current);
            }

            openSet.RemoveAt(0);
            closedSet.Add(current.pos);

            foreach (var neighborPos in GetNeighbors(current.pos))
            {
                if (occupied.Contains(neighborPos) && neighborPos != target)
                {
                    continue;
                }

                if (closedSet.Contains(neighborPos))
                {
                    continue;
                }

                float tentativeG = current.gCost + Heuristic(current.pos, neighborPos);

                if (!allNodes.TryGetValue(neighborPos, out Node neighbor))
                {
                    neighbor = new Node(neighborPos, true);
                    allNodes[neighborPos] = neighbor;
                }

                if (!openSet.Contains(neighbor) || tentativeG < neighbor.gCost)
                {
                    neighbor.gCost = tentativeG;
                    neighbor.hCost = Heuristic(neighborPos, target);
                    neighbor.parent = current;

                    if (!openSet.Contains(neighbor))
                    {
                        openSet.Add(neighbor);
                    }
                }
            }
        }
        return null;
    }

    List<Vector2Int> GetNeighbors(Vector2Int cell)
    {
        List<Vector2Int> neighbors = new List<Vector2Int>();
        Vector2Int[] directions = { new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(-1, 0), new Vector2Int(0, -1) };

        foreach (var dir in directions)
        {
            Vector2Int neighbor = cell + dir;
            if (neighbor.x < -25 || neighbor.x > 25 || neighbor.y < -25 || neighbor.y > 25)
                continue; //Edges
            neighbors.Add(neighbor);
        }

        return neighbors;
    }

    List<Vector2Int> ReconstructPath(Node endNode)
    {
        List<Vector2Int> path = new List<Vector2Int>();
        Node current = endNode;

        while (current != null)
        {
            path.Add(current.pos);
            current = current.parent;
        }
        path.Reverse();
        Debug.Log(path.Count);
        return path;
    }

    float Heuristic(Vector2Int a, Vector2Int b)
    {
        return Mathf.Abs(a.x - b.x) + Mathf.Abs(a.y-b.y);
    }
}

