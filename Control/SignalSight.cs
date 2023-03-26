using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace NavisCustomRibbon.Control
{
    public partial class SignalSight : UserControl
    {
        public SignalSight()
        {
            InitializeComponent();
        }

        protected override void OnParentChanged(EventArgs e)
        {
            base.OnParentChanged(e);
            Dock = DockStyle.Fill;
        }

        private void btnRun_Click(object sender, EventArgs e)
        {
            MessageBox.Show("I am runnign");
        }
    }
}
