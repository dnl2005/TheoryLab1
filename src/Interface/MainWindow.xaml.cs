using ClassLibrary;
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

namespace Interface
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private Good CreateGood()
        {
            (string? item, int? quantity) = Warehouse.ExtractItemAndQuantity(OperationText.Text);

            if (item is null || quantity is null)
            {
                throw new ArgumentException("Введены некорректные данные");
            }

            return new Good { name = item, quantity = quantity };
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var good = CreateGood();

                var operation = OperationList.SelectedIndex;
                switch (operation)
                {
                    case 0:
                        Warehouse.AddNewGood(good);
                        MessageBox.Show("Товар добавлен");
                        break;
                    case 1:
                        break;
                    case 3:
                        break;
                    default:
                        MessageBox.Show("Выберите операцию");
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }
    }
}