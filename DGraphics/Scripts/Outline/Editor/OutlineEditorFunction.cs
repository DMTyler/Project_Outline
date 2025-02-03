#if UNITY_EDITOR

using System.Collections.Generic;
using System.Reflection;
using UnityEditor;
using UnityEditor.Rendering.Universal;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;


namespace DGraphics.PostProcessing.Outline
{
    public class OutlineEditorFunction
    {
        [MenuItem("Tools/DGraphics/Setup Outline")]
        private static void SetupOutline()
        {
            // 1. Add the OutlinePP shader to the Graphics Settings
            AddShaderToGraphicsSettings();
            
            // 2. Add RenderFeature to the Graphics Settings
            AddRenderFeatureToGraphicsSettings();
            
            // 3. Add OutlineSettings to the Scene
            AddOutlineSettingsToScene();
        }
        private static void AddShaderToGraphicsSettings()
        {
            var shaderToAdd = Shader.Find("Hidden/DGraphics/OutlinePP");
            if (shaderToAdd == null)
            {
                Debug.LogError("Shader OutlinePP. Package may corrupted.");
                return;
            }
            
            var graphicsSettings = new SerializedObject(GraphicsSettings.GetGraphicsSettings());
            var alwaysIncludedShaders = graphicsSettings.FindProperty("m_AlwaysIncludedShaders");
            
            foreach (SerializedProperty shaderProp in alwaysIncludedShaders)
            {
                if (shaderProp.objectReferenceValue == shaderToAdd)
                {
                    return;
                }
            }
            
            alwaysIncludedShaders.arraySize++;
            var newShaderProp = alwaysIncludedShaders.GetArrayElementAtIndex(alwaysIncludedShaders.arraySize - 1);
            newShaderProp.objectReferenceValue = shaderToAdd;
            
            graphicsSettings.ApplyModifiedProperties();
        }
        private static void AddRenderFeatureToGraphicsSettings()
        {
            var pipelineAsset = GraphicsSettings.renderPipelineAsset as UniversalRenderPipelineAsset;
            if (pipelineAsset == null)
            {
                Debug.LogError("Current Graphics Settings is not using Universal Render Pipeline (URP). \n" +
                               "Outline feature is only available under URP.");
                return;
            }
            
            // Get the default renderer data
            var indexInfo = typeof(UniversalRenderPipelineAsset)
                .GetField("m_DefaultRendererIndex", BindingFlags.NonPublic | BindingFlags.Instance);
            if (indexInfo == null)
            {
                Debug.LogError("Cannot Retrieve Default Renderer in UniversalRenderPipelineAsset.");
                return;
            }
            
            var rendererIndex = (int)indexInfo.GetValue(pipelineAsset);
            
            var rendererDataListInfo = typeof(UniversalRenderPipelineAsset)
                .GetField("m_RendererDataList", BindingFlags.NonPublic | BindingFlags.Instance);
            if (rendererDataListInfo == null)
            {
                Debug.LogError("Cannot Retrieve Renderer Data List in UniversalRenderPipelineAsset.");
                return;
            }

            var rendererDataList = rendererDataListInfo.GetValue(pipelineAsset) as ScriptableRendererData[];
            if (rendererDataList == null)
            {
                Debug.LogError("Cannot Retrieve Renderer Data List in UniversalRenderPipelineAsset.");
                return;
            }
            
            var rendererData = rendererDataList[rendererIndex] as UniversalRendererData;
            if (rendererData == null)
            {
                Debug.LogError("Cannot Retrieve Default Renderer Data in UniversalRenderPipelineAsset.");
                return;
            }
            
            /*var rendererDataSo = new SerializedObject(rendererData);*/
            
            var rendererFeaturesInfo = typeof(UniversalRendererData)
                .GetField("m_RendererFeatures", BindingFlags.NonPublic | BindingFlags.Instance);
            if (rendererFeaturesInfo == null)
            {
                Debug.LogError("Cannot Retrieve Renderer Features in UniversalRendererData.");
                return;
            }

            var rendererFeatures = rendererFeaturesInfo.GetValue(rendererData) as List<ScriptableRendererFeature>;
            if (rendererFeatures == null)
            {
                Debug.LogError("Cannot Retrieve Renderer Features in UniversalRendererData.");
                return;
            }
            
            var rendererFeatureMapInfo = typeof(UniversalRendererData)
                .GetField("m_RendererFeatureMap", BindingFlags.NonPublic | BindingFlags.Instance);
            if (rendererFeatureMapInfo == null)
            {
                Debug.LogError("Cannot Retrieve Renderer Feature Map in UniversalRendererData.");
                return;
                
            }
            
            var rendererFeatureMap = rendererFeatureMapInfo.GetValue(rendererData) as List<long>;
            if (rendererFeatureMap == null)
            {
                Debug.LogError("Cannot Retrieve Renderer Feature Map in UniversalRendererData.");
                return;
            }
            
            foreach (var feature in rendererFeatures)
            {
                if (feature.GetType() == typeof(OutlinePostProcessingRendererFeature))
                {
                    return;
                }
            }

            var outlineFeature = ScriptableObject.CreateInstance<OutlinePostProcessingRendererFeature>();
            outlineFeature.name = "Outline PostProcessing Renderer Feature";
            Undo.RegisterCreatedObjectUndo(outlineFeature, "Add Outline PostProcessing Renderer Feature");
            if (EditorUtility.IsPersistent(rendererData))
            {
                AssetDatabase.AddObjectToAsset(outlineFeature, rendererData);
            }
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(rendererData, out var guid, out long localId);
            
            var editor = Editor.CreateEditor(rendererData);
            var rendererFeaturesInEditorInfo = typeof(ScriptableRendererDataEditor)
                .GetField("m_RendererFeatures", BindingFlags.NonPublic | BindingFlags.Instance);
            if (rendererFeaturesInEditorInfo == null)
            {
                Debug.LogError("Cannot Retrieve Renderer Features in Editor in UniversalRendererData.");
                return;
            }
            
            var rendererFeaturesInEditor = editor.serializedObject.FindProperty("m_RendererFeatures");
            rendererFeaturesInEditorInfo.SetValue(editor, rendererFeaturesInEditor);
            
            var rendererFeatureMapInEditorInfo = typeof(ScriptableRendererDataEditor)
                .GetField("m_RendererFeaturesMap", BindingFlags.NonPublic | BindingFlags.Instance);
            if (rendererFeatureMapInEditorInfo == null)
            {
                Debug.LogError("Cannot Retrieve Renderer Features Map in Editor in UniversalRendererData.");
                return;
            }
            
            var rendererFeatureMapInEditor = editor.serializedObject.FindProperty("m_RendererFeatureMap");
            rendererFeatureMapInEditorInfo.SetValue(editor, rendererFeatureMapInEditor);
            
            var addComponentMethodInfo = editor.GetType()
                .GetMethod("AddComponent", BindingFlags.NonPublic | BindingFlags.Instance);
            if (addComponentMethodInfo == null)
            {
                Debug.LogError("Cannot Retrieve AddComponent method in UniversalRendererData Editor.");
                return;
            }
            addComponentMethodInfo.Invoke(editor, new object[] {nameof(OutlinePostProcessingRendererFeature)});
        }
        private static void AddOutlineSettingsToScene()
        {
            var outlineSettings = Object.FindObjectOfType<OutlineSettings>();
            if (outlineSettings != null)
            {
                return;
            }
            
            var go = new GameObject("[DO NOT DELETE] Outline Settings");
            go.AddComponent<OutlineSettings>();
            Undo.RegisterCreatedObjectUndo(go, "Add Outline Settings");
        }
    }
}

#endif
