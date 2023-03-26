using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using Rhino;

namespace NavisCustomRibbon.Control
{
    /// <summary>
    /// Interaction logic for UserControl2.xaml
    /// </summary>
    public partial class UserControl2 : UserControl
    {
        Model m { get; set; }

        public UserControl2()
        {
            

            InitializeComponent();
        }



        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                m = new Model();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message); }

            try { 
            MessageBox.Show(m.GetSignal());
            }
            catch { }
            //MessageBox.Show($"{tboxSpeed.Text},{tbox_Interval.Text},{tbox_DriverH.Text}");
        }
    }
}
