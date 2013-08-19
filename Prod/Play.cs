﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ded.Wordox
{
    [Flags]
    public enum WordStrategy
    {
        None = 0,
        NoOneFixes = 1,
        NoTwoMoreFixes = 2,
        NoFixes = NoOneFixes | NoTwoMoreFixes,
    }
    public enum ScoreStrategy
    {
        None,
        MaxPoints,
        MaxDiff,
    }
    class LetterPlay
    {
        #region Fields
        private readonly Cell cell;
        private readonly char letter;
        #endregion
        public LetterPlay(Cell cell, char letter)
        {
            this.cell = cell;
            this.letter = letter;
        }
        public Cell Cell { get { return cell; } }
        public char Letter { get { return letter; } }
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} @ {1}", letter, cell);
        }
    }
    class PlayPath
    {
        #region Fields
        private readonly WordPart main;
        private readonly WordPartCollection extras;
        private readonly ConstantList<LetterPlay> played;
        private readonly ConstantSet<char> pending;
        #endregion
        public PlayPath(WordPart main, LetterPlay letter, ConstantSet<char> pending)
            : this(main, WordPartCollection.Empty, new ConstantList<LetterPlay>(letter), pending)
        {
        }
        public PlayPath(WordPart main, WordPart extra, LetterPlay letter, ConstantSet<char> pending)
            : this(main, new WordPartCollection(extra), new ConstantList<LetterPlay>(letter), pending)
        {
        }
        public PlayPath(WordPart main, WordPartCollection extras, ConstantList<LetterPlay> played, ConstantSet<char> pending)
        {
            this.main = main;
            this.extras = extras;
            this.played = played;
            this.pending = pending;
        }
        public WordPart Main { get { return main; } }
        public WordPartCollection Extras { get { return extras; } }
        public ConstantList<LetterPlay> Played { get { return played; } }
        public ConstantSet<char> Pending { get { return pending; } }
    }
    class WordPartPair
    {
        #region Fields
        private readonly WordPart before;
        private readonly WordPart after;
        #endregion
        public WordPartPair(WordPart before, WordPart after)
        {
            this.before = before;
            this.after = after;
        }
        public WordPart Before { get { return before; } }
        public WordPart After { get { return after; } }
        public WordPart Play(Cell cell, char letter)
        {
            WordPart part = null;
            var beforePlayed = before == null ? (WordPart)null : before.Play(cell, letter);
            var afterPlayed = after == null ? (WordPart)null : after.Play(cell, letter);
            if (beforePlayed != null && afterPlayed != null)
                part = beforePlayed.Merge(afterPlayed);
            else if (beforePlayed != null)
                part = beforePlayed;
            else if (afterPlayed != null)
                part = afterPlayed;
            return part;
        }
    }
    class PlayGraph
    {
        #region class CtorHelper
        private sealed class CtorHelper
        {
            #region Fields
            private readonly WordGraph graph;
            private readonly Board board;
            private readonly ConstantDictionary<WordPart, PlayPath> paths;
            private readonly ConstantList<PlayPath> valids;
            #endregion
            #region Private stuff
            private static PlayPath GetPath(WordPart mainPart, WordPart extraPart, LetterPlay played, ConstantSet<char> pending)
            {
                if (extraPart == null)
                    return new PlayPath(mainPart, played, pending.ToConstant());
                return new PlayPath(mainPart, extraPart, played, pending.ToConstant());
            }
            #endregion
            public CtorHelper(WordGraph graph, Board board, Rack rack)
            {
                this.graph = graph;
                this.board = board;
                var tempPaths = new Dictionary<WordPart, PlayPath>();
                var tempValids = new List<PlayPath>();
                var cells = board.GetStartCells();
                foreach (Cell cell in cells)
                {
                    foreach (Direction direction in new[] { Direction.Bottom, Direction.Right })
                    {
                        var otherDirection = direction == Direction.Bottom ? Direction.Right : Direction.Bottom;
                        var choices = new HashSet<char>(rack.Letters);
                        WordPartPair extraPair = GetBeforeAfter(board, cell, otherDirection, graph, choices);
                        WordPartPair mainPair = GetBeforeAfter(board, cell, direction, graph, choices);
                        if (choices.Count == 0)
                            continue;
                        foreach (char letter in choices)
                        {
                            WordPart mainPart = mainPair.Play(cell, letter) ?? new WordPart(letter.ToString(), cell, direction);
                            WordPart extraPart = extraPair.Play(cell, letter);
                            if (extraPart != null && !graph.IsValid(extraPart.Word))
                                continue;
                            var played = new LetterPlay(cell, letter);
                            var pending = new HashSet<char>(rack.Letters);
                            pending.Remove(letter);
                            bool valid = mainPart.Word.Length == 1 || graph.IsValid(mainPart.Word);
                            var path = GetPath(mainPart, extraPart, played, pending.ToConstant());
                            if (valid)
                            {
                                tempValids.Add(path);
                                //Debug(path);
                            }
                            tempPaths.Add(mainPart, path);
                        }
                    }
                }
                paths = new ConstantDictionary<WordPart, PlayPath>(tempPaths);
                valids = new ConstantList<PlayPath>(tempValids);
            }
            public WordGraph Graph { get { return graph; } }
            public Board Board { get { return board; } }
            public ConstantDictionary<WordPart, PlayPath> Paths { get { return paths; } }
            public ConstantList<PlayPath> Valids { get { return valids; } }
        }
        #endregion
        #region class Choices
        private sealed class Choices
        {
            #region Fields
            private readonly WordPartPair main;
            private readonly WordPartPair extra;
            private readonly ConstantSet<char> letters;
            #endregion
            public Choices(WordGraph graph, Board board, PlayPath path, Fix fix, Cell cell)
            {
                Direction direction = path.Main.Direction;
                var otherDirection = direction == Direction.Bottom ? Direction.Right : Direction.Bottom;
                var choices = new HashSet<char>(path.Pending);
                WordPart before = fix == Fix.Suffix ? path.Main : null;
                WordPart after = fix == Fix.Prefix ? path.Main : null;
                main = GetBeforeAfter(board, cell, direction, graph, choices, before, after);
                extra = GetBeforeAfter(board, cell, otherDirection, graph, choices);
                letters = choices.ToConstant();
            }
            public WordPartPair Main { get { return main; } }
            public WordPartPair Extra { get { return extra; } }
            public ConstantSet<char> Letters { get { return letters; } }
        }
        #endregion
        #region Fields
        private readonly WordGraph graph;
        private readonly Board board;
        private readonly ConstantDictionary<WordPart, PlayPath> paths;
        private readonly ConstantList<PlayPath> valids;
        #endregion
        #region Private stuff
        private PlayGraph(WordGraph graph, Board board, ConstantDictionary<WordPart, PlayPath> paths, ConstantList<PlayPath> valids)
        {
            this.graph = graph;
            this.board = board;
            this.paths = paths;
            this.valids = valids;
        }
        private PlayGraph(CtorHelper init)
            : this(init.Graph, init.Board, init.Paths, init.Valids)
        {
        }
        private static WordPartPair GetBeforeAfter(Board board, Cell cell, Direction direction, WordGraph graph, HashSet<char> choices, WordPart before = null, WordPart after = null)
        {
            before = before ?? board.GetBeforePart(cell, direction);
            after = after ?? board.GetAfterPart(cell, direction);
            if (before != null && graph.Contains(before.Word))
                choices.IntersectWith(graph.GetLetters(before.Word, Fix.Suffix));
            if (after != null && graph.Contains(after.Word))
                choices.IntersectWith(graph.GetLetters(after.Word, Fix.Prefix));
            return new WordPartPair(before, after);
        }
        private static IEnumerable<Tuple<Fix, Cell>> GetFixCells(PlayPath path)
        {
            Direction direction = path.Main.Direction;
            var start = new List<Tuple<Fix, Cell>>();
            switch (direction)
            {
                case Direction.Right:
                    if (path.Main.First.HasLeft)
                        start.Add(new Tuple<Fix, Cell>(Fix.Prefix, path.Main.First.Left));
                    if (path.Main.Last.HasRight)
                        start.Add(new Tuple<Fix, Cell>(Fix.Suffix, path.Main.Last.Right));
                    break;
                case Direction.Bottom:
                    if (path.Main.First.HasTop)
                        start.Add(new Tuple<Fix, Cell>(Fix.Prefix, path.Main.First.Top));
                    if (path.Main.Last.HasBottom)
                        start.Add(new Tuple<Fix, Cell>(Fix.Suffix, path.Main.Last.Bottom));
                    break;
            }
            return start;
        }
        //private static void Debug(PlayPath path)
        //{
        //    var sb = new StringBuilder();
        //    sb.AppendFormat("{0} {1} {2}", path.Main.First, path.Main.Direction, path.Main.Word);
        //    if (path.Extras.Count > 0)
        //        foreach (WordPart extra in path.Extras)
        //            sb.AppendFormat(" [{0} {1} {2}]", extra.First, extra.Direction, extra.Word);
        //    sb.AppendFormat(" +{0}", string.Join(string.Empty, (from lp in path.Played select lp.Letter).ToArray()));
        //    if (path.Pending.Count > 0)
        //        sb.AppendFormat(" [{0}]", new string(path.Pending.ToArray()));
        //    Console.WriteLine(sb);
        //}
        #endregion
        public PlayGraph(WordGraph graph, Board board, Rack rack)
            : this(new CtorHelper(graph, board, rack))
        {
        }
        public IEnumerable<PlayPath> Paths
        {
            get { return paths.Values; }
        }
        public ConstantList<PlayPath> Valids { get { return valids; } }
        public bool Contains(WordPart part)
        {
            return paths.ContainsKey(part);
        }
        public PlayGraph Next()
        {
            var newPaths = new Dictionary<WordPart, PlayPath>();
            var newValids = new List<PlayPath>(valids);
            foreach (PlayPath path in paths.Values)
            {
                IEnumerable<Tuple<Fix, Cell>> start = GetFixCells(path);
                foreach (Tuple<Fix, Cell> fixCell in start)
                {
                    Fix fix = fixCell.Item1;
                    Cell cell = fixCell.Item2;
                    var choices = new Choices(graph, board, path, fix, cell);
                    if (choices.Letters.Count == 0)
                        continue;
                    foreach (char letter in choices.Letters)
                    {
                        WordPart mainPart = choices.Main.Play(cell, letter);
                        if (newPaths.ContainsKey(mainPart))
                            continue;
                        WordPart extraPart = choices.Extra.Play(cell, letter);
                        if (extraPart != null && !graph.IsValid(extraPart.Word))
                            continue;
                        var pending = new HashSet<char>(path.Pending);
                        pending.Remove(letter);
                        if (pending.Count == 0 && !graph.IsValid(mainPart.Word))
                            continue;
                        var extras = new List<WordPart>(path.Extras);
                        if (extraPart != null)
                            extras.Add(extraPart);
                        var letterPlay = new LetterPlay(cell, letter);
                        var played = new List<LetterPlay>(path.Played);
                        if (fix == Fix.Prefix)
                            played.Insert(0, letterPlay);
                        else
                            played.Add(letterPlay);
                        bool valid = mainPart.Word.Length == 1 || graph.IsValid(mainPart.Word);
                        var newPath = new PlayPath(mainPart, new WordPartCollection(extras.ToConstant()), played.ToConstant(), pending.ToConstant());
                        if (valid)
                        {
                            newValids.Add(newPath);
                            //Debug(newPath);
                        }
                        newPaths.Add(mainPart, newPath);
                    }
                }
            }
            return new PlayGraph(graph, board, newPaths.ToConstant(), newValids.ToConstant());
        }
        public Fix GetFixes(PlayPath path)
        {
            var result = Fix.None;
            IEnumerable<Tuple<Fix, Cell>> start = GetFixCells(path);
            foreach (Tuple<Fix, Cell> fixCell in start)
            {
                Fix fix = fixCell.Item1;
                Cell cell = fixCell.Item2;
                var choices = new Choices(graph, board, path, fix, cell);
                if (choices.Letters.Count > 0)
                    result |= fix;
            }
            return result;
        }
    }
    class PlayInfo : IComparable<PlayInfo>
    {
        #region Fields
        private readonly bool vortex;
        private readonly Score score;
        private readonly bool hasOneFixes;
        private readonly bool hasTwoMoreFixes;
        #endregion
        public PlayInfo(bool vortex, Score score, bool hasOneFixes, bool hasTwoMoreFixes)
        {
            this.vortex = vortex;
            this.score = score;
            this.hasOneFixes = hasOneFixes;
            this.hasTwoMoreFixes = hasTwoMoreFixes;
        }
        public bool HasVortex { get { return vortex; } }
        public bool Wins { get { return score.Other.Wins; } }
        public int Diff { get { return score.Other.Points - score.Current.Points; } }
        public int Points { get { return score.Other.Points; } }
        public int Stars { get { return score.Other.Stars; } }
        public bool HasOneFixes { get { return hasOneFixes; } }
        public bool HasTwoMoreFixes { get { return hasTwoMoreFixes; } }
        public bool HasFixes { get { return hasOneFixes || hasTwoMoreFixes; } }
        public int CompareTo(PlayInfo other)
        {
            return PlayInfoComparer.Default.Compare(this, other);
        }
    }
    class PlayInfoComparer : IComparer<PlayInfo>
    {
        #region Fields
        private static readonly Lazy<PlayInfoComparer> none = new Lazy<PlayInfoComparer>(() => new PlayInfoComparer(ScoreStrategy.None, WordStrategy.None));
        private readonly ScoreStrategy scoreStrategy;
        private readonly WordStrategy wordStrategy;
        #endregion
        #region Private stuff
        //private static int Or(int first, int second)
        //{
        //    return first != 0 ? first : second;
        //}
        private static bool NoVortex(PlayInfo x, PlayInfo y)
        {
            return !x.HasVortex && !y.HasVortex;
        }
        private static int CompareVortexes(PlayInfo x, PlayInfo y)
        {
            return x.HasVortex.CompareTo(y.HasVortex);
        }
        private int CompareWords(PlayInfo x, PlayInfo y)
        {
            switch (wordStrategy)
            {
                case WordStrategy.None:
                    return 0;
                case WordStrategy.NoOneFixes:
                    if (NoVortex(x, y))
                        return -x.HasOneFixes.CompareTo(y.HasOneFixes);
                    return CompareVortexes(x, y);
                case WordStrategy.NoTwoMoreFixes:
                    if (NoVortex(x, y))
                        return -x.HasTwoMoreFixes.CompareTo(y.HasTwoMoreFixes);
                    return CompareVortexes(x, y);
                case WordStrategy.NoFixes:
                    if (NoVortex(x, y))
                        return -x.HasFixes.CompareTo(y.HasFixes);
                    return CompareVortexes(x, y);
                default:
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Unknown word strategy : {0}", wordStrategy));
            }
        }
        private int CompareScores(PlayInfo x, PlayInfo y)
        {
            switch (scoreStrategy)
            {
                case ScoreStrategy.None:
                    return 0;
                case ScoreStrategy.MaxDiff:
                    return x.Diff.CompareTo(y.Diff);
                case ScoreStrategy.MaxPoints:
                    return x.Points.CompareTo(y.Points);
                default:
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Unknown score strategy : {0}", scoreStrategy));
            }
        }
        private static int CompareStars(PlayInfo x, PlayInfo y)
        {
            return x.Stars.CompareTo(y.Stars);
        }
        #endregion
        public PlayInfoComparer(ScoreStrategy scoreStrategy, WordStrategy wordStrategy)
        {
            this.scoreStrategy = scoreStrategy;
            this.wordStrategy = wordStrategy;
        }
        public int Compare(PlayInfo x, PlayInfo y)
        {
            if (x.Wins ^ y.Wins)
                return x.Wins ? 1 : -1;
            int result = CompareWords(x, y);
            if (result == 0)
                result = CompareScores(x, y);
            if (result == 0)
                result = CompareStars(x, y);
            return result;
        }
        public static PlayInfoComparer Default
        {
            get { return none.Value; }
        }
    }
    class ReverseComparer<T> : IComparer<T>
    {
        #region Fields
        private readonly IComparer<T> wrapped;
        #endregion
        public ReverseComparer(IComparer<T> wrapped)
        {
            this.wrapped = wrapped;
        }
        public int Compare(T x, T y)
        {
            return wrapped.Compare(y, x);
        }
    }
}
