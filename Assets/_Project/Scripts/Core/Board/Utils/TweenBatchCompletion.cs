using System;
using DG.Tweening;
using OpenMyGame.Core.Board.Data;

namespace OpenMyGame.Core.Board.Utils
{
    public sealed class TweenBatchCompletion
    {
        private readonly Func<bool> _isValidFunc;
        private readonly BoardDelta _boardDelta;
        private readonly Action<BoardDelta> _onComplete;

        private int _remaining;
        private bool _completed;

        public TweenBatchCompletion(
            Func<bool> isValidFunc,
            BoardDelta boardDelta,
            Action<BoardDelta> onComplete
        )
        {
            _isValidFunc = isValidFunc;
            _boardDelta = boardDelta;
            _onComplete = onComplete;
        }

        public void RegisterTween(Tween tween, Action onCompleteOrKill = null)
        {
            _remaining++;

            var completedOrKilled = false;

            tween.OnComplete(OnCompleteOrKill);
            tween.OnKill(OnCompleteOrKill);
            return;

            void OnCompleteOrKill()
            {
                if (completedOrKilled)
                    return;

                completedOrKilled = true;
                _remaining--;

                if (!IsValid())
                    return;

                onCompleteOrKill?.Invoke();
                CompleteIfEmpty();
            }
        }

        public void CompleteIfEmpty()
        {
            if (!IsValid())
                return;

            if (_remaining > 0 || _completed)
                return;

            _completed = true;
            _onComplete?.Invoke(_boardDelta);
        }

        private bool IsValid()
        {
            return _isValidFunc?.Invoke() ?? false;
        }
    }
}