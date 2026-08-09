using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

// 카메라 컬러를 축소·블러해 전역 텍스처 _UIBlurTex로 노출한다.
// Custom/UI-BlurBackdrop 셰이더를 쓰는 UI가 이 텍스처를 화면 좌표로 샘플해 뒷배경으로 깐다.
// UIRealtimeBackdropBlur가 하나도 켜져 있지 않으면 패스 자체를 등록하지 않는다.
public class UIBlurFeature : ScriptableRendererFeature
{
    [SerializeField, Tooltip("Hidden/UIBlurBlit")]
    private Shader _blurShader;

    [SerializeField, Range(1, 8), Tooltip("해상도 축소 배율. 클수록 싸고 더 뿌옇다.")]
    private int _downSample = 4;

    [SerializeField, Range(1, 4), Tooltip("가로+세로 블러 반복 횟수")]
    private int _iterations = 2;

    [SerializeField, Range(0.5f, 4f), Tooltip("한 번의 블러가 훑는 픽셀 간격")]
    private float _blurSize = 2f;

    private Material _material;
    private UIBlurPass _pass;

    public override void Create()
    {
        if (_blurShader == null) return;

        _material = CoreUtils.CreateEngineMaterial(_blurShader);
        _pass = new UIBlurPass(_material)
        {
            renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing
        };
    }

    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        if (_pass == null) return;
        if (!UIRealtimeBackdropBlur.AnyActive) return;
        if (renderingData.cameraData.cameraType != CameraType.Game) return;

        _pass.Setup(_downSample, _iterations, _blurSize);
        renderer.EnqueuePass(_pass);
    }

    protected override void Dispose(bool disposing)
    {
        CoreUtils.Destroy(_material);
        _material = null;
        _pass = null;
    }

    private class UIBlurPass : ScriptableRenderPass
    {
        private static readonly int BlurTexId = Shader.PropertyToID("_UIBlurTex");
        private static readonly int BlurSizeId = Shader.PropertyToID("_BlurSize");

        private readonly Material _material;
        private int _downSample;
        private int _iterations;

        private class PassData
        {
            public TextureHandle Source;
            public Material Material;
        }

        public UIBlurPass(Material material)
        {
            _material = material;

            // 이게 없으면 URP가 백버퍼에 직접 그릴 때 카메라 컬러를 텍스처로 읽을 수 없다.
            requiresIntermediateTexture = true;
        }

        public void Setup(int downSample, int iterations, float blurSize)
        {
            _downSample = downSample;
            _iterations = iterations;
            _material.SetFloat(BlurSizeId, blurSize);
        }

        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData)
        {
            var resourceData = frameData.Get<UniversalResourceData>();
            var cameraData = frameData.Get<UniversalCameraData>();

            // 백버퍼가 활성 타깃이면 컬러를 텍스처로 읽을 수 없다.
            if (resourceData.isActiveTargetBackBuffer) return;

            var desc = cameraData.cameraTargetDescriptor;
            desc.width = Mathf.Max(1, desc.width / _downSample);
            desc.height = Mathf.Max(1, desc.height / _downSample);
            desc.depthBufferBits = 0;
            desc.msaaSamples = 1;

            TextureHandle source = resourceData.activeColorTexture;

            for (int i = 0; i < _iterations; i++)
            {
                TextureHandle horizontal = UniversalRenderer.CreateRenderGraphTexture(
                    renderGraph, desc, $"_UIBlurH{i}", false, FilterMode.Bilinear, TextureWrapMode.Clamp);
                TextureHandle vertical = UniversalRenderer.CreateRenderGraphTexture(
                    renderGraph, desc, $"_UIBlurV{i}", false, FilterMode.Bilinear, TextureWrapMode.Clamp);

                renderGraph.AddBlitPass(
                    new RenderGraphUtils.BlitMaterialParameters(source, horizontal, _material, 0),
                    "UI Blur Horizontal");

                bool isLast = i == _iterations - 1;
                if (!isLast)
                {
                    renderGraph.AddBlitPass(
                        new RenderGraphUtils.BlitMaterialParameters(horizontal, vertical, _material, 1),
                        "UI Blur Vertical");

                    source = vertical;
                    continue;
                }

                // 마지막 세로 패스만 직접 기록한다. AddBlitPass로는 전역 텍스처 등록을 걸 수 없다.
                using var builder = renderGraph.AddRasterRenderPass<PassData>("UI Blur Publish", out var passData);

                passData.Source = horizontal;
                passData.Material = _material;

                builder.UseTexture(horizontal);
                builder.SetRenderAttachment(vertical, 0);
                builder.SetGlobalTextureAfterPass(vertical, BlurTexId);
                builder.AllowPassCulling(false);
                builder.SetRenderFunc((PassData data, RasterGraphContext context) =>
                    Blitter.BlitTexture(context.cmd, data.Source, new Vector4(1f, 1f, 0f, 0f), data.Material, 1));
            }
        }
    }
}
