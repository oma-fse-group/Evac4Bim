using Evac4Bim;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CmdMakePaths
{
    public partial class Form1 : Form
    {
        public Form1(List<string> items,string levelName)
        {
            InitializeComponent();
            m_items = items;

            
            this.comboBox1.DataSource = m_items;
            this.labelLevelName.Text = levelName;

        }

        public int selectedExitIndex { get; set; }
        public List<string> m_items { get; set; }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void buttonCancel_Click(object sender, EventArgs e)
        {
            this.selectedExitIndex = -1;
            this.Close();
        }

        private void buttonOK_Click(object sender, EventArgs e)
        {
            this.selectedExitIndex = comboBox1.SelectedIndex;
            this.Close();
        }
    }
}
