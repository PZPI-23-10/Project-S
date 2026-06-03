using System.Collections.Generic;

namespace Project_S.Runtime.Gameplay.Upgrades
{
    public class UpgradeCheck
    {
        private readonly List<string> _problems = new List<string>();

        public IReadOnlyList<string> Problems => _problems;
        public bool CanPurchase => _problems.Count == 0;
        public string Message => CanPurchase ? "Готово" : string.Join("\n", _problems);

        public void AddProblem(string problem)
        {
            if (!string.IsNullOrWhiteSpace(problem))
                _problems.Add(problem);
        }
    }
}
