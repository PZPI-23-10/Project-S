using System;
using UnityEngine;

namespace Project_S.Runtime.Services.Save
{
    [DisallowMultipleComponent]
    public class SaveableObjectId : MonoBehaviour
    {
        [SerializeField] private string _id;

        public string Id => _id;

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (string.IsNullOrWhiteSpace(_id))
                _id = Guid.NewGuid().ToString("N");

            foreach (var other in FindObjectsByType<SaveableObjectId>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (other != null && other != this && other._id == _id)
                {
                    Debug.LogWarning($"[Save] Duplicate SaveableObjectId '{_id}' on {name} and {other.name}.", this);
                    break;
                }
            }
        }
#endif
    }
}
