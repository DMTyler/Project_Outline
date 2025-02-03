using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

namespace DGraphics.PostProcessing.Outline
{
    [VolumeComponentMenu("DGraphics/Post-Processing/Outline")]
    public class OutlinePostProcessingVolume : VolumeComponent, IPostProcessComponent
    {
        public ColorParameter OutlineColor = new ColorParameter(Color.white);
        public FloatParameter OutlineWidth = new FloatParameter(0f);
        public FloatParameter Threshold = new FloatParameter(0.5f);
        
        public FloatParameter NormalStrength = new FloatParameter(0.5f);
        public FloatParameter DepthStrength = new FloatParameter(0.01f);
        
        public bool IsActive() => OutlineWidth.value > 0f;

        public bool IsTileCompatible() => false;
    }
}