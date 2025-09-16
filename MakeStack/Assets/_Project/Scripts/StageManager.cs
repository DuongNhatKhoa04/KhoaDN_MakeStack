using MakeStack.Map;
using MakeStack.Ultilities;
using System;
using System.Collections;
using System.Collections.Generic;
using MakeStack.Mechanic;
using UnityEngine;

namespace MakeStack.Manager
{
    public class StageManager : MonoBehaviour
    {
        public int BrickNeeded { get; set; }
        public bool IsFinalStage { get; set; }

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player")) return;

            if (IsFinalStage)
            {
                Debug.Log("[StageManager] Final stage reached! Start runway stacking...");
                // TODO: gọi logic runway hoặc báo MapGenerator
                return;
            }

            if (CollectBrick.TotalBricksCollected >= BrickNeeded)
            {
                Debug.Log("[StageManager] Stage Passed!");
                // TODO: cho phép mở cổng, load tiếp stage, v.v.
            }
            else
            {
                Debug.Log("[StageManager] Not enough bricks! Need " + BrickNeeded +
                          ", have " + CollectBrick.TotalBricksCollected);
                // TODO: block player hoặc bật hiệu ứng
            }
        }
    }
}