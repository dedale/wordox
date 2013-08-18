using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ded.Wordox
{
    class Cell : IEquatable<Cell>
    {
        #region Fields
        private readonly int row;
        private readonly int column;
        #endregion
        #region Private stuff
        private Cell GetNext(Direction direction)
        {
            switch (direction)
            {
                case Direction.Bottom:
                    if (row == Board.Height - 1)
                        return null;
                    return new Cell(row + 1, column);
                case Direction.Right:
                    if (column == Board.Width - 1)
                        return null;
                    return new Cell(row, column + 1);
                default:
                    throw new ArgumentException(string.Format("Unknown direction : {0}", direction), "direction");
            }
        }
        #endregion
        public Cell(int row, int column)
        {
            if (row < 0
                || column < 0
                || row >= Board.Height
                || column >= Board.Width)
                throw new IndexOutOfRangeException(string.Format("Cannot create cell in {0},{1}", row, column));
            this.row = row;
            this.column = column;
        }
        public int Row { get { return row; } }
        public int Column { get { return column; } }
        public override string ToString()
        {
            return string.Format("{0},{1}", row, column);
        }
        public override int GetHashCode()
        {
            // 463 is 90th prime (90 > 9 x 9)
            return row ^ (column * 463);
        }
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            return Equals(obj as Cell);
        }
        public bool TryGetNext(Direction direction, out Cell next)
        {
            next = GetNext(direction);
            if (next != null)
            {
                if (next.row >= Board.Height || next.column >= Board.Width)
                    next = null;
            }
            return next != null;
        }
        public Cell Left { get { return new Cell(row, column - 1); } }
        public Cell Right { get { return new Cell(row, column + 1); } }
        public Cell Top { get { return new Cell(row - 1, column); } }
        public Cell Bottom { get { return new Cell(row + 1, column); } }
        public bool HasTop { get { return row > 0; } }
        public bool HasBottom { get { return row < Board.Height - 1; } }
        public bool HasLeft { get { return column > 0; } }
        public bool HasRight { get { return column < Board.Width - 1; } }
        public bool Equals(Cell other)
        {
            if (ReferenceEquals(other, null))
                return false;
            return row == other.row && column == other.column;
        }
        public static bool operator ==(Cell c1, Cell c2)
        {
            if (ReferenceEquals(c1, null))
                return ReferenceEquals(c2, null);
            return c1.Equals(c2);
        }
        public static bool operator !=(Cell c1, Cell c2)
        {
            return !(c1 == c2);
        }
        public bool IsStar
        {
            get
            {
                return row + column == 4
                    || row - column == 4
                    || column - row == 4
                    || row + column == 12;
            }
        }
        public bool IsVortex
        {
            get
            {
                return row == 0 && column == 0
                    || row == 0 && column == Board.Width - 1
                    || row == Board.Height - 1 && column == 0
                    || row == Board.Height - 1 && column == Board.Width - 1;
            }
        }
    }
    class CellUpdatedEventArgs : EventArgs
    {
        #region Fields
        private readonly Cell cell;
        private readonly char letter;
        #endregion
        public CellUpdatedEventArgs(Cell cell, char letter)
        {
            this.cell = cell;
            this.letter = letter;
        }
        public Cell Cell { get { return cell; } }
        public char Letter { get { return letter; } }
    }
    delegate void CellUpdatedEventHandler(object sender, CellUpdatedEventArgs args);
    class Board
    {
        #region Constants
        public const int Height = 9;
        public const int Width = Height;
        internal const int Center = Height / 2;
        private const char Empty = '\0';
        #endregion
        #region Fields
        private readonly char[,] board;
        private readonly ConstantSet<Cell> cells;
        #endregion
        #region Private stuff
        private Board(char[,] board, ConstantSet<Cell> cells)
        {
            this.board = board;
            this.cells = cells;
        }
        private bool Contains(ConstantSet<Cell> start, IEnumerable<Cell> played)
        {
            foreach (Cell cell in played)
                if (start.Contains(cell))
                    return true;
            return false;
        }
        #endregion
        public Board()
            : this(new char[Height, Width], new ConstantSet<Cell>())
        {
        }
        public event CellUpdatedEventHandler CellUpdated;
        public ConstantSet<Cell> GetStartCells()
        {
            if (cells.Count == 0)
                return new ConstantSet<Cell>(new[] { new Cell(Center, Center) });
            var result = new HashSet<Cell>();
            foreach (Cell cell in cells)
            {
                for (int i = 0; i < 4; i++)
                {
                    int dr = i == 3 ? 0 : i - 1;
                    int dc = i == 0 ? 0 : i - 2;
                    int row = cell.Row + dr;
                    int column = cell.Column + dc;
                    if (row >= 0 && column >= 0 && row < Board.Height && column < Board.Width && board[row, column] == Empty)
                        result.Add(new Cell(row, column));
                }
            }
            return result.ToConstant();
        }
        public Board Play(string word, Cell cell, Direction direction)
        {
            var start = new ConstantSet<Cell>(GetStartCells());
            var list = new List<Cell>();
            list.Add(cell);
            var current = cell;
            for (int i = 1; i < word.Length; i++)
            {
                if (!current.TryGetNext(direction, out current))
                    throw new ArgumentException(string.Format("Cannot play {0} at {1}", word, cell));
                list.Add(current);
            }
            if (!Contains(start, list))
                throw new ArgumentException(string.Format("Cannot play {0} at {1}", word, cell));
            var newBoard = new char[Height, Width];
            for (int i = 0; i < Height; i++)
                for (int j = 0; j < Width; j++)
                    newBoard[i, j] = board[i, j];
            var newCells = new HashSet<Cell>(cells);
            for (int i = 0; i < word.Length; i++)
            {
                Cell c = list[i];
                char letter = word[i];
                newBoard[c.Row, c.Column] = letter;
                newCells.Add(c);
                if (CellUpdated != null)
                    CellUpdated(this, new CellUpdatedEventArgs(c, letter));
            }
            var result = new Board(newBoard, newCells.ToConstant());
            result.CellUpdated = CellUpdated;
            return result;
        }
        public WordPart GetBeforePart(Cell cell, Direction direction)
        {
            string word = string.Empty;
            var before = cell;
            switch (direction)
            {
                case Direction.Bottom:
                    while (before.HasTop)
                    {
                        var top = before.Top;
                        char prefix = board[top.Row, top.Column];
                        if (prefix == Empty)
                            break;
                        word = prefix + word;
                        before = top;
                    }
                    break;
                case Direction.Right:
                    while (before.HasLeft)
                    {
                        var left = before.Left;
                        char prefix = board[left.Row, left.Column];
                        if (prefix == Empty)
                            break;
                        word = prefix + word;
                        before = left;
                    }
                    break;
            }
            if (word.Length == 0)
                return null;
            return new WordPart(word, before, direction);
        }
        public WordPart GetAfterPart(Cell cell, Direction direction)
        {
            string word = string.Empty;
            var after = cell;
            switch (direction)
            {
                case Direction.Bottom:
                    while (after.HasBottom)
                    {
                        var bottom = after.Bottom;
                        char suffix = board[bottom.Row, bottom.Column];
                        if (suffix == Empty)
                            break;
                        word += suffix;
                        after = bottom;
                    }
                    break;
                case Direction.Right:
                    while (after.HasRight)
                    {
                        var right = after.Right;
                        char suffix = board[right.Row, right.Column];
                        if (suffix == Empty)
                            break;
                        word += suffix;
                        after = right;
                    }
                    break;
            }
            if (word.Length == 0)
                return null;
            return new WordPart(word, direction == Direction.Bottom ? cell.Bottom : cell.Right, direction);
        }
    }
    class WordPart
    {
        #region Fields
        private readonly string word;
        private readonly Cell first;
        private readonly Direction direction;
        private readonly Cell last;
        #endregion
        public WordPart(string word, Cell first, Direction direction)
        {
            this.word = word;
            this.first = first;
            this.direction = direction;
            int d = word.Length - 1;
            int dr = direction == Direction.Bottom ? d : 0;
            int dc = direction == Direction.Right ? d : 0;
            last = new Cell(first.Row + dr, first.Column + dc);
        }
        public string Word { get { return word; } }
        public Cell First { get { return first; } }
        public Cell Last { get { return last; } }
        public Direction Direction { get { return direction; } }
        public WordPart Play(Cell played, char letter)
        {
            switch (direction)
            {
                case Direction.Right:
                    if (first.HasLeft && first.Left == played)
                        return new WordPart(letter + word, played, direction);
                    if (last.HasRight && last.Right == played)
                        return new WordPart(word + letter, first, direction);
                    break;
                case Direction.Bottom:
                    if (first.HasTop && first.Top == played)
                        return new WordPart(letter + word, played, direction);
                    if (last.HasBottom && last.Bottom == played)
                        return new WordPart(word + letter, first, direction);
                    break;
                default:
                    throw new NotSupportedException();
            }
            return this;
        }
        public override int GetHashCode()
        {
            return word.GetHashCode() ^ first.GetHashCode() ^ direction.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(obj, this))
                return true;
            var other = obj as WordPart;
            if (other == null)
                return false;
            return word == other.word && first == other.first && direction == other.direction;
        }
        public WordPart Merge(WordPart other)
        {
            if (last != other.first)
                throw new ArgumentException(string.Format("Cannot merge last {0} with first {1}", last, other.first), "other");
            if (direction != other.direction)
                throw new ArgumentException("Cannot merge two directions", "other");
            if (word[word.Length - 1] != other.word[0])
                throw new ArgumentException("Letter mismatch", "other");
            return new WordPart(word + other.Word.Substring(1), first, direction);
        }
    }
    class WordPartCollection : IEnumerable<WordPart>
    {
        #region Fields
        private static readonly WordPartCollection empty = new WordPartCollection(new ConstantList<WordPart>());
        private readonly ConstantList<WordPart> parts;
        #endregion
        #region Private stuff
        private static void Add(Dictionary<Direction, Dictionary<Cell, WordPart>> index, WordPart part, Func<WordPart, Cell> func)
        {
            Dictionary<Cell, WordPart> map;
            if (!index.TryGetValue(part.Direction, out map))
            {
                map = new Dictionary<Cell, WordPart>();
                index.Add(part.Direction, map);
            }
            map.Add(func(part), part);
        }
        #endregion
        public static WordPartCollection Empty { get { return empty; } }
        public WordPartCollection(WordPart part)
            : this(new ConstantList<WordPart>(new[] { part }))
        {
        }
        public WordPartCollection(ConstantList<WordPart> parts)
        {
            this.parts = parts;
        }
        public int Count { get { return parts.Count; } }
        public WordPart this[int i]
        {
            get { return parts[i]; }
        }
        public IEnumerator<WordPart> GetEnumerator()
        {
            return parts.GetEnumerator();
        }
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public WordPartCollection Play(Cell cell, char letter)
        {
            var byFirst = new Dictionary<Direction, Dictionary<Cell, WordPart>>();
            var byLast = new Dictionary<Direction, Dictionary<Cell, WordPart>>();
            foreach (WordPart part in parts)
            {
                var played = part.Play(cell, letter);
                Add(byFirst, played, wp => wp.First);
                Add(byLast, played, wp => wp.Last);
            }
            var directions = new HashSet<Direction>(byFirst.Keys);
            directions.IntersectWith(new HashSet<Direction>(byLast.Keys));
            foreach (Direction d in directions)
            {
                var cells = new HashSet<Cell>(byFirst[d].Keys);
                cells.IntersectWith(new HashSet<Cell>(byLast[d].Keys));
                foreach (Cell c in cells)
                {
                    var merged = byFirst[d][c];
                    byLast[d][c] = byLast[d][c].Merge(merged);
                    byLast[d].Remove(merged.Last);
                }
            }
            var result = new List<WordPart>();
            foreach (Direction d in byLast.Keys)
                result.AddRange(byLast[d].Values);
            return new WordPartCollection(result.ToConstant());
        }
    }
}
