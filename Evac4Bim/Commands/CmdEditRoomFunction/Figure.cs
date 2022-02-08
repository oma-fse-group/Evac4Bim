using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CmdEditRoomFunction
{
    public partial class Figure : Form
    {
        public Figure(string[] items , int SelectedIndex, string label, string title)
        {
            InitializeComponent();

            // set content of combo box
            //string[] items = new string[] { "Ram", "Shyam" };
            this.comboBox1.DataSource = items;
            this.comboBox1.SelectedIndex = SelectedIndex;
            this.label1.Text = label;
            this.Text = title;

        }
        public int selectedFunctionIndex { get; set; }
         

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void Figure_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.selectedFunctionIndex = this.comboBox1.SelectedIndex;
            this.DialogResult = DialogResult.OK;
            this.Close();
            
        }

        private void button2_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
}
