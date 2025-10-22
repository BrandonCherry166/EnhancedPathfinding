using System.Collections;
using System.Collections.Generic;
//using System.Drawing; //Might cause conflict
using UnityEngine;
using UnityEngine.EventSystems;

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

    [Header("Grid Settings")]
    [SerializeField] float gridSize;

    private List<GameObject> spawnedObstacles = new List<GameObject>();
    private GameObject sourceInstance;
    private GameObject targetInstance;
    private GameObject agentInstance;
    private GameObject ghostObject;

    private HashSet<Vector2Int> occupiedPositions = new HashSet<Vector2Int>();

    private Vector2Int? sourcePos;
    private Vector2Int? targetPos;

    private Vector2Int? agentPos;

    private void Start()
    {
        CreateGhostObject(obstaclePrefab);
    }

    private void Update()
    {
        if (currentMode != PlacementMode.None)
        {
            UpdateGhostPosition();

            if (Input.GetMouseButtonDown(0))
            {
                if (!EventSystem.current.IsPointerOverGameObject())
                {
                    PlaceObject();
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
            SetMarker(ref agentInstance, agentPrefab, ref agentPos, gridCell);
        }
    }

    // Using to help with placing.
    void SetMarker(ref GameObject instance, GameObject prefab, ref Vector2Int? gridPos, Vector2Int newCell)
    {
        // Clear old Pos
        if (instance != null)
        {
            occupiedPositions.Remove(gridPos.Value);
            Destroy(instance);
        }

        // Place and label taken
        Vector3 placePos = GridToWorld(newCell);
        instance = Instantiate(prefab, placePos, Quaternion.identity);
        gridPos = newCell;
        occupiedPositions.Add(newCell);
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

        // Destory Markers
        if (sourceInstance != null) Destroy(sourceInstance);
        if (targetInstance != null) Destroy(targetInstance);
        if (agentInstance != null) Destroy(agentInstance);

        // Clear
        occupiedPositions.Clear();
        sourcePos = null;
        targetPos = null;
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
        if (!sourcePos.HasValue || !targetPos.HasValue)
        {
            return;
        }

        GameObject[] agents = GameObject.FindGameObjectsWithTag("Agent");

        for (int i = 0; i < agents.Length; i++)
        {
            Debug.Log("Check 1");
            List<Vector2Int> path = agents[i].GetComponent<Pathfinding>().FindPath(sourcePos.Value, targetPos.Value, occupiedPositions);
            Debug.Log("Check 2.5");
            if (path != null && agentInstance != null)
            {
                List<Vector3> worldPath = new List<Vector3>();
                foreach(var cell in path)
                {
                    worldPath.Add(GridToWorld(cell));
                }
                Debug.Log("Check 2.75");
                agents[i].GetComponent<FollowPath>().SetPath(worldPath);
            }
        }
    }
}
