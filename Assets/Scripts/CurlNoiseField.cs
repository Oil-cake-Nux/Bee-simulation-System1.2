using UnityEngine;

// 改为静态类，不需要挂载到GameObject上
public static class CurlNoiseField
{
    // 噪声场参数
    public static Vector3 fieldPosition = new Vector3(20f, 10f, 20f); // 噪声场的中心位置
    //public static Vector3 fieldSize = new Vector3(80f, 80f, 80f); // 增大噪声场大小

    // 噪声参数
    public static float noiseScale = 1f; // 降低噪声的缩放比例，使得噪声更平滑
    public static float forceStrength = 2.0f; // 增加力的强度
    public static float timeScale = 1f; // 降低时间缩放，让噪声变化更平滑

    // 内部参数
    private static float timeOffset = 0f;
    private static float lastUpdateTime = 0f;

    // 更新时间偏移
    public static void UpdateTime()
    {
        float currentTime = Time.time;
        float deltaTime = currentTime - lastUpdateTime;

        timeOffset += deltaTime * timeScale;
        lastUpdateTime = currentTime;
    }

    // 获取给定位置的卷曲噪声力
    public static Vector3 GetCurlNoiseForce(Vector3 position)
    {
        Vector3 localPos = position - fieldPosition;

        ////检查是否在噪声场内
        //if (Mathf.Abs(localPos.x) > fieldSize.x / 2 ||
        //    Mathf.Abs(localPos.y) > fieldSize.y / 2 ||
        //    Mathf.Abs(localPos.z) > fieldSize.z / 2)
        //    //if (true)
        //{
        //    // 平滑衰减场边界
        //    float fadeDistance = 2.0f;
        //    float fadeFactorX = 1f - Mathf.Clamp01((Mathf.Abs(localPos.x) - fieldSize.x / 2 + fadeDistance) / fadeDistance);
        //    float fadeFactorY = 1f - Mathf.Clamp01((Mathf.Abs(localPos.y) - fieldSize.y / 2 + fadeDistance) / fadeDistance);
        //    float fadeFactorZ = 1f - Mathf.Clamp01((Mathf.Abs(localPos.z) - fieldSize.z / 2 + fadeDistance) / fadeDistance);
        //    float fadeFactor = fadeFactorX * fadeFactorY * fadeFactorZ;

        //    if (fadeFactor <= 0)
        //        return Vector3.zero;

        //    return CalculateCurlNoise(localPos) * forceStrength * fadeFactor;
        //}

        return CalculateCurlNoise(localPos) * forceStrength;
    }

    // 计算卷曲噪声
    private static Vector3 CalculateCurlNoise(Vector3 position)
    {
        Vector3 scaledPos = position * noiseScale;
        float t = timeOffset;
        float epsilon = 0.0001f;

        // 使用中心差分法计算梯度
        Vector3 dx = new Vector3(epsilon, 0, 0);
        Vector3 dy = new Vector3(0, epsilon, 0);
        Vector3 dz = new Vector3(0, 0, epsilon);

        // x方向梯度
        float gradientX = ImprovedPerlinNoise3D(scaledPos + dx, t) -
                          ImprovedPerlinNoise3D(scaledPos - dx, t);

        // y方向梯度
        float gradientY = ImprovedPerlinNoise3D(scaledPos + dy, t) -
                          ImprovedPerlinNoise3D(scaledPos - dy, t);

        // z方向梯度
        float gradientZ = ImprovedPerlinNoise3D(scaledPos + dz, t) -
                          ImprovedPerlinNoise3D(scaledPos - dz, t);

        // 计算卷曲 (curl) = ∇ × F
        return new Vector3(
            (gradientZ - gradientY) / (2 * epsilon),
            (gradientX - gradientZ) / (2 * epsilon),
            (gradientY - gradientX) / (2 * epsilon)
        );
    }

    // 改进的3D Perlin噪声实现
    private static float ImprovedPerlinNoise3D(Vector3 position, float time)
    {
        // 使用不同频率和振幅的噪声叠加，创建更复杂的细节
        float noise = 0f;
        float amplitude = 1.0f;
        float frequency = 1.0f;
        float persistence = 0.5f;

        // 添加多个八度噪声
        for (int i = 0; i < 4; i++)
        {
            // 使用不同的维度组合以获得更自然的3D噪声
            float n1 = Mathf.PerlinNoise((position.x + time * 0.1f) * frequency,
                                        (position.y + time * 0.2f) * frequency);

            float n2 = Mathf.PerlinNoise((position.y + time * 0.15f) * frequency,
                                        (position.z + time * 0.1f) * frequency);

            float n3 = Mathf.PerlinNoise((position.z + time * 0.05f) * frequency,
                                        (position.x + time * 0.15f) * frequency);

            float n4 = Mathf.PerlinNoise((position.x + position.y) * frequency * 0.7f,
                                        (position.z + time * 0.25f) * frequency);

            float n5 = Mathf.PerlinNoise((position.y + position.z) * frequency * 0.7f,
                                        (position.x + time * 0.3f) * frequency);

            // 组合不同的噪声
            float combinedNoise = (n1 + n2 + n3 + n4 + n5) / 5.0f;

            // 累加噪声
            noise += combinedNoise * amplitude;

            // 为下一个八度调整频率和振幅
            amplitude *= persistence;
            frequency *= 2.0f;
        }

        // 将噪声范围映射到[-1, 1]
        return noise * 2.0f - 1.0f;
    }
}