using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace MakeStack.Input
{
    public class InputHandler : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float _minSwipeDistance = 50f;
        [SerializeField] private float _maxSlideDistance = 100f;
        [SerializeField] private LayerMask _wallLayerMask;
        [SerializeField] private Transform _baseTransform;
        [SerializeField] private Collider _baseCollider;
        [SerializeField] private Rigidbody _baseRigidbody;
        [SerializeField] private float _slideSpeed = 5f;

        private PlayerInput _playerInput;
        private Vector2 _startPos;
        private Vector2 _endPos;
        private bool _isSwipe;
        private Vector3 _targetPos;
        private bool _isSliding;
        private Vector3 _moveDir;

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
            if (_playerInput != null)
            {
                _playerInput.Swipe.PointerPress.started -= OnSwipeStarted;
                _playerInput.Swipe.PointerPress.canceled -= OnSwipeEnd;
                _playerInput.Swipe.Disable();
            }
        }

        private void Update()
        {
            Sliding();
        }

        private void Sliding()
        {
            if (_isSliding)
            {
                Vector3 newPos = Vector3.MoveTowards(_baseRigidbody.position, _targetPos, _slideSpeed * Time.deltaTime);
                _baseRigidbody.MovePosition(newPos);

                if (Vector3.Distance(_baseRigidbody.position, _targetPos) < 0.1f)
                {
                    _baseRigidbody.MovePosition(_targetPos);
                    _isSliding = false;
                }
            }
        }

        private void OnSwipeStarted(InputAction.CallbackContext context)
        {
            _startPos = _playerInput.Swipe.PointerDirector.ReadValue<Vector2>();
            _isSwipe = true;
        }

        private void OnSwipeEnd(InputAction.CallbackContext context)
        {
            if (!_isSwipe) return;

            _endPos = _playerInput.Swipe.PointerDirector.ReadValue<Vector2>();
            DetectDirection();
            _isSwipe = false;
        }

        private void DetectDirection()
        {
            Vector2 swipeVector = _endPos - _startPos;
            if (swipeVector.magnitude < _minSwipeDistance) return;

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
            Debug.Log("Hi");
            RaycastHit hit;
            Vector3 origin = _baseTransform.position;

            if (Physics.Raycast(origin, dir, out hit, _maxSlideDistance, _wallLayerMask))
            {
                Debug.Log("Can");
                float distance = hit.distance - 0.5f;
                _targetPos = transform.position + dir * distance;
            }
            else
            {
                Debug.Log("Cant");
                _targetPos = transform.position + dir * _maxSlideDistance;
            }

            _isSliding = true;
        }

    }
}
