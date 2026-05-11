using UnityEngine;

namespace Project_S.Runtime.Gameplay.Character.Stats
{
    public class SurvivalController : MonoBehaviour
    {
        [SerializeField] private CharacterStats _stats;
        [SerializeField] private float _hungerPerSecond = 0.03f;
        [SerializeField] private float _thirstPerSecond = 0.05f;
        [SerializeField] private float _cursePerSecondWithoutCharge = 0.02f;

        private void Update()
        {
            _stats.Add(StatType.Hunger, _hungerPerSecond * Time.deltaTime);
            _stats.Add(StatType.Thirst, _thirstPerSecond * Time.deltaTime);

            if (_stats.Get(StatType.PhylacteryCharge) <= 0f)
                _stats.Add(StatType.Curse, _cursePerSecondWithoutCharge * Time.deltaTime);
        }
    }
}
