using UnityEngine;

namespace ljk
{
    public static class CurlNoiseField
    {
        public static Vector3 fieldPosition = new Vector3(20f, 10f, 20f);
        public static float noiseScale = 1f;
        public static float forceStrength = 2f;
        public static float timeScale = 1f;

        private static float timeOffset = 0f;
        private static float lastUpdateTime = 0f;

        public static void UpdateTime()
        {
            float currentTime = Time.time;
            float deltaTime = currentTime - lastUpdateTime;

            timeOffset += deltaTime * timeScale;
            lastUpdateTime = currentTime;
        }

        public static Vector3 GetCurlNoiseForce(Vector3 position)
        {
            Vector3 localPos = position - fieldPosition;
            return CalculateCurlNoise(localPos) * forceStrength;
        }

        private static Vector3 CalculateCurlNoise(Vector3 position)
        {
            Vector3 scaledPos = position * noiseScale;
            float t = timeOffset;
            float epsilon = 0.0001f;

            Vector3 dx = new Vector3(epsilon, 0f, 0f);
            Vector3 dy = new Vector3(0f, epsilon, 0f);
            Vector3 dz = new Vector3(0f, 0f, epsilon);

            float gradientX = ImprovedPerlinNoise3D(scaledPos + dx, t) -
                              ImprovedPerlinNoise3D(scaledPos - dx, t);

            float gradientY = ImprovedPerlinNoise3D(scaledPos + dy, t) -
                              ImprovedPerlinNoise3D(scaledPos - dy, t);

            float gradientZ = ImprovedPerlinNoise3D(scaledPos + dz, t) -
                              ImprovedPerlinNoise3D(scaledPos - dz, t);

            return new Vector3(
                (gradientZ - gradientY) / (2f * epsilon),
                (gradientX - gradientZ) / (2f * epsilon),
                (gradientY - gradientX) / (2f * epsilon)
            );
        }

        private static float ImprovedPerlinNoise3D(Vector3 position, float time)
        {
            float noise = 0f;
            float amplitude = 1f;
            float frequency = 1f;
            float persistence = 0.5f;

            for (int i = 0; i < 4; i++)
            {
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

                float combinedNoise = (n1 + n2 + n3 + n4 + n5) / 5f;
                noise += combinedNoise * amplitude;

                amplitude *= persistence;
                frequency *= 2f;
            }

            return noise * 2f - 1f;
        }
    }
}
