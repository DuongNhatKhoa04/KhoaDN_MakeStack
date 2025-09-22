using System.Collections;
using MakeStack.Input;
using MakeStack.Manager;
using UnityEngine;

namespace MakeStack.Mechanic
{
    public class Runway : MonoBehaviour
    {
        private bool processed = false;
        private float stayTimer = 0f;
        private bool waitingForMenu = false;

        private void OnTriggerEnter(Collider other)
        {
            Logic(other);
        }

        private void OnTriggerStay(Collider other)
        {
            Logic(other);

            if (waitingForMenu)
            {
                stayTimer += Time.deltaTime;
                if (stayTimer >= 2f)
                {
                    LevelManager.Instance.OnRunwayFinished();
                    waitingForMenu = false;
                }
            }
        }

        private void OnTriggerExit(Collider other)
        {
            stayTimer = 0f;
            waitingForMenu = false;
        }

        private void Logic(Collider other)
        {
            if (processed) return;

            var collector = other.GetComponent<CollectBrick>();
            if (collector == null) return;

            if (collector.StackCount > 0)
            {
                collector.PlaceOneBrick(transform.position);
                processed = true;
                waitingForMenu = true;
            }
            else
            {
                var inputHandler = other.GetComponent<InputHandler>();
                if (inputHandler != null)
                {
                    inputHandler.StopSliding();
                }
                
                waitingForMenu = true;
                stayTimer = 0f;
                processed = true;
            }
        }
    }
}