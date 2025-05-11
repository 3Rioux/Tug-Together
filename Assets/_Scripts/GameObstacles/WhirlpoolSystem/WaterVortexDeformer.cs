// using UnityEngine;
// using UnityEngine.Rendering.HighDefinition;
//
// [ExecuteAlways]
// public class WaterVortexDeformer : WaterDeformer
// {
//
//     [Header("Vortex Parameters")]
//     public float radius = 5f;
//     public float strength = 1f;
//     public float rotationSpeed = 2f;
//     //public Vector3 center = Vector3.zero;
//     //public float radius = 5f;
//     //public float depth = 2f;
//     //public float swirlStrength = 3f;
//
//
//
//     protected override void ExecuteDeformerCommand(WaterSimSearchData data)
//     {
//         // Prepare parameters for compute shader
//         data.position = transform.position;
//         data.size = new Vector2(radius, radius);
//         data.rotation = rotationSpeed;
//         data.amplitude = strength;
//
//         data.deformerType = WaterDeformerType.Custom;
//         data.customDeformerID = GetDeformerID(); // Register custom kernel
//     }
//
//     private int GetDeformerID()
//     {
//         // Use your custom compute shader ID or register it in WaterSystem
//         return Shader.PropertyToID("WHIRLPOOL_DEFORMER");
//     }
// }
