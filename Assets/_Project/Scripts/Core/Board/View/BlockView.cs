using UnityEngine;

namespace OpenMyGame.Core.Board.View
{
    public sealed class BlockView : MonoBehaviour
    {
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

            gameObject.name = $"Block ({BlockId})";
        }

        public void SetDebugCoordinates(int x, int y)
        {
            gameObject.name += $"({x},{y})";
        }

        public void SetPosition(Vector3 position)
        {
            transform.position = position;
        }

        public void PlayDestroy()
        {
            gameObject.SetActive(false);
        }
    }
}