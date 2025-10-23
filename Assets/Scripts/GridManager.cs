using System.Collections;
using System.Collections.Generic;
using System.Linq;

//using System.Drawing; //Might cause conflict
using UnityEngine;
using UnityEngine.EventSystems;
using static Unity.IO.LowLevel.Unsafe.AsyncReadManagerMetrics;

public class GridManager : MonoBehaviour
{
    // Enum for placing elements
    public enum PlacementMode
    {
        None,
        PlaceObstacle,
        SetSource,
        SetTarget,
        PlaceAgent
    }

    [Header("Mode")]
    private PlacementMode currentMode = PlacementMode.PlaceObstacle;

    [Header("Prefabs")]
    [SerializeField] GameObject obstaclePrefab;
    [SerializeField] GameObject sourcePrefab;
    [SerializeField] GameObject targetPrefab;
    [SerializeField] GameObject agentPrefab;
    [SerializeField] GameObject pathTilePrefab;

    [Header("Grid Settings")]
    [SerializeField] float gridSize;
    [SerializeField] LayerMask obstacleLayer;

    private List<GameObject> spawnedAgents = new List<GameObject>();
    private List<GameObject> spawnedObstacles = new List<GameObject>();
    private List<GameObject> spawnedPathTiles = new List<GameObject>();

    private GameObject sourceInstance;
    private GameObject targetInstance;
    private GameObject agentInstance;
    private GameObject ghostObject;

    private HashSet<Vector2Int> occupiedPositions = new HashSet<Vector2Int>();

    private Vector2Int? sourcePos;
    private Vector2Int? targetPos;

    private Vector2Int? agentPos;

    public bool isRunning = false;
    public List<bool> runningList;
    private Dictionary<int, List <Vector2Int>> currentPaths;

    int id = 0;

    private void Start()
    {
        CreateGhostObject(obstaclePrefab);
        currentPaths = new();
        runningList = new();
    }

    private void Update()
    {
        if (currentMode != PlacementMode.None)
        {
            UpdateGhostPosition();

            if (EventSystem.current.IsPointerOverGameObject())
            {
                return;
            }

            if (currentMode == PlacementMode.PlaceObstacle)
            {
                if (Input.GetMouseButton(0))
                {
                    PlaceObject();
                }
            }
            else
            {
                if (Input.GetMouseButton(0))
                {
                    PlaceObject();
                }
            }
            
            if (Input.GetMouseButton(1))
            {
                if (currentMode == PlacementMode.PlaceObstacle)
                {
                    DeleteObject();
                }
            }

            /*
            UpdateGhostPosition();

            if (Input.GetMouseButtonDown(0))
            {
                PlaceObject();
            }
            */
        }
        bool flag = false;
        for (int i = 0; i < runningList.Count; i++)
        {
            if (runningList[i])
            {
                flag = true;
                break;
            }
        }

        if (!flag)
        {
            isRunning = false;
        }
    }
    void CreateGhostObject(GameObject obstaclePrefab)
    {
        // Destroy any old ghosts
        if (ghostObject != null)
        {
            Destroy(ghostObject);
        }

        ghostObject = Instantiate(obstaclePrefab);
        ghostObject.GetComponent<Collider>().enabled = false;
        ghostObject.name = "Ghost";

        Renderer[] renderers = ghostObject.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            Material mat = renderer.material;
            UnityEngine.Color color = mat.color;
            color.a = 0.5f;
            mat.color = color;

            mat.SetFloat("_Mode", 2);
            mat.SetInt("_ScrBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
        }
    }

    void UpdateGhostPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Vector2Int gridCell = WorldToGrid(hit.point);
            Vector3 snappedPos = GridToWorld(gridCell);
            ghostObject.transform.position = snappedPos;

            if (occupiedPositions.Contains(gridCell))
            {
                if (currentMode == PlacementMode.PlaceObstacle)
                {
                    SetGhostColor(UnityEngine.Color.red);
                }
            }
            else // Open Space
            {
                // Change the color based on mode
                switch (currentMode)
                {
                    case PlacementMode.PlaceObstacle:
                        SetGhostColor(new UnityEngine.Color(1f, 1f, 1f, 0.5f));
                        break;
                    case PlacementMode.SetSource:
                        SetGhostColor(new UnityEngine.Color(0f, 1f, 0f, 0.5f));
                        break;
                    case PlacementMode.SetTarget:
                        SetGhostColor(new UnityEngine.Color(0f, 0f, 0f, 0.5f));
                        break;
                    case PlacementMode.PlaceAgent:
                        SetGhostColor(new UnityEngine.Color(1f, 1f, 0f, 0.5f));
                        break;
                }
                //SetGhostColor(new UnityEngine.Color(1f, 1f, 1f, 0.5f));
            }


        }
    }

    void SetGhostColor(UnityEngine.Color color)
    {
        Renderer[] renderers = ghostObject.GetComponentsInChildren<Renderer>();

        foreach (Renderer renderer in renderers)
        {
            Material mat = renderer.material;
            mat.color = color;
        }
    }

    /*
    void PlaceObject()
    {
        Vector2Int gridCell = WorldToGrid(ghostObject.transform.position);
        if (!occupiedPositions.Contains(gridCell))
        {
            Vector3 placePos = GridToWorld(gridCell);
            Instantiate(obstaclePrefab, placePos, Quaternion.identity);
            occupiedPositions.Add(gridCell);
        }
    }
    */

    // Gets the right placement to place
    void PlaceObject()
    {
        Vector2Int gridCell = WorldToGrid(ghostObject.transform.position);

        // Place Obstacles
        if (currentMode == PlacementMode.PlaceObstacle)
        {
            if (!occupiedPositions.Contains(gridCell))
            {
                Vector3 placePos = GridToWorld(gridCell);
                GameObject newObstacle = Instantiate(obstaclePrefab, placePos, Quaternion.identity);
                spawnedObstacles.Add(newObstacle);
                occupiedPositions.Add(gridCell); // The cell is filled
            }

            if (!isRunning)
            {
                return;
            }
            

           ReRunPathfinding(gridCell, false);

        }

        // Set Source
        else if (currentMode == PlacementMode.SetSource)
        {
            SetMarker(ref sourceInstance, sourcePrefab, ref sourcePos, gridCell);
        }

        // Set Target
        else if (currentMode == PlacementMode.SetTarget)
        {
            SetMarker(ref targetInstance, targetPrefab, ref targetPos, gridCell);
        }

        // Place Agent
        else if (currentMode == PlacementMode.PlaceAgent)
        {
            if (!occupiedPositions.Contains(gridCell))
            {
                Vector3 placePos = GridToWorld(gridCell);
                GameObject newAgent = Instantiate(agentPrefab, placePos, Quaternion.identity);
                newAgent.name = "agent" + id;
                id++;
                spawnedAgents.Add(newAgent);
                occupiedPositions.Add(gridCell);
            }
        }
    }

    //Delete
    void DeleteObject()
    {
        Vector2Int gridCell = WorldToGrid(ghostObject.transform.position);

        if (occupiedPositions.Contains(gridCell))
        {
            GameObject obstacleToRemove = null;

            // Loop through obstacles
            foreach (GameObject obstacle in spawnedObstacles)
            {
                if (WorldToGrid(obstacle.transform.position) == gridCell)
                {
                    obstacleToRemove = obstacle;
                    break;
                }
            }

            // Remove if Found
            if (obstacleToRemove != null)
            {
                spawnedObstacles.Remove(obstacleToRemove);
                Destroy(obstacleToRemove);
                occupiedPositions.Remove(gridCell);

                if (isRunning)
                {
                    for (int i = 0; i < currentPaths.Count; i++)
                    {
                        bool wasOnPath = false;
                        foreach (var cell in currentPaths[i])
                        {
                            if (cell == gridCell)
                            {
                                wasOnPath = true;
                                break;
                            }
                        }

                        // Rerun
                        if (wasOnPath)
                        {
                            ReRunPathfinding(WorldToGrid(obstacleToRemove.transform.position), false); 
                        }
                    }
                }
            }
        }
    }

    // Using to help with placing.
    void SetMarker(ref GameObject instance, GameObject prefab, ref Vector2Int? gridPos, Vector2Int newCell)
    {
        // Clear old Pos
        if (instance != null)
        {
            if (prefab != targetPrefab)
            {
                if (gridPos.HasValue)
                    occupiedPositions.Remove(gridPos.Value);
            }
            Destroy(instance);
        }

        // Place and label taken
        Vector3 placePos = GridToWorld(newCell);
        instance = Instantiate(prefab, placePos, Quaternion.identity);
        gridPos = newCell;
        //occupiedPositions.Add(newCell);

        if (prefab != targetPrefab)
        {
            occupiedPositions.Add(newCell);
        }
    }

    Vector2Int WorldToGrid(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x / gridSize);
        int z = Mathf.RoundToInt(worldPos.z / gridSize);

        return new Vector2Int(x, z);

    }

    Vector3 GridToWorld(Vector2Int gridPos)
    {
        return new Vector3(gridPos.x * gridSize, 0f, gridPos.y * gridSize);
    }

    public void ResetBoard()
    {
        // Destroy Spawned Obst
        foreach (GameObject obstacle in spawnedObstacles)
        {
            Destroy(obstacle);
        }
        spawnedObstacles.Clear();

        foreach (GameObject agent in spawnedAgents)
        {
            Destroy(agent);
        }
        spawnedAgents.Clear();

        // Destory Markers
        if (sourceInstance != null) Destroy(sourceInstance);
        if (targetInstance != null) Destroy(targetInstance);
        if (agentInstance != null) Destroy(agentInstance);

        // Clear
        occupiedPositions.Clear();
        ClearPath();
        sourcePos = null;
        targetPos = null;

        runningList.Clear();
        isRunning = false;
        id = 0;
    }

    public void PlaceObstacle()
    {
        currentMode = PlacementMode.PlaceObstacle;
        CreateGhostObject(obstaclePrefab);
    }

    public void SetSource()
    {
        currentMode = PlacementMode.SetSource;
        CreateGhostObject(sourcePrefab);
    }

    public void SetTarget()
    {
        currentMode = PlacementMode.SetTarget;
        CreateGhostObject(targetPrefab);
    }

    public void PlaceAgent()
    {
        currentMode = PlacementMode.PlaceAgent;
        CreateGhostObject(agentPrefab);
    }

    public void RunPathfinding()
    {
        if (spawnedAgents.Count == 0 || !targetPos.HasValue)
        {
            if (spawnedAgents.Count == 0) Debug.LogWarning("No agents placed to run!");
            if (!targetPos.HasValue) Debug.LogWarning("Target not set!");
            return;
        }

        isRunning = true;

        GameObject[] agents = GameObject.FindGameObjectsWithTag("Agent").OrderBy(a => a.name).ToArray();
        ClearPath();

        for (int i = 0; i < agents.Length; i++)
        {
            if (agents[i].name == "Ghost") continue;

            runningList.Add(true);
            Vector2Int agentGridPos = WorldToGrid(agents[i].transform.position);

            List<Vector2Int> path = agents[i].GetComponent<Pathfinding>().FindPath(agentGridPos, targetPos.Value, occupiedPositions);

            if (path != null)
            {
                List<Vector3> worldPath = new List<Vector3>();
                foreach(var cell in path)
                {
                    worldPath.Add(GridToWorld(cell));
                }
                currentPaths[i] = path;

                List<Vector3> smoothedWorldPath = SmoothPath(worldPath);
                agents[i].GetComponent<FollowPath>().SetPath(smoothedWorldPath);
                VisualizePath(smoothedWorldPath);
            }
            else
            {
                // Stop if no path
                agents[i].GetComponent<FollowPath>().SetPath(new List<Vector3>());
            }
        }
    }

    public void ReRunPathfinding(Vector2Int newCell, bool addingCell)
    {
        ClearPath();

        Debug.Log("Rerun"); 
        if (spawnedAgents.Count == 0 || !targetPos.HasValue)
        {
            return;
        }

        if (addingCell)
        {
            for (int i = 0; i < currentPaths.Count; i++)
            {
                bool flag = false;
                foreach (var cell in currentPaths[i])
                {
                    if (cell == newCell)
                    {
                        flag = true;
                        break;
                    }
                }

                if (!flag)
                {
                    Debug.Log("Path Not Updated");
                    return;
                }

            }
        }

         GameObject[] agents = GameObject.FindGameObjectsWithTag("Agent").OrderBy(a => a.name).ToArray();


        for (int i = 0; i < agents.Length; i++)
        {
            if (agents[i].name == "Ghost") continue;

            List<Vector2Int> path = agents[i].GetComponent<Pathfinding>().FindPath(WorldToGrid(agents[i].transform.position), targetPos.Value, occupiedPositions);
            if (path != null)
            {
                List<Vector3> worldPath = new List<Vector3>();
                foreach (var cell in path)
                {
                    worldPath.Add(GridToWorld(cell));
                }
                currentPaths[i] = path;
                Debug.Log("Path Updated");

                List<Vector3> smoothedWorldPath = SmoothPath(worldPath);

                //agents[i].GetComponent<FollowPath>().SetPath(worldPath);
                agents[i].GetComponent<FollowPath>().SetPath(smoothedWorldPath);
                VisualizePath(smoothedWorldPath);
                StartCoroutine(agents[i].GetComponent<FollowPath>().TryEndPathFollow(i));
            }
        }
    }

    // Path
    void VisualizePath(List<Vector3> path)
    {
        Quaternion tileRotation = Quaternion.Euler(90, 0, 0);
        float tileScale = gridSize * 0.9f;
        Vector3 tileScaleVector = new Vector3(tileScale, tileScale, 1f);

        foreach (Vector3 pos in path)
        {
            // Tiles
            Vector3 tilePos = new Vector3(pos.x, 0.05f, pos.z);
            GameObject tile = Instantiate(pathTilePrefab, tilePos, tileRotation);
            tile.transform.localScale = tileScaleVector;
            spawnedPathTiles.Add(tile);
        }
    }

    // Clear Path
    void ClearPath()
    {
        foreach (GameObject tile in spawnedPathTiles)
        {
            Destroy(tile);
        }
        spawnedPathTiles.Clear();
    }

    // Path Smoothing
    List<Vector3> SmoothPath(List<Vector3> path)
    {
        if (path == null || path.Count < 2)
        {
            return path;
        }

        List<Vector3> smoothedPath = new List<Vector3>();
        smoothedPath.Add(path[0]);

        int currentIndex = 0;

        // Check ahead for blockage
        while (currentIndex < path.Count - 1)
        {
            int lookIndex = currentIndex + 1;

            while (lookIndex < path.Count - 1)
            {
                Vector3 startPos = path[currentIndex];
                Vector3 endPos = path[lookIndex + 1];

                // Make sure it is not looking at floor
                startPos.y += 0.5f;
                endPos.y += 0.5f;

                // If LoS is hit then blocked
                if (Physics.Linecast(startPos, endPos, obstacleLayer))
                {
                    break;
                }
                else
                {
                    lookIndex++;
                }
            }

            smoothedPath.Add(path[lookIndex]);
            currentIndex = lookIndex;
        }

        return smoothedPath;
    }

}
