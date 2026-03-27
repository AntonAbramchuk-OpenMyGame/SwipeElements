using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;

namespace OpenMyGame.Core.Board.View
{
    public sealed class BlockView : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        public event Action<int, PointerEventData> PointerDownEvent;
        public event Action<int, PointerEventData> DragEvent;
        public event Action<int, PointerEventData> PointerUpEvent;

        [SerializeField] private SpriteRenderer spriteRenderer;

        public int BlockTypeId { get; private set; }
        public int BlockId { get; private set; }

        public void Initialize(int blockTypeId, int blockId)
        {
            BlockTypeId = blockTypeId;
            BlockId = blockId;

            switch (blockTypeId)
            {
                case 0:
                    spriteRenderer.color = Color.red;
                    break;
                case 1:
                    spriteRenderer.color = Color.blue;
                    break;
            }

#if UNITY_EDITOR
            gameObject.name = $"Block ({BlockId})";
#endif
        }

        public void SetPosition(Vector3 position)
        {
            transform.position = position;
        }

        public void PlayDestroy()
        {
            gameObject.SetActive(false);
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

        public void Release()
        {
            KillTweens();
            Destroy(gameObject);
        }

        public void KillTweens()
        {
            DOTween.Kill(this);
        }

        private void OnDisable()
        {
            KillTweens();
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