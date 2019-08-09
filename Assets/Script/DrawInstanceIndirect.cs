using UnityEngine;

public class DrawInstanceIndirect : MonoBehaviour
{
    public int InstanceCount;
    public Mesh mesh;
    public Material material;
    public ComputeShader cmpShader;

    [Range(0, 50f)]
    public float Speed;

    public Transform CenterTrans;

    private ComputeBuffer m_dataBuffer;
    private ComputeBuffer m_argBuffer;
    private int GroupX;
    private int m_kernel;
    private Bounds m_rangeBound;

    private readonly Vector3 BOUND_CENTER;
    private const int BOUND_SIZE= 100;

    private struct InstanceData
    {
        public Vector3 translation;
        public Vector3 eulerAngle;
        public Vector3 scale;
    }

    void Start()
    {
        if (InstanceCount == 0 || material == null || cmpShader == null)
        {
            enabled = false;
            return;
        }

        m_kernel = cmpShader.FindKernel("CSMain");
        if (m_kernel == -1)
        {
            enabled = false;
            return;
        }

        uint threadGroupSizeX=0;
        uint threadGroupSizeY=0;
        uint threadGroupSizeZ=0;
        cmpShader.GetKernelThreadGroupSizes(m_kernel, out threadGroupSizeX, out threadGroupSizeY, out threadGroupSizeZ);

        GroupX = Mathf.CeilToInt(InstanceCount / (int)threadGroupSizeX);

        m_dataBuffer = new ComputeBuffer(InstanceCount, (3+3+3)*sizeof(float));

        // init array //
        InstanceData[] dataArray = new InstanceData[InstanceCount];
        for (int i = 0; i < InstanceCount; i++)
        {
            Vector3 translation = new Vector3(Random.value, Random.value, Random.value) * 2 - Vector3.one;
            translation *= BOUND_SIZE;
            dataArray[i].translation = translation;

            dataArray[i].eulerAngle = Vector3.zero; // new Vector3(Random.value, Random.value, Random.value) * 360;
            dataArray[i].scale = new Vector3(Random.value, Random.value, Random.value) * 5.0f;
        }
        m_dataBuffer.SetData(dataArray);
        cmpShader.SetBuffer(m_kernel, "_DataBuffer", m_dataBuffer);

        // 初始位置buffer, 直接使用  originBuffer 给 _DataBuffer_Origin 赋值的话不生效，因此需要重新new一个computebuffer //
        ComputeBuffer originBuffer = new ComputeBuffer(InstanceCount, (3 + 3 + 3) * sizeof(float));
        originBuffer.SetData(dataArray);
        cmpShader.SetBuffer(m_kernel, "_DataBuffer_Origin", originBuffer);

        // WayPoint //
        Vector3[] wayPointArray = new Vector3[8]
        {
            // 底部 //
            BOUND_CENTER + new Vector3(-BOUND_SIZE*0.5f,  -BOUND_SIZE*0.5f, -BOUND_SIZE*0.5f),
            BOUND_CENTER + new Vector3( BOUND_SIZE*0.5f,  -BOUND_SIZE*0.5f, -BOUND_SIZE*0.5f),
            BOUND_CENTER + new Vector3( BOUND_SIZE*0.5f,  -BOUND_SIZE*0.5f,  BOUND_SIZE*0.5f),
            BOUND_CENTER + new Vector3(-BOUND_SIZE*0.5f,  -BOUND_SIZE*0.5f, BOUND_SIZE*0.5f),

            BOUND_CENTER + new Vector3(-BOUND_SIZE*0.5f,  BOUND_SIZE*0.5f, -BOUND_SIZE*0.5f),
            BOUND_CENTER + new Vector3( BOUND_SIZE*0.5f,  BOUND_SIZE*0.5f, -BOUND_SIZE*0.5f),
            BOUND_CENTER + new Vector3( BOUND_SIZE*0.5f,  BOUND_SIZE*0.5f,  BOUND_SIZE*0.5f),
            BOUND_CENTER + new Vector3(-BOUND_SIZE*0.5f,  BOUND_SIZE*0.5f,  BOUND_SIZE*0.5f),
    };
        ComputeBuffer wayPointBuffer = new ComputeBuffer(8, 3*sizeof(float));
        wayPointBuffer.SetData(wayPointArray);
        cmpShader.SetBuffer(m_kernel, "_WayPointBuffer", wayPointBuffer);

        material.SetBuffer("_DataBuffer", m_dataBuffer);

        // arg buffer //
        int[] args = new int[5]
        {
            (int)mesh.GetIndexCount(0),
            InstanceCount,
            (int)mesh.GetIndexStart(0),
            (int)mesh.GetBaseVertex(0),
            0
        };

        m_argBuffer = new ComputeBuffer(1, 5*sizeof(int), ComputeBufferType.IndirectArguments);
        m_argBuffer.SetData(args);

        // 全部物体所在的范围 //
        m_rangeBound = new Bounds(BOUND_CENTER, new Vector3(BOUND_SIZE * 2, BOUND_SIZE * 2, BOUND_SIZE * 2));
    }

    void Update()
    {
        cmpShader.SetFloat("_Speed", Speed);
        cmpShader.SetFloat("_DeltaTime", Time.deltaTime);
        if (CenterTrans != null)
        {
            cmpShader.SetVector("_Center", CenterTrans.position);
        }
        cmpShader.Dispatch(m_kernel, GroupX, 1, 1);

        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, m_rangeBound, m_argBuffer);
    }

    void OnDisable()
    {
        if (m_dataBuffer != null)
        {
            m_dataBuffer.Release();
            m_dataBuffer = null;
        }

        if (m_argBuffer != null)
        {
            m_argBuffer.Release();
            m_argBuffer = null;
        }
    }
}