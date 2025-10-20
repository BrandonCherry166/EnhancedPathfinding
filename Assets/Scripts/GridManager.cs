using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] GameObject wallPrefab;
    private GameObject ghostObject;
    [SerializeField] float gridSize;

    private HashSet<Vector2Int> occupiedPositions = new HashSet<Vector2Int>();


    private void Start()
    {
        CreateGhostObject();
    }

    private void Update()
    {
        UpdateGhostPosition();

        if (Input.GetMouseButtonDown(0))
        {
            PlaceObject();
        }
    }
    void CreateGhostObject()
    {
        ghostObject = Instantiate(wallPrefab);
        ghostObject.GetComponent<Collider>().enabled = false;

        Renderer[] renderers = ghostObject.GetComponentsInChildren<Renderer>();

        foreach(Renderer renderer in renderers)
        {
            Material mat = renderer.material;
            UnityEngine.Color color = mat.color;
            color.a = 0.5f;
            mat.color = color;

            mat.SetFloat("_Mode", 2);
            mat.SetInt("_ScrBlend", (int) UnityEngine.Rendering.BlendMode.SrcAlpha);
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
                SetGhostColor(UnityEngine.Color.red);
            }
            else
            {
                SetGhostColor(new UnityEngine.Color(1f, 1f, 1f, 0.5f));
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

    void PlaceObject()
    {
        Vector2Int gridCell = WorldToGrid(ghostObject.transform.position);
        if (!occupiedPositions.Contains(gridCell))
        {
            Vector3 placePos = GridToWorld(gridCell);
            Instantiate(wallPrefab, placePos, Quaternion.identity);
            occupiedPositions.Add(gridCell);
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
}
