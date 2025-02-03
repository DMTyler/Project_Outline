using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DGraphics.PostProcessing.Outline
{
    public class OutlinePostProcessingRendererFeature : ScriptableRendererFeature
    {
        private OutlineRenderPass _outlineRenderPass;
        public struct ShaderTag
        {
            public static int OutlineInfo = Shader.PropertyToID("_OutlineInfo");
            public static int BlitTemp = Shader.PropertyToID("_BlitTemp");
            public static int DepthBuffer = Shader.PropertyToID("_DepthBuffer");
            public static int OutlineColors = Shader.PropertyToID("_OutlineColors");
            public static int OutlineParams = Shader.PropertyToID("_OutlineParams");
            public static int Width = Shader.PropertyToID("_Width");
            public static int Height = Shader.PropertyToID("_Height");
        }
        private class OutlineRenderPass : ScriptableRenderPass
        {
            private ComputeBuffer _outlineColors;
            private ComputeBuffer _outlineParams;
            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
            {
                var camera = renderingData.cameraData.camera;
                if (camera.cameraType != CameraType.Game) return;
                if (!camera.TryGetCullingParameters(out var cullingParams)) return;
                var cmd = CommandBufferPool.Get("OutlineInfo");
                
                var width = renderingData.cameraData.camera.scaledPixelWidth;
                var height = renderingData.cameraData.camera.scaledPixelHeight;
                var material = new Material(Shader.Find("Hidden/DGraphics/OutlinePP"));
                if (material == null) return;
                
                if (!OutlineController.TryGetShaderBuffers(
                        out _outlineColors, 
                        out _outlineParams)
                    ) 
                    return;
                
                material.SetBuffer(ShaderTag.OutlineColors, _outlineColors);
                material.SetBuffer(ShaderTag.OutlineParams, _outlineParams);
                
                var originalColorHandle = renderingData.cameraData.renderer.cameraColorTargetHandle; 
                var rt = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGBFloat);
                rt.name = "MaterialBuffer";
                
                try
                {
                    cmd.GetTemporaryRT(ShaderTag.DepthBuffer, width, height, 24, FilterMode.Point, RenderTextureFormat.Depth,
                        RenderTextureReadWrite.Default);
                    cmd.GetTemporaryRT(ShaderTag.OutlineInfo, width, height, 0, FilterMode.Bilinear, 
                        RenderTextureFormat.ARGBFloat);
                    cmd.SetRenderTarget(color:ShaderTag.OutlineInfo, depth:ShaderTag.DepthBuffer);
                    cmd.ClearRenderTarget(true, true, Color.clear);
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                    
                    var drawSettings = CreateDrawingSettings(new ShaderTagId("DepthOnly"), ref renderingData, SortingCriteria.CommonOpaque);
                    var filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
                    context.DrawRenderers(renderingData.cullResults, ref drawSettings, ref filteringSettings);
                    
                    cmd.SetRenderTarget(color:ShaderTag.OutlineInfo, depth:ShaderTag.DepthBuffer);
                    cmd.ClearRenderTarget(false, true, Color.clear);
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();

                    var cullingResult = context.Cull(ref cullingParams);
                    drawSettings = CreateDrawingSettings(new ShaderTagId("OutlineInfo"), ref renderingData, SortingCriteria.CommonOpaque);
                    filteringSettings = new FilteringSettings(RenderQueueRange.opaque);
                    context.DrawRenderers(cullingResult, ref drawSettings, ref filteringSettings);
                    
                    material.SetInt(ShaderTag.Width, width);
                    material.SetInt(ShaderTag.Height, height);
                    material.SetTexture(ShaderTag.OutlineInfo, rt);
                    
                    cmd.GetTemporaryRT(ShaderTag.BlitTemp, width, height, 0, FilterMode.Point, RenderTextureFormat.ARGBFloat);
                    cmd.Blit(ShaderTag.OutlineInfo, rt);
                    cmd.Blit(originalColorHandle, ShaderTag.BlitTemp, material);
                    cmd.Blit(ShaderTag.BlitTemp, originalColorHandle);
                    cmd.SetRenderTarget(originalColorHandle);
                    cmd.ReleaseTemporaryRT(ShaderTag.OutlineInfo);
                    context.ExecuteCommandBuffer(cmd);
                    cmd.Clear();
                    
                    context.Submit();
                }
                finally
                {
                    CommandBufferPool.Release(cmd);
                    RenderTexture.ReleaseTemporary(rt);
                }
            }

            public override void OnCameraCleanup(CommandBuffer cmd)
            {
                _outlineColors?.Release();
                _outlineParams?.Release();
            }
        }
        
        public override void Create()
        {
            _outlineRenderPass = new OutlineRenderPass();
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
        {
            if (_outlineRenderPass == null) return;
            renderer.EnqueuePass(_outlineRenderPass);
            _outlineRenderPass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;
        }
    }
}