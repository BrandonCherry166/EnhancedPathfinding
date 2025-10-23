using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPath : MonoBehaviour
{
    private List<Vector3> path;
    private int currentIndex = 0;
    public float speed = 3f;

    public void SetPath(List<Vector3> newPath)
    {
        path = newPath;
        currentIndex = 0;
        transform.position = path[0];
    }

    private void Update()
    {
        if (path == null || currentIndex >= path.Count)
        {
            return;
        }

        Vector3 target = path[currentIndex];
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        if (Vector3.Distance(transform.position, target) < 0.05f)
        {
            currentIndex++;
        }

        if (currentIndex == path.Count - 1)
        {
            GameObject.FindGameObjectWithTag("GridManager").GetComponent<GridManager>().isRunning = false;
        }
    }
}
