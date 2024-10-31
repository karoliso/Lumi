using UnityEngine;
using UnityEditor;

namespace Lumi
{
    [CustomEditor(typeof(LightDetector))]
    public class YourScriptClassNameInspector : Editor
    {
        private Texture2D customIcon;

        private LightDetector lightDetector;

        SerializedProperty samplePoints;
        SerializedProperty lights;
        SerializedProperty lightRaycastMask;
        SerializedProperty perceivedBrightness;
        SerializedProperty lightSampleEvaluationMode;
        SerializedProperty lightAdjustmentMode;
        SerializedProperty bakedLightSampleMode;
        SerializedProperty bakedLightContribution;
        SerializedProperty runInEditor;
        SerializedProperty drawSamplePointGizmos;
        SerializedProperty samplePointGizmoSize;

        private void OnEnable()
        {
            lightDetector = (LightDetector)target;

            samplePoints = serializedObject.FindProperty("samplePoints");
            lights = serializedObject.FindProperty("lights");
            lightRaycastMask = serializedObject.FindProperty("lightRaycastMask");
            perceivedBrightness = serializedObject.FindProperty("perceivedBrightness");
            lightSampleEvaluationMode = serializedObject.FindProperty("lightSampleEvaluationMode");
            lightAdjustmentMode = serializedObject.FindProperty("lightAdjustmentMode");
            bakedLightSampleMode = serializedObject.FindProperty("bakedLightSampleMode");
            bakedLightContribution = serializedObject.FindProperty("bakedLightContribution");
            runInEditor = serializedObject.FindProperty("runInEditor");
            drawSamplePointGizmos = serializedObject.FindProperty("drawSamplePointGizmos");
            samplePointGizmoSize = serializedObject.FindProperty("samplePointGizmoSize");

            EditorApplication.update += OnEditorUpdate;

            customIcon =
                AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Lumi/Textures/LightDetectorIcon.png");
            EditorGUIUtility.SetIconForObject(target, customIcon);
        }

        private void OnDisable()
        {
            EditorApplication.update -= OnEditorUpdate;
        }

        private void OnEditorUpdate()
        {
            if (lightDetector.runInEditor || Application.isPlaying)
            {
                Repaint();
            }
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(samplePoints, new GUIContent("Sample Points"), true);
            EditorGUILayout.PropertyField(lights, new GUIContent("Lights"), true);

            if (GUILayout.Button("Get All Realtime Scene Lights"))
            {
                lightDetector.GetAllRealtimeSceneLights();
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Settings", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(lightRaycastMask,
                new GUIContent("Light Raycast Mask", "Layers that light raycasting is done against."));
            EditorGUILayout.PropertyField(perceivedBrightness,
                new GUIContent("Perceived Brightness",
                    "Sampled light amount is adjusted based on colour - a green light will produce a higher intensity than a blue light at the same sample point."));
            EditorGUILayout.PropertyField(lightSampleEvaluationMode,
                new GUIContent("Light Sample Evaluation Mode",
                    "Average - averages the intensity of all sample points.\nMax - picks the highest intensity of all sample points."));
            EditorGUILayout.PropertyField(lightAdjustmentMode,
                new GUIContent("Light Adjustment Mode",
                    "None - No adjustments to sampled light amount.\nGamma - Converts sampled light amount from linear to gamma (sRGB) color space."));
            EditorGUILayout.PropertyField(bakedLightSampleMode,
                new GUIContent("Baked Light Sample Mode",
                    "None - Baked lighting will not be sampled.\nLight Probes - Baked lighting will be sampled from Light Probes.\nLightmap - Baked lighting will be sampled from a lightmap underneath a sample point."));
            if (bakedLightSampleMode.enumValueIndex == (int)LightDetector.BakedLightSampleMode.Lightmap)
            {
                EditorGUILayout.HelpBox(
                    "Warning: Lightmap sampling can be expensive and cause memory (GC) allocations.\nLightmap textures must be made readable in the Texture Import Settings.",
                    MessageType.Warning);
            }

            if (bakedLightSampleMode.enumValueIndex == (int)LightDetector.BakedLightSampleMode.LightProbes)
            {
                EditorGUILayout.PropertyField(bakedLightContribution, new GUIContent("Baked Light Contribution"));
            }

            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(runInEditor, new GUIContent("Run in Editor"));
            EditorGUILayout.PropertyField(drawSamplePointGizmos, new GUIContent("Draw Sample Point Gizmos"));

            if (drawSamplePointGizmos.boolValue)
            {
                EditorGUILayout.PropertyField(samplePointGizmoSize, new GUIContent("Sample Point Gizmo Size"));
            }

            if (lightDetector.runInEditor || Application.isPlaying)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Output", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("Sampled Light Amount", lightDetector.SampledLightAmount.ToString("F2"));
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}