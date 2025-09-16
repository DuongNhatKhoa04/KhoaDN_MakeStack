using UnityEngine;
using UnityEngine.InputSystem;

namespace MakeStack.Input
{
    public class InputHandler : MonoBehaviour
    {
        #region --- Fields ---

        [SerializeField] private float minSwipeDistance = 50f;
        [SerializeField] private float maxSlideDistance = 100f;
        [SerializeField] private float slideSpeed;
        [SerializeField] private LayerMask wallLayerMask;
        [SerializeField] private Transform baseTransform;
        [SerializeField] private Collider baseCollider;
        [SerializeField] private Rigidbody baseRigidbody;

        private PlayerInput _playerInput;
        
        private Vector3 _targetPos;
        private Vector3 _moveDir;
        
        private Vector2 _startPos;
        private Vector2 _endPos;
        
        private bool _isSwipe;
        private bool _isSliding;

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

            if (_moveDir != Vector3.zero)
                CalculateTargetPosition(_moveDir);
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
        
        #endregion
    }
}
