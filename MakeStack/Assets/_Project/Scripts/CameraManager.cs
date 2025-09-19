using MakeStack.Mechanic;
using UnityEngine;

namespace MakeStack.Manager
{
    public class CameraManager : MonoBehaviour
    {
        [SerializeField] private Transform playerTransform;
        [SerializeField] private CollectBrick collectBrick;

        [Header("Offsets")]
        [SerializeField] private float baseHeight;
        [SerializeField] private float heightPerBrick;
        [SerializeField] private float baseDepthOffset;
        [SerializeField] private float extraDepthPerBrick;
        [SerializeField] private float maxExtraDepth;
        [SerializeField] private float smoothSpeed;

        void LateUpdate()
        {
            if (playerTransform == null || collectBrick == null) return;

            var playerPos = playerTransform.position;
            var currentPos = transform.position;
            
            float targetHeight = baseHeight + collectBrick.StackCount * heightPerBrick;
            
            float extraDepth = Mathf.Clamp(
                collectBrick.StackCount * extraDepthPerBrick,
                maxExtraDepth,
                0f
            );
            float targetDepth = playerPos.z + baseDepthOffset + extraDepth;
            
            Vector3 targetPos = new Vector3(
                currentPos.x,
                playerPos.y + targetHeight,
                targetDepth
            );
            
            transform.position = Vector3.Lerp(currentPos, targetPos, Time.deltaTime * smoothSpeed);
        }
    }
}