using System;
using System.Collections;
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
            return string.Format(CultureInfo.InvariantCulture, "{0} {1}", letter, cell);
        }
    }
    class PlayPath
    {
        #region Fields
        private readonly WordPart main;
        private readonly WordPartCollection extras;
        private readonly ConstantList<LetterPlay> played;
        private readonly ConstantList<char> pending;
        #endregion
        public PlayPath(WordPart main)
            : this(main, new WordPartCollection(), main.GetPlayed(), new ConstantList<char>())
        {
        }
        public PlayPath(WordPart main, LetterPlay letter, ConstantList<char> pending)
            : this(main, WordPartCollection.Empty, new ConstantList<LetterPlay>(letter), pending)
        {
        }
        public PlayPath(WordPart main, WordPart extra, LetterPlay letter, ConstantList<char> pending)
            : this(main, new WordPartCollection(extra), new ConstantList<LetterPlay>(letter), pending)
        {
        }
        public PlayPath(WordPart main, WordPartCollection extras, ConstantList<LetterPlay> played, ConstantList<char> pending)
        {
            this.main = main;
            this.extras = extras;
            this.played = played;
            this.pending = pending;
        }
        public WordPart Main { get { return main; } }
        public WordPartCollection Extras { get { return extras; } }
        public ConstantList<LetterPlay> Played { get { return played; } }
        public ConstantList<char> Pending { get { return pending; } }
        public void Write()
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{0}", main);
            if (extras.Count > 0)
                foreach (WordPart extra in extras)
                    sb.AppendFormat(" [{0}]", extra);
            sb.AppendFormat(" +{0}", string.Join(string.Empty, (from lp in played select lp.Letter).ToArray()));
            if (pending.Count > 0)
                sb.AppendFormat(" [{0}]", new string(pending.ToArray()));
            Console.WriteLine(sb);
        }
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
            private readonly ConstantList<PlayPath> valids;
            #endregion
            #region Private stuff
            private static PlayPath GetPath(WordPart mainPart, WordPart extraPart, LetterPlay played, ConstantList<char> pending)
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
                var validMap = new Dictionary<WordPart, PlayPath>();
                var cells = board.GetStartCells();
                var done = new HashSet<WordPart>();
                foreach (Cell cell in cells)
                {
                    foreach (Direction direction in new[] { Direction.Down, Direction.Right })
                    {
                        var otherDirection = direction == Direction.Down ? Direction.Right : Direction.Down;
                        var playable = new PlayableLetters(rack.Letters);
                        WordPartPair extraPair = GetBeforeAfter(board, cell, otherDirection, graph, playable);
                        WordPartPair mainPair = GetBeforeAfter(board, cell, direction, graph, playable);
                        if (playable.Count == 0)
                            continue;
                        foreach (char letter in playable)
                        {
                            WordPart mainPart = mainPair.Play(cell, letter) ?? new WordPart(letter.ToString(), cell, direction);
                            WordPart extraPart = extraPair.Play(cell, letter);
                            if (extraPart != null && !graph.IsValid(extraPart.Word))
                                continue;
                            var played = new LetterPlay(cell, letter);
                            var pending = new List<char>(rack.Letters);
                            pending.Remove(letter);
                            bool valid = mainPart.Word.Length == 1 || graph.IsValid(mainPart.Word);
                            var path = GetPath(mainPart, extraPart, played, pending.ToConstant());
                            if (valid && !validMap.ContainsKey(mainPart))
                                validMap.Add(mainPart, path);

                            var min = new Dictionary<Direction, HashSet<int>>();
                            min.Add(Direction.Down, new HashSet<int>());
                            min.Add(Direction.Right, new HashSet<int>());
                            var max = new Dictionary<Direction, HashSet<int>>();
                            max.Add(Direction.Down, new HashSet<int>());
                            max.Add(Direction.Right, new HashSet<int>());

                            GetNewPaths(graph, board, path, path, done, validMap, true, min, max);
                        }
                    }
                }
                valids = new ConstantList<PlayPath>(validMap.Values.ToList());
            }
            public WordGraph Graph { get { return graph; } }
            public Board Board { get { return board; } }
            public ConstantList<PlayPath> Valids { get { return valids; } }
        }
        #endregion
        #region class PlayableLetters
        private sealed class PlayableLetters : IEnumerable<char>
        {
            #region Fieldsx
            private HashSet<char> choices;
            #endregion
            public PlayableLetters(IEnumerable<char> choices)
                : this(choices == null ? null : new HashSet<char>(choices))
            {
            }
            public PlayableLetters(HashSet<char> choices)
            {
                this.choices = choices == null ? null : new HashSet<char>(choices);
            }
            public void IntersectWith(ISet<char> letters)
            {
                if (choices == null)
                    choices = new HashSet<char>(letters);
                else
                    choices.IntersectWith(letters);
            }
            public ISet<char> Choices
            {
                get { return choices ?? new HashSet<char>(); }
            }
            public int Count
            {
                get { return Choices.Count; }
            }
            public IEnumerator<char> GetEnumerator()
            {
                return Choices.GetEnumerator();
            }
            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }
        }
        #endregion
        #region class Choices
        internal sealed class Choices
        {
            #region Fields
            private readonly Fix fix;
            private readonly Cell cell;
            private readonly WordPartPair main;
            private readonly WordPartPair extra;
            private readonly ConstantSet<char> letters;
            #endregion
            public Choices(WordGraph graph, Board board, PlayPath path, Fix fix, Cell cell)
            {
                this.fix = fix;
                this.cell = cell;
                Direction direction = path.Main.Direction;
                var otherDirection = direction == Direction.Down ? Direction.Right : Direction.Down;
                var playable = new PlayableLetters(path.Pending);
                WordPart before = fix == Fix.Suffix ? path.Main : null;
                WordPart after = fix == Fix.Prefix ? path.Main : null;
                main = GetBeforeAfter(board, cell, direction, graph, playable, before, after);
                extra = GetBeforeAfter(board, cell, otherDirection, graph, playable);
                letters = playable.Choices.ToConstant();
            }
            public Fix Fix { get { return fix; } }
            public Cell Cell { get { return cell; } }
            public WordPartPair Main { get { return main; } }
            public WordPartPair Extra { get { return extra; } }
            public ConstantSet<char> Letters { get { return letters; } }
        }
        #endregion
        #region Fields
        private readonly WordGraph graph;
        private readonly Board board;
        private readonly ConstantList<PlayPath> valids;
        #endregion
        #region Private stuff
        private PlayGraph(WordGraph graph, Board board, ConstantList<PlayPath> valids)
        {
            this.graph = graph;
            this.board = board;
            this.valids = valids;
        }
        private PlayGraph(CtorHelper init)
            : this(init.Graph, init.Board, init.Valids)
        {
        }
        private static WordPartPair GetBeforeAfter(Board board, Cell cell, Direction direction, WordGraph graph, PlayableLetters choices, WordPart before = null, WordPart after = null)
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
                case Direction.Down:
                    if (path.Main.First.HasTop)
                        start.Add(new Tuple<Fix, Cell>(Fix.Prefix, path.Main.First.Up));
                    if (path.Main.Last.HasBottom)
                        start.Add(new Tuple<Fix, Cell>(Fix.Suffix, path.Main.Last.Down));
                    break;
            }
            return start;
        }
        internal static void GetNewPaths(WordGraph graph, Board board, PlayPath origin, PlayPath path, ISet<WordPart> done, IDictionary<WordPart, PlayPath> newValids, Choices choices, bool validOnly, Dictionary<Direction, HashSet<int>> min, Dictionary<Direction, HashSet<int>> max)
        {
            foreach (char letter in choices.Letters)
            {
                WordPart mainPart = choices.Main.Play(choices.Cell, letter);
                if (done.Contains(mainPart))
                    continue;
                if (!validOnly)
                {
                    if (choices.Fix == Fix.Prefix)
                    {
                        if (min[path.Main.Direction].Contains(0))
                            break;
                    }
                    else if (choices.Fix == Fix.Suffix)
                    {
                        if (max[path.Main.Direction].Contains(8))
                            break;
                    }
                }
                WordPart extraPart = choices.Extra.Play(choices.Cell, letter);
                if (extraPart != null && (validOnly && !graph.IsValid(extraPart.Word) || !validOnly && !graph.Contains(extraPart.Word)))
                    continue;
                ConstantList<char> pending = null;
                if (path.Pending != null)
                {
                    var temp = new List<char>(path.Pending);
                    temp.Remove(letter);
                    if (temp.Count == 0 && !graph.IsValid(mainPart.Word))
                        continue;
                    pending = temp.ToConstant();
                }
                var extras = new List<WordPart>(path.Extras);
                if (extraPart != null)
                    extras.Add(extraPart);
                var letterPlay = new LetterPlay(choices.Cell, letter);
                var played = new List<LetterPlay>(path.Played);
                if (choices.Fix == Fix.Prefix)
                    played.Insert(0, letterPlay);
                else
                    played.Add(letterPlay);
                bool valid = mainPart.Word.Length == 1 || graph.IsValid(mainPart.Word);
                var newPath = new PlayPath(mainPart, new WordPartCollection(extras.ToConstant()), played.ToConstant(), pending);
                if (valid && !newValids.ContainsKey(mainPart))
                    newValids.Add(mainPart, newPath);
                done.Add(mainPart);
                {
                    if (!validOnly && valid)
                    {
                        min[mainPart.Direction].Add(mainPart.Direction == Direction.Right ? newPath.Main.First.Column : newPath.Main.First.Row);
                        max[mainPart.Direction].Add(mainPart.Direction == Direction.Right ? newPath.Main.Last.Column : newPath.Main.Last.Row);

                        if (choices.Fix == Fix.Prefix)
                        {
                            if (path.Main.Direction == Direction.Right)
                            {
                                if (origin.Main.First.Column - path.Main.First.Column >= 2 || path.Main.First.Column == 0)
                                    break;
                            }
                            else
                            {
                                if (origin.Main.First.Row - path.Main.First.Row >= 2 || path.Main.First.Row == 0)
                                    break;
                            }
                        }
                        else
                        {
                            if (path.Main.Direction == Direction.Right)
                            {
                                if (path.Main.Last.Column - origin.Main.Last.Column >= 2 || path.Main.Last.Column == 8)
                                    break;
                            }
                            else
                            {
                                if (path.Main.Last.Row - origin.Main.Last.Row >= 2 || path.Main.Last.Row == 8)
                                    break;
                            }
                        }

                    }
                    GetNewPaths(graph, board, origin, newPath, done, newValids, validOnly, min, max);
                }
            }
        }
        internal static void GetNewPaths(WordGraph graph, Board board, PlayPath origin, PlayPath path, ISet<WordPart> done, IDictionary<WordPart, PlayPath> newValids, bool validOnly, Dictionary<Direction, HashSet<int>> min, Dictionary<Direction, HashSet<int>> max)
        {
            if (!validOnly)
            {
                //if (min[path.Main.Direction].Contains(0) && max[path.Main.Direction].Contains(8))
                //    return;

                //Console.WriteLine("{0}{1}", new string('.', path.Main.Direction == Direction.Bottom ? path.Main.First.Row : path.Main.First.Column), path.Main.Word);
            }

            IEnumerable<Tuple<Fix, Cell>> start = GetFixCells(path);
            foreach (Tuple<Fix, Cell> fixCell in start)
            {
                Fix fix = fixCell.Item1;
                Cell cell = fixCell.Item2;

                if (fix == Fix.Prefix)
                {
                    if (min[path.Main.Direction].Contains(0))
                        break;
                }
                else if (fix == Fix.Suffix)
                {
                    if (max[path.Main.Direction].Contains(0))
                        break;
                }

                if (fix == Fix.Prefix)
                {
                    if (path.Main.Direction == Direction.Right)
                    {
                        if (origin.Main.First.Column - path.Main.First.Column >= 2 || path.Main.First.Column == 0)
                            break;
                    }
                    else
                    {
                        if (origin.Main.First.Row - path.Main.First.Row >= 2 || path.Main.First.Row == 0)
                            break;
                    }
                }
                else
                {
                    if (path.Main.Direction == Direction.Right)
                    {
                        if (path.Main.Last.Column - origin.Main.Last.Column >= 2 || path.Main.Last.Column == 8)
                            break;
                    }
                    else
                    {
                        if (path.Main.Last.Row - origin.Main.Last.Row >= 2 || path.Main.Last.Row == 8)
                            break;
                    }
                }

                var choices = new Choices(graph, board, path, fix, cell);
                if (choices.Letters.Count == 0)
                    continue;
                GetNewPaths(graph, board, origin, path, done, newValids, choices, validOnly, min, max);
            }
        }
        #endregion
        public PlayGraph(WordGraph graph, Board board, Rack rack)
            : this(new CtorHelper(graph, board, rack))
        {
        }
        public ConstantList<PlayPath> Valids { get { return valids; } }
        private static Tuple<Fix, Fix> GetFixes(WordPart part, WordPart future)
        {
            Fix one = Fix.None;
            Fix twoMore = Fix.None;
            int before = (part.First.Row - future.First.Row)
                        + (part.First.Column - future.First.Column);
            if (before >= 1)
                one |= Fix.Prefix;
            if (before >= 2)
                twoMore |= Fix.Prefix;
            int after = (future.Last.Row - part.Last.Row)
                        + (future.Last.Column - part.Last.Column);
            if (after >= 1)
                one |= Fix.Suffix;
            if (after >= 2)
                twoMore |= Fix.Suffix;
            return new Tuple<Fix, Fix>(one, twoMore);
        }
        private static Tuple<Fix, Fix> GetFixes(WordPart part, IEnumerable<PlayPath> allValids)
        {
            Fix one = Fix.None;
            Fix twoMore = Fix.None;
            foreach (PlayPath future in allValids)
            {
                Tuple<Fix, Fix> tuple = GetFixes(part, future.Main);
                one |= tuple.Item1;
                twoMore |= tuple.Item2;
            }
            return new Tuple<Fix, Fix>(one, twoMore);
        }
        public Tuple<Fix, Fix> GetFixes(PlayPath path)
        {
            var min = new Dictionary<Direction, HashSet<int>>();
            min.Add(Direction.Down, new HashSet<int>());
            min.Add(Direction.Right, new HashSet<int>());
            var max = new Dictionary<Direction, HashSet<int>>();
            max.Add(Direction.Down, new HashSet<int>());
            max.Add(Direction.Right, new HashSet<int>());

            //var chrono = new Chrono();
            var done = new HashSet<WordPart>();
            var allValids = new Dictionary<WordPart, PlayPath>();
            var temp = new PlayPath(path.Main, path.Extras, path.Played, null);
            GetNewPaths(graph, board, temp, temp, done, allValids, false, min, max);
            //double seconds = Convert.ToInt32(chrono.Elapsed.TotalSeconds * 10) * .1;
            //if (seconds > 0)
            //    Console.WriteLine("Analyzed {0} fix paths in {1} s", done.Count, seconds);
            return GetFixes(path.Main, allValids.Values);
        }
    }
    class PlayInfo : IComparable<PlayInfo>
    {
        #region Fields
        private readonly bool vortex;
        private readonly Score score;
        private readonly Fix oneFixes;
        private readonly Fix twoMoreFixes;
        #endregion
        public PlayInfo(bool vortex, Score score, Fix oneFixes, Fix twoMoreFixes)
        {
            this.vortex = vortex;
            this.score = score;
            this.oneFixes = oneFixes;
            this.twoMoreFixes = twoMoreFixes;
        }
        public bool HasVortex { get { return vortex; } }
        public bool Wins { get { return score.Other.Wins; } }
        public int Diff { get { return score.Other.Points - score.Current.Points; } }
        public int Points { get { return score.Other.Points; } }
        public int Stars { get { return score.Other.Stars; } }
        public Fix OneFixes { get { return oneFixes; } }
        public Fix TwoMoreFixes { get { return twoMoreFixes; } }
        public bool HasOneFixes { get { return oneFixes != Fix.None; } }
        public bool HasTwoMoreFixes { get { return twoMoreFixes != Fix.None; } }
        public bool HasFixes { get { return HasOneFixes || HasTwoMoreFixes; } }
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
        private static bool NoVortex(PlayInfo x, PlayInfo y)
        {
            return !x.HasVortex && !y.HasVortex;
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
                    return 0;
                case WordStrategy.NoTwoMoreFixes:
                    if (NoVortex(x, y))
                        return -x.HasTwoMoreFixes.CompareTo(y.HasTwoMoreFixes);
                    return 0;
                case WordStrategy.NoFixes:
                    if (NoVortex(x, y))
                        return -x.HasFixes.CompareTo(y.HasFixes);
                    return 0;
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
            if (x.Wins && y.Wins)
                return x.Points.CompareTo(y.Points);
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
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1}", scoreStrategy, wordStrategy);
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
