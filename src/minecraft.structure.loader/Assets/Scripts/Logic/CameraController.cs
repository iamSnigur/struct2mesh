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
        [Tooltip("Optional: assign a Transform to use as the initial pivot. " +
                 "Leave empty to start with a pivot directly in front of the camera.")]
        public Transform Target;

        [Header("Orbit")]
        public float orbitSensitivity = 0.5f;
        [Tooltip("Clamp vertical (pitch) angle in degrees.")]
        public float minPitch = -80f;
        public float maxPitch =  80f;
        public bool  invertY  = false;

        [Header("Pan")]
        public float panSensitivity = 0.01f;

        [Header("Zoom")]
        public float zoomSensitivity  = 2f;
        public float minZoomDistance  = 0.5f;
        public float maxZoomDistance  = 100f;
        public float zoomSmoothing    = 8f;   // 0 = instant

        [Header("Smoothing (orbit & pan)")]
        [Tooltip("Set to 0 for instant response.")]
        public float rotationSmoothing = 10f;

        [Header("Orbit animation params")]
        public AnimationCurve SpinRotationCurve;
        public float SpinAngle = 360f;
        
        public bool InputDisabled;
        
        private Vector3  _resetPivotPosition;       
        private Vector3  _pivotPosition;       // world-space centre of orbit
        private float    _yaw;                 // horizontal angle (degrees)
        private float    _pitch;               // vertical angle   (degrees)
        private float    _targetDistance;      // desired camera ↔ pivot distance
        private float    _currentDistance;     // smoothed distance

        // smoothed rotation quaternion
        private Quaternion _currentRotation;
        private Quaternion _targetRotation;

        private Mouse   _mouse;
        private Vector2 _prevMousePos;

        private Camera _camera;
    
        private void Start()
        {
            _camera = GetComponent<Camera>();
            
            // Derive initial pivot
            if (Target != null)
            {
                _pivotPosition = Target.position;
            }
            else
            {
                // Place pivot 10 units in front of the camera
                _pivotPosition = transform.position + transform.forward * 10f;
            }

            // Derive initial distance
            _currentDistance = _targetDistance =
                Vector3.Distance(transform.position, _pivotPosition);
            _currentDistance = Mathf.Clamp(_currentDistance, minZoomDistance, maxZoomDistance);
            _targetDistance  = _currentDistance;

            // Decompose the camera's current rotation into yaw / pitch
            Vector3 euler = transform.rotation.eulerAngles;
            _yaw   = euler.y;
            _pitch = euler.x;
            // Normalise pitch to [-180, 180] so clamping works correctly
            if (_pitch > 180f) _pitch -= 360f;

            _currentRotation = _targetRotation = Quaternion.Euler(_pitch, _yaw, 0f);
        
            _mouse        = Mouse.current;
            _prevMousePos = _mouse != null ? _mouse.position.ReadValue() : Vector2.zero;
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
            if (_mouse == null || !_mouse.leftButton.isPressed) return;
 
            Vector2 delta = _mouse.delta.ReadValue();
 
            float deltaX =  delta.x * orbitSensitivity;
            float deltaY = (delta.y * orbitSensitivity) * (invertY ? -1f : 1f);
 
            _yaw   += deltaX;
            _pitch  = Mathf.Clamp(_pitch + deltaY, minPitch, maxPitch);
 
            _targetRotation = Quaternion.Euler(_pitch, _yaw, 0f);
        }
 
        private void HandlePan()
        {
            if (_mouse == null || !_mouse.rightButton.isPressed) return;
 
            Vector2 delta = _mouse.delta.ReadValue();
 
            float deltaX = -delta.x;
            float deltaY = -delta.y;
 
            // Scale pan speed with distance so it feels consistent at any zoom level
            float scale = _currentDistance * panSensitivity;
 
            // Move pivot along the camera's local right and up axes
            Vector3 right = _currentRotation * Vector3.right;
            Vector3 up    = _currentRotation * Vector3.up;
 
            _pivotPosition += (right * deltaX + up * deltaY) * scale;
        }

        private void HandleZoom()
        {
            if (_mouse == null) return;
            
            if (UIRoot.IsPointerOverUI()) return;
 
            float scroll = _mouse.scroll.ReadValue().y;
            if (Mathf.Abs(scroll) < 0.0001f) return;
 
            // scroll.y is in pixels (120 per notch on most mice) — normalise to ~1 per notch
            scroll *= 0.01f;
 
            _targetDistance -= scroll * zoomSensitivity * _currentDistance;
            _targetDistance  = Mathf.Clamp(_targetDistance, minZoomDistance, maxZoomDistance);
        }
 
        private void ApplyTransform()
        {
            float t = Time.deltaTime;
 
            // Smooth rotation
            if (rotationSmoothing > 0f)
                _currentRotation = Quaternion.Slerp(
                    _currentRotation, _targetRotation, t * rotationSmoothing);
            else
                _currentRotation = _targetRotation;
 
            // Smooth zoom
            if (zoomSmoothing > 0f)
                _currentDistance = Mathf.Lerp(
                    _currentDistance, _targetDistance, t * zoomSmoothing);
            else
                _currentDistance = _targetDistance;
 
            // Position = pivot − forward * distance
            Vector3 offset = _currentRotation * new Vector3(0f, 0f, -_currentDistance);
            transform.position = _pivotPosition + offset;
            transform.rotation = _currentRotation;
        }

        public void ResetPivot() =>
            _pivotPosition = _resetPivotPosition;
    
        /// <summary>Instantly re-centre the pivot on a world-space position.</summary>
        public void SetPivot(Vector3 worldPosition)
        {
            _pivotPosition = worldPosition;
            _resetPivotPosition = worldPosition;
        }

        /// <summary>Instantly re-centre the pivot on a Transform.</summary>
        public void SetPivot(Transform t) => SetPivot(t.position);

        /// <summary>Snap orbit angles without smoothing.</summary>
        public void SetAngles(float yaw, float pitch)
        {
            _yaw   = yaw;
            _pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            _targetRotation  = Quaternion.Euler(_pitch, _yaw, 0f);
            _currentRotation = _targetRotation;
        }

        public async UniTaskVoid DoOrbitAnimationAsync(Bounds structureBounds, float duration = 1f, Action onComplete = null)
        {
            _pitch = 15f;
            _yaw = 0f;
            _targetDistance = GetDistanceToFit(_camera, structureBounds);
            _currentDistance = _targetDistance;
            maxZoomDistance = 1.5f * _targetDistance;
            _targetRotation = Quaternion.Euler(_pitch, _yaw, 0f);
            _currentRotation = _targetRotation;
            
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