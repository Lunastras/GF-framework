using System;
using UnityEngine;

namespace UnityEditor.Rendering.Universal.ShaderGUI
{

    internal class SimpleLitDitheredShader : BaseShaderGUI
    {
        public class DitheringShaderGUI
        {
            const string DITHERED_SHADOWS = "_DITHERED_SHADOWS";
            const string FADE_START = "_StartFadeDistance";
            const string FADE_OFFSET = "_FadeDistanceOffset";
            const string DITHER_INTENSITY = "_DitherIntensity";
            const string DITHER_SCALE = "_DitherScale";


            public static void DrawDitherOptions(Material material)
            {
                EditorGUILayout.LabelField("");
                bool ditheredShadows = Array.IndexOf(material.shaderKeywords, DITHERED_SHADOWS) != -1;
                float distanceFadeStart = material.GetFloat(FADE_START);
                float distanceOffset = material.GetFloat(FADE_OFFSET);
                float ditherIntensity = material.GetFloat(DITHER_INTENSITY);
                float ditherScale = material.GetFloat(DITHER_SCALE);

                EditorGUI.BeginChangeCheck();
                ditheredShadows = EditorGUILayout.Toggle("Dithered shadows", ditheredShadows);
                float.TryParse(EditorGUILayout.TextField("Dither fade start", distanceFadeStart.ToString()), out distanceFadeStart);
                float.TryParse(EditorGUILayout.TextField("Fade start offset", distanceOffset.ToString()), out distanceOffset);
                float.TryParse(EditorGUILayout.TextField("Dither intensity", ditherIntensity.ToString()), out ditherIntensity);
                float.TryParse(EditorGUILayout.TextField("Dither scale", ditherScale.ToString()), out ditherScale);

                if (EditorGUI.EndChangeCheck())
                {
                    // enable or disable the keyword based on checkbox
                    if (ditheredShadows)
                        material.EnableKeyword(DITHERED_SHADOWS);
                    else
                        material.DisableKeyword(DITHERED_SHADOWS);

                    material.SetFloat(FADE_START, distanceFadeStart);
                    material.SetFloat(FADE_OFFSET, distanceOffset);
                    material.SetFloat(DITHER_INTENSITY, ditherIntensity);
                    material.SetFloat(DITHER_SCALE, ditherScale);
                }
            }
        }

        public class CustomLighting
        {
            const string LAMBERT_DISBLED = "_LAMBERT_DISABLED";
            const string FOG_ENABLE = "_FOG_DISABLED";
            const string LAMBERT_OVERRIDE = "_LambertDotOverride";

            public static void DrawCustomLightingOptions(Material material)
            {
                EditorGUILayout.LabelField("");
                bool lambertDisabled = Array.IndexOf(material.shaderKeywords, LAMBERT_DISBLED) != -1;
                bool fogEnabled = Array.IndexOf(material.shaderKeywords, FOG_ENABLE) != -1;
                float lambertOverride = material.GetFloat(LAMBERT_OVERRIDE);

                EditorGUI.BeginChangeCheck();
                lambertDisabled = EditorGUILayout.Toggle("Lambert disabled", lambertDisabled);
                fogEnabled = EditorGUILayout.Toggle("Fog disabled", fogEnabled);
                //float.TryParse(EditorGUILayout.TextField("Lambert override", lambertOverride.ToString()), out lambertOverride);
                if (lambertDisabled)
                    lambertOverride = EditorGUILayout.Slider("Lambert override", lambertOverride, 0, 1);


                if (EditorGUI.EndChangeCheck())
                {
                    // enable or disable the keyword based on checkbox
                    if (lambertDisabled)
                        material.EnableKeyword(LAMBERT_DISBLED);
                    else
                        material.DisableKeyword(LAMBERT_DISBLED);

                    if (fogEnabled)
                        material.EnableKeyword(FOG_ENABLE);
                    else
                        material.DisableKeyword(FOG_ENABLE);

                    material.SetFloat(LAMBERT_OVERRIDE, lambertOverride);
                }
            }
        }



        // Properties
        private SimpleLitGUI.SimpleLitProperties shadingModelProperties;

        // collect properties from the material properties
        public override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);
            shadingModelProperties = new SimpleLitGUI.SimpleLitProperties(properties);
        }

        // material changed check
        public override void ValidateMaterial(Material material)
        {
            SetMaterialKeywords(material, SimpleLitGUI.SetMaterialKeywords);
        }

        // material main surface options
        public override void DrawSurfaceOptions(Material material)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            // Use default labelWidth
            EditorGUIUtility.labelWidth = 0f;
            base.DrawSurfaceOptions(material);

            DitheringShaderGUI.DrawDitherOptions(material);
            CustomLighting.DrawCustomLightingOptions(material);
        }



        // material main surface inputs
        public override void DrawSurfaceInputs(Material material)
        {
            base.DrawSurfaceInputs(material);
            SimpleLitGUI.Inputs(shadingModelProperties, materialEditor, material);
            DrawEmissionProperties(material, true);
            DrawTileOffset(materialEditor, baseMapProp);
        }

        public override void DrawAdvancedOptions(Material material)
        {
            SimpleLitGUI.Advanced(shadingModelProperties);
            base.DrawAdvancedOptions(material);
        }

        public override void AssignNewShaderToMaterial(Material material, Shader oldShader, Shader newShader)
        {
            if (material == null)
                throw new ArgumentNullException("material");

            // _Emission property is lost after assigning Standard shader to the material
            // thus transfer it before assigning the new shader
            if (material.HasProperty("_Emission"))
            {
                material.SetColor("_EmissionColor", material.GetColor("_Emission"));
            }

            base.AssignNewShaderToMaterial(material, oldShader, newShader);

            if (oldShader == null || !oldShader.name.Contains("Legacy Shaders/"))
            {
                SetupMaterialBlendMode(material);
                return;
            }

            SurfaceType surfaceType = SurfaceType.Opaque;
            BlendMode blendMode = BlendMode.Alpha;
            if (oldShader.name.Contains("/Transparent/Cutout/"))
            {
                surfaceType = SurfaceType.Opaque;
                material.SetFloat("_AlphaClip", 1);
            }
            else if (oldShader.name.Contains("/Transparent/"))
            {
                // NOTE: legacy shaders did not provide physically based transparency
                // therefore Fade mode
                surfaceType = SurfaceType.Transparent;
                blendMode = BlendMode.Alpha;
            }
            material.SetFloat("_Surface", (float)surfaceType);
            material.SetFloat("_Blend", (float)blendMode);
        }
    }
}
