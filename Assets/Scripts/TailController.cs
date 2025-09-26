// Filename: TailController.cs
using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(LineRenderer))]
public class TailController : MonoBehaviour
{
    [Header("References")]
    public GameObject tailTipPrefab;
    public BacteriophageController phageController;

    [Header("Tail Settings")]
    public float extensionSpeed = 2f;
    public float rotationSpeed = 150f;
    public float retractionSpeed = 4f;

    // --- Private Variables ---
    private GameObject activeTailTip;
    private Rigidbody2D tailTipRb;
    private LineRenderer lineRenderer;
    private List<Vector3> pathPoints = new List<Vector3>();
    private Coroutine currentCoroutine;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }

    public void StartPenetration()
    {
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        
        activeTailTip = Instantiate(tailTipPrefab, transform.position, transform.rotation);
        tailTipRb = activeTailTip.GetComponent<Rigidbody2D>();
        activeTailTip.GetComponent<TailTip>().tailController = this;
        
        pathPoints.Clear();
        pathPoints.Add(transform.position);
        lineRenderer.positionCount = 1;
        lineRenderer.SetPosition(0, transform.position);

        currentCoroutine = StartCoroutine(ExtendTail());
    }

    private IEnumerator ExtendTail()
    {
        while (true)
        {
            tailTipRb.linearVelocity = activeTailTip.transform.up * extensionSpeed;
            
            float distanceToLastPoint = Vector3.Distance(activeTailTip.transform.position, pathPoints[pathPoints.Count - 1]);
            if (distanceToLastPoint > 0.1f)
            {
                pathPoints.Add(activeTailTip.transform.position);
                UpdateLineRenderer();
            }
            
            yield return null;
        }
    }

    public void UpdateTailDirection(float horizontalInput)
    {
        if (activeTailTip != null)
        {
            activeTailTip.transform.Rotate(0, 0, -horizontalInput * rotationSpeed * Time.deltaTime);
        }
    }
    
    public void RetractTail()
    {
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        currentCoroutine = StartCoroutine(RetractTailRoutine());
    }

    private IEnumerator RetractTailRoutine()
    {
        for (int i = pathPoints.Count - 1; i >= 0; i--)
        {
            Vector3 targetPoint = pathPoints[i];
            while (Vector3.Distance(activeTailTip.transform.position, targetPoint) > 0.1f)
            {
                Vector3 direction = (targetPoint - activeTailTip.transform.position).normalized;
                tailTipRb.linearVelocity = direction * retractionSpeed;
                
                lineRenderer.positionCount = i + 1;
                lineRenderer.SetPosition(i, activeTailTip.transform.position);

                yield return null;
            }
        }
        CleanUp();
    }
    
    public void CutTail()
    {
        Debug.Log("Nucleus hit! Cutting tail.");
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);
        CleanUp();
    }

    private void CleanUp()
    {
        if (activeTailTip != null) Destroy(activeTailTip);
        pathPoints.Clear();
        lineRenderer.positionCount = 0;
        currentCoroutine = null;
        phageController.EndPenetrationMode();
    }

    private void UpdateLineRenderer()
    {
        lineRenderer.positionCount = pathPoints.Count;
        lineRenderer.SetPositions(pathPoints.ToArray());
    }

    // This is the new method, placed correctly at the end of the class,
    // not inside any other method.
    public void StopExtension()
    {
        if (currentCoroutine != null)
        {
            StopCoroutine(currentCoroutine);
        }
        if (tailTipRb != null)
        {
            tailTipRb.linearVelocity = Vector2.zero;
        }
    }
}