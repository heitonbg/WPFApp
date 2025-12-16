using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Closing += MainWindow_Closing;
        }

        private void MainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void Button_ClickDichotomy(object sender, RoutedEventArgs e)
        {
            BisectionMethodWindow objBisectionMethod = new BisectionMethodWindow();
            objBisectionMethod.Closed += Window_Closed;
            this.Hide();
            objBisectionMethod.Show();
        }

        private void Button_ClickLeast(object sender, RoutedEventArgs e)
        {
            LeastSquaresWindow objLeastSquares = new LeastSquaresWindow();
            objLeastSquares.Closed += Window_Closed;
            this.Hide();
            objLeastSquares.Show();
        }

        private void Button_ClickGolden(object sender, RoutedEventArgs e)
        {
            GoldenRatioWindow objGoldenRatio = new GoldenRatioWindow();
            objGoldenRatio.Closed += Window_Closed;
            this.Hide();
            objGoldenRatio.Show();
        }

        private void Button_ClickSLAE(object sender, RoutedEventArgs e)
        {
            SLAEWindow objSLAE = new SLAEWindow();
            objSLAE.Closed += Window_Closed;
            this.Hide();
            objSLAE.Show();
        }

        private void Button_ClickSorting(object sender, RoutedEventArgs e)
        {
            SortingWindow objSorting = new SortingWindow();
            objSorting.Closed += Window_Closed;
            this.Hide();
            objSorting.Show();
        }

        private void Button_ClickNewton(object sender, RoutedEventArgs e)
        {
            NewtonMethodWindow objSLAE = new NewtonMethodWindow();
            objSLAE.Closed += Window_Closed;
            this.Hide();
            objSLAE.Show();
        }


        private void Button_ClickIntegral(object sender, RoutedEventArgs e)
        {
            IntegralMethodsWindow objIntegralMethods = new IntegralMethodsWindow();
            objIntegralMethods.Closed += Window_Closed;
            this.Hide();
            objIntegralMethods.Show();
        }

        private void Button_ClickCoordinate(object sender, RoutedEventArgs e)
        {
            CoordinateDescentWindow objCoordinateDescentWindow = new CoordinateDescentWindow();
            objCoordinateDescentWindow.Closed += Window_Closed;
            this.Hide();
            objCoordinateDescentWindow.Show();
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            this.Show(); 
        }
    }
}