using UnityEngine;

public class UpdateGrid : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        AstarPath.active.UpdateGraphs(transform.GetComponent<Collider>().bounds);
    }

}
