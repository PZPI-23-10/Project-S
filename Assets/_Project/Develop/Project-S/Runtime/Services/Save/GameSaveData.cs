using System;
using System.Collections.Generic;
using Project_S.Runtime.Gameplay.Character.Stats;
using UnityEngine;

namespace Project_S.Runtime.Services.Save
{
    [Serializable]
    public class GameSaveData
    {
        public const int CurrentVersion = 1;

        public int Version = CurrentVersion;
        public bool HasSave;
        public long SavedUtcTicks;
        public string ActiveSceneName;
        public PlayerSaveData Player = new PlayerSaveData();
        public List<SceneSaveData> Scenes = new List<SceneSaveData>();
    }

    [Serializable]
    public class PlayerSaveData
    {
        public SaveVector3 Position;
        public SaveQuaternion Rotation;
        public List<ItemStackSaveData> InventorySlots = new List<ItemStackSaveData>();
        public int SoulAsh;
        public List<string> EquipmentItemIds = new List<string>();
        public int CurrentEquipmentSlot;
        public string CurrentWeaponId;
        public string OffhandWeaponId;
        public List<string> AccessoryItemIds = new List<string>();
        public List<StatValueSaveData> Stats = new List<StatValueSaveData>();
    }

    [Serializable]
    public class SceneSaveData
    {
        public string SceneName;
        public List<WorldObjectSaveData> Objects = new List<WorldObjectSaveData>();
        public List<WorldPickupSaveData> RuntimePickups = new List<WorldPickupSaveData>();
    }

    [Serializable]
    public class WorldObjectSaveData
    {
        public string Id;
        public string Type;
        public List<ItemStackSaveData> Slots = new List<ItemStackSaveData>();
        public int SoulAsh;
        public float FuelSeconds;
        public string ActiveRecipeId;
        public float ActiveDurationSeconds;
        public float RemainingCraftSeconds;
        public float CurrentHealth;
        public bool Depleted;
        public bool Dead;
        public bool BossDefeated;
        public bool PortalClosed;
        public string ItemId;
        public int Amount;
        public bool Collected;
    }

    [Serializable]
    public class WorldPickupSaveData
    {
        public string Id;
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
    public class StatValueSaveData
    {
        public StatType Type;
        public float Value;
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
