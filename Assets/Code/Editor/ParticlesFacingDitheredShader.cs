using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Rendering.Universal.ShaderGUI
{
    internal class ParticlesFacingDitheredShader : BaseShaderGUI
    {
        private const string X_FOLLOW = "_xAxisFollowFactor";
        // Properties
        private SimpleLitGUI.SimpleLitProperties shadingModelProperties;
        private ParticleGUI.ParticleProperties particleProps;

        // List of renderers using this material in the scene, used for validating vertex streams
        List<ParticleSystemRenderer> m_RenderersUsingThisMaterial = new List<ParticleSystemRenderer>();

        public override void FindProperties(MaterialProperty[] properties)
        {
            base.FindProperties(properties);
            shadingModelProperties = new SimpleLitGUI.SimpleLitProperties(properties);
            particleProps = new ParticleGUI.ParticleProperties(properties);
        }

        public override void ValidateMaterial(Material material)
        {
            SetMaterialKeywords(material, SimpleLitGUI.SetMaterialKeywords, ParticleGUI.SetMaterialKeywords);
        }

        public override void DrawSurfaceOptions(Material material)
        {
            base.DrawSurfaceOptions(material);
            DoPopup(ParticleGUI.Styles.colorMode, particleProps.colorMode, Enum.GetNames(typeof(ParticleGUI.ColorMode)));

            float xFollowFactor = material.GetFloat(X_FOLLOW);

            EditorGUI.BeginChangeCheck();
            xFollowFactor = EditorGUILayout.Slider("X axis follow coef", xFollowFactor, 0, 1);
            if (EditorGUI.EndChangeCheck())
            {
                material.SetFloat(X_FOLLOW, xFollowFactor);
            }

            SimpleLitDitheredShader.DitheringShaderGUI.DrawDitherOptions(material);
            SimpleLitDitheredShader.CustomLighting.DrawCustomLightingOptions(material);
        }

        public override void DrawSurfaceInputs(Material material)
        {
            base.DrawSurfaceInputs(material);
            SimpleLitGUI.Inputs(shadingModelProperties, materialEditor, material);
            DrawEmissionProperties(material, true);
        }

        public override void DrawAdvancedOptions(Material material)
        {
            SimpleLitGUI.Advanced(shadingModelProperties);

            materialEditor.ShaderProperty(particleProps.flipbookMode, ParticleGUI.Styles.flipbookMode);
            ParticleGUI.FadingOptions(material, materialEditor, particleProps);
            ParticleGUI.DoVertexStreamsArea(material, m_RenderersUsingThisMaterial, true);

            DrawQueueOffsetField();
        }

        public override void OnOpenGUI(Material material, MaterialEditor materialEditor)
        {
            CacheRenderersUsingThisMaterial(material);
            base.OnOpenGUI(material, materialEditor);
        }

        void CacheRenderersUsingThisMaterial(Material material)
        {
            m_RenderersUsingThisMaterial.Clear();

            ParticleSystemRenderer[] renderers = UnityEngine.Object.FindObjectsByType(typeof(ParticleSystemRenderer), FindObjectsSortMode.None) as ParticleSystemRenderer[];
            foreach (ParticleSystemRenderer renderer in renderers)
            {
                if (renderer.sharedMaterial == material)
                    m_RenderersUsingThisMaterial.Add(renderer);
            }
        }
    }
} // namespace UnityEditor
