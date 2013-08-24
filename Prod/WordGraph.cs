using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ded.Wordox
{
    class Alphabet
    {
        #region Fields
        private static readonly ConstantList<char> letters = GetLetters();
        #endregion
        #region Private stuff
        private static ConstantList<char> GetLetters()
        {
            var list = new List<char>();
            for (char c = 'A'; c <= 'Z'; c++)
                list.Add(c);
            return list.ToConstant();
        }
        #endregion
        public static ConstantList<char> Letters { get { return letters; } }
    }
    [Flags]
    public enum Fix
    {
        None = 0,
        Prefix = 1,
        Suffix = 2,
        All = Prefix | Suffix,
    }
    class FixEdge
    {
        #region Fields
        private readonly char letter;
        private readonly Fix fix;
        private readonly WordVertex vertex;
        #endregion
        public FixEdge(char letter, Fix fix, WordVertex vertex)
        {
            if (fix != Fix.Prefix && fix != Fix.Suffix)
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Invalid fixes : {0}", fix), "fix");
            this.letter = letter;
            this.fix = fix;
            this.vertex = vertex;
        }
        public char Letter { get { return letter; } }
        public Fix Fix { get { return fix; } }
        public WordVertex Vertex { get { return vertex; } }
    }
    class WordVertex
    {
        #region Fields
        private readonly string word;
        private readonly bool valid;
        private readonly Dictionary<Fix, Dictionary<char, FixEdge>> edges;
        #endregion
        public WordVertex(string word, bool valid)
        {
            this.word = word;
            this.valid = valid;
            edges = new Dictionary<Fix, Dictionary<char, FixEdge>>();
        }
        public string Word { get { return word; } }
        public bool IsValid { get { return valid; } }
        public void Add(FixEdge edge)
        {
            Dictionary<char, FixEdge> map;
            if (!edges.TryGetValue(edge.Fix, out map))
            {
                map = new Dictionary<char, FixEdge>();
                edges.Add(edge.Fix, map);
            }
            map.Add(edge.Letter, edge);
        }
        public Fix OneFixes
        {
            get
            {
                var fixes = Fix.None;
                foreach (Fix f in edges.Keys)
                    foreach (FixEdge edge in edges[f].Values)
                        if (edge.Vertex.IsValid)
                            fixes |= f;
                return fixes;
            }
        }
        public IEnumerable<FixEdge> Edges
        {
            get
            {
                foreach (var map in edges.Values)
                    foreach (FixEdge edge in map.Values)
                        yield return edge;
            }
        }
        public ConstantSet<char> GetLetters(Fix fix)
        {
            if (fix != Fix.Prefix && fix != Fix.Suffix)
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Bad fix : {0}", fix), "fix");
            Dictionary<char, FixEdge> map;
            if (!edges.TryGetValue(fix, out map))
                return new ConstantSet<char>();
            return new ConstantSet<char>(map.Keys);
        }
    }
    class ValidWord
    {
        #region Fields
        private readonly string word;
        private readonly Fix oneFixes;
        private readonly Fix twoMoreFixes;
        #endregion
        public ValidWord(string word, Fix oneFixes, Fix twoMoreFixes)
        {
            this.word = word;
            this.oneFixes = oneFixes;
            this.twoMoreFixes = twoMoreFixes;
        }
        public string Word { get { return word; } }
        public Fix OneFixes { get { return oneFixes; } }
        public Fix TwoMoreFixes { get { return twoMoreFixes; } }
        public override string ToString()
        {
            return word;
        }
        public override int GetHashCode()
        {
            return word.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            var other = obj as ValidWord;
            if (other == null)
                return false;
            return word == other.word;
        }
    }
    class ValidWordComparer : IComparer<ValidWord>
    {
        #region Fields
        private readonly WordStrategy strategy;
        #endregion
        private int CompareStrategy(ValidWord x, ValidWord y)
        {
            switch (strategy)
            {
                case WordStrategy.None:
                    return 0;
                case WordStrategy.NoOneFixes:
                    return -(x.OneFixes != Fix.None).CompareTo(y.OneFixes != Fix.None);
                case WordStrategy.NoTwoMoreFixes:
                    return -(x.TwoMoreFixes != Fix.None).CompareTo(y.TwoMoreFixes != Fix.None);
                case WordStrategy.NoFixes:
                    return 0;
                default:
                    throw new InvalidOperationException(string.Format(CultureInfo.InvariantCulture, "Unknown word strategy : {0}", strategy));
            }
        }
        public ValidWordComparer(WordStrategy strategy)
        {
            this.strategy = strategy;
        }
        public int Compare(ValidWord x, ValidWord y)
        {
            int result = CompareStrategy(x, y);
            if (result == 0)
                return x.Word.Length.CompareTo(y.Word.Length);
            return result;
        }
    }
    class WordGraph
    {
        #region Fields
        private static readonly Lazy<WordGraph> french = new Lazy<WordGraph>(GetFrench);
        private readonly Dictionary<string, HashSet<string>> valids;
        private readonly Dictionary<string, WordVertex> vertices;
        private readonly Dictionary<char, int> counts;
        private readonly Dictionary<char, int> ranges;
        private readonly Dictionary<string, Fix> twoMoreFixes;
        private readonly Random random;
        private readonly int total;
        #endregion
        #region Private stuff
        private static IEnumerable<string> GetAllWords()
        {
            var resources = new AssemblyResources(typeof(WordDownloader), "Ded.Wordox.Resources.");
            string frenchContent = resources.GetContent("fr.txt");
            return frenchContent.Split(new[] { "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
        }
        private static WordGraph GetFrench()
        {
            var words = GetAllWords();
            return new WordGraph(words);
        }
        private void AddPrefixEdge(WordVertex vertex)
        {
            string prefix = vertex.Word.Substring(0, vertex.Word.Length - 1);
            WordVertex prefixVertex = GetVertex(prefix, false);
            prefixVertex.Add(new FixEdge(vertex.Word[prefix.Length], Fix.Suffix, vertex));
        }
        private void AddSuffixEdge(WordVertex vertex)
        {
            string suffix = vertex.Word.Substring(1);
            WordVertex suffixVertex = GetVertex(suffix, false);
            suffixVertex.Add(new FixEdge(vertex.Word[0], Fix.Prefix, vertex));
        }
        private void AddEdgesIfNeeded(WordVertex vertex)
        {
            if (vertex.Word.Length == 1)
                return;
            AddPrefixEdge(vertex);
            AddSuffixEdge(vertex);
        }
        private WordVertex GetVertex(string word, bool valid)
        {
            WordVertex vertex;
            if (vertices.TryGetValue(word, out vertex))
            {
                if (valid)
                    throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "{0} already found but not valid (are words sorted by size?)", word), "valid");
                return vertex;
            }
            vertex = new WordVertex(word, valid);
            vertices.Add(word, vertex);
            AddEdgesIfNeeded(vertex);
            return vertex;
        }
        private void AddValid(string word)
        {
            var sorted = word.Sort();
            HashSet<string> set;
            if (!valids.TryGetValue(sorted, out set))
            {
                set = new HashSet<string>();
                valids.Add(sorted, set);
            }
            set.Add(word);
        }
        internal char GetLetter(double d)
        {
            int value = Convert.ToInt32(d * total);
            char c = 'A';
            while (value > ranges[c] && c < 'Z')
            {
                if (value < ranges[c] + counts[c])
                    break;
                c++;
            }
            return c;
        }
        private ValidWord GetValid(string word)
        {
            WordVertex vertex = vertices[word];
            Fix one = vertex.OneFixes;
            Fix twoMore = twoMoreFixes[word];
            return new ValidWord(word, one, twoMore);
        }
        #endregion
        public WordGraph(IEnumerable<string> words)
        {
            vertices = new Dictionary<string, WordVertex>();
            valids = new Dictionary<string, HashSet<string>>();
            counts = new Dictionary<char, int>();
            ranges = new Dictionary<char, int>();
            twoMoreFixes = new Dictionary<string, Fix>();
            random = new Random();
            foreach (char c in Alphabet.Letters)
                counts.Add(c, 0);
            foreach (string word in words)
            {
                GetVertex(word, true);
                AddValid(word);
                foreach (char c in word)
                    counts[c]++;
            }
            total = 0;
            foreach (char c in Alphabet.Letters)
            {
                ranges.Add(c, total);
                total += counts[c];
            }
            foreach (WordVertex vertex in vertices.Values)
            {
                Fix one = Fix.None;
                Fix twoMore = Fix.None;
                foreach (FixEdge edge in vertex.Edges)
                {
                    if (edge.Vertex.IsValid)
                        one |= edge.Fix;
                    if ((edge.Vertex.OneFixes & edge.Fix) == edge.Fix)
                        twoMore |= edge.Fix;
                }
                twoMoreFixes.Add(vertex.Word, twoMore);
            }
        }
        public int Count { get { return vertices.Count; } }
        public IEnumerable<WordVertex> Vertices
        {
            get { return vertices.Values; }
        }
        public ConstantSet<ValidWord> GetValids(string letters)
        {
            var sorted = letters.Sort();
            HashSet<string> set;
            if (valids.TryGetValue(sorted, out set))
            {
                var result = new HashSet<ValidWord>();
                foreach (var word in set)
                    result.Add(GetValid(word));
                return result.ToConstant();
            }
            return new ConstantSet<ValidWord>();
        }
        public ConstantSet<char> GetLetters(string part, Fix fix)
        {
            WordVertex vertex;
            if (!vertices.TryGetValue(part, out vertex))
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Unknown word part : {0}", part), "part");
            return vertex.GetLetters(fix);
        }
        public bool IsValid(string word)
        {
            if (string.IsNullOrEmpty(word) || word.Length == 1)
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "{0} is too short", word), "word");
            WordVertex vertex;
            if (vertices.TryGetValue(word, out vertex))
                return vertex.IsValid;
            return false;
        }
        public static WordGraph French
        {
            get { return french.Value; }
        }
        public string GetRandom(string pending = "")
        {
            var ai = new AI(this);
            while (true)
            {
                var sb = new StringBuilder(Rack.Size);
                sb.Append(pending);
                while (sb.Length < sb.Capacity)
                    sb.Append(GetLetter(random.NextDouble()));
                string value = sb.ToString();
                if (ai.FindLongest(value).Count > 0)
                    return value;
            }
        }
        public void TellStats()
        {
            var sorted = new SortedDictionary<int, char>();
            foreach (char c in counts.Keys)
                sorted.Add(counts[c], c);
            foreach (int count in sorted.Keys)
                Console.WriteLine("{0} : {1}", sorted[count], count);
        }
        public bool Contains(string part)
        {
            return vertices.ContainsKey(part);
        }
        public double GetWeight(string word)
        {
            double p = 0;
            foreach (char c in word)
                p += counts[c];
            return p / total;
        }
    }
    class Rack
    {
        public const int Size = 6;
        #region Fields
        private readonly ConstantList<char> rack;
        private readonly string value;
        #endregion
        public Rack(string rack)
            : this(new ConstantList<char>(rack.ToArray()))
        {
        }
        public Rack(ConstantList<char> rack)
        {
            this.rack = rack;
            value = new string(rack.ToArray());
            if (value.Length != Size)
                throw new ArgumentException(string.Format(CultureInfo.InvariantCulture, "Expected {0} letters but got {1} ({2})", Size, value.Length, value));
        }
        public ConstantList<char> Letters { get { return rack; } }
        public string Value { get { return value; } }
        public override string ToString()
        {
            return value;
        }
    }
    class AI
    {
        #region Fields
        private readonly WordGraph graph;
        #endregion
        #region Private stuff
        private void FindLongest(string word, HashSet<ValidWord> set, int pending)
        {
            if (pending == 0)
                set.AddRange(Find(word));
            else
                for (int i = 0; i < word.Length; i++)
                    FindLongest(word.Remove(i, 1), set, pending - 1);
        }
        #endregion
        public AI(WordGraph graph)
        {
            this.graph = graph;
        }
        public ConstantSet<ValidWord> Find(string word)
        {
            return graph.GetValids(word);
        }
        public ConstantSet<ValidWord> FindLongest(Rack rack)
        {
            return FindLongest(rack.Value);
        }
        public ConstantSet<ValidWord> FindLongest(string word)
        {
            ConstantSet<ValidWord> found = Find(word);
            if (found.Count > 0)
                return found;
            for (int pending = 1; pending <= word.Length - 2; pending++)
            {
                var set = new HashSet<ValidWord>();
                FindLongest(word, set, pending);
                if (set.Count > 0)
                    return set.ToConstant();
            }
            return new ConstantSet<ValidWord>();
        }
    }
}
