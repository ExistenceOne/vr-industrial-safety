using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshColliderMaker : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    [ContextMenu("Add Mesh Colliders To All Children")]
    void AddColliders()
    {
        MeshFilter[] meshFilters = GetComponentsInChildren<MeshFilter>();
        foreach (MeshFilter mf in meshFilters)
        {
            if (mf.GetComponent<Collider>() == null)
            {
                MeshCollider mc = mf.gameObject.AddComponent<MeshCollider>();
                mc.convex = true;
            }
        }
        Debug.Log($"{meshFilters.Length}개에 Collider 추가 완료");
    }
}
