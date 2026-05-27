namespace Project_S.Runtime.Gameplay.Harvesting
{
    public interface IResourceDepletionHandler
    {
        void HandleResourceDepleted(HarvestableResourceNode node);
    }
}
