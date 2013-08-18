using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ded.Wordox
{
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
                throw new ArgumentException(string.Format("Invalid fixes : {0}", fix), "fix");
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
        public Fix Fixes
        {
            get
            {
                var fixes = Fix.None;
                foreach (Fix f in edges.Keys)
                    if (edges[f].Count > 0)
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
                throw new ArgumentException(string.Format("Bad fix : {0}", fix), "fix");
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
        private readonly Fix fixes;
        #endregion
        public ValidWord(string word, Fix fixes)
        {
            this.word = word;
            this.fixes = fixes;
        }
        public string Word { get { return word; } }
        public Fix Fixes { get { return fixes; } }
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
    class WordGraph
    {
        #region Fields
        private static readonly Lazy<WordGraph> french = new Lazy<WordGraph>(GetFrench);
        private readonly Dictionary<string, HashSet<string>> valids;
        private readonly Dictionary<string, WordVertex> vertices;
        private readonly Dictionary<char, int> counts;
        private readonly Dictionary<char, int> ranges;
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
                    throw new ArgumentException(string.Format("{0} already found but not valid (are words sorted by size?)", word), "valid");
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
        internal static bool Select(Fix fixes, Fix allowed)
        {
            switch (fixes)
            {
                case Fix.None:
                    return true;
                case Fix.Prefix:
                    return (allowed & Fix.Prefix) == Fix.Prefix;
                case Fix.Suffix:
                    return (allowed & Fix.Suffix) == Fix.Suffix;
                case Fix.All:
                    return allowed == Fix.All;
                default:
                    throw new ArgumentException(string.Format("Unknown {0} fixes", fixes), "fixes");
            }
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
        #endregion
        public WordGraph(IEnumerable<string> words)
        {
            vertices = new Dictionary<string, WordVertex>();
            valids = new Dictionary<string, HashSet<string>>();
            counts = new Dictionary<char, int>();
            ranges = new Dictionary<char, int>();
            random = new Random();
            for (char c = 'A'; c <= 'Z'; c++)
                counts.Add(c, 0);
            foreach (string word in words)
            {
                GetVertex(word, true);
                AddValid(word);
                foreach (char c in word)
                    counts[c]++;
            }
            total = 0;
            for (char c = 'A'; c <= 'Z'; c++)
            {
                ranges.Add(c, total);
                total += counts[c];
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
                    result.Add(new ValidWord(word, vertices[word].Fixes));
                return result.ToConstant();
            }
            return new ConstantSet<ValidWord>();
        }
        public ConstantSet<ValidWord> GetValids(string letters, Fix fixes)
        {
            var sorted = letters.Sort();
            HashSet<string> set;
            if (valids.TryGetValue(sorted, out set))
            {
                var result = new HashSet<ValidWord>();
                foreach (string word in set)
                    if (Select(vertices[word].Fixes, fixes))
                        result.Add(new ValidWord(word, vertices[word].Fixes));
                return result.ToConstant();
            }
            return new ConstantSet<ValidWord>();
        }
        public Fix GetFixes(string word)
        {
            WordVertex vertex;
            if (vertices.TryGetValue(word, out vertex))
                return vertex.Fixes;
            return Fix.None;
        }
        public ConstantSet<char> GetLetters(string part, Fix fix)
        {
            WordVertex vertex;
            if (!vertices.TryGetValue(part, out vertex))
                throw new ArgumentException(string.Format("Unknown word part : {0}", part), "part");
            //if (fix != Fix.Prefix && fix != Fix.Suffix)
            //    throw new ArgumentException(string.Format("Bad fix : {0}", fix), "fix");
            return vertex.GetLetters(fix);
        }
        public bool IsValid(string word)
        {
            if (string.IsNullOrEmpty(word) || word.Length == 1)
                throw new ArgumentException(string.Format("{0} is too short", word), "word");
            WordVertex vertex;
            if (vertices.TryGetValue(word, out vertex))
                return vertex.IsValid;
            return false;
        }
        public static WordGraph French
        {
            get { return french.Value; }
        }
        public string GetRandom()
        {
            var sb = new StringBuilder(Rack.Size);
            for (int i = 0; i < sb.Capacity; i++)
                sb.Append(GetLetter(random.NextDouble()));
            return sb.ToString();
        }
        public void TellStats()
        {
            var sorted = new SortedDictionary<int, char>();
            foreach (char c in counts.Keys)
                sorted.Add(counts[c], c);
            foreach (int count in sorted.Keys)
                Console.WriteLine("{0} : {1}", sorted[count], count);
        }
    }
    class Rack
    {
        public const int Size = 6;
        #region Fields
        private readonly ConstantSet<char> rack;
        #endregion
        public Rack(string rack)
            : this(new ConstantSet<char>(rack))
        {
        }
        public Rack(ConstantSet<char> rack)
        {
            this.rack = rack;
        }
        public ConstantSet<char> Letters { get { return rack; } }
    }
}
