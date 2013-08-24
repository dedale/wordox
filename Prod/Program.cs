using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Ded.Wordox
{
    class Program
    {
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
        private static WordPart ReadMove()
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
                    Match m = regex.Match(line);
                    if (m.Success)
                    {
                        string word = m.Groups["word"].Value;
                        int row = int.Parse(m.Groups["row"].Value);
                        int column = int.Parse(m.Groups["column"].Value);
                        Direction direction = (Direction)Enum.Parse(typeof(Direction), m.Groups["direction"].Value);
                        var part = new WordPart(word, new Cell(row, column), direction);
                        return part;
                    }
                    Console.Write("? ");
                }
                catch (Exception e)
                {
                    Console.WriteLine("{0} : {1}", e.GetType().Name, e.Message);
                    Console.Write("? ");
                }
            }
        }
        public static int Main(params string[] args)
        {
            Console.WriteLine("Loading...");

            var graph = WordGraph.French;
            var board = new Board();
            
            board.Write();
            Console.WriteLine();

            Rack rack = ReadRack();
            WordPart move = ReadMove();
            if (move == null)
            {

            }
            
            return 0;
        }
    }
}
