using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class CaveSpawner : MonoBehaviour
{
    [Tooltip("Список префабів (каміння, кристали, руда) для печер")]
    public List<GameObject> cavePrefabs = new List<GameObject>();

    [Header("Кількість спавну (Діапазон)")]
    [Tooltip("Мінімальна кількість об'єктів для спавну")]
    public int minSpawnCount = 20;

    [Tooltip("Максимальна кількість об'єктів для спавну")]
    public int maxSpawnCount = 40;

    [Tooltip("Радіус сфери навколо цього об'єкта, де будуть з'являтися ресурси")]
    public float radius = 15f;

    [Tooltip("Шар печери/землі (Digger mesh або Terrain)")]
    public LayerMask terrainLayer = ~0;

    [Tooltip("Шари, які блокують спавн (інші камені, ресурси, гравець)")]
    public LayerMask obstacleLayers;

    [Header("Спеціальні налаштування")]
    [Tooltip("Пропустити найпершу знайдену точку спавну")]
    public bool skipFirstSpawn = true;

    [Tooltip("Максимальна кількість спроб для розміщення одного об'єкта")]
    public int maxAttemptsPerRock = 20;

    [Tooltip("Мінімальна відстань між об'єктами")]
    public float minDistanceBetweenRocks = 1.5f;

    [Tooltip("Випадковий розмір")]
    public float minScale = 0.8f;
    public float maxScale = 1.6f;

    [Tooltip("Спавнити автоматично при старті гри")]
    public bool spawnOnStart = false;

    [Tooltip("Використовувати Seed для однакового спавну")]
    public bool useSeed = false;
    public int randomSeed = 12345;

    [Tooltip("Якщо true - каміння повертається паралельно до стіни/стелі")]
    public bool alignToNormal = true;

    [Tooltip("Відступ від стіни (щоб об'єкт не провалювався глибоко в текстуру)")]
    public float surfaceOffset = 0.05f;

    System.Random rng;

    void Start()
    {
        if (Application.isPlaying && spawnOnStart)
            SpawnAll();
    }

    void OnValidate()
    {
        minSpawnCount = Mathf.Max(0, minSpawnCount);
        maxSpawnCount = Mathf.Max(minSpawnCount, maxSpawnCount);
        radius = Mathf.Max(0f, radius);
        maxAttemptsPerRock = Mathf.Max(1, maxAttemptsPerRock);
        minDistanceBetweenRocks = Mathf.Max(0f, minDistanceBetweenRocks);
        minScale = Mathf.Max(0.01f, minScale);
        maxScale = Mathf.Max(minScale, maxScale);
    }

    [ContextMenu("Spawn Cave Rocks")]
    public void SpawnAll()
    {
        if (cavePrefabs == null || cavePrefabs.Count == 0)
        {
            Debug.LogWarning("CaveSpawner: Немає префабів для спавну.");
            return;
        }

        if (useSeed) rng = new System.Random(randomSeed); else rng = new System.Random();

        // Визначаємо фінальну кількість об'єктів для цього конкретного запуску
        int targetSpawnCount = useSeed ? rng.Next(minSpawnCount, maxSpawnCount + 1) : Random.Range(minSpawnCount, maxSpawnCount + 1);

        var placedPositions = new List<Vector3>();
        int spawned = 0;

        // Прапорець для перевірки, чи пропустили ми перший спавн
        bool hasSkippedFirst = !skipFirstSpawn;

        for (int i = 0; i < targetSpawnCount; i++)
        {
            bool placed = false;
            for (int attempt = 0; attempt < maxAttemptsPerRock; attempt++)
            {
                // Визначаємо випадковий напрямок з центру спавнера на всі 360 градусів
                Vector3 direction = RandomDirectionOnSphere();

                // Стріляємо променем з центру спавнера на заданий радіус
                if (Physics.Raycast(transform.position, direction, out RaycastHit hit, radius))
                {
                    // Ігноруємо попадання, які сталися ближче ніж 0.5 метра 
                    // (щоб не спавнило каміння в повітрі на самому об'єкті спавнера)
                    if (hit.distance < 0.5f) continue;

                    // Перевіряємо, чи влучили в правильний шар (печеру/землю)
                    if ((terrainLayer.value & (1 << hit.collider.gameObject.layer)) == 0) continue;

                    // Перевірка на перешкоди сферою
                    if (Physics.OverlapSphere(hit.point, minDistanceBetweenRocks, obstacleLayers).Length > 0)
                    {
                        continue;
                    }

                    Vector3 pos = hit.point;

                    // Перевірка відстані між вже заспавненими об'єктами
                    bool tooClose = false;
                    for (int j = 0; j < placedPositions.Count; j++)
                    {
                        if (Vector3.Distance(placedPositions[j], pos) < minDistanceBetweenRocks) { tooClose = true; break; }
                    }
                    if (tooClose) continue;

                    // 🔥 ЛОГІКА ПРОПУСКУ ПЕРШОГО СПАВНУ
                    if (!hasSkippedFirst)
                    {
                        hasSkippedFirst = true;
                        continue; // Викидаємо цю ідеальну точку і йдемо шукати іншу
                    }

                    GameObject prefab = cavePrefabs[RandomIndex(0, cavePrefabs.Count)];
                    if (prefab == null) continue;

#if UNITY_EDITOR
                    GameObject go;
                    if (!Application.isPlaying)
                    {
                        go = (GameObject)PrefabUtility.InstantiatePrefab(prefab, this.transform);
                        Undo.RegisterCreatedObjectUndo(go, "Spawn Cave Rock");
                    }
                    else
                    {
                        go = Instantiate(prefab, this.transform);
                        go.transform.parent = this.transform;
                    }
#else
                    var go = Instantiate(prefab, this.transform);
#endif

                    // Відступ робиться по нормалі стіни/стелі
                    go.transform.position = pos + hit.normal * surfaceOffset;

                    float yaw = (float)RandomFloat(0f, 360f);
                    if (alignToNormal)
                    {
                        // Приліплюємо до стіни/стелі
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

        Debug.Log($"CaveSpawner: заспавнено {spawned} об'єктів у печері (було заплановано рандомом: {targetSpawnCount}).");
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

    Vector3 RandomDirectionOnSphere()
    {
        if (useSeed)
        {
            double u = rng.NextDouble() * 2.0 - 1.0;
            double theta = rng.NextDouble() * 2.0 * System.Math.PI;
            double r = System.Math.Sqrt(1 - u * u);
            return new Vector3((float)(r * System.Math.Cos(theta)), (float)(r * System.Math.Sin(theta)), (float)u);
        }
        else
        {
            return Random.onUnitSphere;
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
        Gizmos.color = new Color(0.2f, 0.6f, 0.8f, 0.5f);
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}