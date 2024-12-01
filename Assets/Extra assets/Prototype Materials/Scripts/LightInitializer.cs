using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
#if USING_URP
using UnityEngine.Rendering.Universal;
#elif USING_HDRP
using UnityEngine.Rendering.HighDefinition;
#endif

namespace PrototypeMaterials
{
    /// <summary>
    /// The script is used only for the demo scene, to match up the light intensities across each render pipeline.
    /// </summary>
    [ExecuteInEditMode]
    public class LightInitializer : MonoBehaviour
    {
        public float intensityBuiltIn;
        public float intensityUrp;
        public float intensityHdrp;

        public void OnEnable()
        {
#if USING_URP
            var urpLight = GetComponent<Light>();
            if (urpLight && GraphicsSettings.defaultRenderPipeline is UniversalRenderPipelineAsset)
            {
                urpLight.intensity = intensityUrp;
            }
            else
            {
                Debug.Log("Could not find URP light or the pipeline asset is not set.");
            }

#elif USING_HDRP

        var lightData = GetComponent<HDAdditionalLightData>();
        if (!lightData && GraphicsSettings.renderPipelineAsset is HDRenderPipelineAsset)
        {
            lightData = gameObject.AddComponent<HDAdditionalLightData>();
        }

        if (lightData && GraphicsSettings.renderPipelineAsset is HDRenderPipelineAsset)
        {
            lightData.lightUnit = LightUnit.Lumen;
            lightData.intensity = intensityHdrp;
        }
        else
        {
            Debug.Log("Could not find HD additional light data or HDRP asset is not set.");
        }
#else
        var light = GetComponent<Light>();
        if (light && GraphicsSettings.currentRenderPipeline == null) 
        {
            light.intensity = intensityBuiltIn;
        }
        else
        {
            Debug.Log("Could not find Built-in light.");
        }
#endif
        }
    }
}
