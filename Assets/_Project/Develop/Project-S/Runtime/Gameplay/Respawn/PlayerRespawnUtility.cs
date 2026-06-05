using KinematicCharacterController;
using Project_S.Runtime.Gameplay.Character.Stats;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Respawn
{
    public static class PlayerRespawnUtility
    {
        public static void MovePlayer(Component player, Vector3 position, Quaternion rotation)
        {
            if (player == null)
                return;

            KinematicCharacterMotor motor = player.GetComponent<KinematicCharacterMotor>();
            if (motor != null)
            {
                motor.BaseVelocity = Vector3.zero;
                motor.SetPositionAndRotation(position, rotation);
                return;
            }

            player.transform.SetPositionAndRotation(position, rotation);
        }

        public static void RestoreHealthToMax(CharacterStats stats)
        {
            if (stats == null)
                return;

            float maxHealth = stats.Get(StatType.MaxHealth);
            if (maxHealth <= 0f)
                maxHealth = stats.GetMax(StatType.Health);

            if (maxHealth > 0f)
                stats.Set(StatType.Health, maxHealth);
        }

        public static void RestoreStatFraction(CharacterStats stats, StatType statType, StatType maxStatType, float fraction)
        {
            if (stats == null)
                return;

            float max = stats.Get(maxStatType);
            if (max <= 0f)
                max = stats.GetMax(statType);

            if (max > 0f)
                stats.Set(statType, Mathf.Max(1f, max * Mathf.Clamp01(fraction)));
        }
    }
}
