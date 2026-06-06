using System;
using System.Collections.Generic;
using Project_S.Runtime.Gameplay.Character.Stats;
using UnityEngine;

namespace Project_S.Runtime.Services.Save
{
    [Serializable]
    public class GameSaveData
    {
        public const int CurrentVersion = 2;

        public int Version = CurrentVersion;
        public bool HasSave;
        public long SavedUtcTicks;
        public string ActiveSceneName;
        public PlayerState Player = new PlayerState();
        public WorldState World = new WorldState();
    }

    [Serializable]
    public class PlayerState
    {
        public SaveVector3 Position;
        public SaveQuaternion Rotation;
        public InventoryState Inventory = new InventoryState();
        public int SoulAsh;
        public EquipmentState Equipment = new EquipmentState();
        public CombatSaveState Combat = new CombatSaveState();
        public List<string> AccessoryItemIds = new List<string>();
        public Dictionary<StatType, float> Stats = new Dictionary<StatType, float>();
        public List<string> PurchasedUpgradeIds = new List<string>();
    }

    [Serializable]
    public class WorldState
    {
        public HashSet<string> Flags = new HashSet<string>();
        public Dictionary<string, InventoryState> Inventories = new Dictionary<string, InventoryState>();
        public Dictionary<string, CraftingStationState> CraftingStations = new Dictionary<string, CraftingStationState>();
        public Dictionary<string, ResourceNodeState> Resources = new Dictionary<string, ResourceNodeState>();
        public Dictionary<string, EnemyState> Enemies = new Dictionary<string, EnemyState>();
        public PickupWorldState Pickups = new PickupWorldState();

        public bool HasFlag(string key)
        {
            return !string.IsNullOrWhiteSpace(key) && Flags != null && Flags.Contains(key);
        }

        public void SetFlag(string key, bool value)
        {
            if (string.IsNullOrWhiteSpace(key))
                return;

            Flags ??= new HashSet<string>();

            if (value)
                Flags.Add(key);
            else
                Flags.Remove(key);
        }
    }

    [Serializable]
    public class InventoryState
    {
        public List<ItemStackSaveData> Slots = new List<ItemStackSaveData>();
        public int SoulAsh;
    }

    [Serializable]
    public class EquipmentState
    {
        public List<string> ItemIds = new List<string>();
        public int CurrentSlot;
    }

    [Serializable]
    public class CombatSaveState
    {
        public string CurrentWeaponId;
        public string OffhandWeaponId;
    }

    [Serializable]
    public class CraftingStationState
    {
        public float FuelSeconds;
        public string ActiveRecipeId;
        public float ActiveDurationSeconds;
        public float RemainingCraftSeconds;
    }

    [Serializable]
    public class ResourceNodeState
    {
        public float CurrentHealth;
        public bool Depleted;
    }

    [Serializable]
    public class EnemyState
    {
        public float CurrentHealth;
        public bool Dead;
    }

    [Serializable]
    public class PickupWorldState
    {
        public HashSet<string> CollectedAuthoredIds = new HashSet<string>();
        public List<RuntimePickupState> RuntimeDropped = new List<RuntimePickupState>();
    }

    [Serializable]
    public class RuntimePickupState
    {
        public string Id;
        public string SceneName;
        public string ItemId;
        public int Amount;
        public SaveVector3 Position;
        public SaveQuaternion Rotation;
    }

    [Serializable]
    public class ItemStackSaveData
    {
        public string ItemId;
        public int Amount;
    }

    [Serializable]
    public struct SaveVector3
    {
        public float X;
        public float Y;
        public float Z;

        public SaveVector3(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static SaveVector3 From(Vector3 value)
        {
            return new SaveVector3(value.x, value.y, value.z);
        }

        public Vector3 ToVector3()
        {
            return new Vector3(X, Y, Z);
        }
    }

    [Serializable]
    public struct SaveQuaternion
    {
        public float X;
        public float Y;
        public float Z;
        public float W;

        public SaveQuaternion(float x, float y, float z, float w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public static SaveQuaternion From(Quaternion value)
        {
            return new SaveQuaternion(value.x, value.y, value.z, value.w);
        }

        public Quaternion ToQuaternion()
        {
            return new Quaternion(X, Y, Z, W);
        }
    }
}
