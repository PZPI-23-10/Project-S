using Project_S.Runtime.Gameplay.Character.Inventory;
using Project_S.Runtime.Gameplay.Character.Combat;
using Project_S.Runtime.Gameplay.Loot;
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
            var ironOreData = Resources.Load<ResourceNodeData>("Harvesting/IronOreNode");
            var gromovytsiaData = Resources.Load<ResourceNodeData>("Harvesting/GromovytsiaNode");
            var berryItem = Resources.Load<ItemData>("Crafting/Items/Consumables/Berry");
            var basicEnemyLoot = Resources.Load<LootTableData>("Loot/BasicEnemyLoot");
            var toughEnemyLoot = Resources.Load<LootTableData>("Loot/ToughEnemyLoot");

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

            SpawnBerryBush(root.transform, player.position + player.forward * 3.1f + player.right * 3.1f, berryItem);
            SpawnBerryBush(root.transform, player.position + player.forward * 4.7f + player.right * 3.7f, berryItem);

            if (ironOreData != null)
            {
                SpawnIronOre(root.transform, player.position + player.forward * 7.2f - player.right * 2.4f, ironOreData);
                SpawnIronOre(root.transform, player.position + player.forward * 8.4f - player.right * 3.1f, ironOreData);
            }

            if (gromovytsiaData != null)
                SpawnGromovytsia(root.transform, player.position + player.forward * 8.6f + player.right * 2.8f, gromovytsiaData);

            SpawnEnemy(root.transform, player.position + player.forward * 7.1f + player.right * 0.9f, basicEnemyLoot, "[MVP] Basic Loot Target", 35f, 10, new Color(0.45f, 0.12f, 0.1f));
            SpawnEnemy(root.transform, player.position + player.forward * 8.5f + player.right * 1.4f, toughEnemyLoot, "[MVP] Tough Loot Target", 85f, 35, new Color(0.28f, 0.08f, 0.12f), 1.2f);
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

        private static void SpawnIronOre(Transform parent, Vector3 position, ResourceNodeData data)
        {
            var ore = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ore.name = "[MVP] Harvestable Iron Ore";
            ore.transform.SetParent(parent);
            ore.transform.position = position + Vector3.up * 0.55f;
            ore.transform.localScale = new Vector3(1.25f, 1.1f, 1.05f);

            var renderer = ore.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = new Color(0.34f, 0.29f, 0.24f);

            ore.AddComponent<IronOreNode>().Configure(data);
        }

        private static void SpawnGromovytsia(Transform parent, Vector3 position, ResourceNodeData data)
        {
            var node = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            node.name = "[MVP] Harvestable Gromovytsia";
            node.transform.SetParent(parent);
            node.transform.position = position + Vector3.up * 0.7f;
            node.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);

            var renderer = node.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = new Color(0.25f, 0.5f, 0.95f);

            node.AddComponent<GromovytsiaNode>().Configure(data);
        }

        private static void SpawnBerryBush(Transform parent, Vector3 position, ItemData berryItem)
        {
            if (berryItem == null)
                return;

            var bush = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bush.name = "[MVP] Berry Bush";
            bush.transform.SetParent(parent);
            bush.transform.position = position + Vector3.up * 0.45f;
            bush.transform.localScale = new Vector3(1.1f, 0.75f, 1.1f);

            var renderer = bush.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = new Color(0.2f, 0.42f, 0.18f);

            bush.AddComponent<BerryBushResourceNode>().Configure(berryItem);
        }

        private static void SpawnEnemy(
            Transform parent,
            Vector3 position,
            LootTableData lootTable,
            string enemyName,
            float health,
            int fallbackSoulAsh,
            Color color,
            float scale = 1f)
        {
            var enemyObject = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            enemyObject.name = enemyName;
            enemyObject.transform.SetParent(parent);
            enemyObject.transform.position = position + Vector3.up * scale;
            enemyObject.transform.localScale = Vector3.one * scale;

            var renderer = enemyObject.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = color;

            var enemy = enemyObject.AddComponent<SimpleEnemy>();
            enemy.Health = health;
            enemy.SoulAshReward = fallbackSoulAsh;
            enemyObject.AddComponent<LootDropper>().Configure(lootTable);
        }
    }
}
