using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace OpenMyGame.Core.Background
{
    public sealed class BalloonController : MonoBehaviour
    {
        [Header("Refs")] [SerializeField] private BalloonView balloonPrefab;
        [SerializeField] private Camera targetCamera;
        [SerializeField] private Transform activeRoot;
        [SerializeField] private Transform poolRoot;

        [Header("Lifecycle")] [SerializeField] private bool playOnEnable = true;
        [SerializeField] private int prewarmCount = 4;
        [SerializeField] private bool spawnOneImmediatelyOnStart = true;

        [Header("Population")] [SerializeField]
        private int maxActiveCount = 3;

        [Header("Spawn Delay")] [SerializeField]
        private float minSpawnDelay = 0.8f;

        [SerializeField] private float maxSpawnDelay = 2.0f;

        [Header("Spawn Area")] [SerializeField, Range(0f, 1f)]
        private float minViewportY = 0.2f;

        [SerializeField, Range(0f, 1f)] private float maxViewportY = 0.9f;
        [SerializeField] private float spawnOffsetX = 1f;
        [SerializeField] private float despawnOffsetX = 1f;

        [Header("Path")] [SerializeField] private float minDuration = 8f;
        [SerializeField] private float maxDuration = 14f;
        [SerializeField] private float minPathAmplitude = 0.2f;
        [SerializeField] private float maxPathAmplitude = 0.75f;
        [SerializeField] private float minPathFrequency = 0.4f;
        [SerializeField] private float maxPathFrequency = 1.2f;
        [SerializeField] private float maxEndYOffset = 0.6f;

        [Header("Rotation Sway")] [SerializeField]
        private float minRotationAmplitude = 4f;

        [SerializeField] private float maxRotationAmplitude = 12f;
        [SerializeField] private float minRotationFrequency = 0.15f;
        [SerializeField] private float maxRotationFrequency = 0.5f;

        [Header("Visual")] [SerializeField] private bool randomizeScale = true;
        [SerializeField] private float minScale = 0.8f;
        [SerializeField] private float maxScale = 1.2f;
        [SerializeField] private bool flipByDirection = true;
        [SerializeField] private int minSortingOrder = 10;
        [SerializeField] private int maxSortingOrder = 20;

        [Header("World")] [SerializeField] private float zPosition;

        private BalloonPool _pool;
        private readonly List<BalloonView> _activeBalloons = new(8);

        private bool _isRunning;
        private float _spawnTimer;
        private float _nextSpawnDelay;

        private void Awake()
        {
            InitializeReferences();

            if (!balloonPrefab)
            {
                Debug.LogError($"{nameof(BalloonController)}: Balloon prefab is not assigned.", this);
                enabled = false;
                return;
            }

            if (!targetCamera)
            {
                Debug.LogError(
                    $"{nameof(BalloonController)}: Target camera is not assigned and Camera.main was not found.", this);
                enabled = false;
                return;
            }

            _pool = new BalloonPool(balloonPrefab, activeRoot, poolRoot, prewarmCount);
            ScheduleNextSpawn();
        }

        private void OnEnable()
        {
            if (playOnEnable)
                StartAmbient();
        }

        private void OnDisable()
        {
            StopAmbient();
        }

        private void Update()
        {
            if (!_isRunning)
                return;

            float deltaTime = Time.deltaTime;

            TickActiveBalloons(deltaTime);
            TickSpawner(deltaTime);
        }

        private void StartAmbient()
        {
            if (!isActiveAndEnabled)
                return;

            if (_pool == null)
                return;

            if (_isRunning)
                return;

            _isRunning = true;
            ScheduleNextSpawn();

            if (spawnOneImmediatelyOnStart && TrySpawnBalloon())
            {
                ScheduleNextSpawn();
            }
        }

        private void StopAmbient()
        {
            if (!_isRunning && _activeBalloons.Count == 0)
                return;

            _isRunning = false;
            _spawnTimer = 0f;

            for (int i = _activeBalloons.Count - 1; i >= 0; i--)
            {
                _pool.Return(_activeBalloons[i]);
            }

            _activeBalloons.Clear();
        }

        private void InitializeReferences()
        {
            if (!targetCamera)
                targetCamera = Camera.main;

            if (!activeRoot)
                activeRoot = transform;

            if (poolRoot)
                return;

            GameObject poolObject = new GameObject("[BalloonPool]");
            poolObject.transform.SetParent(transform, false);
            poolRoot = poolObject.transform;
        }

        private void TickActiveBalloons(float deltaTime)
        {
            for (int i = _activeBalloons.Count - 1; i >= 0; i--)
            {
                BalloonView balloon = _activeBalloons[i];
                bool completed = balloon.Tick(deltaTime);

                if (!completed)
                    continue;

                _activeBalloons.RemoveAt(i);
                _pool.Return(balloon);
            }
        }

        private void TickSpawner(float deltaTime)
        {
            if (_activeBalloons.Count >= maxActiveCount)
                return;

            _spawnTimer += deltaTime;
            if (_spawnTimer < _nextSpawnDelay)
                return;

            if (TrySpawnBalloon())
            {
                ScheduleNextSpawn();
            }
        }

        private bool TrySpawnBalloon()
        {
            if (_activeBalloons.Count >= maxActiveCount)
                return false;

            BalloonView.FlightData flightData = CreateFlightData();
            BalloonView balloon = _pool.Get();

            int sortingOrder = Random.Range(minSortingOrder, maxSortingOrder + 1);
            balloon.SetSortingOrder(sortingOrder);
            balloon.Play(flightData, OnBalloonClicked);

            _activeBalloons.Add(balloon);
            return true;
        }

        private BalloonView.FlightData CreateFlightData()
        {
            CameraBounds bounds = GetCameraBounds();

            bool leftToRight = Random.value < 0.5f;

            float startY = ViewportToWorldY(Random.Range(minViewportY, maxViewportY));
            float endY = startY + Random.Range(-maxEndYOffset, maxEndYOffset);

            float startX = leftToRight
                ? bounds.Left - spawnOffsetX
                : bounds.Right + spawnOffsetX;

            float endX = leftToRight
                ? bounds.Right + despawnOffsetX
                : bounds.Left - despawnOffsetX;

            Vector3 startPosition = new(startX, startY, zPosition);
            Vector3 endPosition = new(endX, endY, zPosition);

            float scale = randomizeScale
                ? Random.Range(minScale, maxScale)
                : 1f;

            return new BalloonView.FlightData(
                startPosition: startPosition,
                endPosition: endPosition,
                duration: Random.Range(minDuration, maxDuration),
                pathAmplitude: Random.Range(minPathAmplitude, maxPathAmplitude),
                pathFrequency: Random.Range(minPathFrequency, maxPathFrequency),
                pathPhase: Random.Range(0f, Mathf.PI * 2f),
                rotationAmplitude: Random.Range(minRotationAmplitude, maxRotationAmplitude),
                rotationFrequency: Random.Range(minRotationFrequency, maxRotationFrequency),
                rotationPhase: Random.Range(0f, Mathf.PI * 2f),
                scale: scale,
                flipX: flipByDirection && !leftToRight);
        }

        private void ScheduleNextSpawn()
        {
            _spawnTimer = 0f;
            _nextSpawnDelay = Random.Range(minSpawnDelay, maxSpawnDelay);
        }

        private CameraBounds GetCameraBounds()
        {
            float distance = Mathf.Abs(zPosition - targetCamera.transform.position.z);

            Vector3 leftBottom = targetCamera.ViewportToWorldPoint(new Vector3(0f, 0f, distance));
            Vector3 rightTop = targetCamera.ViewportToWorldPoint(new Vector3(1f, 1f, distance));

            return new CameraBounds(
                left: leftBottom.x,
                right: rightTop.x
            );
        }

        private float ViewportToWorldY(float viewportY)
        {
            float distance = Mathf.Abs(zPosition - targetCamera.transform.position.z);
            Vector3 point = targetCamera.ViewportToWorldPoint(new Vector3(0f, viewportY, distance));
            return point.y;
        }

        private void OnBalloonClicked(BalloonView balloon)
        {
            int index = _activeBalloons.IndexOf(balloon);
            if (index < 0)
                return;

            _activeBalloons.RemoveAt(index);
            _pool.Return(balloon);
        }

        private void OnValidate()
        {
            if (maxActiveCount < 0)
                maxActiveCount = 0;

            if (prewarmCount < 0)
                prewarmCount = 0;

            if (maxSpawnDelay < minSpawnDelay)
                maxSpawnDelay = minSpawnDelay;

            if (maxViewportY < minViewportY)
                maxViewportY = minViewportY;

            if (maxDuration < minDuration)
                maxDuration = minDuration;

            if (maxPathAmplitude < minPathAmplitude)
                maxPathAmplitude = minPathAmplitude;

            if (maxPathFrequency < minPathFrequency)
                maxPathFrequency = minPathFrequency;

            if (maxRotationAmplitude < minRotationAmplitude)
                maxRotationAmplitude = minRotationAmplitude;

            if (maxRotationFrequency < minRotationFrequency)
                maxRotationFrequency = minRotationFrequency;

            minScale = Mathf.Max(0.01f, minScale);
            if (maxScale < minScale)
                maxScale = minScale;

            if (maxSortingOrder < minSortingOrder)
                maxSortingOrder = minSortingOrder;
        }

        private readonly struct CameraBounds
        {
            public readonly float Left;
            public readonly float Right;

            public CameraBounds(float left, float right)
            {
                Left = left;
                Right = right;
            }
        }
    }
}