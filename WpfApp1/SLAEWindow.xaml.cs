using System.Windows;

namespace WpfApp1
{
    public partial class SLAEWindow : Window
    {
        public SLAEWindow()
        {
            InitializeComponent();
        }

        private void MenuItem_Exit_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}