using UnityEngine;

public class DrawInstance : MonoBehaviour
{
    public int InstanceCount;
    public Mesh mesh;
    public Material material;
    public float Speed;

    private Matrix4x4[] m_matrixArray;
    private Vector3[] m_posArray;

    void Start()
    {
        if (InstanceCount == 0 || mesh == null)
        {
            enabled = false;
            return;
        }

        m_matrixArray = new Matrix4x4[InstanceCount];
        m_posArray = new Vector3[InstanceCount];
        InitMatrix(m_matrixArray, m_posArray);
    }

    private void InitMatrix(Matrix4x4[] matrixArray, Vector3[] posArray)
    {
        if (matrixArray == null || posArray == null ||
            matrixArray.Length != InstanceCount || posArray.Length != InstanceCount)
        {
            return;
        }

        for (int i = 0; i < InstanceCount; i++)
        {
            Matrix4x4 matrix = Matrix4x4.identity;

            // scale //
            matrix.m00 = Random.Range(0.1f, 1f);
            matrix.m11 = Random.Range(0.1f, 1f);
            matrix.m22 = Random.Range(0.1f, 1f);

            // rotation //

            // translation //
            Vector3 translation = new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), Random.Range(-5f, 5f));
            matrix.m03 = translation.x;
            matrix.m13 = translation.y;
            matrix.m23 = translation.z;

            posArray[i] = translation;
            matrixArray[i] = matrix;
        }
    }

    private void UpdateMatrix()
    {
        Vector3 CENTER = Vector3.zero;

        for (int i = 0; i < InstanceCount; i++)
        {
            Matrix4x4 matrix = m_matrixArray[i];
            Vector3 curPos = m_posArray[i];

            Vector3 dir = CENTER - curPos;
            dir = Vector3.Normalize(dir);

            Vector3 deltaPos = dir * Speed * Time.deltaTime;

            curPos += deltaPos;
            // 重新指定位置 //
            if (Mathf.Abs((curPos - CENTER).x) + Mathf.Abs((curPos - CENTER).y) + Mathf.Abs((curPos - CENTER).z) < 0.1f)
            {
                curPos = new Vector3(Random.Range(-5f, 5f), Random.Range(-5f, 5f), Random.Range(-5f, 5f));
            }

            // translation //
            matrix.m03 = curPos.x;
            matrix.m13 = curPos.y;
            matrix.m23 = curPos.z;

            m_matrixArray[i] = matrix;
            m_posArray[i] = curPos;
        }
    }

    void Update()
    {
        UpdateMatrix();
        Graphics.DrawMeshInstanced(mesh, 0, material, m_matrixArray, InstanceCount);
    }
}