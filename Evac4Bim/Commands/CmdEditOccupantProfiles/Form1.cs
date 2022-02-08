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

namespace CmdEditOccupantProfiles
{
    public partial class Form1 : Form
    {
        public Form1(List<OccupantProfile> pfl)
        {
            InitializeComponent();
            // set content of combo box
            this.profilesList = pfl;
            List<string>items = new List<string>();
            foreach (OccupantProfile p in this.profilesList)
            {
                items.Add(p.profileId);

            }
                        
            this.comboBox1.DataSource = items;
            //this.comboBox1.SelectedIndex = 0;

        }

        public List<OccupantProfile> profilesList { get; set; }

        private void Form1_Load(object sender, EventArgs e)
        {

        }

        private void label1_Click(object sender, EventArgs e)
        {

        }

        private void label2_Click(object sender, EventArgs e)
        {

        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            int idx = this.comboBox1.SelectedIndex;
            OccupantProfile p = this.profilesList.ElementAt(idx);

            this.textBoxName.Text = p.name;
            this.textBoxSpeed.Text = p.speed;
            this.textBoxSpeedProfile.Text = p.speedProfile;
            this.textBoxDiameter.Text = p.diameter;
            this.textBoxIsMobilityImpaired.Text = p.isMobilityImpaired;


        }

        private void button1_Click(object sender, EventArgs e)
        {
            int idx = this.comboBox1.SelectedIndex;
            OccupantProfile p = this.profilesList.ElementAt(idx);
            
            p.name = this.textBoxName.Text;
            p.speed = this.textBoxSpeed.Text;
            p.speedProfile = this.textBoxSpeedProfile.Text;
            p.diameter = this.textBoxDiameter.Text;
            p.isMobilityImpaired = this.textBoxIsMobilityImpaired.Text;

            //this.profilesList.Insert

        }

        private void buttonClose_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void buttonReset_Click(object sender, EventArgs e)
        {
            int idx = this.comboBox1.SelectedIndex;
            OccupantProfile p = this.profilesList.ElementAt(idx);

            this.textBoxName.Text = "";
            this.textBoxSpeed.Text = "";
            this.textBoxSpeedProfile.Text = "";
            this.textBoxDiameter.Text = "";
            this.textBoxIsMobilityImpaired.Text = "";

        }
    }
}
