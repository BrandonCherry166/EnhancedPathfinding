using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowPath : MonoBehaviour
{
    private List<Vector3> path;
    private int currentIndex = 0;
    public float speed = 3f;
    public float arrivalDistance = 0.5f;

    private GridManager gridManager;
    private float currentGridSize = 1f;

    // Animation
    private Animator animator;
    private readonly int isRunningHash = Animator.StringToHash("IsRunning");

    void Start()
    {
        animator = GetComponent<Animator>();
        if (animator == null)
        {
            animator = GetComponentInChildren<Animator>();
            if (animator == null)
            {
                Debug.LogError("I am broken :( The animation", this);
            }
        }

        GameObject gameObject = GameObject.FindGameObjectWithTag("GridManager");
        if (gameObject != null)
        {
            gridManager = gameObject.GetComponent<GridManager>();
            currentGridSize = gridManager.gridSize;
        }
        else
        {
            Debug.LogError("FollowPath cannot find the GridManager!", this);
        }

        if (animator != null)
        {
            animator.SetBool(isRunningHash, false);
        }
    }

    public void SetPath(List<Vector3> newPath)
    {
        path = newPath;
        currentIndex = 0;

        if (gridManager != null)
        {
            currentGridSize = gridManager.gridSize;
        }

        if (animator != null)
        {
            // Start running
            bool shouldRun = (path != null && newPath.Count > 0);
            Debug.Log($"SetPath: Setting IsRunning to {shouldRun}");
            animator.SetBool(isRunningHash, shouldRun);
        }

        /*
        path = newPath;
        currentIndex = 0;
        transform.position = path[0];
        */
    }

    private void Update()
    {
        if (path == null || path.Count == 0 || currentIndex >= path.Count || gridManager == null)
        {
            // Check if it can run
            if (animator != null && animator.GetBool(isRunningHash))
            {
                Debug.Log($"Update (No Path/End): Setting IsRunning to false");
                animator.SetBool(isRunningHash, false);
            }

            return;
        }

        if (animator != null && !animator.GetBool(isRunningHash))
        {
            Debug.Log($"Update (Has Path): Setting IsRunning to true");
            animator.SetBool(isRunningHash, true);
        }

        currentGridSize = gridManager.gridSize;
        float currentSpeed = speed * currentGridSize;
        float currentArrivalDistance = arrivalDistance * currentGridSize;

        Vector3 target = path[currentIndex];

        transform.position = Vector3.MoveTowards(transform.position, target, currentSpeed * Time.deltaTime);

        if (Vector3.Distance(transform.position, target) < currentArrivalDistance)
        {
            // Check oath
            if (currentIndex < path.Count - 1)
            {
                currentIndex++;
            }
            else
            {
                // End of Path
                EndOfPath();
            }
        }

        Vector3 direction = (target - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            transform.rotation = Quaternion.LookRotation(direction);
        }
    }

    void EndOfPath()
    {
        path = null;
        currentIndex = 0;

        if (animator != null)
        {
            Debug.Log($"EndOfPath: Setting IsRunning to false");
            animator.SetBool(isRunningHash, false);
        }

        string agentName = gameObject.name;
        if (agentName.StartsWith("agent") && gridManager != null)
        {
            if (int.TryParse(agentName.Substring(5), out int index))
            {
                if (index >= 0 && index < gridManager.runningList.Count)
                {
                    gridManager.runningList[index] = false;
                }
            }
        }
    }

    /*
    private void Update()
    {
        if (path == null || currentIndex >= path.Count)
        {
            return;
        }

        Vector3 target = path[currentIndex];
        transform.position = Vector3.MoveTowards(transform.position, target, speed * Time.deltaTime);
        if (Vector3.Distance(transform.position, target) < 1.0f) // Changed from 0.05f
        {
            currentIndex++;
        }

        
    }
    public IEnumerator TryEndPathFollow(int index)
    {
        bool flag = true;
        while (flag)
        {
            if (currentIndex == path.Count - 1)
            {
                GameObject.FindGameObjectWithTag("GridManager").GetComponent<GridManager>().runningList[index] = false;
                flag = false;
            }
            yield return new WaitForSeconds(1.0f);
        }
    }
    */
}
