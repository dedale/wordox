using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ded.Wordox
{
    class Shell
    {
        #region Fields
        private readonly WordGraph graph;
        private Board board;
        #endregion
        private static Rack ReadRack()
        {
            string letters = null;
            while (string.IsNullOrEmpty(letters) || letters.Length != Rack.Size)
            {
                Console.Write("rack? ");
                letters = Console.ReadLine();
            }
            return new Rack(letters);
        }
        private WordPart ReadMove(Rack rack)
        {
            var regex = new Regex(@"^\s*(?<row>\d),(?<column>\d)\s*(?<direction>(bottom|right))\s*(?<word>[a-z]+)\s*$", RegexOptions.IgnoreCase);
            Console.Write("move? [r,c bottom|right] word] [skip] ");
            while (true)
            {
                try
                {
                    string line = Console.ReadLine().Trim();
                    if (line == "skip")
                        return null;
                    if (line == "guess")
                    {
                        PlayGraph play = new PlayGraph(graph, board, rack);
                        var moveFinder = new MoveFinder(graph, board, play);
                        moveFinder.GetBestMove();
                    }
                    Match m = regex.Match(line);
                    if (m.Success)
                    {
                        string word = m.Groups["word"].Value;
                        int row = int.Parse(m.Groups["row"].Value, CultureInfo.InvariantCulture);
                        int column = int.Parse(m.Groups["column"].Value, CultureInfo.InvariantCulture);
                        Direction direction = (Direction)Enum.Parse(typeof(Direction), m.Groups["direction"].Value);
                        var part = new WordPart(word, new Cell(row, column), direction);
                        return part;
                    }
                    Console.Write("? ");
                }
                catch (FormatException e)
                {
                    Console.WriteLine("{0} : {1}", e.GetType().Name, e.Message);
                    Console.Write("? ");
                }
            }
        }
        public Shell()
        {
            graph = WordGraph.French;
            board = new Board();
        }
        public void Run()
        {
            Console.WriteLine("Loading...");

            board.Write();
            Console.WriteLine();

            Rack rack = ReadRack();
            WordPart move = ReadMove(rack);
            if (move == null)
            {

            }
        }
    }
    class Program
    {
        [STAThread]
        public static int Main(/*params string[] args*/)
        {
            new Shell().Run();            
            return 0;
        }
    }
}
