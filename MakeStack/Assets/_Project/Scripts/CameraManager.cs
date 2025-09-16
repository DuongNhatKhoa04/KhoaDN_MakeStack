using MakeStack.Mechanic;
using UnityEngine;

namespace MakeStack.Camera
{
    public class CameraManager : MonoBehaviour
    {
        [SerializeField] private Transform playerTransform;
        [SerializeField] private CollectBrick collectBrick;

        [Header("Offsets")]
        [SerializeField] private float baseHeight;
        [SerializeField] private float heightPerBrick;
        [SerializeField] private float depthOffset;

        [Header("Rotation")]
        [SerializeField] private float minAngleX;
        [SerializeField] private float maxAngleX;
        [SerializeField] private int maxBrickForMaxAngle;

        [SerializeField] private float smoothSpeed;

        void LateUpdate()
        {
            if (playerTransform == null || collectBrick == null) return;

            var playerPos = playerTransform.position;
            var currentPos = transform.position;
            
            float targetHeight = baseHeight + collectBrick.StackCount * heightPerBrick;
            float targetDepth = playerPos.z + depthOffset;
            
            Vector3 targetPos = new Vector3(
                currentPos.x,        
                playerPos.y + targetHeight,
                targetDepth
            );
            
            transform.position = Vector3.Lerp(currentPos, targetPos, Time.deltaTime * smoothSpeed);
            
            float t = Mathf.Clamp01((float)collectBrick.StackCount / maxBrickForMaxAngle);
            float targetAngleX = Mathf.Lerp(minAngleX, maxAngleX, t);

            Quaternion targetRot = Quaternion.Euler(targetAngleX, 0, 0);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * smoothSpeed);
        }
    }
}
