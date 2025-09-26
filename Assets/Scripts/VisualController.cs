// Filename: VisualController.cs
using UnityEngine;

/// <summary>
/// Manages ALL visual effects for a cell, including glowing and pulsing emission and scale.
/// This script requires that the object's material uses a shader that supports Emission.
/// </summary>
public class VisualController : MonoBehaviour
{
    [Header("Component References")]
    [Tooltip("The Transform of the nucleus child object.")]
    public Transform nucleusTransform;
    [Tooltip("The SpriteRenderer for the main cell body.")]
    public SpriteRenderer cellRenderer;
    [Tooltip("The SpriteRenderer for the nucleus.")]
    public SpriteRenderer nucleusRenderer;

    [Header("Glow Configuration")]
    [ColorUsage(true, true)]
    public Color cellGlowColor = Color.black;
    [ColorUsage(true, true)]
    public Color nucleusGlowColor = new Color(0.1f, 0f, 0.1f);

    [Header("Pulse Effect")]
    public float pulseSpeed = 2f;
    [Tooltip("How much the glow intensity increases at the peak of the pulse.")]
    public float pulseIntensity = 1.5f;
    [Tooltip("How much the nucleus scale changes at the peak of the pulse.")]
    public float pulseMagnitude = 0.1f; // MOVED from Cell.cs

    private MaterialPropertyBlock cellPropertyBlock;
    private MaterialPropertyBlock nucleusPropertyBlock;
    
    // MOVED from Cell.cs: These handle the scale correction and pulsing
    private Vector3 initialNucleusLocalScale;
    private Vector3 correctedBaseNucleusScale;

    void Awake()
    {
        cellPropertyBlock = new MaterialPropertyBlock();
        nucleusPropertyBlock = new MaterialPropertyBlock();

        if(cellRenderer == null || nucleusRenderer == null || nucleusTransform == null)
        {
            Debug.LogError("A Renderer or the Nucleus Transform is not assigned in the VisualController!", this.gameObject);
            return;
        }

        // Store the nucleus's original local scale as designed in the prefab
        initialNucleusLocalScale = nucleusTransform.localScale;
    }

    void Start()
    {
        // NEW: This logic is now handled here to centralize visual setup.
        CorrectNucleusScale();
        // The pulsing will now use the corrected scale as its base
        correctedBaseNucleusScale = nucleusTransform.localScale;
    }
    
    /// <summary>
    /// Counteracts the parent's scaling to ensure the nucleus remains a perfect circle.
    /// </summary>
    void CorrectNucleusScale()
    {
        Vector3 parentScale = transform.localScale;
        if (parentScale.x == 0 || parentScale.y == 0) return;

        nucleusTransform.localScale = new Vector3(
            initialNucleusLocalScale.x / parentScale.x,
            initialNucleusLocalScale.y / parentScale.y,
            initialNucleusLocalScale.z
        );
    }

    void Update()
    {
        if (cellRenderer == null || nucleusRenderer == null) return;

        // Calculate a single pulse factor using a Sine wave for both effects.
        float sinWave = Mathf.Sin(Time.time * pulseSpeed);
        float pulseFactor = (1f + sinWave) / 2f; // Oscillates between 0 and 1
        float glowIntensity = 1f + (pulseFactor * pulseIntensity);
        
        // --- Update Cell Glow ---
        cellRenderer.GetPropertyBlock(cellPropertyBlock);
        cellPropertyBlock.SetColor("_EmissionColor", cellGlowColor * glowIntensity);
        cellRenderer.SetPropertyBlock(cellPropertyBlock);

        // --- Update Nucleus Glow ---
        nucleusRenderer.GetPropertyBlock(nucleusPropertyBlock);
        nucleusPropertyBlock.SetColor("_EmissionColor", nucleusGlowColor * glowIntensity);
        nucleusRenderer.SetPropertyBlock(nucleusPropertyBlock);

        // --- FIXED: Update Nucleus Scale Pulse ---
        float scaleOffset = sinWave * pulseMagnitude; // Use original -1 to 1 sin wave for scale
        nucleusTransform.localScale = correctedBaseNucleusScale + (Vector3.one * scaleOffset);
    }
}