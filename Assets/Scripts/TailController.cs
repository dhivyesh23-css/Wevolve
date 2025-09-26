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
    public float extensionSpeed = 3f;
    public float rotationSpeed = 200f;
    public float retractionSpeed = 4f;

    // --- Private Variables ---
    private GameObject activeTailTip;
    private Rigidbody2D tailTipRb;
    private LineRenderer lineRenderer;
    private List<Vector3> pathPoints = new List<Vector3>();
    private Coroutine currentCoroutine;
    private bool isExtending = true;
    private bool isRetracting = false;

    void Awake()
    {
        lineRenderer = GetComponent<LineRenderer>();
    }
    
    // THE MISSING METHOD IS HERE
    public bool IsRetracting()
    {
        return isRetracting;
    }

    public void StartPenetration()
    {
        if (currentCoroutine != null) StopCoroutine(currentCoroutine);

        if (lineRenderer.positionCount > 0)
        {
            lineRenderer.positionCount = 0;
            pathPoints.Clear();
        }
        
        activeTailTip = Instantiate(tailTipPrefab, transform.position, transform.rotation);
        tailTipRb = activeTailTip.GetComponent<Rigidbody2D>();
        activeTailTip.GetComponent<TailTip>().tailController = this;
        
        pathPoints.Add(transform.position);
        UpdateLineRenderer();

        isExtending = true;
        currentCoroutine = StartCoroutine(ExtendTail());
    }

    private IEnumerator ExtendTail()
    {
        while (isExtending && activeTailTip != null)
        {
            tailTipRb.linearVelocity = activeTailTip.transform.up * extensionSpeed;
            
            if (Vector3.Distance(activeTailTip.transform.position, pathPoints[pathPoints.Count - 1]) > 0.1f)
            {
                pathPoints.Add(activeTailTip.transform.position);
                UpdateLineRenderer();
            }
            yield return new WaitForFixedUpdate();
        }
    }

    public void UpdateTailDirection(float horizontalInput)
    {
        if (activeTailTip != null && isExtending && !isRetracting)
        {
            tailTipRb.angularVelocity = -horizontalInput * rotationSpeed;
        }
    }
    
    public void StartRetraction()
    {
        if (!isRetracting)
        {
            isRetracting = true;
            StopExtension();
            if(gameObject.activeInHierarchy)
                currentCoroutine = StartCoroutine(RetractTailRoutine());
        }
    }

    public void StopRetraction()
    {
        isRetracting = false;
        if (tailTipRb != null) { tailTipRb.linearVelocity = Vector2.zero; }
    }

    private IEnumerator RetractTailRoutine()
    {
        if (tailTipRb != null) { tailTipRb.bodyType = RigidbodyType2D.Kinematic; }

        while (isRetracting && pathPoints.Count > 1 && activeTailTip != null)
        {
            Vector3 targetPoint = pathPoints[pathPoints.Count - 2];
            activeTailTip.transform.position = Vector3.MoveTowards(activeTailTip.transform.position, targetPoint, retractionSpeed * Time.deltaTime);

            pathPoints[pathPoints.Count - 1] = activeTailTip.transform.position;
            UpdateLineRenderer();

            if (Vector3.Distance(activeTailTip.transform.position, targetPoint) < 0.01f)
            {
                pathPoints.RemoveAt(pathPoints.Count - 1);
            }
            yield return null;
        }
        
        if (pathPoints.Count <= 1) { CleanUpAndRemoveLine(); }
        else { isRetracting = false; }
    }
    
    public void CutTail()
    {
        Debug.Log("Nucleus hit! Cutting tail.");
        StopExtension();
        if (activeTailTip != null) Destroy(activeTailTip);
        currentCoroutine = null;
        if(phageController != null)
            phageController.EndPenetrationMode();
    }

    private void CleanUpAndRemoveLine()
    {
        if (activeTailTip != null) Destroy(activeTailTip);
        pathPoints.Clear();
        if(lineRenderer != null) lineRenderer.positionCount = 0;
        currentCoroutine = null;
        isRetracting = false;
        if (phageController != null)
            phageController.EndPenetrationMode();
    }

    private void UpdateLineRenderer()
    {
        if (lineRenderer != null)
        {
            lineRenderer.positionCount = pathPoints.Count;
            lineRenderer.SetPositions(pathPoints.ToArray());
        }
    }

    public void StopExtension()
    {
        isExtending = false;
        if (tailTipRb != null)
        {
            tailTipRb.linearVelocity = Vector2.zero;
            tailTipRb.angularVelocity = 0f;
        }
    }
    
    public void HandleWallCollision(Collision2D collision)
    {
        if (!isExtending) return;

        StopExtension();

        Vector2 wallNormal = collision.contacts[0].normal;
        tailTipRb.position += wallNormal * 0.05f;

        Vector2 slideDirection = Vector2.Reflect(activeTailTip.transform.up, wallNormal);
        activeTailTip.transform.up = slideDirection;

        isExtending = true;
        currentCoroutine = StartCoroutine(ExtendTail());
    }
}