using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEditor.Rendering.Universal.ShaderGUI
{
    internal class ParticlesSpinningDitheredShader : BaseShaderGUI
    {
        private const string BOP_RANGE = "_BopRangeHalf";
        private const string SPIN_SPEED = "_SpinSpeed";
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

            float bopRange = material.GetFloat(BOP_RANGE) * 2.0f;
            float spinSpeed = material.GetFloat(SPIN_SPEED);

            EditorGUI.BeginChangeCheck();
            float.TryParse(EditorGUILayout.TextField("Bop range", bopRange.ToString()), out bopRange);
            float.TryParse(EditorGUILayout.TextField("Spinning speed", spinSpeed.ToString()), out spinSpeed);
            if (EditorGUI.EndChangeCheck())
            {
                material.SetFloat(BOP_RANGE, bopRange * 0.5f);
                material.SetFloat(SPIN_SPEED, spinSpeed);
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
