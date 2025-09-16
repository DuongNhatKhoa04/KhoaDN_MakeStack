using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace MakeStack.Mechanic
{
    public class CollectBrick : MonoBehaviour
    {
        #region --- Fields ---

        [SerializeField] private float objHeight = 0.45f;
        [SerializeField] private Transform baseTransform;
        
        private int _stackCounter;

        #endregion
        
        #region --- Properties ---
        
        public static int TotalBricksCollected { get; private set; }
        public int StackCount => _stackCounter;
        
        #endregion

        #region --- Methods ---
        
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

        public static void ResetCounter()
        {
            TotalBricksCollected = 0;
        }
        
        #endregion
    }
}