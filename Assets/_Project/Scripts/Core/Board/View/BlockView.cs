using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using Random = UnityEngine.Random;

namespace OpenMyGame.Core.Board.View
{
    public sealed class BlockView : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public event Action<int, PointerEventData> PointerDownEvent;
        public event Action<int, PointerEventData> DragEvent;
        public event Action<int, PointerEventData> PointerUpEvent;

        [SerializeField] private SpriteRenderer spriteRenderer;

        [SerializeField] private Sprite[] idleFrames;
        [SerializeField] private Sprite[] destroyFrames;

        [SerializeField] private float idleFps = 30.0f;
        [SerializeField] private float destroyFps = 30.0f;

        private Coroutine _idleRoutine;

        public int BlockTypeId { get; private set; }
        public int BlockId { get; private set; }

        public void Initialize(int blockTypeId, int blockId)
        {
            BlockTypeId = blockTypeId;
            BlockId = blockId;

            var startFrame = Random.Range(0, idleFrames.Length);
            PlayIdle(startFrame);

#if UNITY_EDITOR
            gameObject.name = $"Block ({BlockId})";
#endif
        }

        public void SetSorting(string sortingLayerName, int sortingOrder)
        {
            spriteRenderer.sortingLayerName = sortingLayerName;
            spriteRenderer.sortingOrder = sortingOrder;
        }

        public void SetPosition(Vector3 position)
        {
            transform.position = position;
        }

        public Tween PlayMove(Vector3 targetPosition, float duration)
        {
            KillTweens();

            return transform.DOMove(targetPosition, duration)
                .SetEase(Ease.OutQuad)
                .SetTarget(this);
        }

        public Tween PlayFall(Vector3 targetPosition, float duration)
        {
            KillTweens();

            return transform.DOMove(targetPosition, duration)
                .SetEase(Ease.InQuad)
                .SetTarget(this);
        }

        public Tween PlayDestroy()
        {
            StopIdle();

            var duration = destroyFrames.Length / destroyFps;
            var frame = 0;

            return DOTween.To(
                    () => frame,
                    x =>
                    {
                        frame = x;

                        var clamped = Mathf.Clamp(frame, 0, destroyFrames.Length - 1);
                        spriteRenderer.sprite = destroyFrames[clamped];
                    },
                    destroyFrames.Length - 1,
                    duration
                )
                .SetEase(Ease.Linear)
                .SetTarget(this);
        }

        public void Release()
        {
            StopIdle();
            KillTweens();
        }

        private void PlayIdle(int startFrame = 0)
        {
            StopIdle();
            _idleRoutine = StartCoroutine(IdleRoutine(startFrame));
        }

        private IEnumerator IdleRoutine(int startFrame)
        {
            var delay = 1.0f / idleFps;
            var waitForSeconds = new WaitForSeconds(delay);

            var index = startFrame;

            while (gameObject.activeSelf)
            {
                spriteRenderer.sprite = idleFrames[index];

                index = (index + 1) % idleFrames.Length;
                yield return waitForSeconds;
            }
        }

        private void StopIdle()
        {
            if (_idleRoutine != null)
            {
                StopCoroutine(_idleRoutine);
                _idleRoutine = null;
            }
        }

        private void KillTweens()
        {
            DOTween.Kill(this);
        }

        void IPointerDownHandler.OnPointerDown(PointerEventData eventData)
        {
            PointerDownEvent?.Invoke(BlockId, eventData);
        }

        void IDragHandler.OnDrag(PointerEventData eventData)
        {
            DragEvent?.Invoke(BlockId, eventData);
        }

        void IPointerUpHandler.OnPointerUp(PointerEventData eventData)
        {
            PointerUpEvent?.Invoke(BlockId, eventData);
        }
    }
}