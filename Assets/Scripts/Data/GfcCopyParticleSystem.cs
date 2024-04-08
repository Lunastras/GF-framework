using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GfcCopyParticleSystem
{
    private static ParticleSystem.Burst[] auxBursts;
    private static Mesh[] auxMeshes;
    private static float[] auxFloats;

    public static void CopyMain(ParticleSystem original, ParticleSystem copy)
    {
        var copyMain = copy.main;
        var originalMain = original.main;

        copyMain.duration = originalMain.duration;

        copyMain.loop = originalMain.loop;
        copyMain.prewarm = originalMain.prewarm;

        copyMain.startDelay = originalMain.startDelay;
        copyMain.startLifetime = originalMain.startLifetime;
        copyMain.startSpeed = originalMain.startSpeed;

        copyMain.startSize3D = originalMain.startSize3D;

        if (originalMain.startSize3D)
        {
            copyMain.startSizeX = originalMain.startSizeX;
            copyMain.startSizeY = originalMain.startSizeY;
            copyMain.startSizeX = originalMain.startSizeZ;
        }
        else
        {
            copyMain.startSize = originalMain.startSize;
        }

        copyMain.startRotation3D = originalMain.startRotation3D;

        if (originalMain.startRotation3D)
        {
            copyMain.startRotationX = originalMain.startRotationX;
            copyMain.startRotationY = originalMain.startRotationY;
            copyMain.startRotationZ = originalMain.startRotationZ;
        }
        else
        {
            copyMain.startSize = originalMain.startSize;
        }

        copyMain.flipRotation = originalMain.flipRotation;

        copyMain.startColor = originalMain.startColor;
        copyMain.gravityModifier = originalMain.gravityModifier;
        copyMain.simulationSpace = originalMain.simulationSpace;
        copyMain.customSimulationSpace = originalMain.customSimulationSpace;

        copyMain.simulationSpeed = originalMain.simulationSpeed;
        copyMain.useUnscaledTime = originalMain.useUnscaledTime;
        copyMain.scalingMode = originalMain.scalingMode;
        copyMain.flipRotation = originalMain.flipRotation;
        copyMain.playOnAwake = originalMain.playOnAwake;

        copyMain.emitterVelocityMode = originalMain.emitterVelocityMode;
        copyMain.maxParticles = originalMain.maxParticles;

        copyMain.stopAction = originalMain.stopAction;
        copyMain.cullingMode = originalMain.cullingMode;
        copyMain.ringBufferMode = originalMain.ringBufferMode;
        copyMain.ringBufferLoopRange = originalMain.ringBufferLoopRange;
    }

    /*
     * Generates a bit of garbage if the burst sizes are different because burts cannot be manually removed
     */
    public static void CopyEmission(ParticleSystem original, ParticleSystem copy, bool forceCopy = false)
    {
        var copyEmission = copy.emission;
        var originalEmission = original.emission;
        copyEmission.enabled = originalEmission.enabled;

        if (copyEmission.enabled || forceCopy)
        {
            copyEmission.rateOverTime = originalEmission.rateOverTime;
            copyEmission.rateOverDistance = originalEmission.rateOverDistance;

            int countBurst = originalEmission.burstCount;

            if (originalEmission.burstCount != countBurst)
            {
                int countBursts = originalEmission.burstCount;
                if (auxBursts.Length < countBursts)
                    auxBursts = new ParticleSystem.Burst[originalEmission.burstCount];

                originalEmission.GetBursts(auxBursts);

                copyEmission.SetBursts(auxBursts, countBursts);
            }
            else
            {
                for (int i = 0; i < countBurst; ++i)
                    copyEmission.SetBurst(i, originalEmission.GetBurst(i));
            }

        }
    }

    public static void CopyShape(ParticleSystem original, ParticleSystem copy, bool forceCopy = false)
    {
        var copyShape = copy.shape;
        var originalShape = original.shape;
        copyShape.enabled = originalShape.enabled;

        if (originalShape.enabled || forceCopy)
        {
            copyShape.shapeType = originalShape.shapeType;

            copyShape.angle = originalShape.angle;
            copyShape.radius = originalShape.radius;
            copyShape.radiusThickness = originalShape.radiusThickness;

            copyShape.arc = originalShape.arc;
            copyShape.arcMode = originalShape.arcMode;
            copyShape.arcSpread = originalShape.arcSpread;
            copyShape.arcSpeed = originalShape.arcSpeed;

            //special
            //donut
            copyShape.donutRadius = originalShape.donutRadius;

            //Box
            copyShape.boxThickness = originalShape.boxThickness;

            if (ParticleSystemShapeType.Mesh == copyShape.shapeType
              || ParticleSystemShapeType.MeshRenderer == copyShape.shapeType
              || ParticleSystemShapeType.SkinnedMeshRenderer == copyShape.shapeType)
            {
                //Mesh / Mesh Renderer / skinned mesh renderer
                copyShape.meshShapeType = originalShape.meshShapeType; //also for sprite
                copyShape.mesh = originalShape.mesh;
                copyShape.useMeshMaterialIndex = originalShape.useMeshMaterialIndex;
                copyShape.meshMaterialIndex = originalShape.meshMaterialIndex;
                copyShape.normalOffset = originalShape.normalOffset;
            }


            //Sprite 
            copyShape.sprite = originalShape.sprite;


            copyShape.texture = originalShape.texture;

            if (null != copyShape.texture)
            {
                copyShape.textureClipChannel = originalShape.textureClipChannel;
                copyShape.textureClipThreshold = originalShape.textureClipThreshold;
                copyShape.textureColorAffectsParticles = originalShape.textureColorAffectsParticles;
                copyShape.textureAlphaAffectsParticles = originalShape.textureAlphaAffectsParticles;
                copyShape.textureBilinearFiltering = originalShape.textureBilinearFiltering;
            }

            copyShape.position = originalShape.position;
            copyShape.rotation = originalShape.rotation;
            copyShape.scale = originalShape.scale;

            copyShape.alignToDirection = originalShape.alignToDirection;
            copyShape.randomDirectionAmount = originalShape.randomDirectionAmount;
            copyShape.sphericalDirectionAmount = originalShape.sphericalDirectionAmount;
            copyShape.randomPositionAmount = originalShape.randomPositionAmount;
        }
    }

    public static void CopyVelocityOverLifetime(ParticleSystem original, ParticleSystem copy, bool forceCopy = false)
    {
        var copyModule = copy.velocityOverLifetime;
        var originalModule = original.velocityOverLifetime;
        copyModule.enabled = originalModule.enabled;

        if (copyModule.enabled || forceCopy)
        {
            copyModule.x = originalModule.x;
            copyModule.y = originalModule.y;
            copyModule.z = originalModule.z;

            copyModule.space = originalModule.space;

            copyModule.orbitalX = originalModule.orbitalX;
            copyModule.orbitalY = originalModule.orbitalY;
            copyModule.orbitalZ = originalModule.orbitalZ;

            copyModule.orbitalOffsetX = originalModule.orbitalOffsetX;
            copyModule.orbitalOffsetY = originalModule.orbitalOffsetY;
            copyModule.orbitalOffsetZ = originalModule.orbitalOffsetZ;

            copyModule.radial = originalModule.radial;

            copyModule.speedModifier = originalModule.speedModifier;
        }
    }

    public static void CopyLimitVelocityOverLifetime(ParticleSystem original, ParticleSystem copy, bool forceCopy = false)
    {
        var copyModule = copy.limitVelocityOverLifetime;
        var originalModule = original.limitVelocityOverLifetime;
        copyModule.enabled = originalModule.enabled;

        if (copyModule.enabled || forceCopy)
        {
            copyModule.separateAxes = originalModule.separateAxes;
            if (copyModule.separateAxes)
            {
                copyModule.limitX = originalModule.limitX;
                copyModule.limitY = originalModule.limitY;
                copyModule.limitZ = originalModule.limitZ;

                copyModule.space = originalModule.space;

            }
            else
            {
                copyModule.limit = originalModule.limit;
            }


            copyModule.dampen = originalModule.dampen;
            copyModule.drag = originalModule.drag;
            copyModule.multiplyDragByParticleSize = originalModule.multiplyDragByParticleSize;
            copyModule.multiplyDragByParticleVelocity = originalModule.multiplyDragByParticleVelocity;
        }
    }

    public static void CopyInheritVelocity(ParticleSystem original, ParticleSystem copy, bool forceCopy = false)
    {
        var copyModule = copy.inheritVelocity;
        var originalModule = original.inheritVelocity;
        copyModule.enabled = originalModule.enabled;

        if (copyModule.enabled || forceCopy)
        {
            copyModule.mode = originalModule.mode;
            copyModule.curve = originalModule.curve;
            copyModule.curveMultiplier = originalModule.curveMultiplier;
        }
    }

    public static void CopyLifeTimeByEmitterSpeed(ParticleSystem original, ParticleSystem copy, bool forceCopy = false)
    {
        var copyModule = copy.lifetimeByEmitterSpeed;
        var originalModule = original.lifetimeByEmitterSpeed;
        copyModule.enabled = originalModule.enabled;

        if (copyModule.enabled || forceCopy)
        {
            copyModule.curve = originalModule.curve;
            copyModule.curveMultiplier = originalModule.curveMultiplier;
            copyModule.range = originalModule.range;
        }
    }

    public static void CopyForceOverLifeTime(ParticleSystem original, ParticleSystem copy, bool forceCopy = false)
    {
        var copyModule = copy.forceOverLifetime;
        var originalModule = original.forceOverLifetime;
        copyModule.enabled = originalModule.enabled;

        if (copyModule.enabled || forceCopy)
        {
            copyModule.x = originalModule.x;
            copyModule.y = originalModule.y;
            copyModule.z = originalModule.z;

            copyModule.randomized = originalModule.randomized;
            copyModule.space = originalModule.space;
        }
    }

    public static void CopyColorOverLifetime(ParticleSystem original, ParticleSystem copy, bool forceCopy = false)
    {
        var copyModule = copy.colorOverLifetime;
        var originalModule = original.colorOverLifetime;
        copyModule.enabled = originalModule.enabled;

        if (copyModule.enabled || forceCopy)
        {
            copyModule.color = originalModule.color;
        }
    }

    public static void CopyColorBySpeed(ParticleSystem original, ParticleSystem copy, bool forceCopy = false)
    {
        var copyModule = copy.colorBySpeed;
        var originalModule = original.colorBySpeed;
        copyModule.enabled = originalModule.enabled;

        if (copyModule.enabled || forceCopy)
        {
            copyModule.color = originalModule.color;
            copyModule.range = originalModule.range;
        }
    }

    public static void CopySizeOverLifeTime(ParticleSystem original, ParticleSystem copy, bool forceCopy = false)
    {
        var copyModule = copy.sizeOverLifetime;
        var originalModule = original.sizeOverLifetime;
        copyModule.enabled = originalModule.enabled;

        if (copyModule.enabled || forceCopy)
        {
            copyModule.separateAxes = originalModule.separateAxes;
            if (copyModule.separateAxes)
            {
                copyModule.x = originalModule.x;
                copyModule.y = originalModule.y;
                copyModule.z = originalModule.z;

            }
            else
            {
                copyModule.size = originalModule.size;
            }
        }
    }

    public static void CopySizeBySpeed(ParticleSystem original, ParticleSystem copy, bool forceCopy = false)
    {
        var copyModule = copy.sizeBySpeed;
        var originalModule = original.sizeBySpeed;
        copyModule.enabled = originalModule.enabled;

        if (copyModule.enabled || forceCopy)
        {
            copyModule.separateAxes = originalModule.separateAxes;
            if (copyModule.separateAxes)
            {
                copyModule.x = originalModule.x;
                copyModule.y = originalModule.y;
                copyModule.z = originalModule.z;

            }
            else
            {
                copyModule.size = originalModule.size;
            }

            copyModule.range = originalModule.range;
        }
    }

    public static void CopyRotationOverLifeTime(ParticleSystem original, ParticleSystem copy, bool forceCopy = false)
    {
        var copyModule = copy.rotationOverLifetime;
        var originalModule = original.rotationOverLifetime;
        copyModule.enabled = originalModule.enabled;

        if (copyModule.enabled || forceCopy)
        {
            copyModule.separateAxes = originalModule.separateAxes;

            copyModule.x = originalModule.x;
            copyModule.y = originalModule.y;
            copyModule.z = originalModule.z;
        }
    }


    public static void CopyRotationBySpeed(ParticleSystem original, ParticleSystem copy, bool forceCopy = false)
    {
        var copyModule = copy.rotationBySpeed;
        var originalModule = original.rotationBySpeed;
        copyModule.enabled = originalModule.enabled;

        if (copyModule.enabled || forceCopy)
        {
            copyModule.separateAxes = originalModule.separateAxes;

            copyModule.x = originalModule.x;
            copyModule.y = originalModule.y;
            copyModule.z = originalModule.z;

            copyModule.range = originalModule.range;
        }
    }

    public static void CopyExternalForces(ParticleSystem original, ParticleSystem copy, bool forceCopy = false)
    {
        var copyModule = copy.externalForces;
        var originalModule = original.externalForces;
        copyModule.enabled = originalModule.enabled;

        if (copyModule.enabled || forceCopy)
        {
            copyModule.multiplier = originalModule.multiplier;
            copyModule.multiplierCurve = originalModule.multiplierCurve;

            copyModule.influenceFilter = originalModule.influenceFilter;
            copyModule.influenceMask = originalModule.influenceMask;

            if (ParticleSystemGameObjectFilter.LayerMask != copyModule.influenceFilter)
            {
                int countList = originalModule.influenceCount;

                while (copyModule.influenceCount > countList)
                {
                    copyModule.RemoveInfluence(copyModule.influenceCount - 1);
                }

                for (int i = 0; i < countList; ++i)
                {
                    copyModule.SetInfluence(i, originalModule.GetInfluence(i));
                }
            }
        }
    }

    public static void CopyNoise(ParticleSystem original, ParticleSystem copy, bool forceCopy = false)
    {
        var copyModule = copy.noise;
        var originalModule = original.noise;
        copyModule.enabled = originalModule.enabled;

        if (copyModule.enabled || forceCopy)
        {
            copyModule.separateAxes = originalModule.separateAxes;
            copyModule.remapEnabled = originalModule.remapEnabled;

            if (copyModule.separateAxes)
            {
                copyModule.strengthX = originalModule.strengthX;
                copyModule.strengthY = originalModule.strengthY;
                copyModule.strengthZ = originalModule.strengthZ;

                if (copyModule.remapEnabled)
                {
                    copyModule.remapX = originalModule.remapX;
                    copyModule.remapY = originalModule.remapY;
                    copyModule.remapZ = originalModule.remapZ;
                }
            }
            else
            {
                copyModule.strength = originalModule.strength;
                copyModule.remap = originalModule.remap;
            }

            copyModule.frequency = originalModule.frequency;
            copyModule.scrollSpeed = originalModule.scrollSpeed;
            copyModule.damping = originalModule.damping;
            copyModule.frequency = originalModule.frequency;

            copyModule.octaveCount = originalModule.octaveCount;
            copyModule.octaveMultiplier = originalModule.octaveMultiplier;
            copyModule.octaveScale = originalModule.octaveScale;

            copyModule.quality = originalModule.quality;

            copyModule.positionAmount = originalModule.positionAmount;
            copyModule.rotationAmount = originalModule.rotationAmount;
            copyModule.sizeAmount = originalModule.sizeAmount;
        }
    }

    public static void CopyCollision(ParticleSystem original, ParticleSystem copy, bool forceCopy = false)
    {
        var copyModule = copy.collision;
        var originalModule = original.collision;
        copyModule.enabled = originalModule.enabled;

        if (copyModule.enabled || forceCopy)
        {
            copyModule.type = originalModule.type;

            if (ParticleSystemCollisionType.World == copyModule.type)
            {
                copyModule.quality = originalModule.quality;

                copyModule.collidesWith = originalModule.collidesWith;
                copyModule.maxCollisionShapes = originalModule.maxCollisionShapes;
                copyModule.enableDynamicColliders = originalModule.enableDynamicColliders;
                copyModule.colliderForce = originalModule.colliderForce;

                copyModule.multiplyColliderForceByCollisionAngle = originalModule.multiplyColliderForceByCollisionAngle;
                copyModule.multiplyColliderForceByParticleSpeed = originalModule.multiplyColliderForceByParticleSpeed;
                copyModule.multiplyColliderForceByParticleSize = originalModule.multiplyColliderForceByParticleSize;
            }
            else
            {
                int countPlanes = originalModule.planeCount;
                while (copyModule.planeCount > countPlanes)
                    copyModule.RemovePlane(copyModule.planeCount - 1);

                for (int i = 0; i < countPlanes; ++i)
                    copyModule.SetPlane(i, originalModule.GetPlane(i));
            }

            copyModule.mode = originalModule.mode;
            copyModule.dampen = originalModule.dampen;
            copyModule.bounce = originalModule.bounce;
            copyModule.lifetimeLoss = originalModule.lifetimeLoss;
            copyModule.minKillSpeed = originalModule.minKillSpeed;
            copyModule.maxKillSpeed = originalModule.maxKillSpeed;

            copyModule.radiusScale = originalModule.radiusScale;

            copyModule.sendCollisionMessages = originalModule.sendCollisionMessages;
        }
    }

    public static void CopyTriggers(ParticleSystem original, ParticleSystem copy, bool forceCopy = false)
    {
        var copyModule = copy.trigger;
        var originalModule = original.trigger;
        copyModule.enabled = originalModule.enabled;

        if (copyModule.enabled || forceCopy)
        {
            int countColliders = originalModule.colliderCount;
            while (copyModule.colliderCount > countColliders)
                copyModule.RemoveCollider(copyModule.colliderCount - 1);

            for (int i = 0; i < countColliders; ++i)
                copyModule.SetCollider(i, originalModule.GetCollider(i));

            copyModule.inside = originalModule.inside;
            copyModule.outside = originalModule.outside;
            copyModule.enter = originalModule.enter;
            copyModule.exit = originalModule.exit;
            copyModule.colliderQueryMode = originalModule.colliderQueryMode;
            copyModule.radiusScale = originalModule.radiusScale;
        }
    }

    public static void CopySubEmitters(ParticleSystem original, ParticleSystem copy, bool forceCopy = false)
    {
        var copyModule = copy.subEmitters;
        var originalModule = original.subEmitters;
        copyModule.enabled = originalModule.enabled;

        if (copyModule.enabled || forceCopy)
        {
            int countEmitters = originalModule.subEmittersCount;
            while (copyModule.subEmittersCount > countEmitters)
                copyModule.RemoveSubEmitter(copyModule.subEmittersCount - 1);

            for (int i = 0; i < countEmitters; ++i)
            {
                if (copyModule.subEmittersCount > i) //check if index exists
                {
                    copyModule.SetSubEmitterEmitProbability(i, originalModule.GetSubEmitterEmitProbability(i));
                    copyModule.SetSubEmitterProperties(i, originalModule.GetSubEmitterProperties(i));
                    copyModule.SetSubEmitterSystem(i, originalModule.GetSubEmitterSystem(i));
                    copyModule.SetSubEmitterType(i, originalModule.GetSubEmitterType(i));
                }
                else //modify subsystem
                {
                    copyModule.AddSubEmitter(originalModule.GetSubEmitterSystem(i),
                        originalModule.GetSubEmitterType(i),
                        originalModule.GetSubEmitterProperties(i),
                        originalModule.GetSubEmitterEmitProbability(i));
                }

            }
        }

        copyModule.enabled = false;
    }

    public static void CopyTextureSheetAnimation(ParticleSystem original, ParticleSystem copy, bool forceCopy = false)
    {
        var copyModule = copy.textureSheetAnimation;
        var originalModule = original.textureSheetAnimation;
        copyModule.enabled = originalModule.enabled;

        if (copyModule.enabled || forceCopy)
        {
            copyModule.mode = originalModule.mode;
            if (ParticleSystemAnimationMode.Grid == copyModule.mode)
            {
                copyModule.numTilesX = originalModule.numTilesX;
                copyModule.numTilesY = originalModule.numTilesY;

                copyModule.animation = originalModule.animation;
                copyModule.rowMode = originalModule.rowMode;
                copyModule.rowIndex = originalModule.rowIndex;
            }
            else
            {
                int spriteCount = originalModule.spriteCount;
                while (copyModule.spriteCount > spriteCount)
                    copyModule.RemoveSprite(copyModule.spriteCount - 1);

                for (int i = 0; i < spriteCount; ++i)
                    copyModule.SetSprite(i, originalModule.GetSprite(i));
            }

            copyModule.timeMode = originalModule.timeMode;
            copyModule.frameOverTime = originalModule.frameOverTime;
            copyModule.speedRange = originalModule.speedRange;
            copyModule.cycleCount = originalModule.cycleCount;
            copyModule.uvChannelMask = originalModule.uvChannelMask;
            copyModule.startFrame = originalModule.startFrame;
        }
    }

    public static void CopyLights(ParticleSystem original, ParticleSystem copy, bool forceCopy = false)
    {
        var copyModule = copy.lights;
        var originalModule = original.lights;
        copyModule.enabled = originalModule.enabled;

        if (copyModule.enabled || forceCopy)
        {
            copyModule.light = originalModule.light;
            copyModule.ratio = originalModule.ratio;
            copyModule.useRandomDistribution = originalModule.useRandomDistribution;
            copyModule.useParticleColor = originalModule.useParticleColor;
            copyModule.sizeAffectsRange = originalModule.sizeAffectsRange;
            copyModule.alphaAffectsIntensity = originalModule.alphaAffectsIntensity;
            copyModule.rangeMultiplier = originalModule.rangeMultiplier;
            copyModule.intensityMultiplier = originalModule.intensityMultiplier;
            copyModule.maxLights = originalModule.maxLights;
        }
    }

    public static void CopyTrails(ParticleSystem original, ParticleSystem copy, bool forceCopy = false)
    {
        var copyModule = copy.trails;
        var originalModule = original.trails;
        copyModule.enabled = originalModule.enabled;

        if (copyModule.enabled || forceCopy)
        {
            copyModule.mode = originalModule.mode;

            if (ParticleSystemTrailMode.PerParticle == copyModule.mode)
            {
                copyModule.ratio = originalModule.ratio;
                copyModule.lifetime = originalModule.lifetime;
                copyModule.minVertexDistance = originalModule.minVertexDistance;
                copyModule.worldSpace = originalModule.worldSpace;
                copyModule.dieWithParticles = originalModule.dieWithParticles;
                copyModule.sizeAffectsLifetime = originalModule.sizeAffectsLifetime;
            }
            else
            {
                copyModule.ribbonCount = originalModule.ribbonCount;
                copyModule.splitSubEmitterRibbons = originalModule.splitSubEmitterRibbons;
                copyModule.attachRibbonsToTransform = originalModule.attachRibbonsToTransform;
            }

            copyModule.textureMode = originalModule.textureMode;
            copyModule.colorOverLifetime = originalModule.colorOverLifetime;
            copyModule.widthOverTrail = originalModule.widthOverTrail;
            copyModule.colorOverTrail = originalModule.colorOverTrail;
            copyModule.inheritParticleColor = originalModule.inheritParticleColor;
            copyModule.generateLightingData = originalModule.generateLightingData;
            copyModule.sizeAffectsWidth = originalModule.sizeAffectsWidth;
            copyModule.shadowBias = originalModule.shadowBias;

        }
    }

    public static void CopyCustomData(ParticleSystem original, ParticleSystem copy, bool forceCopy = false)
    {
        var copyModule = copy.customData;
        var originalModule = original.customData;
        copyModule.enabled = originalModule.enabled;

        if (copyModule.enabled || forceCopy)
        {

            originalModule.SetMode(ParticleSystemCustomData.Custom1, originalModule.GetMode(ParticleSystemCustomData.Custom1));

            if (ParticleSystemCustomDataMode.Color == originalModule.GetMode(ParticleSystemCustomData.Custom1))
            {
                int componentCount = originalModule.GetVectorComponentCount(ParticleSystemCustomData.Custom1);

                originalModule.SetVectorComponentCount(ParticleSystemCustomData.Custom1, componentCount);

                for (int i = 0; i < componentCount; ++i)
                    originalModule.SetVector(ParticleSystemCustomData.Custom1, i, originalModule.GetVector(ParticleSystemCustomData.Custom1, i));
            }
            else if (ParticleSystemCustomDataMode.Vector == originalModule.GetMode(ParticleSystemCustomData.Custom1))
            {
                originalModule.SetColor(ParticleSystemCustomData.Custom1, originalModule.GetColor(ParticleSystemCustomData.Custom1));
            }

            originalModule.SetMode(ParticleSystemCustomData.Custom2, originalModule.GetMode(ParticleSystemCustomData.Custom2));

            if (ParticleSystemCustomDataMode.Color == originalModule.GetMode(ParticleSystemCustomData.Custom2))
            {
                int componentCount = originalModule.GetVectorComponentCount(ParticleSystemCustomData.Custom2);

                originalModule.SetVectorComponentCount(ParticleSystemCustomData.Custom2, componentCount);

                for (int i = 0; i < componentCount; ++i)
                    originalModule.SetVector(ParticleSystemCustomData.Custom2, i, originalModule.GetVector(ParticleSystemCustomData.Custom2, i));
            }
            else if (ParticleSystemCustomDataMode.Vector == originalModule.GetMode(ParticleSystemCustomData.Custom2))
            {
                originalModule.SetColor(ParticleSystemCustomData.Custom2, originalModule.GetColor(ParticleSystemCustomData.Custom2));
            }
        }
    }

    public static void CopyRenderer(ParticleSystem original, ParticleSystem copy, bool forceCopy = false)
    {
        // Debug.Log("THE name of the weird ass copy particle is " + copy.name);
        //  Debug.Log("THE name of the weird ass original particle is " + original.name);

        var copyModule = copy.GetComponent<ParticleSystemRenderer>();
        var originalModule = original.GetComponent<ParticleSystemRenderer>();
        copyModule.enabled = originalModule.enabled;

        //work left to be done
        if (copyModule.enabled || forceCopy)
        {
            copyModule.renderMode = originalModule.renderMode;

            switch (copyModule.renderMode)
            {
                case (ParticleSystemRenderMode.Stretch):
                    copyModule.cameraVelocityScale = originalModule.cameraVelocityScale;
                    copyModule.velocityScale = originalModule.velocityScale;
                    copyModule.lengthScale = originalModule.lengthScale;
                    copyModule.freeformStretching = originalModule.freeformStretching;
                    copyModule.rotateWithStretchDirection = originalModule.rotateWithStretchDirection;

                    break;

                case (ParticleSystemRenderMode.Mesh):
                    copyModule.meshDistribution = originalModule.meshDistribution;

                    int countMeshes = originalModule.meshCount;

                    if (auxMeshes.Length < countMeshes)
                        auxMeshes = new Mesh[countMeshes];

                    originalModule.GetMeshes(auxMeshes);
                    copyModule.SetMeshes(auxMeshes, countMeshes);

                    if (ParticleSystemMeshDistribution.NonUniformRandom == copyModule.meshDistribution)
                    {
                        if (auxFloats.Length == 0 || auxFloats.Length < countMeshes)
                            auxFloats = new float[countMeshes];

                        originalModule.GetMeshWeightings(auxFloats);
                        copyModule.SetMeshWeightings(auxFloats, countMeshes);
                    }

                    copyModule.normalDirection = originalModule.normalDirection;
                    copyModule.normalDirection = originalModule.normalDirection;

                    break;

            }

            copyModule.normalDirection = originalModule.normalDirection;
            copyModule.sharedMaterial = originalModule.sharedMaterial;
            copyModule.sortMode = originalModule.sortMode;
            copyModule.sortingFudge = originalModule.sortingFudge;
            copyModule.minParticleSize = originalModule.minParticleSize;
            copyModule.maxParticleSize = originalModule.maxParticleSize;

            copyModule.alignment = originalModule.alignment;
            copyModule.flip = originalModule.flip;
            copyModule.allowRoll = originalModule.allowRoll;
            copyModule.pivot = originalModule.pivot;
            copyModule.maskInteraction = originalModule.maskInteraction;
            copyModule.shadowCastingMode = originalModule.shadowCastingMode;
            copyModule.receiveShadows = originalModule.receiveShadows;
            copyModule.staticShadowCaster = originalModule.staticShadowCaster;
            copyModule.shadowBias = originalModule.shadowBias;
            copyModule.sortingFudge = originalModule.sortingFudge;
            copyModule.sortingLayerID = originalModule.sortingLayerID;
            copyModule.sortingOrder = originalModule.sortingOrder;
            copyModule.lightProbeUsage = originalModule.lightProbeUsage;

            copyModule.probeAnchor = originalModule.probeAnchor;
            copyModule.renderingLayerMask = originalModule.renderingLayerMask;


            copyModule.renderingLayerMask = originalModule.renderingLayerMask;
            copyModule.enableGPUInstancing = true;




        }
    }


    public static void CopyFrom(ParticleSystem original, ParticleSystem copy, bool forceCopy = false)
    {
        CopyMain(original, copy);
        CopyEmission(original, copy, forceCopy);
        CopyShape(original, copy, forceCopy);
        CopyVelocityOverLifetime(original, copy, forceCopy);
        CopyLimitVelocityOverLifetime(original, copy, forceCopy);
        CopyInheritVelocity(original, copy, forceCopy);
        CopyLifeTimeByEmitterSpeed(original, copy, forceCopy);
        CopyForceOverLifeTime(original, copy, forceCopy);
        CopyColorOverLifetime(original, copy, forceCopy);
        CopyColorBySpeed(original, copy, forceCopy);
        CopySizeOverLifeTime(original, copy, forceCopy);
        CopyExternalForces(original, copy, forceCopy);
        CopyNoise(original, copy, forceCopy);
        CopyCollision(original, copy, forceCopy);
        CopyTriggers(original, copy, forceCopy);
        CopySubEmitters(original, copy, forceCopy);
        CopyTextureSheetAnimation(original, copy, forceCopy);
        CopyLights(original, copy, forceCopy);
        CopyTrails(original, copy, forceCopy);
        CopyCustomData(original, copy, forceCopy);
        CopyRenderer(original, copy, forceCopy);
    }
}
