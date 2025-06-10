using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.HighDefinition;


public class WaterFoamTextureSwapper : MonoBehaviour
{
    [Tooltip("Tileable greyscale texture (white = foam, black = no foam).")]
    [SerializeField] private Texture2D foamTexture;

    private static readonly string[] PossibleFoamTextureNames = new[] {
        "_FoamTex", "_FoamTexture", "_WaterFoamBuffer", "_SimulationFoamMask"
    };
    
    private WaterSurface waterSurface;
    private Material runtimeMaterial;

    // ---- life-cycle --------------------------------------------------------

    private void OnEnable()
    {
        waterSurface = GetComponent<WaterSurface>();
        EnsureWritableMaterial();
        ApplyFoam();
    }

    private void OnValidate()
    {
        // Whenever you change the texture in the Inspector, re-apply it.
        if (waterSurface == null) waterSurface = GetComponent<WaterSurface>();
        ApplyFoam();
    }

    // ---- helpers -----------------------------------------------------------

    /// <summary>
    /// Makes sure we have a material instance we’re allowed to edit.
    /// If WaterSurface.customMaterial is null we clone the internal one once
    /// and assign the clone back to customMaterial.
    /// </summary>
    private void EnsureWritableMaterial()
    {
        if (waterSurface == null) return;

        runtimeMaterial = waterSurface.customMaterial;

        if (runtimeMaterial == null)
        {
            // HDRP falls back to an internal shared material when this is null.
            // Cloning it once keeps the clone local to *this* WaterSurface.
            var renderer = GetComponent<Renderer>();
            runtimeMaterial = new Material(renderer.sharedMaterial);
            {
                name = $"{name}_Water (Instance)";
            };

            waterSurface.customMaterial = runtimeMaterial;
        }
    }

    /// <summary>
    /// Pushes the user texture into the water shader’s _FoamTex slot.
    /// </summary>
    private void ApplyFoam()
    {
        if (runtimeMaterial == null || foamTexture == null) return;

        // Apply the foam texture to all possible property names
        foreach (var texName in PossibleFoamTextureNames)
        {
            runtimeMaterial.SetTexture(Shader.PropertyToID(texName), foamTexture);
        }
    
        // Try setting additional foam parameters if they exist
        // You might need to adjust these values to match your desired blending
        if (runtimeMaterial.HasProperty("_FoamSmoothness"))
            runtimeMaterial.SetFloat("_FoamSmoothness", 0.5f);
        if (runtimeMaterial.HasProperty("_FoamFalloff"))
            runtimeMaterial.SetFloat("_FoamFalloff", 1.2f);
    
        // Force material update
        waterSurface.customMaterial = runtimeMaterial;
    }
}
