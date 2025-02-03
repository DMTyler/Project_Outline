using System;
using DGraphics.Inspector;
using UnityEditor;
using UnityEngine;

namespace DGraphics.PostProcessing.Outline
{
    [ExecuteAlways]
    public class OutlineSetter : MonoBehaviour, IDisposable
    {
        [SerializeField, Min(1)] 
        public int Index = 1;
        [SerializeField, ReadOnly] 
        private bool _active = false;
        [SerializeField, ReadOnly]
        private string _originalShaderName = null;
        public OutlineInfo OutlineInfo = new();
        
        private MaterialPropertyBlock _propertyBlock = null;
        
        private static readonly int _DG_AUTOGEN_OUTLINE_INDEX_ = Shader.PropertyToID("_DG_AUTOGEN_OUTLINE_INDEX_");
        private void Reset()
        {
            Init();
        }

        private void Awake()
        {
#if UNITY_EDITOR
            if (Application.isPlaying) 
                ForceInit();
#else
            Init();
#endif
        }
        
        private void OnDestroy()
        {
            Dispose();
        }

        private void OnEnable()
        {
#if UNITY_EDITOR
            if (!EditorApplication.isPlaying && EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (Application.isPlaying)
            {
                Init();
                return;
            }
            
            // Delay to avoid issues with OnEnable being called before the inspector is fully initialized.
            EditorApplication.delayCall += Init;
#else
            Init();
#endif
        }
        private void OnDisable()
        {
            Dispose();
        }

        public void Dispose()
        {
            if (!_active) return;
#if UNITY_EDITOR
            OutlineController.RevertGameObjectShader(gameObject);
#endif
            OutlineController.UnregisterInfo(this);
            _active = false;
        }
        
        public void UpdateIndex(int index)
        {
            var renderer = GetComponent<Renderer>();
            if (renderer == null) return;
            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }
            _propertyBlock.SetInt(_DG_AUTOGEN_OUTLINE_INDEX_, index);
            renderer.SetPropertyBlock(_propertyBlock);
            Index = index;
        }

        private void ForceInit()
        {
            if (_propertyBlock == null)
            {
                _propertyBlock = new MaterialPropertyBlock();
            }
            
            if (_originalShaderName == null)
            {
                var renderer = GetComponent<Renderer>();
                if (renderer == null) return;
                var material = renderer.sharedMaterial;
                if (material == null) return;
                _originalShaderName = material.shader.name;
            }
            
            
            
#if UNITY_EDITOR
            InitInEditor();
#else
            InitInRuntime();
#endif
            _active = true;
        }

        public void Init()
        {
            if (_active) return;
            ForceInit();
        }

        private void InitInRuntime()
        {
            OutlineController.RegisterInfo(this);
            Debug.Log("OutlineSetter.InitInRuntime");
        }
        
#if UNITY_EDITOR
        private void InitInEditor()
        {
            if (!OutlineController.RegisterInfo(this)) return;
            OutlineController.SetupGameObjectShader(gameObject);
        }
#endif
    }

    [Serializable]
    public class OutlineInfo
    {
        [ColorUsage(true, true)]
        public Color OutlineColor = Color.black;
        public float OutlineWidth = 1;
        public float Threshold = 0.5f;
        public float NormalStrength = 1;
        public float DepthStrength = 1;
    }
}