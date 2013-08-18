using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ded.Wordox
{
    class PlayPath
    {
        #region Fields
        private readonly WordPart main;
        private readonly WordPartCollection extras;
        private readonly string played;
        private readonly ConstantSet<char> pending;
        #endregion
        public PlayPath(WordPart main, char played, ConstantSet<char> pending)
            : this(main, WordPartCollection.Empty, played.ToString(), pending)
        {
        }
        public PlayPath(WordPart main, WordPart extra, char played, ConstantSet<char> pending)
            : this(main, new WordPartCollection(extra), played.ToString(), pending)
        {
        }
        public PlayPath(WordPart main, WordPartCollection extras, string played, ConstantSet<char> pending)
        {
            this.main = main;
            this.extras = extras;
            this.played = played;
            this.pending = pending;
        }
        public WordPart Main { get { return main; } }
        public WordPartCollection Extras { get { return extras; } }
        public string Played { get { return played; } }
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
        #region class Init
        private sealed class Init
        {
            #region Fields
            private readonly WordGraph graph;
            private readonly Board board;
            private readonly Dictionary<WordPart, PlayPath> paths;
            #endregion
            public Init(WordGraph graph, Board board, Rack rack)
            {
                this.graph = graph;
                this.board = board;
                paths = new Dictionary<WordPart, PlayPath>();
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
                            WordPart extraPart = extraPair.Play(cell, letter);
                            if (extraPart != null && !graph.IsValid(extraPart.Word))
                                continue;
                            WordPart mainPart = mainPair.Play(cell, letter) ?? new WordPart(letter.ToString(), cell, direction);
                            var pending = new HashSet<char>(rack.Letters);
                            pending.Remove(letter);
                            bool isValid = mainPart.Word.Length == 1 || graph.IsValid(mainPart.Word);
                            var path = extraPart == null ? new PlayPath(mainPart, letter, pending.ToConstant()) : new PlayPath(mainPart, extraPart, letter, pending.ToConstant());
                            if (isValid)
                                Debug(path);
                            paths.Add(mainPart, path);
                        }
                    }
                }
            }
            public WordGraph Graph { get { return graph; } }
            public Board Board { get { return board; } }
            public Dictionary<WordPart, PlayPath> Paths { get { return paths; } }
        }
        #endregion
        #region Fields
        private readonly WordGraph graph;
        private readonly Board board;
        private readonly Dictionary<WordPart, PlayPath> paths;
        #endregion
        #region Private stuff
        private PlayGraph(WordGraph graph, Board board, Dictionary<WordPart, PlayPath> paths)
        {
            this.graph = graph;
            this.board = board;
            this.paths = paths;
        }
        private PlayGraph(Init init)
            : this(init.Graph, init.Board, init.Paths)
        {
        }
        private static WordPartPair GetBeforeAfter(Board board, Cell cell, Direction direction, WordGraph graph, HashSet<char> choices, WordPart before = null, WordPart after = null)
        {
            before = before ?? board.GetBeforePart(cell, direction);
            after = after ?? board.GetAfterPart(cell, direction);
            if (before != null)
                choices.IntersectWith(graph.GetLetters(before.Word, Fix.Suffix));
            if (after != null)
                choices.IntersectWith(graph.GetLetters(after.Word, Fix.Prefix));
            return new WordPartPair(before, after);
        }
        private static void Debug(PlayPath path)
        {
            var sb = new StringBuilder();
            sb.AppendFormat("{0} {1} {2}", path.Main.First, path.Main.Direction, path.Main.Word);
            if (path.Extras.Count > 0)
                foreach (WordPart extra in path.Extras)
                    sb.AppendFormat(" [{0} {1} {2}]", extra.First, extra.Direction, extra.Word);
            sb.AppendFormat(" {0}", path.Played);
            if (path.Pending.Count > 0)
                sb.AppendFormat(" {0}", new string(path.Pending.ToArray()));
            Console.WriteLine(sb);
        }
        #endregion
        public PlayGraph(WordGraph graph, Board board, Rack rack)
            : this(new Init(graph, board, rack))
        {
        }
        public IEnumerable<PlayPath> Paths
        {
            get { return paths.Values; }
        }
        public bool Contains(WordPart part)
        {
            return paths.ContainsKey(part);
        }
        public PlayGraph Next()
        {
            var newPaths = new Dictionary<WordPart, PlayPath>();
            foreach (PlayPath path in paths.Values)
            {
                Direction direction = path.Main.Direction;
                var start = new List<Tuple<Cell, Fix>>();
                switch (direction)
                {
                    case Direction.Right:
                        if (path.Main.First.HasLeft)
                            start.Add(new Tuple<Cell, Fix>(path.Main.First.Left, Fix.Prefix));
                        if (path.Main.Last.HasRight)
                            start.Add(new Tuple<Cell, Fix>(path.Main.Last.Right, Fix.Suffix));
                        break;
                    case Direction.Bottom:
                        if (path.Main.First.HasTop)
                            start.Add(new Tuple<Cell, Fix>(path.Main.First.Top, Fix.Prefix));
                        if (path.Main.Last.HasBottom)
                            start.Add(new Tuple<Cell, Fix>(path.Main.Last.Bottom, Fix.Suffix));
                        break;
                }
                foreach (Tuple<Cell, Fix> cellFix in start)
                {
                    Cell cell = cellFix.Item1;
                    Fix fix = cellFix.Item2;
                    var otherDirection = direction == Direction.Bottom ? Direction.Right : Direction.Bottom;
                    var choices = new HashSet<char>(path.Pending);
                    WordPartPair extraPair = GetBeforeAfter(board, cell, otherDirection, graph, choices);
                    WordPart before = fix == Fix.Suffix ? path.Main : null;
                    WordPart after = fix == Fix.Prefix ? path.Main : null;
                    WordPartPair mainPair = GetBeforeAfter(board, cell, direction, graph, choices, before, after);
                    if (choices.Count == 0)
                        continue;
                    foreach (char letter in choices)
                    {
                        WordPart extraPart = extraPair.Play(cell, letter);
                        if (extraPart != null && !graph.IsValid(extraPart.Word))
                            continue;
                        WordPart mainPart = mainPair.Play(cell, letter);
                        if (newPaths.ContainsKey(mainPart))
                            continue;
                        var pending = new HashSet<char>(path.Pending);
                        pending.Remove(letter);
                        if (pending.Count == 0 && !graph.IsValid(mainPart.Word))
                            continue;
                        var extras = new List<WordPart>(path.Extras);
                        if (extraPart != null)
                            extras.Add(extraPart);
                        string played = fix == Fix.Prefix ? letter + path.Played : path.Played + letter;
                        bool isValid = mainPart.Word.Length == 1 || graph.IsValid(mainPart.Word);
                        var newPath = new PlayPath(mainPart, new WordPartCollection(extras.ToConstant()), played, pending.ToConstant());
                        if (isValid)
                            Debug(newPath);
                        newPaths.Add(mainPart, newPath);
                    }
                }
            }
            return new PlayGraph(graph, board, newPaths);
        }
    }
}
