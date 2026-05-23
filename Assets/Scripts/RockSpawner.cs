using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class RockSpawner : MonoBehaviour
{
    [Tooltip("List of rock prefabs to spawn (drag your rock prefabs here)")]
    public List<GameObject> rockPrefabs = new List<GameObject>();

    [Tooltip("How many rocks to attempt to spawn")]
    public int spawnCount = 40;

    [Tooltip("Radius (in meters) around this GameObject to spawn rocks")]
    public float radius = 50f;

    [Tooltip("Which layers to consider as ground/terrain for raycast")]
    public LayerMask terrainLayer = ~0;

    [Tooltip("Maximum attempts per rock placement")]
    public int maxAttemptsPerRock = 20;

    [Tooltip("Minimum distance between spawned rocks")]
    public float minDistanceBetweenRocks = 1.5f;

    [Tooltip("Random scale range applied to spawned rocks")]
    public float minScale = 0.8f;
    public float maxScale = 1.6f;

    [Tooltip("If true, spawns automatically on Start / in Edit mode when script changes")]
    public bool spawnOnStart = false;

    [Tooltip("Use a deterministic seed for repeatable placement")]
    public bool useSeed = false;
    public int randomSeed = 12345;

    [Tooltip("If true, align rock 'up' with surface normal")]
    public bool alignToNormal = true;

    [Tooltip("Vertical offset applied to spawned rocks (meters)")]
    public float verticalOffset = 0.02f;

    System.Random rng;

    void Start()
    {
        if (Application.isPlaying && spawnOnStart)
            SpawnAll();
    }

    void OnValidate()
    {
        spawnCount = Mathf.Max(0, spawnCount);
        radius = Mathf.Max(0f, radius);
        maxAttemptsPerRock = Mathf.Max(1, maxAttemptsPerRock);
        minDistanceBetweenRocks = Mathf.Max(0f, minDistanceBetweenRocks);
        minScale = Mathf.Max(0.01f, minScale);
        maxScale = Mathf.Max(minScale, maxScale);
    }

    [ContextMenu("Spawn Rocks")]
    public void SpawnAll()
    {
        if (rockPrefabs == null || rockPrefabs.Count == 0)
        {
            Debug.LogWarning("RockSpawner: no prefabs assigned.");
            return;
        }

        if (useSeed) rng = new System.Random(randomSeed); else rng = new System.Random();

        var placedPositions = new List<Vector3>();
        int spawned = 0;

        for (int i = 0; i < spawnCount; i++)
        {
            bool placed = false;
            for (int attempt = 0; attempt < maxAttemptsPerRock; attempt++)
            {
                Vector2 circle = RandomPointInCircle();
                Vector3 origin = new Vector3(transform.position.x + circle.x, transform.position.y + 200f, transform.position.z + circle.y);

                if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 1000f, terrainLayer))
                {
                    Vector3 pos = hit.point;

                    bool tooClose = false;
                    for (int j = 0; j < placedPositions.Count; j++)
                    {
                        if (Vector3.Distance(placedPositions[j], pos) < minDistanceBetweenRocks) { tooClose = true; break; }
                    }
                    if (tooClose) continue;

                    GameObject prefab = rockPrefabs[RandomIndex(0, rockPrefabs.Count)];
                    if (prefab == null) continue;

#if UNITY_EDITOR
                    GameObject go;
                    if (!Application.isPlaying)
                    {
                        go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, this.transform);
                        Undo.RegisterCreatedObjectUndo(go, "Spawn Rock");
                    }
                    else
                    {
                        go = Instantiate(prefab, this.transform);
                        go.transform.parent = this.transform;
                    }
#else
                    var go = Instantiate(prefab, this.transform);
#endif

                    // position and rotation
                    go.transform.position = pos + Vector3.up * verticalOffset;
                    float yaw = (float)RandomFloat(0f, 360f);
                    if (alignToNormal)
                    {
                        go.transform.rotation = Quaternion.FromToRotation(Vector3.up, hit.normal) * Quaternion.Euler(0f, yaw, 0f);
                    }
                    else
                    {
                        go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
                    }

                    float s = (float)RandomFloat(minScale, maxScale);
                    go.transform.localScale = go.transform.localScale * s;

                    placedPositions.Add(pos);
                    placed = true;
                    spawned++;
                    break;
                }
            }

            if (!placed) continue;
        }

        Debug.Log($"RockSpawner: spawned {spawned} rocks (attempted {spawnCount}).");
    }

    [ContextMenu("Clear Spawned Rocks")]
    public void ClearSpawned()
    {
        var children = new List<GameObject>();
        foreach (Transform t in transform) children.Add(t.gameObject);

        foreach (var c in children)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) Undo.DestroyObjectImmediate(c); else Destroy(c);
#else
            Destroy(c);
#endif
        }
    }

    Vector2 RandomPointInCircle()
    {
        if (useSeed)
        {
            double u = rng.NextDouble();
            double r = System.Math.Sqrt(u) * radius;
            double theta = rng.NextDouble() * System.Math.PI * 2.0;
            return new Vector2((float)(System.Math.Cos(theta) * r), (float)(System.Math.Sin(theta) * r));
        }
        else
        {
            float r = Mathf.Sqrt(Random.value) * radius;
            float ang = Random.value * Mathf.PI * 2f;
            return new Vector2(Mathf.Cos(ang) * r, Mathf.Sin(ang) * r);
        }
    }

    int RandomIndex(int minInclusive, int maxExclusive)
    {
        if (useSeed) return rng.Next(minInclusive, maxExclusive);
        return Random.Range(minInclusive, maxExclusive);
    }

    double RandomFloat(double a, double b)
    {
        if (useSeed) return a + rng.NextDouble() * (b - a);
        return Random.Range((float)a, (float)b);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.4f, 0.3f, 0.2f, 0.6f);
        Vector3 center = transform.position;
        RaycastHit hit;
        if (Physics.Raycast(center + Vector3.up * 50f, Vector3.down, out hit, 200f, terrainLayer))
        {
            center.y = hit.point.y + 0.01f;
        }

        Gizmos.DrawWireSphere(transform.position, radius);

        int segments = 48;
        Vector3 prev = center + new Vector3(radius, 0f, 0f);
        for (int i = 1; i <= segments; i++)
        {
            float ang = (i / (float)segments) * Mathf.PI * 2f;
            Vector3 next = center + new Vector3(Mathf.Cos(ang) * radius, 0f, Mathf.Sin(ang) * radius);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }
}
