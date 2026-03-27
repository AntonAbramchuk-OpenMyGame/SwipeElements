using UnityEngine;

namespace OpenMyGame.Core.Board.Runtime
{
    public interface IBoardInput
    {
        void OnBlockPointerDown(int blockId, Vector2 screenPosition);
        void OnBlockDrag(int blockId, Vector2 screenPosition);
        void OnBlockPointerUp(int blockId, Vector2 screenPosition);
    }
}