using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Ded.Wordox
{
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
                cell = part.Direction == Direction.Right ? cell.Right : cell.Bottom;
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
            return string.Format(CultureInfo.InvariantCulture, "{0} ({1})", points, stars);
        }
        public bool Wins { get { return points >= WinPoints; } }
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
        public Score Play(WordPart part)
        {
            return new Score(other, PlayerScore.Play(part));
        }
        public Score Play(PlayPath path)
        {
            int points = path.Played.Count;
            int stars = 0;
            bool vortex = false;
            foreach (LetterPlay lp in path.Played)
                if (lp.Cell.IsStar)
                    stars++;
                else if (lp.Cell.IsVortex)
                    vortex = true;
            int taken = path.Main.Word.Length - path.Played.Count;
            foreach (WordPart extra in path.Extras)
                taken += extra.Word.Length - 1;
            var newOther = new PlayerScore(current.Points + points + taken + (vortex ? current.Stars + stars : 0), vortex ? 0 : current.Stars + stars);
            var newCurrent = new PlayerScore(other.Points - taken, vortex ? 0 : other.Stars);
            return new Score(newCurrent, newOther);
        }
        public override string ToString()
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} / {1}", current, other);
        }
    }
}
