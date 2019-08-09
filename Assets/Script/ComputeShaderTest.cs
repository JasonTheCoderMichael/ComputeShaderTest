using System;
using UnityEngine;
using UnityEngine.Profiling;
using Random = UnityEngine.Random;

public class ComputeShaderTest : MonoBehaviour
{
    public ComputeShader computeShader;
    public EMethod method;
    public Transform prefab;

    // KernelName //
    private const string KernelName_Texture = "CSMain_Texture";
    private const string KernelName_Buffer = "CSMain_Buffer";

    // 方式1要用到的变量 //
    private RenderTexture m_rt;
    private const int Width = 1024;
    private const int Height = 1024;
    private Material m_material;
    private Transform m_object;

    // 方式2要用到的变量 //
    private const int MaxObjectNum = 100;
    private ComputeBuffer m_comBuffer;
    private DataStruct[] m_dataArr;
    private Transform[] m_objArr;
    private Material[] m_materialArr;

    public enum EMethod : int
    {
        RenderTexture = 0,                              // 方式1: 使用 RenderTexture 来存储结算结果 //
        ComputerBuffer = 1,                             // 方式2: 使用 ComputeBuffer 来存储结算结果 //
    }

    struct DataStruct
    {
        public Vector4 pos;
        public Vector3 scale;
        public Vector3 eulerAngle;
        public Matrix4x4 matrix;
    }

    private const int Stride = sizeof(float) * (4 + 3 + 3 + 16);

    void Start()
    {
        switch (method)
        {
            case EMethod.RenderTexture:
                m_object = Instantiate(prefab);
                m_object.position = Vector3.zero;
                m_object.localScale = Vector3.one*5;
                MeshRenderer render = m_object.GetComponent<MeshRenderer>();
                if (render != null)
                {
                    m_material = render.material;
                }
                break;

            case EMethod.ComputerBuffer:
                GameObject parent = new GameObject("Parent");
                parent.transform.position = Vector3.zero;
                // 初始化物体数组 //
                m_objArr = new Transform[MaxObjectNum];
                for (int i = 0; i < MaxObjectNum; i++)
                {
                    Transform obj = Instantiate(prefab);
                    obj.transform.SetParent(parent.transform);
                    obj.transform.localPosition = Vector3.zero;
                    obj.transform.localScale = Vector3.one;
                    m_objArr[i] = obj;
                }
                break;
        }
        
        //uint x = 0;
        //uint y = 0;
        //uint z = 0;
        //// 获取的是shader文件中的数值, 即 [numthreads(X, X, X)] 中的数值 //
        //computeShader.GetKernelThreadGroupSizes(kernelIndex, out x, out y, out z);
        //Debug.LogFormat("x = {0}, y = {1}, z = {2}", x, y, z);
    }

    void OnGUI()
    {
        if (GUI.Button(new Rect(0, 0, 200, 100), "Dispatch"))
        {
            Dispach();
        }

        if (GUI.Button(new Rect(200, 0, 200, 100), "Get Result"))
        {
            GetResult();
            //GetResultAsync();
        }
    }

    void Dispach()
    {
        if (computeShader == null)
        {
            return;
        }

        int kernelIndex = -1;
        try
        {
            kernelIndex = computeShader.FindKernel(GetKernelName(method));
        }
        catch (Exception error)
        {
            Debug.LogFormat("Error: {0}", error.Message);
            return;
        }

        switch (method)
        {
            case EMethod.RenderTexture:
                if (m_rt != null)
                {
                    Destroy(m_rt);
                    m_rt = null;
                }

                m_rt = new RenderTexture(Width, Height, 0, RenderTextureFormat.ARGB32);
                m_rt.enableRandomWrite = true;
                m_rt.Create();
                computeShader.SetTexture(kernelIndex, "ResultTex", m_rt);
                // 在Shader中需要用到X维和Y维的数据作为坐标去读取和设置Texture2D的像素，因此需要给X维和Y维的thread group设置数值，Z维的thread group数量为1即可 //
                computeShader.Dispatch(kernelIndex, 32, 32, 1);
                break;

            case EMethod.ComputerBuffer:
                if (m_comBuffer != null)
                {
                    m_comBuffer.Release();
                }

                // 初始化m_dataArr //
                InitDataArr();

                m_comBuffer = new ComputeBuffer(m_dataArr.Length, sizeof(float) * Stride);
                m_comBuffer.SetData(m_dataArr);
                computeShader.SetBuffer(kernelIndex, "ResultBuffer", m_comBuffer);

                // 在Shader中只需要用到X维的数据作为数组索引，因此只需要给X维的thread group设置数值，Y维和Z维的thread group数量为1即可 //
                computeShader.Dispatch(kernelIndex, 32, 1, 1);
                break;
        }
    }

    void GetResult()
    {
        switch (method)
        {
            case EMethod.RenderTexture:
                //GameUtils.Instance().SaveToPng(m_rt, "test.png");
                m_material.SetTexture("_MainTex", m_rt);
                break;

            case EMethod.ComputerBuffer:
                if (m_comBuffer == null || 
                    m_objArr == null || m_objArr.Length != MaxObjectNum ||
                    m_dataArr == null || m_dataArr.Length != MaxObjectNum)
                {
                    break;
                }

                Profiler.BeginSample("GetDataFromGPU");
                m_comBuffer.GetData(m_dataArr);
                Profiler.EndSample();

                // 根据计算结果设置物体位置 //
                for (int i = 0; i < MaxObjectNum; i++)
                {
                    m_objArr[i].localPosition = m_dataArr[i].pos;
                    m_objArr[i].eulerAngles = m_dataArr[i].eulerAngle;
                    m_objArr[i].localScale = m_dataArr[i].scale;
                }
                break;
        }
    }

    void Callback()
    {
        // 根据计算结果设置物体位置 //
        for (int i = 0; i < MaxObjectNum; i++)
        {
            m_objArr[i].localPosition = m_dataArr[i].pos;
            m_objArr[i].localScale = m_dataArr[i].scale;
        }
    }

    //private AsyncGPUReadbackRequest m_request;
    //private bool m_processed = false;
    //void GetResultAsync()
    //{
    //    switch (method)
    //    {
    //        case EMethod.ComputerBuffer:
    //            if (m_comBuffer == null || 
    //                m_objArr == null ||
    //                m_dataArr == null)
    //            {
    //                break;
    //            }
    //            m_processed = false;
    //            m_request = AsyncGPUReadback.Request(m_comBuffer, m_dataArr.Length * Stride, 0);                
    //            break;
    //    }
    //}

    //void Update()
    //{
    //    if (!m_processed && m_request.done && !m_request.hasError)
    //    {
    //        m_processed = true;

    //        Profiler.BeginSample("GetDataFromGPU");

    //        // 方式2 //
    //        //m_dataArr = null;
    //        m_request.GetData<DataStruct>(0).CopyTo(m_dataArr);

    //        //// 方式1 //
    //        ////m_dataArr = null;
    //        //m_dataArr = m_request.GetData<DataStruct>(0).ToArray();

    //        Profiler.EndSample();

    //        Callback();
    //    }
    //}

    // 初始化传给GPU的数据 //
    void InitDataArr()
    {
        if (m_dataArr == null)
        {
            m_dataArr = new DataStruct[MaxObjectNum];
        }

        const int PosRange = 10;
        for (int i = 0; i < MaxObjectNum; i++)
        {
            m_dataArr[i].pos = new Vector4(0, 0, 0, 1);
            m_dataArr[i].scale = Vector3.one;

            Matrix4x4 matrix = Matrix4x4.identity;

            // 位移信息 //
            matrix.m03 = (Random.value * 2 - 1) * PosRange;
            matrix.m13 = (Random.value * 2 - 1) * PosRange;
            matrix.m23 = (Random.value * 2 - 1) * PosRange;

            // 缩放信息 //
            matrix.m00 = Random.value * 2 + 1;              // 从[0,1]映射到[1,3] //
            matrix.m11 = Random.value * 2 + 1;
            matrix.m22 = Random.value * 2 + 1;

            m_dataArr[i].matrix = matrix;
        }
    }

    string GetKernelName(EMethod method)
    {
        string kernelName = "";
        switch (method)
        {
            case EMethod.RenderTexture:
                kernelName = KernelName_Texture;
                break;
            case EMethod.ComputerBuffer:
                kernelName = KernelName_Buffer;
                break;
        }
        return kernelName;
    }

    void OnDisable()
    {
        if (m_comBuffer != null)
        {
            m_comBuffer.Release();
        }
    }
}