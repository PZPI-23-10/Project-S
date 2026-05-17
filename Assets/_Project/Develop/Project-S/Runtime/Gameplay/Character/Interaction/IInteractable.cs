namespace Project_S.Runtime.Gameplay.Character.Interaction
{
    public interface IInteractable
    {
        string InteractionPrompt { get; }
        void Interact(PlayerInteractor interactor);
    }
}
