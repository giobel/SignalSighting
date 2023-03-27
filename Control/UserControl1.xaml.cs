using Rhino.Geometry;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Forms.Integration;
using System.Windows.Input;

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
            CleanRunLabel();
        }

        private void BtnGetAlignment_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            try
            {
                this.labelTrack.Text = ViewPointsModel.GetTrack();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void ButtonRun_Click(object sender, RoutedEventArgs e)
        {
            try
            {
            double speed = double.Parse(tboxSpeed.Text);
            double interval = double.Parse(tbox_Interval.Text);
            double driverHeight = double.Parse(tbox_DriverH.Text);

            labelGenerateVpoints.Text = ViewPointsModel.Run(speed, interval, driverHeight, cboxUpDirection.IsChecked.Value, cboxRhinoOutput.IsChecked.Value);
            }
            catch{
                labelGenerateVpoints.Text = "Something went wrong";
            }
        }


        private void CleanRunLabel()
        {
            labelGenerateVpoints.Text = "";
        }

        private void cboxUpDirectionClick(object sender, RoutedEventArgs e)
        {
            CleanRunLabel();
        }

        private void cboxRhinoOutputClick(object sender, RoutedEventArgs e)
        {
            CleanRunLabel();
        }


        private void tboxSpeedChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (tboxSpeed != null && tbox_Interval != null && tbox_TotalLength != null)
                {
                    double distance = double.Parse(tboxSpeed.Text) * 1000 * double.Parse(tbox_Interval.Text) / 3600;
                    tbox_TotalLength.Text = $"Total distance: {String.Format("{0:0.##}", distance)}m";
                }
            }
            catch { }
        }

        private void tbox_Interval_TextChanged(object sender, TextChangedEventArgs e)
        {
            try
            {
                if (tboxSpeed != null && tbox_Interval != null && tbox_TotalLength != null)
                {
                    double distance = double.Parse(tboxSpeed.Text) * 1000 * double.Parse(tbox_Interval.Text) / 3600;
                    tbox_TotalLength.Text = $"Total distance: {String.Format("{0:0.##}", distance)}m";
                }
            }
            catch { }
        }
    }

}
