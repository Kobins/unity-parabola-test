using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshRenderer), typeof(LineRenderer))]
public class BombController : MonoBehaviour {
    
    [SerializeField] private MeshRenderer m_renderer;
    [SerializeField] private LineRenderer m_lineRenderer;

    public void Initialize(LineRenderer from) {
        var count = from.positionCount;
        var positions = new Vector3[count];
        m_lineRenderer.positionCount = count;
        from.GetPositions(positions);
        m_lineRenderer.SetPositions(positions);
    }

    private void OnCollisionEnter(Collision collision) {
        m_renderer.enabled = false;
        Destroy(gameObject, 1f);
    }
}
