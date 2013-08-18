using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Ded.Wordox
{
    public partial class WordoxForm : Form
    {
        private Board board;
        private void State_DirectionChanged(object sender, DirectionChangedEventArgs args)
        {
            label.Text = BoardState.ToString(args.Direction);
        }
        private void WordoxForm_Load(object sender, EventArgs e)
        {
            board = new Board();
            boardUserControl.State.DirectionChanged += State_DirectionChanged;
        }
        public WordoxForm()
        {
            InitializeComponent();
        }
    }
}
