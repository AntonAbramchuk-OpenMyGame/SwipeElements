using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OpenMyGame.Core.Background
{
    public sealed class BalloonView : MonoBehaviour, IPointerDownHandler
    {
        [SerializeField] private SpriteRenderer spriteRenderer;

        private FlightState _flightState;
        private Action<BalloonView> _onPointerDown;

        public bool IsPlaying { get; private set; }

        public void Play(FlightData flightData, Action<BalloonView> onPointerDown)
        {
            _flightState = FlightState.Create(flightData);
            _onPointerDown = onPointerDown;

            IsPlaying = true;

            transform.position = flightData.StartPosition;
            transform.localScale = Vector3.one * flightData.Scale;
            transform.rotation = Quaternion.identity;

            if (spriteRenderer)
                spriteRenderer.flipX = flightData.FlipX;
        }

        public bool Tick(float deltaTime)
        {
            if (!IsPlaying)
                return false;

            _flightState.Elapsed += deltaTime;

            float normalizedTime = _flightState.Elapsed / _flightState.Duration;
            if (normalizedTime > 1f)
                normalizedTime = 1f;

            Vector3 linearPosition = Vector3.LerpUnclamped(
                _flightState.StartPosition,
                _flightState.EndPosition,
                normalizedTime);

            float pathSine = Mathf.Sin(
                normalizedTime * _flightState.PathFrequency * Mathf.PI * 2f + _flightState.PathPhase);

            Vector3 pathOffset = _flightState.Perpendicular * (_flightState.PathAmplitude * pathSine);
            transform.position = linearPosition + pathOffset;

            float rotationSine = Mathf.Sin(
                _flightState.Elapsed * _flightState.RotationFrequency * Mathf.PI * 2f + _flightState.RotationPhase);

            float zRotation = _flightState.RotationAmplitude * rotationSine;
            transform.rotation = Quaternion.Euler(0f, 0f, zRotation);

            if (_flightState.Elapsed < _flightState.Duration)
                return false;

            IsPlaying = false;
            return true;
        }

        public void StopImmediate()
        {
            IsPlaying = false;
            _onPointerDown = null;
            transform.rotation = Quaternion.identity;
        }

        public void SetSortingOrder(int sortingOrder)
        {
            if (spriteRenderer)
                spriteRenderer.sortingOrder = sortingOrder;
        }

        public void OnPointerDown(PointerEventData eventData)
        {
            if (!IsPlaying)
                return;

            _onPointerDown?.Invoke(this);
        }

        public readonly struct FlightData
        {
            public readonly Vector3 StartPosition;
            public readonly Vector3 EndPosition;
            public readonly float Duration;
            public readonly float PathAmplitude;
            public readonly float PathFrequency;
            public readonly float PathPhase;
            public readonly float RotationAmplitude;
            public readonly float RotationFrequency;
            public readonly float RotationPhase;
            public readonly float Scale;
            public readonly bool FlipX;

            public FlightData(
                Vector3 startPosition,
                Vector3 endPosition,
                float duration,
                float pathAmplitude,
                float pathFrequency,
                float pathPhase,
                float rotationAmplitude,
                float rotationFrequency,
                float rotationPhase,
                float scale,
                bool flipX)
            {
                StartPosition = startPosition;
                EndPosition = endPosition;
                Duration = duration;
                PathAmplitude = pathAmplitude;
                PathFrequency = pathFrequency;
                PathPhase = pathPhase;
                RotationAmplitude = rotationAmplitude;
                RotationFrequency = rotationFrequency;
                RotationPhase = rotationPhase;
                Scale = scale;
                FlipX = flipX;
            }
        }

        private struct FlightState
        {
            public Vector3 StartPosition;
            public Vector3 EndPosition;
            public Vector3 Perpendicular;
            public float Duration;
            public float PathAmplitude;
            public float PathFrequency;
            public float PathPhase;
            public float RotationAmplitude;
            public float RotationFrequency;
            public float RotationPhase;
            public float Elapsed;

            public static FlightState Create(FlightData data)
            {
                Vector3 direction = data.EndPosition - data.StartPosition;
                Vector3 perpendicular = Vector3.zero;

                if (direction.sqrMagnitude > 0.0001f)
                {
                    Vector3 normalized = direction.normalized;
                    perpendicular = new Vector3(-normalized.y, normalized.x, 0f);
                }

                return new FlightState
                {
                    StartPosition = data.StartPosition,
                    EndPosition = data.EndPosition,
                    Perpendicular = perpendicular,
                    Duration = Mathf.Max(0.0001f, data.Duration),
                    PathAmplitude = data.PathAmplitude,
                    PathFrequency = data.PathFrequency,
                    PathPhase = data.PathPhase,
                    RotationAmplitude = data.RotationAmplitude,
                    RotationFrequency = data.RotationFrequency,
                    RotationPhase = data.RotationPhase,
                    Elapsed = 0f
                };
            }
        }
    }
}