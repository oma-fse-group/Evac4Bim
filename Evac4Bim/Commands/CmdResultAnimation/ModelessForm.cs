using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Autodesk.Revit.UI;

namespace Revit.SDK.Samples.ModelessForm_ExternalEvent.CS
{
    public partial class ModelessForm : Form
    {
        // In this sample, the dialog owns the handler and the event objects,
        // but it is not a requirement. They may as well be static properties
        // of the application.

        private RequestHandler m_Handler;
        private ExternalEvent m_ExEvent;

        private double m_currentTime;
        private double m_totalTime;
        private double m_timeStep;
        private bool playerState = false;

        /// <summary>
        ///   Dialog instantiation
        /// </summary>
        /// 
        public ModelessForm(ExternalEvent exEvent, RequestHandler handler, int numberOfValues, double timestep)
        {
            InitializeComponent();
            m_Handler = handler;
            m_ExEvent = exEvent;

            // init slider
            this.trackBar1.Maximum = numberOfValues;
            this.m_timeStep = timestep;
            this.m_totalTime = this.m_timeStep * this.trackBar1.Maximum;
            this.m_currentTime = 0;

            // init labels 
            this.maxTimeLabel.Text = this.m_totalTime.ToString();

            // init timer 
            this.timer1.Interval = (int)(timestep * 1000);
            
        }

        /// <summary>
        /// Form closed event handler
        /// </summary>
        /// <param name="e"></param>
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // we own both the event and the handler
            // we should dispose it before we are closed
            m_ExEvent.Dispose();
            m_ExEvent = null;
            m_Handler = null;

            // do not forget to call the base class
            base.OnFormClosed(e);
        }


        /// <summary>
        ///   Control enabler / disabler 
        /// </summary>
        ///
        private void EnableCommands(bool status)
        {
            foreach (Control ctrl in this.Controls)
            {
                ctrl.Enabled = status;
            }
            if (!status)
            {
                this.btnExit.Enabled = true;
            }
        }


        /// <summary>
        ///   A private helper method to make a request
        ///   and put the dialog to sleep at the same time.
        /// </summary>
        /// <remarks>
        ///   It is expected that the process which executes the request 
        ///   (the Idling helper in this particular case) will also
        ///   wake the dialog up after finishing the execution.
        /// </remarks>
        ///
        private void MakeRequest(RequestId request)
        {
            m_Handler.Request.Make(request);
            m_ExEvent.Raise();
            DozeOff();
        }


        /// <summary>
        ///   DozeOff -> disable all controls (but the Exit button)
        /// </summary>
        /// 
        private void DozeOff()
        {
            EnableCommands(false);
        }


        /// <summary>
        ///   WakeUp -> enable all controls
        /// </summary>
        /// 
        public void WakeUp()
        {
            EnableCommands(true);
        }


        /// <summary>
        ///   Exit - closing the dialog
        /// </summary>
        /// 
        
        private void btnExit_Click_1(object sender, EventArgs e)
        {
            MakeRequest(RequestId.Exit);
           // Close();
        }

        

        private void bwdButtom_Click(object sender, EventArgs e)
        {
            moveBkwd();

        }
        private void moveBkwd()
        {
            int val = this.trackBar1.Value;
            if (val > this.trackBar1.Minimum)
            {
                val--;
                this.trackBar1.Value = val;
                MakeRequest(RequestId.SliderScroll);
            }
            
        }

        private void fwdButtom_Click(object sender, EventArgs e)
        {
            movefrwd();
             
        }
        private void movefrwd()
        {
            int val = this.trackBar1.Value;
            if (val < this.trackBar1.Maximum)
            {
                val++;
                this.trackBar1.Value = val;
                MakeRequest(RequestId.SliderScroll);

            }
        }

        private void trackBar1_MouseUp(object sender, MouseEventArgs e)
        {            
            MakeRequest(RequestId.SliderScroll);
        }

        private void trackBar1_ValueChanged(object sender, EventArgs e)
        {
            m_Handler.m_sliderVal = this.trackBar1.Value;

            this.m_currentTime = this.trackBar1.Value * this.m_timeStep;

            this.timerLabel.Text = this.m_currentTime.ToString() + " / "+ m_totalTime.ToString() + " s";
        }

        
        private void switchTimer()
        {
            this.playerState = !playerState;
            timer1.Enabled = playerState;
            // also switch icon 
            if (timer1.Enabled)
            {
                playButton.Text = "||";
            }
            else
            {
                playButton.Text = ">";
            }
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            if (this.trackBar1.Value >= this.trackBar1.Maximum)
            {
                switchTimer();
            }
            else
            {
                this.movefrwd();
            }
            
        }

        private void playButton_Click(object sender, EventArgs e)
        {
            switchTimer();
        }

        private void ModelessForm_Load(object sender, EventArgs e)
        {

        }
    }  // class
}
