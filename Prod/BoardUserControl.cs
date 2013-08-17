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
    public partial class BoardUserControl : UserControl
    {
        private BoardState state;
        private void FixStyles()
        {
            foreach (ColumnStyle style in tableLayoutPanel.ColumnStyles)
            {
                style.SizeType = SizeType.Percent;
                style.Width = 1.0f / tableLayoutPanel.ColumnCount;
            }
            foreach (RowStyle style in tableLayoutPanel.RowStyles)
            {
                style.SizeType = SizeType.Percent;
                style.Height = 1.0f / tableLayoutPanel.RowCount;
            }
        }
        private void BoardUserControl_Load(object sender, EventArgs e)
        {
            FixStyles();
            state = new BoardState();
            for (int row = 0; row < tableLayoutPanel.RowCount; row++)
            {
                for (int column = 0; column < tableLayoutPanel.ColumnCount; column++)
                {
                    var cell = new CellUserControl();
                    cell.Tag = state;
                    tableLayoutPanel.Controls.Add(cell, row, column);
                    if (row == 0 && column == 0)
                        tableLayoutPanel.Width = tableLayoutPanel.Height = tableLayoutPanel.RowCount * (cell.Width + Margin.All * 2);
                }
            }
        }
        public BoardUserControl()
        {
            InitializeComponent();
        }
        internal BoardState State { get { return state; } }
    }
}
