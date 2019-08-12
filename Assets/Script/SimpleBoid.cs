using UnityEngine;

public class SimpleBoid : MonoBehaviour
{
    public int BoidCount;
    public Mesh mesh;
    public Material material;
    public ComputeShader cmpShader;

    [Range(0, 50f)]
    public float Speed;

    [Range(0, 50f)]
    public float BoidDistance;

    public Transform Target;

    private ComputeBuffer m_dataBuffer;
    private ComputeBuffer m_argBuffer;
    private int GroupX;
    private int m_kernel;

    private Bounds m_rangeBound;
    private readonly Vector3 BOUND_CENTER;
    private const int BOUND_SIZE = 10;

    private struct BoidData
    {
        public Vector3 pos;
        public Vector3 rot;
        public Vector3 scale;
    }

    void Start()
    {
        if (BoidCount == 0 || material == null || cmpShader == null)
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

        int threadGroupSizeX = 256;
        // key, 注意乘以 1.0f //
        GroupX = Mathf.CeilToInt(BoidCount*1.0f / threadGroupSizeX);

        // init array //
        BoidData[] dataArray = InitBoidData();

        // data buffer //
        const int STRIDE = (3 + 3 + 3) * sizeof(float);
        m_dataBuffer = new ComputeBuffer(BoidCount, STRIDE);
        m_dataBuffer.SetData(dataArray);

        // set parameter //
        cmpShader.SetBuffer(m_kernel, "_BoidDataBuffer", m_dataBuffer);
        cmpShader.SetInt("_BoidCount", BoidCount);
        
        material.SetBuffer("_BoidDataBuffer", m_dataBuffer);

        // arg buffer //
        int[] args = new int[5]
        {
            (int)mesh.GetIndexCount(0),
            BoidCount,
            (int)mesh.GetIndexStart(0),
            (int)mesh.GetBaseVertex(0),
            0
        };

        m_argBuffer = new ComputeBuffer(1, 5 * sizeof(int), ComputeBufferType.IndirectArguments);
        m_argBuffer.SetData(args);

        // 全部物体所在的范围 //
        m_rangeBound = new Bounds(BOUND_CENTER, new Vector3(BOUND_SIZE * 2, BOUND_SIZE * 2, BOUND_SIZE * 2));
    }

    void Update()
    {
        cmpShader.SetFloat("_Speed", Speed);
        cmpShader.SetFloat("_DeltaTime", Time.deltaTime);
        cmpShader.Dispatch(m_kernel, GroupX, 1, 1);
        cmpShader.SetFloat("_BoidDistance", BoidDistance);

        if (Target != null)
        {
            cmpShader.SetVector("_TargetPos", Target.position);
        }

        Graphics.DrawMeshInstancedIndirect(mesh, 0, material, m_rangeBound, m_argBuffer);
    }

    private BoidData[] InitBoidData()
    {
        BoidData[] dataArray = new BoidData[BoidCount];
        for (int i = 0; i < BoidCount; i++)
        {
            Vector3 translation = new Vector3(Random.value, Random.value, Random.value) * 2 - Vector3.one;
            translation *= BOUND_SIZE;
            dataArray[i].pos = translation;

            dataArray[i].rot = new Vector3(0,0,1); // new Vector3(Random.value, Random.value, Random.value) * 360;
            dataArray[i].scale = Vector3.one;
        }

        return dataArray;
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