using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class WindField : MonoBehaviour
{
    [Range(0.001f, 1f)]
    public float noiseSpeed = 200f;
    [Range(0.001f, 10f)]
    public float noiseResolution = 200f;
    [Range(100f, 1000f)]
    public float vectorSpacing = 200f;
    [Range(100f, 10000f)]
    public float fieldHeight = 800f;
    [Range(100f, 10000f)]
    public float fieldWidth = 1600f;
    [Range(0, 10000)]
    public int particles = 0;
    [Range(0.01f, 100f)]
    public float particleSpeed = 0f;
    public Mesh arrowMesh;
    public GameObject test;

    bool[,,] highlighted;
    int width_segments, height_segments;
    Vector3[,,] origins, vectors;    Vector3[] particlePositions;

    // Start is called before the first frame update
    void Start()
    {
        Refresh();
    }

    private void Update()
    {
        Refresh();
        // getWind(test.transform.position);
    }
    void Refresh()
    {
        width_segments = Mathf.FloorToInt(fieldWidth / vectorSpacing)+1;
        height_segments = Mathf.FloorToInt(fieldHeight / vectorSpacing)+1;

        if (origins == null || origins.Length != width_segments* width_segments * height_segments)
            SetupField(width_segments, height_segments);
        RefreshPerlinNoice();


        if (particlePositions == null || particles != particlePositions.Length)
            SetupParticles();
        if (particles != 0)
            refreshParticlePositions();
    }
    void RefreshPerlinNoice()
    {
        float t = Time.realtimeSinceStartup;
        for (int xi = 0; xi < width_segments; xi++)
        {
            for (int yi = 0; yi < height_segments; yi++)
            {
                for (int zi = 0; zi < width_segments; zi++)
                {
                    float noise = Perlin.Noise(xi * noiseResolution, zi * noiseResolution, t * noiseSpeed);
                    float noiseRadian = noise * 6.28318530718f;
                    vectors[xi, yi, zi] = new Vector3(Mathf.Cos(noiseRadian), 0f, Mathf.Sin(noiseRadian));
                }
            }
        }

    }
    void SetupField(int width_segments, int height_segments)
    {
        origins = new Vector3[width_segments, height_segments, width_segments];
        vectors = new Vector3[width_segments, height_segments, width_segments];
        for (int xi = 0; xi < width_segments; xi++)
        {
            for (int yi = 0; yi < height_segments; yi++)
            {
                for (int zi = 0; zi < width_segments; zi++)
                {
                    origins[xi, yi, zi].Set(vectorSpacing * xi, vectorSpacing * yi, vectorSpacing * zi);
                }
            }
        }

    }
    void SetupParticles()
    {
        particlePositions = new Vector3[particles];
        for (int i = 0; i < particles; i++)
        {
            particlePositions[i].Set(Random.value * fieldWidth, Random.value * fieldHeight, Random.value * fieldWidth);
        }
    }

    void refreshParticlePositions()
    {
        Vector3 bounds = new Vector3(fieldWidth, fieldHeight, fieldWidth);
        for (int i = 0; i < particles; i++)
        {
            // affected by wind
            Vector3 wind = getWind(particlePositions[i]);
            particlePositions[i] += wind * particleSpeed * Time.deltaTime;

            // stay in field
            Vector3 p = particlePositions[i];
            for (int j = 0; j < 3; j++)
            {
                if (p[j] < 0f)
                    p[j] += bounds[j];
                if (p[j] > bounds[j])
                    p[j] -= bounds[j];
            }
            
            particlePositions[i] = p;
        }

    }
    void OnDrawGizmos()
    {
        Refresh();
        Gizmos.color = Color.red;
        for (int xi = 0; xi < width_segments; xi++){
            for (int yi = 0; yi < height_segments; yi++){
                for (int zi = 0; zi < width_segments; zi++){
                    float meshSize = 2000f;
                    Vector3 v = vectors[xi, yi, zi];
                    float angle = Vector3.Angle(v, Vector3.forward);
                    if (highlighted[xi, yi, zi])
                        Gizmos.color = Color.white;
                    else
                        Gizmos.color = new Color(angle / 180, 1 - (angle / 180), 0);
                    Gizmos.DrawMesh(arrowMesh, origins[xi, yi, zi], Quaternion.LookRotation(v), new Vector3(meshSize, meshSize, meshSize));
                }
            }
        }
        Gizmos.color = Color.magenta;
        for (int i = 0; i < particles; i++)
        {
            Gizmos.DrawSphere(particlePositions[i], 100f);
        }

        float meshSizeTest = 2000f;
        Vector3 pos = test.transform.position;
        Vector3 vTest = getWind(pos);
        float angleTest = Vector3.Angle(vTest, Vector3.forward);
        Gizmos.color = new Color(angleTest / 180, 1 - (angleTest / 180), 0);
        Gizmos.DrawMesh(arrowMesh, pos, Quaternion.LookRotation(vTest), new Vector3(meshSizeTest, meshSizeTest, meshSizeTest));
        
    }

    public Vector3 getWind(Vector3 position)
    {
        int[,] pairs = new int[3,2];
        Vector3Int segmentBounds = new Vector3Int(width_segments, height_segments, width_segments);
        float[,] adjustDistance = new float[3, 2];
        for (int i = 0; i < 3; i++)
        {
            if (position[i] <= 0f)
            {
                pairs[i, 0] = 0;
                pairs[i, 1] = segmentBounds[i] - 1;
                adjustDistance[i, 0] = 0f;
                adjustDistance[i, 1] = ((segmentBounds[i] - 1) * vectorSpacing) + vectorSpacing;
            }
            else if (position[i] >= (segmentBounds[i] - 1) * vectorSpacing)
            {
                pairs[i, 0] = segmentBounds[i] - 1;
                pairs[i, 1] = 0;
                adjustDistance[i, 0] = 0f;
                adjustDistance[i, 1] = -((segmentBounds[i] - 1) * vectorSpacing) - vectorSpacing;
            }
            else
            {
                pairs[i, 0] = Mathf.FloorToInt(position[i] / vectorSpacing);
                pairs[i, 1] = pairs[i, 0] + 1;
                adjustDistance[i, 0] = 0f;
                adjustDistance[i, 1] = 0f;
            }
        }
        Vector3[] windVectors = new Vector3[8];
        Vector3[] windOrigins = new Vector3[8];
        int influenceAmount = 0;

        highlighted = new bool[width_segments, height_segments, width_segments];

        for (int xi = 0; xi < 2; xi++) {
            for (int yi = 0; yi < 2; yi++) {
                for (int zi = 0; zi < 2; zi++) {
                    windVectors[influenceAmount] = vectors[pairs[0, xi], pairs[1, yi], pairs[2, zi]];
                    windOrigins[influenceAmount] = origins[pairs[0, xi], pairs[1, yi], pairs[2, zi]];
                    windOrigins[influenceAmount] -= new Vector3(adjustDistance[0, xi], adjustDistance[1, yi], adjustDistance[2, zi]);
                    highlighted[pairs[0, xi], pairs[1, yi], pairs[2, zi]] = true;
                    influenceAmount++;

                }
            }
        }

        float[,] weights = RBF.Weights(windOrigins, windVectors);
        return RBF.Solve(weights, windOrigins, position, 8);
    }
}
