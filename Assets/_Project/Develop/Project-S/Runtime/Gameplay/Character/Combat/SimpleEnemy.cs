using UnityEngine;
using Project_S.Runtime.Gameplay.Character.Combat;

public class SimpleEnemy : MonoBehaviour, IDamageReceiver
{
    [Header("Характеристики")]
    public float Health = 100f;

    public void ReceiveDamage(DamageRequest request)
    {
        // Віднімаємо саме HealthDamage!
        Health -= request.HealthDamage;

        Debug.Log($"<color=red>[Ворог Сфера]</color> Ай! Отримано {request.HealthDamage} урону. Залишилось ХП: {Health}");

        if (Health <= 0)
        {
            Debug.Log("<color=black>[Ворог Сфера]</color> ЗНИЩЕНА!");
            Destroy(gameObject);
        }
    }
}