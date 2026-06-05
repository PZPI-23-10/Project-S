using System.Collections;
using Cinemachine;
using UnityEngine;

public class RockfallTrigger : MonoBehaviour
{
    [Header("Detection")]
    [SerializeField] private Transform player;
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private BoxCollider triggerBox;

    [Header("Rockfall")]
    [SerializeField] private Rigidbody[] rocks;
    [SerializeField] private GameObject cinemachineVirtualCamera;
    [SerializeField] private int activeCameraPriority = 100;
    [SerializeField] private float cameraDuration = 3f;

    private bool hasTriggered;
    private CinemachineVirtualCamera virtualCamera;
    private int originalCameraPriority;

    private void Awake()
    {
        if (triggerBox == null)
        {
            triggerBox = GetComponent<BoxCollider>();
        }

        FindPlayerIfNeeded();

        if (cinemachineVirtualCamera != null)
        {
            virtualCamera = cinemachineVirtualCamera.GetComponent<CinemachineVirtualCamera>();
            if (virtualCamera != null)
            {
                originalCameraPriority = virtualCamera.Priority;
            }

            cinemachineVirtualCamera.SetActive(false);
        }
    }

    private void Update()
    {
        if (hasTriggered)
        {
            return;
        }

        FindPlayerIfNeeded();

        if (player != null && IsPlayerInsideTriggerVolume())
        {
            hasTriggered = true;
            StartCoroutine(RockfallSequence());
        }
    }

    private void FindPlayerIfNeeded()
    {
        if (player != null || string.IsNullOrEmpty(playerTag))
        {
            return;
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag(playerTag);
        if (playerObject != null)
        {
            player = playerObject.transform;
        }
    }

    private bool IsPlayerInsideTriggerVolume()
    {
        Vector3 center = Vector3.zero;
        Vector3 size = Vector3.one;

        if (triggerBox != null)
        {
            center = triggerBox.center;
            size = triggerBox.size;
        }

        Vector3 localPlayerPosition = transform.InverseTransformPoint(player.position) - center;
        Vector3 halfSize = size * 0.5f;

        return Mathf.Abs(localPlayerPosition.x) <= halfSize.x
            && Mathf.Abs(localPlayerPosition.y) <= halfSize.y
            && Mathf.Abs(localPlayerPosition.z) <= halfSize.z;
    }

    private IEnumerator RockfallSequence()
    {
        if (cinemachineVirtualCamera != null)
        {
            cinemachineVirtualCamera.SetActive(true);
        }

        if (virtualCamera != null)
        {
            virtualCamera.Priority = activeCameraPriority;
        }

        if (rocks == null)
        {
            rocks = new Rigidbody[0];
        }

        foreach (Rigidbody rock in rocks)
        {
            if (rock == null)
            {
                continue;
            }

            rock.isKinematic = false;
            rock.WakeUp();
        }

        yield return new WaitForSeconds(cameraDuration);

        if (cinemachineVirtualCamera != null)
        {
            if (virtualCamera != null)
            {
                virtualCamera.Priority = originalCameraPriority;
            }

            cinemachineVirtualCamera.SetActive(false);
        }

        gameObject.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        BoxCollider box = triggerBox != null ? triggerBox : GetComponent<BoxCollider>();
        Vector3 center = box != null ? box.center : Vector3.zero;
        Vector3 size = box != null ? box.size : Vector3.one;

        Gizmos.color = new Color(1f, 0.6f, 0f, 0.25f);
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.DrawCube(center, size);
        Gizmos.color = new Color(1f, 0.6f, 0f, 1f);
        Gizmos.DrawWireCube(center, size);
        Gizmos.matrix = oldMatrix;
    }
}
