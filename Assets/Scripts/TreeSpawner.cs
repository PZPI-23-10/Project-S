using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class TreeSpawner : MonoBehaviour
{
    [Tooltip("List of tree prefabs to spawn (drag your tree prefabs here)")]
    public List<GameObject> treePrefabs = new List<GameObject>();

    [Tooltip("How many trees to attempt to spawn")]
    public int spawnCount = 50;

    [Tooltip("Radius (in meters) around this GameObject to spawn trees")]
    public float radius = 60f;

    [Tooltip("Which layers to consider as ground/terrain for raycast")]
    public LayerMask terrainLayer = ~0;

    [Tooltip("Шари об'єктів, які БЛОКУЮТЬ спавн (інші дерева, каміння, будинки)")]
    public LayerMask obstacleLayers;

    [Tooltip("Maximum attempts per tree placement")]
    public int maxAttemptsPerTree = 30;

    [Tooltip("Minimum distance between spawned trees")]
    public float minDistanceBetweenTrees = 2f;

    [Tooltip("Random scale range applied to spawned trees")]
    public float minScale = 0.9f;
    public float maxScale = 1.2f;

    [Tooltip("If true, spawns automatically on Start / in Edit mode when script changes")]
    public bool spawnOnStart = false;

    [Tooltip("Use a deterministic seed for repeatable placement")]
    public bool useSeed = false;
    public int randomSeed = 12345;

    [Tooltip("How much to align to surface normal (0 = straight up, 1 = fully aligned to slope)")]
    [Range(0f, 1f)] public float alignToNormal = 0.2f; // 0.2 дасть легкий природний нахил

    [Tooltip("Vertical offset applied to spawned trees (meters)")]
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
        maxAttemptsPerTree = Mathf.Max(1, maxAttemptsPerTree);
        minDistanceBetweenTrees = Mathf.Max(0f, minDistanceBetweenTrees);
        minScale = Mathf.Max(0.01f, minScale);
        maxScale = Mathf.Max(minScale, maxScale);
    }

    [ContextMenu("Spawn Trees")]
    public void SpawnAll()
    {
        if (treePrefabs == null || treePrefabs.Count == 0)
        {
            Debug.LogWarning("TreeSpawner: no prefabs assigned.");
            return;
        }

        if (useSeed) rng = new System.Random(randomSeed); else rng = new System.Random();

        var placedPositions = new List<Vector3>();
        int spawned = 0;

        for (int i = 0; i < spawnCount; i++)
        {
            bool placed = false;
            for (int attempt = 0; attempt < maxAttemptsPerTree; attempt++)
            {
                Vector2 circle = RandomPointInCircle();
                Vector3 origin = new Vector3(transform.position.x + circle.x, transform.position.y + 200f, transform.position.z + circle.y);

                if (Physics.Raycast(origin, Vector3.down, out RaycastHit hit, 1000f))
                {
                    if ((terrainLayer.value & (1 << hit.collider.gameObject.layer)) == 0)
                    {
                        continue;
                    }

                    // 🔥 НОВА ПЕРЕВІРКА: чи є навколо інші ресурси або будинки
                    if (Physics.OverlapSphere(hit.point, minDistanceBetweenTrees, obstacleLayers).Length > 0)
                    {
                        continue; // Знайшли перешкоду - пропускаємо цю точку
                    }

                    Vector3 pos = hit.point;

                    // Залишаємо твою стару перевірку дистанції між деревами цього ж спавнера
                    bool tooClose = false;
                    for (int j = 0; j < placedPositions.Count; j++)
                    {
                        if (Vector3.Distance(placedPositions[j], pos) < minDistanceBetweenTrees) { tooClose = true; break; }
                    }
                    if (tooClose) continue;

                    GameObject prefab = treePrefabs[RandomIndex(0, treePrefabs.Count)];
                    if (prefab == null) continue;

#if UNITY_EDITOR
                    GameObject go;
                    if (!Application.isPlaying)
                    {
                        go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, this.transform);
                        Undo.RegisterCreatedObjectUndo(go, "Spawn Tree");
                    }
                    else
                    {
                        go = Instantiate(prefab, this.transform);
                        go.transform.parent = this.transform;
                    }
#else
                    var go = Instantiate(prefab, this.transform);
#endif

                    go.transform.position = pos + Vector3.up * verticalOffset;

                    float yaw = (float)RandomFloat(0f, 360f);
                    Vector3 blendedNormal = Vector3.Lerp(Vector3.up, hit.normal, alignToNormal).normalized;
                    go.transform.rotation = Quaternion.FromToRotation(Vector3.up, blendedNormal) * Quaternion.Euler(0f, yaw, 0f);

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

        Debug.Log($"TreeSpawner: spawned {spawned} trees (attempted {spawnCount}).");
    }

    [ContextMenu("Clear Spawned Trees")]
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
        Gizmos.color = new Color(0.1f, 0.5f, 0.1f, 0.6f);
        Vector3 center = transform.position;
        RaycastHit hit;
        // Гізмо тепер теж коректно перевіряє висоту
        if (Physics.Raycast(center + Vector3.up * 50f, Vector3.down, out hit, 200f))
        {
            if ((terrainLayer.value & (1 << hit.collider.gameObject.layer)) != 0)
            {
                center.y = hit.point.y + 0.01f;
            }
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
