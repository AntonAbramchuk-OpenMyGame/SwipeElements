using System.Collections.Generic;
using OpenMyGame.Core.Board.Data;
using OpenMyGame.Core.Board.Logic.Abstractions;

namespace OpenMyGame.Core.Board.Logic
{
    public sealed class BoardNormalizer : IBoardNormalizer
    {
        public BoardDelta BuildFallStep(BoardData boardData)
        {
            BoardDelta delta = new(BoardDeltaType.Fall);

            var width = boardData.Size.Width;
            var height = boardData.Size.Height;

            for (var x = 0; x < width; x++)
            {
                ApplyFallToColumn(boardData, x, height, delta);
            }

            return delta;
        }

        public BoardDelta BuildDestroyStep(BoardData boardData)
        {
            BoardDelta delta = new(BoardDeltaType.Destroy);

            var destroyableGroups = FindDestroyableGroups(boardData);

            foreach (var group in destroyableGroups)
            {
                foreach (var coordinates in group)
                {
                    var previousCell = boardData.GetCell(coordinates);

                    if (previousCell.IsEmpty)
                        continue;

                    boardData.SetCell(coordinates, CellData.Empty);
                    delta.AddItem(BoardDeltaItem.CreateDestroy(coordinates, previousCell));
                }
            }

            return delta;
        }

        private static void ApplyFallToColumn(
            BoardData boardData,
            int x,
            int height,
            BoardDelta delta
        )
        {
            var targetY = 0;

            for (var y = 0; y < height; y++)
            {
                BoardCoordinates from = new(x, y);
                var cell = boardData.GetCell(from);

                if (cell.IsEmpty)
                    continue;

                if (y != targetY)
                {
                    BoardCoordinates to = new(x, targetY);

                    boardData.SetCell(to, cell);
                    boardData.SetCell(from, CellData.Empty);

                    delta.AddItem(BoardDeltaItem.CreateMove(from, to, cell));
                }

                targetY++;
            }
        }

        private static List<List<BoardCoordinates>> FindDestroyableGroups(BoardData boardData)
        {
            var width = boardData.Size.Width;
            var height = boardData.Size.Height;

            var visited = new bool[width * height];
            List<List<BoardCoordinates>> result = new();

            for (var y = 0; y < height; y++)
            {
                for (var x = 0; x < width; x++)
                {
                    BoardCoordinates start = new(x, y);
                    var startCell = boardData.GetCell(start);

                    if (startCell.IsEmpty)
                        continue;

                    var index = boardData.ToIndex(start);
                    if (visited[index])
                        continue;

                    var group = CollectGroup(boardData, start, visited);

                    if (HasLineOfThreeOrMore(group))
                    {
                        result.Add(group);
                    }
                }
            }

            return result;
        }

        private static List<BoardCoordinates> CollectGroup(
            BoardData boardData,
            BoardCoordinates start,
            bool[] visited
        )
        {
            List<BoardCoordinates> group = new();
            Queue<BoardCoordinates> queue = new();

            var groupTypeId = boardData.GetCell(start).BlockTypeId;

            queue.Enqueue(start);
            visited[boardData.ToIndex(start)] = true;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                group.Add(current);

                EnqueueNeighbor(boardData, current.X + 1, current.Y, groupTypeId, visited, queue);
                EnqueueNeighbor(boardData, current.X - 1, current.Y, groupTypeId, visited, queue);
                EnqueueNeighbor(boardData, current.X, current.Y + 1, groupTypeId, visited, queue);
                EnqueueNeighbor(boardData, current.X, current.Y - 1, groupTypeId, visited, queue);
            }

            return group;
        }

        private static void EnqueueNeighbor(
            BoardData boardData,
            int x,
            int y,
            int groupTypeId,
            bool[] visited,
            Queue<BoardCoordinates> queue
        )
        {
            BoardCoordinates coordinates = new(x, y);

            if (!boardData.IsInside(coordinates))
                return;

            var index = boardData.ToIndex(coordinates);
            if (visited[index])
                return;

            var cell = boardData.GetCell(coordinates);

            if (cell.IsEmpty)
                return;

            if (!cell.IsMatch(groupTypeId))
                return;

            visited[index] = true;
            queue.Enqueue(coordinates);
        }

        private static bool HasLineOfThreeOrMore(List<BoardCoordinates> group)
        {
            HashSet<BoardCoordinates> groupSet = new(group);

            foreach (var coordinates in group)
            {
                if (IsHorizontalLineStart(groupSet, coordinates))
                {
                    var horizontalLength = CountHorizontalLineLength(groupSet, coordinates);
                    if (horizontalLength >= 3)
                        return true;
                }

                if (IsVerticalLineStart(groupSet, coordinates))
                {
                    var verticalLength = CountVerticalLineLength(groupSet, coordinates);
                    if (verticalLength >= 3)
                        return true;
                }
            }

            return false;
        }

        private static bool IsHorizontalLineStart(
            HashSet<BoardCoordinates> groupSet,
            BoardCoordinates coordinates
        )
        {
            BoardCoordinates left = new(coordinates.X - 1, coordinates.Y);
            return !groupSet.Contains(left);
        }

        private static bool IsVerticalLineStart(
            HashSet<BoardCoordinates> groupSet,
            BoardCoordinates coordinates
        )
        {
            BoardCoordinates down = new(coordinates.X, coordinates.Y - 1);
            return !groupSet.Contains(down);
        }

        private static int CountHorizontalLineLength(
            HashSet<BoardCoordinates> groupSet,
            BoardCoordinates start
        )
        {
            var length = 0;
            var x = start.X;

            while (groupSet.Contains(new BoardCoordinates(x, start.Y)))
            {
                length++;
                x++;
            }

            return length;
        }

        private static int CountVerticalLineLength(
            HashSet<BoardCoordinates> groupSet,
            BoardCoordinates start
        )
        {
            var length = 0;
            var y = start.Y;

            while (groupSet.Contains(new BoardCoordinates(start.X, y)))
            {
                length++;
                y++;
            }

            return length;
        }
    }
}