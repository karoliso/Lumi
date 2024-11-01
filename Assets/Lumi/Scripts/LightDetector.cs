using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Lumi
{
    [ExecuteAlways]
    public class LightDetector : MonoBehaviour
    {
        public List<Transform> samplePoints;
        public List<Light> lights;

        public LayerMask lightRaycastMask = 1 << 0;
        public bool perceivedBrightness = true;
        public LightSampleEvaluationMode lightSampleEvaluationMode = LightSampleEvaluationMode.Average;
        public LightAdjustmentMode lightAdjustmentMode = LightAdjustmentMode.Gamma;
        public BakedLightSampleMode bakedLightSampleMode = BakedLightSampleMode.LightProbes;
        [Range(0f, 1f)] public float bakedLightContribution = 1f;

        public bool runInEditor;
        [SerializeField] private bool drawSamplePointGizmos;
        [Range(0.01f, 1f)] [SerializeField] private float samplePointGizmoSize = 0.1f;

        public float SampledLightAmount { get; private set; }

        private SphericalHarmonicsL2 harmonics;

        public enum BakedLightSampleMode
        {
            None,
            LightProbes,
            Lightmap
        }

        public enum LightSampleEvaluationMode
        {
            Average,
            Max
        }

        public enum LightAdjustmentMode
        {
            None,
            Gamma
        }

        private const float RedCoef = 0.2989f;
        private const float GreenCoef = 0.5870f;
        private const float BlueCoef = 0.1140f;

        void Update()
        {
            if (Application.isPlaying || runInEditor)
            {
                UpdateSampledLightAmount();
            }
        }

        public void UpdateSampledLightAmount()
        {
            SampledLightAmount = 0;

            foreach (Transform samplePoint in samplePoints)
            {
                float sampleIllumination = 0;

                foreach (Light light in lights)
                {
                    if (light == null || !light.enabled || !light.gameObject.activeSelf) continue;

                    switch (light.type)
                    {
                        case LightType.Directional:
                            sampleIllumination += SampleDirectionalLight(light, samplePoint.position);
                            break;
                        case LightType.Point:
                            sampleIllumination += SamplePointLight(light, samplePoint.position);
                            break;
                        case LightType.Spot:
                            sampleIllumination += SampleSpotLight(light, samplePoint.position);
                            break;
                    }
                }

                if (bakedLightSampleMode == BakedLightSampleMode.LightProbes)
                {
                    sampleIllumination += SampleLightProbes(samplePoint.position);
                }
                else if (bakedLightSampleMode == BakedLightSampleMode.Lightmap)
                {
                    sampleIllumination += SampleLightmap(samplePoint.position);
                }

                if (lightSampleEvaluationMode == LightSampleEvaluationMode.Average)
                {
                    SampledLightAmount += sampleIllumination;
                }
                else
                {
                    SampledLightAmount = Mathf.Max(SampledLightAmount, sampleIllumination);
                }
            }

            if (lightSampleEvaluationMode == LightSampleEvaluationMode.Average)
            {
                SampledLightAmount /= samplePoints.Count;
            }

            if (lightAdjustmentMode == LightAdjustmentMode.Gamma)
            {
                SampledLightAmount = Mathf.LinearToGammaSpace(SampledLightAmount);
            }
        }

        private void OnDrawGizmos()
        {
            if (!drawSamplePointGizmos)
            {
                return;
            }

            foreach (Transform samplePoint in samplePoints)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(samplePoint.position, samplePointGizmoSize);
            }
        }

        float SampleDirectionalLight(Light light, Vector3 samplePoint)
        {
            float lightIntensity = light.intensity;
            if (perceivedBrightness)
            {
                float adjustedColorInt =
                    (light.color.r * RedCoef) + (light.color.g * GreenCoef) + (light.color.b * BlueCoef);
                lightIntensity = lightIntensity * adjustedColorInt;
            }

            if (light.shadows == LightShadows.None)
            {
                return lightIntensity;
            }

            Vector3 rayDirectionNorm = -light.transform.forward;
            if (Physics.Raycast(samplePoint, rayDirectionNorm, Mathf.Infinity, lightRaycastMask))
            {
                return 0;
            }

            return lightIntensity;
        }

        float SamplePointLight(Light light, Vector3 samplePoint)
        {
            Transform lightTransform = light.transform;
            Vector3 rayDirectionMag = samplePoint - lightTransform.position;
            float lightDistance = rayDirectionMag.magnitude;
            if (lightDistance > light.range)
            {
                return 0;
            }

            if (light.shadows != LightShadows.None)
            {
                Vector3 rayDirectionNorm = rayDirectionMag.normalized;
                if (Physics.Raycast(lightTransform.position, rayDirectionNorm, lightDistance, lightRaycastMask))
                {
                    return 0;
                }
            }

            float inverseSquareRange = 1f / Mathf.Max(light.range * light.range, 0.00001f);

            float distanceSqr = Mathf.Max(rayDirectionMag.sqrMagnitude, 0.00001f);
            float rangeAttenuation = Mathf.Sqrt(Mathf.Min(1.0f - Mathf.Sqrt(distanceSqr * inverseSquareRange), 1));
            float attenuation = rangeAttenuation / distanceSqr;

            attenuation = Mathf.Min(1, attenuation);

            float lightIntensity = light.intensity;
            if (perceivedBrightness)
            {
                float adjustedColorInt =
                    (light.color.r * RedCoef) + (light.color.g * GreenCoef) + (light.color.b * BlueCoef);
                lightIntensity = lightIntensity * adjustedColorInt;
            }

            return attenuation * lightIntensity;
        }

        float SampleSpotLight(Light light, Vector3 samplePoint)
        {
            Transform lightTransform = light.transform;
            Vector3 rayDirectionMag = samplePoint - lightTransform.position;
            float lightDistance = rayDirectionMag.magnitude;
            if (lightDistance > light.range)
            {
                return 0;
            }

            Vector3 rayDirectionNorm = rayDirectionMag.normalized;
            float outerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.spotAngle);
            float forwardDotRay = Vector3.Dot(lightTransform.forward, rayDirectionNorm);
            if (forwardDotRay < outerCos)
            {
                return 0;
            }

            if (light.shadows != LightShadows.None)
            {
                if (Physics.Raycast(lightTransform.position, rayDirectionNorm, lightDistance, lightRaycastMask))
                {
                    return 0;
                }
            }

            float inverseSquareRange = 1f / Mathf.Max(light.range * light.range, 0.00001f);

            float distanceSqr = Mathf.Max(rayDirectionMag.sqrMagnitude, 0.00001f);
            float rangeAttenuation = Mathf.Sqrt(Mathf.Min(1.0f - Mathf.Sqrt(distanceSqr * inverseSquareRange), 1));

            float innerCos = Mathf.Cos(Mathf.Deg2Rad * 0.5f * light.innerSpotAngle);
            float angleRangeInv = 1f / Mathf.Max(innerCos - outerCos, 0.001f);
            float spotAnglesInner = angleRangeInv;
            float spotAnglesOuter = -outerCos * angleRangeInv;

            float spotAttenuation = Mathf.Sqrt(Mathf.Clamp01(forwardDotRay * spotAnglesInner + spotAnglesOuter));
            float attenuation = spotAttenuation * (rangeAttenuation / distanceSqr);

            attenuation = Mathf.Min(1, attenuation);

            float lightIntensity = light.intensity;
            if (perceivedBrightness)
            {
                float adjustedColorInt =
                    (light.color.r * RedCoef) + (light.color.g * GreenCoef) + (light.color.b * BlueCoef);
                lightIntensity = lightIntensity * adjustedColorInt;
            }

            return attenuation * lightIntensity;
        }

        private float SampleLightProbes(Vector3 samplePosition)
        {
            LightProbes.GetInterpolatedProbe(samplePosition, null, out harmonics);
            float lightLevel;

            float r = harmonics[0, 0];
            float g = harmonics[1, 0];
            float b = harmonics[2, 0];

            if (perceivedBrightness)
            {
                lightLevel = (RedCoef * r) + (GreenCoef * g) + (BlueCoef * b);
            }
            else
            {
                lightLevel = r + g + b;
            }

            float lightLevelAdjusted = lightLevel * bakedLightContribution;

            if (lightAdjustmentMode == LightAdjustmentMode.Gamma)
            {
                lightLevelAdjusted = Mathf.LinearToGammaSpace(lightLevelAdjusted);
            }

            return lightLevelAdjusted;
        }

        private float SampleLightmap(Vector3 samplePosition)
        {
            if (Physics.Raycast(samplePosition, -transform.up, out RaycastHit hit, Mathf.Infinity, lightRaycastMask))
            {
                Renderer colliderRenderer = hit.collider.GetComponent<Renderer>();
                if (colliderRenderer == null)
                {
                    return 0;
                }

                LightmapData lightMapInfo = LightmapSettings.lightmaps[colliderRenderer.lightmapIndex];
                Texture2D tex = lightMapInfo.lightmapColor;
                Vector2 pixelUV = hit.lightmapCoord;
                Color surfaceColor = tex.GetPixelBilinear(pixelUV.x, pixelUV.y);
                
                float r = Mathf.LinearToGammaSpace(surfaceColor.r);
                float g = Mathf.LinearToGammaSpace(surfaceColor.g);
                float b = Mathf.LinearToGammaSpace(surfaceColor.b);
                
                float grayscale;

                if (perceivedBrightness)
                {
                    grayscale = (r * RedCoef) + (g * GreenCoef) + (b * BlueCoef);
                }
                else
                {
                    grayscale = r + g + b;
                }

                return grayscale * bakedLightContribution;
            }

            return 0;
        }

#if UNITY_EDITOR
        public void GetAllRealtimeSceneLights()
        {
            lights.Clear();
            var sceneLights = FindObjectsByType<Light>(FindObjectsInactive.Include, FindObjectsSortMode.None);

            foreach (Light light in sceneLights)
            {
                if (light.lightmapBakeType != LightmapBakeType.Realtime &&
                    light.lightmapBakeType != LightmapBakeType.Mixed)
                {
                    continue;
                }

                lights.Add(light);
            }
        }
#endif
    }
}