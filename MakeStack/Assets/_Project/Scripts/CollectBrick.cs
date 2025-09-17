using System.Collections.Generic;
using UnityEngine;

namespace MakeStack.Mechanic
{
    public class CollectBrick : MonoBehaviour
    {
        [SerializeField] private float objHeight = 0.45f;
        [SerializeField] private Transform baseTransform;
        [SerializeField] private MeshRenderer brickMeshRenderer;

        private Vector3 _baseOriginPos;
        private int _stackCounter;

        public static int TotalBricksCollected { get; private set; }
        public int StackCount => _stackCounter;

        private void Awake()
        {
            if (baseTransform != null)
                _baseOriginPos = baseTransform.position;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (other.CompareTag("Stackable"))
            {
                var newPos = baseTransform.position;
                newPos.y += objHeight;
                baseTransform.position = newPos;

                var stack = other.transform;
                other.tag = "Untagged";
                stack.SetParent(this.transform);
                stack.localPosition = new Vector3(0, 0, _stackCounter * objHeight);
                
                _stackCounter++;
                TotalBricksCollected++;
            }
        }

        private void OnTriggerExit(Collider other)
        {
            /*if (!other.CompareTag("Runway")) return;*/
            
            if (other.CompareTag("Runway"))
            {
                brickMeshRenderer.enabled = true;
                TotalBricksCollected--;
                
                if (TotalBricksCollected > 0)
                {
                    var newPos = baseTransform.position;
                    newPos.y -= objHeight;
                    baseTransform.position = newPos;
                }
            }
        }
        
        public static void ResetStack(CollectBrick collector)
        {
            if (collector == null) return;

            int removedCount = 0;

            for (var i = collector.transform.childCount - 1; i >= 0; i--)
            {
                var child = collector.transform.GetChild(i);

                if (child.name == "dimian")
                {
                    Object.Destroy(child.gameObject);
                    removedCount++;
                }
            }
            
            if (collector.baseTransform != null && removedCount > 0)
            {
                var newPos = collector.baseTransform.position;
                newPos.y -= collector.objHeight * removedCount;
                collector.baseTransform.position = newPos;
            }
            
            TotalBricksCollected = 0;
            collector._stackCounter = 0;
        }
    }
}
