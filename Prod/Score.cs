﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ded.Wordox
{
    enum Player
    {
        None,
        First,
        Second
    }
    class PlayerScore : IEquatable<PlayerScore>
    {
        public const int WinPoints = 25;
        #region Fields
        private readonly int points;
        private readonly int stars;
        #endregion
        public PlayerScore(int points = 0, int stars = 0)
        {
            this.points = points;
            this.stars = stars;
        }
        public int Points { get { return points; } }
        public int Stars { get { return stars; } }
        public static PlayerScore Play(WordPart part)
        {
            int stars = 0;
            Cell cell = part.First;
            for (int i = 0; i < part.Word.Length; i++)
            {
                if (cell.IsStar)
                    stars++;
                if (i == part.Word.Length - 1)
                    break;
                cell = part.Direction == Direction.Right ? cell.Right : cell.Down;
            }
            return new PlayerScore(part.Word.Length, stars);
        }
        public bool Equals(PlayerScore other)
        {
            if (ReferenceEquals(other, null))
                return false;
            return points == other.points && stars == other.stars;
        }
        public override int GetHashCode()
        {
            return points.GetHashCode() ^ (541 * stars.GetHashCode());
        }
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(this, obj))
                return true;
            return Equals(obj as PlayerScore);
        }
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} {1}", points, new string('*', stars));
        }
        public bool Wins { get { return points >= WinPoints; } }
        public static ConsoleColor GetColor(Player player)
        {
            switch (player)
            {
                case Player.First:
                    return ConsoleColor.Blue;
                case Player.Second:
                    return ConsoleColor.Green;
                default:
                    return Console.ForegroundColor;
            }
        }
        public static IDisposable GetCurrentColor(Player current)
        {
            return new DisposableColor(PlayerScore.GetColor(current));
        }
        public static IDisposable GetOtherColor(Player current)
        {
            return GetCurrentColor(current == Player.First ? Player.Second : Player.First);
        }
    }
    class Score
    {
        #region Fields
        private readonly PlayerScore current;
        private readonly PlayerScore other;
        #endregion
        public Score()
            : this(new PlayerScore(), new PlayerScore())
        {
        }
        public Score(PlayerScore current, PlayerScore other)
        {
            this.current = current;
            this.other = other;
        }
        public PlayerScore Current { get { return current; } }
        public PlayerScore Other { get { return other; } }
        public void Write(Player currentPlayer)
        {
            using (PlayerScore.GetCurrentColor(currentPlayer))
                Console.Write(current);
            Console.Write(" / ");
            using (PlayerScore.GetOtherColor(currentPlayer))
                Console.Write(other);
        }
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} / {1}", current, other);
        }
        public Score Skip()
        {
            return new Score(other, current);
        }
    }
}
