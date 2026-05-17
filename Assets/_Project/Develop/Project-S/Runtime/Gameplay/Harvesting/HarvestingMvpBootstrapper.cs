using Project_S.Runtime.Gameplay.Character.Inventory;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Harvesting
{
    public static class HarvestingMvpBootstrapper
    {
        private const string RootName = "[MVP] Harvesting Nodes";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Bootstrap()
        {
            if (GameObject.Find(RootName) != null)
                return;

            var inventory = Object.FindFirstObjectByType<InventoryController>();
            if (inventory == null)
                return;

            var treeData = Resources.Load<ResourceNodeData>("Harvesting/TreeNode");
            var stoneData = Resources.Load<ResourceNodeData>("Harvesting/StoneNode");
            var flintStoneData = Resources.Load<ResourceNodeData>("Harvesting/FlintStoneNode");

            if (treeData == null || stoneData == null)
            {
                Debug.LogWarning("[Harvesting] Missing MVP resource node data.");
                return;
            }

            var root = new GameObject(RootName);
            Transform player = inventory.transform;

            SpawnTree(root.transform, player.position + player.forward * 4f + player.right * 1.8f, treeData);
            SpawnTree(root.transform, player.position + player.forward * 5.8f - player.right * 1.4f, treeData);
            SpawnStone(root.transform, player.position + player.forward * 4.2f - player.right * 2.7f, stoneData);
            SpawnStone(root.transform, player.position + player.forward * 6.1f + player.right * 2.5f, flintStoneData != null ? flintStoneData : stoneData, "[MVP] Harvestable Flint Stone", new Color(0.28f, 0.43f, 0.48f));
        }

        private static void SpawnTree(Transform parent, Vector3 position, ResourceNodeData data)
        {
            var tree = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tree.name = "[MVP] Harvestable Tree";
            tree.transform.SetParent(parent);
            tree.transform.position = position + Vector3.up * 1.25f;
            tree.transform.localScale = new Vector3(0.45f, 1.25f, 0.45f);

            var renderer = tree.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = new Color(0.34f, 0.2f, 0.09f);

            tree.AddComponent<HarvestableResourceNode>().Configure(data);
        }

        private static void SpawnStone(Transform parent, Vector3 position, ResourceNodeData data, string nodeName = "[MVP] Harvestable Stone", Color? color = null)
        {
            var stone = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stone.name = nodeName;
            stone.transform.SetParent(parent);
            stone.transform.position = position + Vector3.up * 0.45f;
            stone.transform.localScale = new Vector3(1.1f, 0.9f, 1.1f);

            var renderer = stone.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = color ?? new Color(0.38f, 0.4f, 0.42f);

            stone.AddComponent<HarvestableResourceNode>().Configure(data);
        }
    }
}
