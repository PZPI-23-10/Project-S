using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

public class MissingScriptsCleaner : MonoBehaviour
{
    [ContextMenu("Clean All Missing Scripts")]
    public void Clean()
    {
        int count = GameObjectUtility.RemoveMonoBehavioursWithMissingScript(gameObject);
        Debug.Log($"🧼 Очищено {count} битих скриптів на головному об'єкті!");

        Transform[] allChildren = GetComponentsInChildren<Transform>(true);
        int totalChildCount = 0;
        
        foreach (Transform child in allChildren)
        {
            totalChildCount += GameObjectUtility.RemoveMonoBehavioursWithMissingScript(child.gameObject);
        }

        Debug.Log($"🏰 Повністю замок очищено! Видалено {totalChildCount} скриптів усередині.");
    }

    [ContextMenu("Fix Duplicate Mesh Colliders")]
    public void FixDuplicateColliders()
    {
        Transform[] allObjects = GetComponentsInChildren<Transform>(true);
        int removedCount = 0;

        foreach (Transform obj in allObjects)
        {
            // Беремо всі MeshCollider на цьому конкретному об'єкті
            MeshCollider[] colliders = obj.GetComponents<MeshCollider>();

            // Якщо їх більше ніж 1, видаляємо зайві
            if (colliders.Length > 1)
            {
                for (int i = 1; i < colliders.Length; i++)
                {
                    DestroyImmediate(colliders[i]);
                    removedCount++;
                }
            }
        }

        Debug.Log($"🛡️ Фізику оптимізовано! Видалено {removedCount} дубльованих MeshCollider.");
    }
}
#endif