using System.Text;
using UnityEngine;

public class SimpleTest : MonoBehaviour
{
    public ComputeShader cmpShader;

    struct Trans
    {
        Vector3 pos;
        Vector3 eulerAngle;
        Vector3 scale;
    }

    void Start()
    {
        if (cmpShader == null)
        {
            enabled = false;
            return;
        }

        ComputeFloat();

        //ComputeTrans();
    }

    private void ComputeFloat()
    {
        int kernelIndex = cmpShader.FindKernel("CSMain_Float");
        if (kernelIndex == -1)
        {
            return;
        }

        uint groupSizeX = 0;
        uint groupSizeY = 0;
        uint groupSizeZ = 0;
        cmpShader.GetKernelThreadGroupSizes(kernelIndex, out groupSizeX, out groupSizeY, out groupSizeZ);
        const int GroupX = 2;
        const int GroupY = 3;
        int TOTAL_SIZE = GroupX * GroupY * (int)groupSizeX * (int)groupSizeY;

        ComputeBuffer cmpBuffer = new ComputeBuffer(TOTAL_SIZE, sizeof(float));
        float[] resultArray = new float[TOTAL_SIZE];
        PrintResult(resultArray);

        // SetFloat //
        cmpShader.SetFloat("_Multiplier", 2.2f);

        // SetBuffer //
        cmpBuffer.SetData(resultArray);
        cmpShader.SetBuffer(kernelIndex, "_Result", cmpBuffer);

        cmpShader.Dispatch(kernelIndex, GroupX, GroupY, 1);
        cmpBuffer.GetData(resultArray);
        cmpBuffer.Release();
        PrintResult(resultArray);
    }

    private void PrintResult(float[] result)
    {
        if (result == null || result.Length == 0)
        {
            return;
        }

        StringBuilder sb = new StringBuilder();
        for (int i = 0; i < result.Length; i++)
        {
            sb.Append(result[i]);
            sb.Append("\t");
        }

        Debug.Log("result = " + sb.ToString());
        sb.Clear();
    }

    private void ComputeTrans()
    {
        const int TOTAL_SIZE = 6 * 8;
        int kernelTrans = cmpShader.FindKernel("CSMain_Trans");
        if (kernelTrans == -1)
        {
            return;
        }

        ComputeBuffer cmpBufferTrans = new ComputeBuffer(TOTAL_SIZE, sizeof(float) * (3 + 3 + 3));
        Trans[] resultTrans = new Trans[TOTAL_SIZE];

        // SetFloat //
        cmpShader.SetFloat("_Multiplier", 3.3f);

        // SetBuffer //
        cmpShader.SetBuffer(kernelTrans, "_ResultTrans", cmpBufferTrans);

        cmpShader.Dispatch(kernelTrans, 6, 1, 1);

        cmpBufferTrans.GetData(resultTrans);
        cmpBufferTrans.Release();
    }
}