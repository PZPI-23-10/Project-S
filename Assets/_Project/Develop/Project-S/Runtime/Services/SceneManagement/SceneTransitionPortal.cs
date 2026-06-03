using Project_S.Runtime.Gameplay.Character.Interaction;
using UnityEngine;

namespace Project_S.Runtime.Services.SceneManagement
{
    public class SceneTransitionPortal : MonoBehaviour, IInteractable, IInteractionActionText
    {
        [SerializeField] private string _targetSceneName;
        [SerializeField] private string _targetSpawnId;
        [SerializeField] private string _interactionPrompt = "Scene Portal";
        [SerializeField] private string _interactionActionText = "E - Enter";

        public string InteractionPrompt => string.IsNullOrWhiteSpace(_interactionPrompt)
            ? name
            : _interactionPrompt;

        public string InteractionActionText => _interactionActionText;

        public void Interact(PlayerInteractor interactor)
        {
            if (string.IsNullOrWhiteSpace(_targetSceneName))
            {
                Debug.LogWarning($"[{nameof(SceneTransitionPortal)}] Target scene is not set.", this);
                return;
            }

            SceneTransitionRequestBus.RequestTransition(_targetSceneName, _targetSpawnId);
        }
    }
}
