using System.Collections;
using System.Collections.Generic;
using MakeStack.Mechanic;
using TMPro;
using UnityEngine;

namespace MakeStack.Manager
{
    public class LevelManager : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI scoreText;

        private void Update()
        {
            scoreText.text = (CollectBrick.TotalBricksCollected * 10).ToString();
        }
    }
}