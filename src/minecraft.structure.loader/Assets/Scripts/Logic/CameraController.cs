using System;
using Cysharp.Threading.Tasks;
using UI;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Logic
{
    public class CameraController : MonoBehaviour
    {
        [Header("Pivot")]
        public Transform Target;

        [Header("Orbit")]
        public float OrbitSensitivity = 0.15f;
        public float MinPitch = -85f;
        public float MaxPitch =  85f;
        public bool  InvertY  = true;

        [Header("Pan")]
        public float PanSensitivity = 0.01f;

        [Header("Zoom")]
        public float ZoomSensitivity  = 5f;
        public float MinZoomDistance  = 0.5f;
        public float MaxZoomDistance  = 120f;
        public float ZoomSmoothing    = 8f;

        [Header("Smoothing (orbit & pan)")]
        public float RotationSmoothing = 10f;

        [Header("Orbit animation params")]
        public AnimationCurve SpinRotationCurve;
        public float SpinAngle = 360f;
        
        public bool InputDisabled;

        private Vector3 _pivotOffset;
        private Vector3 _resetPivotPosition;
        private Vector3 _pivotPosition;
        private float _yaw;
        private float _pitch;
        private float _targetDistance;
        private float _currentDistance;

        private Quaternion _currentRotation;
        private Quaternion _targetRotation;

        private Mouse _mouse;
        private Camera _camera;
        
        private bool _startedLeftButtonDragOverUI;
        private bool _isLeftButtonPressedLastFrame;
    
        private void Start()
        {
            _camera = GetComponent<Camera>();

            if (Target != null)
                _pivotPosition = Target.position;
            else
                _pivotPosition = transform.position + transform.forward * 10f;

            _currentDistance = _targetDistance =
                Vector3.Distance(transform.position, _pivotPosition);
            _currentDistance = Mathf.Clamp(_currentDistance, MinZoomDistance, MaxZoomDistance);
            _targetDistance  = _currentDistance;

            Vector3 euler = transform.rotation.eulerAngles;
            _yaw   = euler.y;
            _pitch = euler.x;
            
            if (_pitch > 180f) _pitch -= 360f;

            _currentRotation = _targetRotation = Quaternion.Euler(_pitch, _yaw, 0f);
        
            _mouse = Mouse.current;
        }

        private void Update()
        {
            if (InputDisabled) return;
            
            bool isLeftButtonPressed = _mouse != null && _mouse.leftButton.isPressed;

            if (isLeftButtonPressed && !_isLeftButtonPressedLastFrame)
                _startedLeftButtonDragOverUI = UIRoot.IsPointerOverUI();

            _isLeftButtonPressedLastFrame = isLeftButtonPressed;
        }

        private void LateUpdate()
        {
            if (!InputDisabled)
            {
                HandleOrbit();
                HandlePan();
                HandleZoom();
            }
            
            ApplyTransform();
        }

        private void HandleOrbit()
        {
            if (_startedLeftButtonDragOverUI) return;
            if (_mouse == null || !_mouse.leftButton.isPressed) return;
 
            Vector2 delta = _mouse.delta.ReadValue();
 
            float deltaX =  delta.x * OrbitSensitivity;
            float deltaY = (delta.y * OrbitSensitivity) * (InvertY ? -1f : 1f);
 
            _yaw   += deltaX;
            _pitch  = Mathf.Clamp(_pitch + deltaY, MinPitch, MaxPitch);
 
            _targetRotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }
 
        private void HandlePan()
        {
            if (_mouse == null || !_mouse.rightButton.isPressed) return;
 
            Vector2 delta = _mouse.delta.ReadValue();
 
            float deltaX = -delta.x;
            float deltaY = -delta.y;
 
            float scale = _currentDistance * PanSensitivity;
 
            Vector3 right = _currentRotation * Vector3.right;
            Vector3 up    = _currentRotation * Vector3.up;

            _pivotOffset += (right * deltaX + up * deltaY) * scale;
            _pivotPosition = _resetPivotPosition + Vector3.ClampMagnitude(_pivotOffset, MaxZoomDistance);
        }

        private void HandleZoom()
        { 
            // Drain accumulated scroll delta from web
            float scroll = GetScroll();
            
            if (UIRoot.IsPointerOverUI()) return;
            if (Mathf.Abs(scroll) < float.Epsilon) return;

            _targetDistance -= scroll * ZoomSensitivity * _currentDistance;
            _targetDistance  = Mathf.Clamp(_targetDistance, MinZoomDistance, MaxZoomDistance);
        }

        private float GetScroll()
        {
            float scroll;
#if UNITY_WEBGL && !UNITY_EDITOR
            scroll = ScrollWheelBridge.GetScrollDelta();
#else
            if (_mouse == null) return 0f;
            scroll = _mouse.scroll.ReadValue().y;
#endif
            return scroll * 0.01f;
        }
 
        private void ApplyTransform()
        {
            float t = Time.deltaTime;
 
            if (RotationSmoothing > 0f)
                _currentRotation = Quaternion.Slerp(
                    _currentRotation, _targetRotation, t * RotationSmoothing);
            else
                _currentRotation = _targetRotation;
 
            if (ZoomSmoothing > 0f)
                _currentDistance = Mathf.Lerp(
                    _currentDistance, _targetDistance, t * ZoomSmoothing);
            else
                _currentDistance = _targetDistance;
 
            Vector3 offset = _currentRotation * new Vector3(0f, 0f, -_currentDistance);
            transform.position = _pivotPosition + offset;
            transform.rotation = _currentRotation;
        }

        public void ResetPivot()
        {
            _pivotOffset = Vector3.zero;
            _pivotPosition = _resetPivotPosition;
        }

        public void SetPivot(Vector3 worldPosition)
        {
            _pivotOffset = Vector3.zero;
            _pivotPosition = worldPosition;
            _resetPivotPosition = worldPosition;
        }

        private void SetAngles(float yaw, float pitch)
        {
            _yaw   = yaw;
            _pitch = Mathf.Clamp(pitch, MinPitch, MaxPitch);
            _targetRotation  = Quaternion.Euler(_pitch, _yaw, 0f);
            _currentRotation = _targetRotation;
        }

        public async UniTaskVoid DoOrbitAnimationAsync(Bounds structureBounds, float duration = 1f, Action onComplete = null)
        {
            SetAngles(0f, 15f);
            _targetDistance = GetDistanceToFit(_camera, structureBounds);
            _currentDistance = _targetDistance;
            MaxZoomDistance = 1.5f * _targetDistance;
            
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float normalized = elapsed / duration;

                float f = SpinRotationCurve.Evaluate(normalized) * SpinAngle;
                _yaw = f;
                _targetRotation = Quaternion.Euler(_pitch, _yaw, 0f);
                elapsed += Time.deltaTime;
                await UniTask.Yield();
            }
            
            onComplete?.Invoke();
        }

        private static float GetDistanceToFit(Camera camera, Bounds bounds)
        {
            float radius = bounds.extents.magnitude;

            float verticalFov = camera.fieldOfView * Mathf.Deg2Rad;
            float horizontalFov = 2f * Mathf.Atan(
                Mathf.Tan(verticalFov * 0.5f) * camera.aspect);

            float distanceV = radius / Mathf.Sin(verticalFov * 0.5f);
            float distanceH = radius / Mathf.Sin(horizontalFov * 0.5f);

            return Mathf.Max(distanceV, distanceH);
        }
        
#if UNITY_EDITOR
        private void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.8f);
            Gizmos.DrawWireSphere(_pivotPosition, 0.15f);
            Gizmos.DrawLine(transform.position, _pivotPosition);
        }
#endif
    }
}