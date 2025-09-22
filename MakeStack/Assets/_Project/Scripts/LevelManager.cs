using MakeStack.Map;
using MakeStack.Mechanic;
using MakeStack.Ultilities;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MakeStack.Manager
{
    public class LevelManager : Singleton<LevelManager>
    {
        [SerializeField] private MapGenerator mapGenerator;
        [SerializeField] private GameObject nextLevelMenu;
        [SerializeField] private Button nextLevelButton;
        [SerializeField] private Transform playerTransform;

        private Vector3 _playerStartPos;

        private void Awake()
        {
            if (playerTransform != null)
                _playerStartPos = playerTransform.position;

            if (nextLevelButton != null)
                nextLevelButton.onClick.AddListener(LoadNextLevel);
        }

        public void OnRunwayFinished()
        {
            nextLevelMenu.SetActive(true);
        }

        public void LoadNextLevel()
        {
            nextLevelMenu.SetActive(false);
            
            CollectBrick playerCollector = null;
            if (playerTransform != null)
                playerCollector = playerTransform.GetComponent<CollectBrick>();
            if (playerCollector == null)
                playerCollector = FindObjectOfType<CollectBrick>();

            if (playerCollector != null)
            {
                CollectBrick.ResetStack(playerCollector);
            }
            
            Input.InputHandler input = null;
            if (playerTransform != null)
                input = playerTransform.GetComponent<Input.InputHandler>();
            if (input == null)
                input = FindObjectOfType<Input.InputHandler>();

            if (input != null)
            {
                input.ResetState();
            }
            
            mapGenerator.ClearMap();
            mapGenerator.GenerateAllStages();
            
            if (playerTransform != null)
                playerTransform.position = _playerStartPos;
        }
    }
}