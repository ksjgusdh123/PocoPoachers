// 더 이상 사용하지 않습니다. FogOfWarRenderer의 InitWallStencils()가 대신 처리합니다.
// PC_Renderer.asset에서 이 Feature를 제거한 뒤 이 파일을 삭제해도 됩니다.
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.Rendering.RenderGraphModule;

public class WallStencilFeature : ScriptableRendererFeature
{
    private class EmptyPass : ScriptableRenderPass
    {
        public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) { }
    }

    private EmptyPass _pass;
    public override void Create() => _pass = new EmptyPass();
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) { }
}
