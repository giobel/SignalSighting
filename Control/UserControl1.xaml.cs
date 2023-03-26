using System;
using System.Windows;
using System.Windows.Controls;

namespace NavisCustomRibbon
{
    /// <summary>
    /// Interaction logic for UserControl1.xaml
    /// </summary>
    public partial class UserControl1 : UserControl
    {
        private Model ViewPointsModel { get; set; }

        public UserControl1()
        {
            InitializeComponent();
            ViewPointsModel = new Model();
        }

        private void BtnGetSignal_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                this.labelSignal.Text = ViewPointsModel.GetSignal();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void BtnGetAlignment_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                this.labelTrack.Content = ViewPointsModel.GetTrack();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ButtonRun_Click(object sender, RoutedEventArgs e)
        {
            double speed = double.Parse(tboxSpeed.Text);
            double interval = double.Parse(tbox_Interval.Text);
            double driverHeight = double.Parse(tbox_DriverH.Text);

            ViewPointsModel.Run(speed, interval, driverHeight, cboxUpDirection.IsChecked.Value);
        }

        private void CheckBox_RhinoOutput(object sender, RoutedEventArgs e)
        {

        }
    }
}
