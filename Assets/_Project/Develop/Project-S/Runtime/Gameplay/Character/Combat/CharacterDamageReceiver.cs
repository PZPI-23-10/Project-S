using Project_S.Runtime.Gameplay.Character.Stats;
using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Combat
{
    public class CharacterDamageReceiver : MonoBehaviour, IDamageReceiver
    {
        [SerializeField] private CharacterStats _stats;
        [SerializeField] private BlockController _block;
        [SerializeField] private PoiseController _poise;

        public void ReceiveDamage(DamageRequest request)
        {
            var modifiedRequest = _block != null ? _block.ModifyIncomingDamage(request) : request;

            _stats.Add(StatType.Health, -modifiedRequest.HealthDamage);
            _poise?.ApplyPoiseDamage(modifiedRequest.PoiseDamage);
        }
    }
}
