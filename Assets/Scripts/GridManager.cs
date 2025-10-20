using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    [SerializeField] GameObject wallPrefab;
    private GameObject ghostObject;
    [SerializeField] float gridSize;

    private HashSet<Vector2> occupiedPositions = new HashSet<Vector2>();


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
            Color color = mat.color;
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
            Vector3 point = hit.point;
            Vector3 snappedPos = new Vector3(
                Mathf.Round(point.x / gridSize) * gridSize,
                Mathf.Round(point.y / gridSize) * gridSize,
                Mathf.Round(point.z / gridSize) * gridSize);

            ghostObject.transform.position = snappedPos;
            Vector2 convertedPos = new Vector2(snappedPos.x, snappedPos.z);
            if (occupiedPositions.Contains(convertedPos))
            {
                SetGhostColor(Color.red);
            }
            else
            {
                SetGhostColor(new Color(1f, 1f, 1f, 0.5f));
            }


        }
    }

    void SetGhostColor(Color color)
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
        Vector3 placePos = ghostObject.transform.position;
        Vector2 convertedPos = new Vector2(placePos.x, placePos.z);

        if (!occupiedPositions.Contains(convertedPos))
        {
            Instantiate(wallPrefab, placePos, Quaternion.identity);
            occupiedPositions.Add(convertedPos);
        }
    }
}
