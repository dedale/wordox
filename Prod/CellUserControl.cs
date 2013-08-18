using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ded.Wordox
{
    public partial class CellUserControl : UserControl
    {
        #region Handlers
        private void CellUserControl_Load(object sender, EventArgs e)
        {
            richTextBox.Width = richTextBox.Height;
            richTextBox.Text = " ";
            richTextBox.SelectionAlignment = HorizontalAlignment.Center;
        }
        private void richTextBox_SelectionChanged(object sender, EventArgs e)
        {
            richTextBox.SelectAll();
        }
        private void richTextBox_Enter(object sender, EventArgs e)
        {
            richTextBox.SelectAll();
        }
        private void richTextBox_MouseDown(object sender, MouseEventArgs e)
        {
            // Flip direction if same
        }
        private void richTextBox_Leave(object sender, EventArgs e)
        {
            // Follow direction
        }
        #endregion
        public CellUserControl()
        {
            InitializeComponent();
        }
    }
    public enum Direction
    {
        Right,
        Bottom
    }
    class DirectionChangedEventArgs : EventArgs
    {
        #region Fields
        private readonly Direction direction;
        #endregion
        public DirectionChangedEventArgs(Direction direction)
        {
            this.direction = direction;
        }
        public Direction Direction { get { return direction; } }
    }
    delegate void DirectionChangedEventHandler(object sender, DirectionChangedEventArgs args);
    class BoardState
    {
        #region Fields
        private Direction direction;
        #endregion
        public BoardState(Direction direction = Direction.Right)
        {
            this.direction = direction;
        }
        public event DirectionChangedEventHandler DirectionChanged;
        public Direction Direction
        {
            get { return direction; }
            set
            {
                if (direction != value)
                {
                    direction = value;
                    if (DirectionChanged != null)
                        DirectionChanged(this, new DirectionChangedEventArgs(direction));
                }
            }
        }
        public int Row { get; set; }
        public int Column { get; set; }
        public static string ToString(Direction direction)
        {
            switch (direction)
            {
                case Direction.Right:
                    return "è";
                case Direction.Bottom:
                    return "ê";
                default:
                    throw new ArgumentException(string.Format("Unknown direction : {0}", direction));
            }
        }
        public string DirectionLabel
        {
            get { return ToString(direction); }
        }
    }
}
