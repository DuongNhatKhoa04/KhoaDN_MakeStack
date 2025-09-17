using MakeStack.Manager;
using MakeStack.Map;
using MakeStack.Mechanic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MakeStack.Input
{
    public class InputHandler : MonoBehaviour
    {
        #region --- Fields ---
        
        [SerializeField] private Transform[] startPoints;
        [SerializeField] private Transform[] endPoints;
        [SerializeField] private GameObject[] stages;
        
        [SerializeField] private float minSwipeDistance = 50f;
        [SerializeField] private float maxSlideDistance = 100f;
        [SerializeField] private float slideSpeed;
        
        [SerializeField] private LayerMask wallLayerMask;
        [SerializeField] private Transform baseTransform;
        [SerializeField] private Collider baseCollider;
        [SerializeField] private Rigidbody baseRigidbody;
        [SerializeField] private CollectBrick brickCollector;
        [SerializeField] private TextMeshProUGUI introText;

        private PlayerInput _playerInput;
        
        private Vector3 _targetPos;
        private Vector3 _moveDir;
        
        private Vector2 _startPos;
        private Vector2 _endPos;
        
        private bool _isSwipe;
        private bool _isSliding;
        private bool _isScoring;

        private int _currentStage = 0;

        #endregion

        #region --- Properties ---
        
        private Transform Transform
        {
            get
            {
                if (baseTransform) return baseTransform;
                
                baseTransform = gameObject.transform;
                return baseTransform;
            }
        }
        public Vector3 LocalPosition
        {
            get => Transform.localPosition;
            set => Transform.localPosition = value;
        }

        public bool LevelPass { get; set; }
        public bool StagesPass { get; set; }
        
        #endregion

        #region --- Methods ---
        
        private void Awake()
        {
            _playerInput = new PlayerInput();
        }

        private void OnEnable()
        {
            _playerInput.Swipe.Enable();
            _playerInput.Swipe.PointerPress.started += OnSwipeStarted;
            _playerInput.Swipe.PointerPress.canceled += OnSwipeEnd;
        }

        private void OnDisable()
        {
            if (_playerInput == null) return;
            
            _playerInput.Swipe.PointerPress.started -= OnSwipeStarted;
            _playerInput.Swipe.PointerPress.canceled -= OnSwipeEnd;
            _playerInput.Swipe.Disable();
        }

        private void Update()
        {
            Sliding();
        }
        
        private void OnSwipeStarted(InputAction.CallbackContext context)
        {
            if (_isSliding) return;

            _startPos = _playerInput.Swipe.PointerDirector.ReadValue<Vector2>();
            _isSwipe = true;
            introText.enabled = false;
        }

        private void OnSwipeEnd(InputAction.CallbackContext context)
        {
            if (_isSliding) return;

            if (!_isSwipe) return;

            _endPos = _playerInput.Swipe.PointerDirector.ReadValue<Vector2>();
            
            DetectDirection();
            _isSwipe = false;
        }

        private void Sliding()
        {
            if (!_isSliding) return;
            
            if (_isScoring && CollectBrick.TotalBricksCollected == 0)
            {
                Debug.Log("Sliding");
                _isSliding = false;
                _isScoring = false;
                return;
            }
            
            var newPos = Vector3.MoveTowards(baseRigidbody.position, _targetPos, slideSpeed * Time.deltaTime);
            baseRigidbody.MovePosition(newPos);

            if (Vector3.Distance(baseRigidbody.position, _targetPos) >= 0.1f) return;
            
            baseRigidbody.MovePosition(_targetPos);
            _isSliding = false;
        }

        private void DetectDirection()
        {
            var swipeVector = _endPos - _startPos;
            if (swipeVector.magnitude < minSwipeDistance) return;

            swipeVector.Normalize();
            _moveDir = Vector3.zero;

            if (Vector2.Dot(swipeVector, Vector2.right) > 0.7f)
                _moveDir = Vector3.right;
            else if (Vector2.Dot(swipeVector, Vector2.left) > 0.7f)
                _moveDir = Vector3.left;
            else if (Vector2.Dot(swipeVector, Vector2.up) > 0.7f)
                _moveDir = Vector3.forward;
            else if (Vector2.Dot(swipeVector, Vector2.down) > 0.7f)
                _moveDir = Vector3.back;

            if (_moveDir == Vector3.zero) return;

            if (_currentStage <= 1 && IsAtEndPoint(_currentStage) && _moveDir == Vector3.forward)
            {
                if (CollectBrick.TotalBricksCollected >= MapGenerator.BrickNeedToPass)
                {
                    stages[_currentStage].layer = 0;

                    var nextStage = _currentStage + 1;
                
                    if (nextStage > startPoints.Length) return;
                
                    CollectBrick.ResetStack(brickCollector);

                    StagesPass = true;

                    _targetPos = startPoints[nextStage].position;
                    _isSliding = true;
                    _currentStage = nextStage;
                    Debug.Log("next "+ nextStage);
                }
                else
                {
                    StagesPass = false;
                }
            }
            else if (_currentStage == 2 && IsFinalStagePoint())
            {
                Debug.Log("Final");
                stages[_currentStage].layer = 0;
                _targetPos = transform.position + _moveDir * maxSlideDistance;
                _isSliding = true;
                _isScoring = true;
            }
            else
            {
                CalculateTargetPosition(_moveDir);
            }
        }

        private void CalculateTargetPosition(Vector3 dir)
        {
            var origin = baseTransform.position;

            if (Physics.Raycast(origin, dir, out RaycastHit hit, maxSlideDistance, wallLayerMask))
            {
                //Debug.Log("Can");
                var distance = hit.distance - 0.5f;
                _targetPos = transform.position + dir * distance;
            }
            else
            {
                //Debug.Log("Cant");
                _targetPos = transform.position + dir * maxSlideDistance;
            }

            _isSliding = true;
        }

        private bool IsAtEndPoint(int stage)
        {
            var endPointRTarget = endPoints[stage];
            return Vector3.Distance(endPointRTarget.position, LocalPosition) < 0.1f;
        }

        private bool IsFinalStagePoint()
        {
            var endPointRTarget = endPoints[2];
            return Vector3.Distance(endPointRTarget.position, LocalPosition) < 0.1f;
        }
        
        #endregion
    }
}
