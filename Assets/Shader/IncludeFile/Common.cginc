#if !defined(_COMMON_INCLUDE_FILE)
#define _COMMON_INCLUDE_FILE

static float PI = 3.141593;

// 对顶点进行欧拉角旋转, ZXY顺序 //
float3 RotateEulerAngle(float3 vertex, float3 angleDeg)
{
    angleDeg = angleDeg*PI/180.0;

    float sinX,cosX;
    sincos(angleDeg.x, sinX, cosX);
    float3x3 rotateMatrix_X = float3x3(
        1, 0, 0,
        0, cosX, sinX,
        0, -sinX, cosX
    );

    float sinY,cosY;
    sincos(angleDeg.y, sinY, cosY);
    float3x3 rotateMatrix_Y = float3x3(
        cosY, 0, -sinY,
        0, 1, 0,
        sinY, 0, cosY
    );

    float sinZ,cosZ;
    sincos(angleDeg.z, sinZ, cosZ);
    float3x3 rotateMatrix_Z = float3x3(
        cosZ, sinZ, 0,
        -sinZ, cosZ, 0,
        0, 0, 1
    );

    // unity中欧拉角的旋转顺序是 Z -> X -> Y //
    // https://docs.unity3d.com/ScriptReference/Transform-eulerAngles.html //
    vertex = mul(rotateMatrix_Z, vertex);
    vertex = mul(rotateMatrix_X, vertex);
    vertex = mul(rotateMatrix_Y, vertex);

    return vertex;
}

// 左手坐标系 //
float3 Rotate(float3 vertex, float3 forward, float3 up)
{
    forward = normalize(forward);
    up = normalize(up);

    float3 right = cross(up, forward);
    right = normalize(right);

    up = cross(forward, right);
    up = normalize(up);

    float3x3 rotateMatrix = float3x3(
        right.x, up.x, forward.x,
        right.y, up.y, forward.y,
        right.z, up.z, forward.z
    );

    // NOTE, 不应该是用下面这个矩阵吗? //
    // float3x3 rotateMatrix = float3x3(
    //     right.x, right.y, right.z,
    //     up.x, up.y, up.z,
    //     forward.x, forward.y, forward.z
    // );

    return mul(rotateMatrix, vertex);
}

#endif      // _COMMON_INCLUDE_FILE //