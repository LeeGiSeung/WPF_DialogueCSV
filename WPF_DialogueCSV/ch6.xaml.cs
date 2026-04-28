using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WPF_DialogueCSV
{
    /// <summary>
    /// ch6.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class ch6 : Window
    {
        public ch6()
        {
            InitializeComponent();
        }

        private void btn_1_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("NO Dialogue Box Name Key");
        }

        private void aaaaa_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("AAAAA Test");
        }

        private void Button_KeyDown(object sender, KeyEventArgs e)
        {
            
        }
    }
}
