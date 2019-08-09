using UnityEngine;

public class OperateOnTex : MonoBehaviour
{
    public ComputeShader cmpShader;
    public Texture2D tex;

    [Range(0, 30)]
    public float DistanceX;

    private RenderTexture m_resultRT;
    private RenderTexture m_srcRT;
    void Start()
    {
        if (cmpShader == null || tex == null)
        {
            enabled = false;
            return;
        }

        Init();
        LaunchComputeShader();
    }

    private void Init()
    {
        m_srcRT = RenderTexture.GetTemporary(1024, 1024, 24, RenderTextureFormat.ARGB32);
        m_srcRT.enableRandomWrite = true;
        m_srcRT.Create();

        Graphics.Blit(tex, m_srcRT);

        m_resultRT = RenderTexture.GetTemporary(1024, 1024, 24, RenderTextureFormat.ARGB32);
        m_resultRT.enableRandomWrite = true;
        m_resultRT.Create();
    }

    private void LaunchComputeShader()
    {
        int kernelIndex = cmpShader.FindKernel("CSMain_TexBlur");
        if (kernelIndex == -1)
        {
            return;
        }

        uint groupSizeX = 0;
        uint groupSizeY = 0;
        uint groupSizeZ = 0;
        cmpShader.GetKernelThreadGroupSizes(kernelIndex, out groupSizeX, out groupSizeY, out groupSizeZ);

        const int GroupX = 32;
        const int GroupY = 32;
        int TOTAL_SIZE = GroupX * GroupY * (int)groupSizeX * (int)groupSizeY;
        
        cmpShader.SetTexture(kernelIndex, "_ResultTex", m_resultRT);
        cmpShader.SetTexture(kernelIndex, "_SrcTex", m_srcRT);
        cmpShader.SetFloat("_Distance", DistanceX);
        cmpShader.SetFloats("_Center", new float[]{ 0.5f, 0.5f});

        // dispatch //
        cmpShader.Dispatch(kernelIndex, GroupX, GroupY, 1);
    }

    void OnGUI()
    {
        GUI.DrawTexture(new Rect(100, 100, 400, 400), m_resultRT);
    }
}