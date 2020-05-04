/* 
 * Copyright (c) 2019 Matias Lavik MIT License
 * https://github.com/mlavik1/UnityVolumeRendering
 */
using UnityEngine;

[ExecuteInEditMode]
public class SlicingPlane : MonoBehaviour
{
    private MeshRenderer meshRenderer;

    private void Start()
    {
        meshRenderer = GetComponent<MeshRenderer>();
    }

    private void Update()
    {
        meshRenderer.sharedMaterial.SetMatrix("_parentInverseMat", transform.parent.worldToLocalMatrix);
        meshRenderer.sharedMaterial.SetMatrix("_planeMat", Matrix4x4.TRS(transform.position, transform.rotation, Vector3.one)); // TODO: allow changing scale
    }
}
