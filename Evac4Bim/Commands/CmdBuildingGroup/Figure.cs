using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CmdBuildingGroup
{
    public partial class Figure : Form
    {
        public Figure(string[] items, int SelectedIndex, string label, string title, bool hasSpk, bool EmergencyCommunication)
        {
            InitializeComponent();

            // set content of combo box
            //string[] items = new string[] { "Ram", "Shyam" };
            this.comboBox1.DataSource = items;
            this.comboBox1.SelectedIndex = SelectedIndex;
            this.label1.Text = label;
            this.Text = title;
            this.checkBox1.Checked = hasSpk;
            this.checkBox2.Checked = EmergencyCommunication;


        }
        public int selectedFunctionIndex { get; set; }
        public bool SprinklerProtection { get; set; }

        public bool EmergencyCommunication { get; set; }

        private void Figure_Load(object sender, EventArgs e)
        {

        }

        private void button1_Click(object sender, EventArgs e)
        {
            this.selectedFunctionIndex = this.comboBox1.SelectedIndex;
            this.SprinklerProtection = this.checkBox1.Checked;
            this.EmergencyCommunication = this.checkBox2.Checked;
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
