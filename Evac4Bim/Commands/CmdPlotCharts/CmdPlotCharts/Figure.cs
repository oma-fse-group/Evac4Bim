using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CmdPlotCharts
{
    public partial class Figure : Form
    {
        public Figure()
        {
            InitializeComponent();
        }

        private void formsPlot1_Load(object sender, EventArgs e)
        {

        }

        private void Figure_Load(object sender, EventArgs e)
        {

        }
        /// <summary>
        /// Customize the scatter plot object
        /// </summary>
        /// <param name="dataX">X axis data</param>
        /// <param name="dataY">Y axis data</param>
        /// <param name="label">Legend text</param>
        /// <param name="title">Title of the plot</param>
        /// <param name="XLabel">X axis label</param>
        /// <param name="YLabel">Y axis label</param>
        public void initPlot (double[] dataX, double[] dataY, string label, string title, string XLabel, string YLabel)
        {

            ScottPlot.FormsPlot plt = this.plt;

            plt.Plot.AddScatter(dataX, dataY, label: label, color: System.Drawing.Color.Green, lineWidth: 2);

            plt.Plot.SetAxisLimitsX(0, dataX.Max() + 1);
            plt.Plot.SetAxisLimitsY(0, dataY.Max() + 1);
            plt.Plot.Title(title, true);
            plt.Plot.XAxis2.LabelStyle(fontSize: 32);
            plt.Plot.SetInnerViewLimits(0, dataX.Max() + 1, 0, dataY.Max() + 1);
            plt.Plot.SetOuterViewLimits(0, dataX.Max() + 1, 0, dataY.Max() + 1);
            var legend = plt.Plot.Legend(true, location: ScottPlot.Alignment.UpperRight);
            legend.FontSize = 24;


            plt.Plot.XAxis.Label(XLabel);
            plt.Plot.XAxis.MinimumTickSpacing(1);
            plt.Plot.XAxis.TickDensity(0.2);
            plt.Plot.XAxis.TickLabelStyle(fontSize: 20);
            plt.Plot.XAxis.LabelStyle(fontSize: 28);


            plt.Plot.YAxis.Label(YLabel);
            plt.Plot.YAxis.MinimumTickSpacing(1);
            plt.Plot.YAxis.TickDensity(0.2);
            plt.Plot.YAxis.TickLabelStyle(fontSize: 20);
            plt.Plot.YAxis.LabelStyle(fontSize: 28);

            plt.Refresh();

        }
    }
}
