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

namespace UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            // Сразу вызовем проверку, чтобы индикатор был красным/зеленым при запуске, если текст уже есть
            OperationText_TextChanged(null, null);
        }

        private Good CreateGood()
        {
            (string? item, int? quantity) = Warehouse.ExtractItemAndQuantity(OperationText.Text);

            if (item is null || quantity is null)
            {
                throw new ArgumentException("Введены некорректные данные. Ожидается: \"Название\", Количество.");
            }

            return new Good { name = item, quantity = quantity };
        }

        // Обработчик кнопки "Выполнить"
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            PostConditionIndicator.Fill = Brushes.Red;

            try
            {
                var good = CreateGood();

                var operation = OperationList.SelectedIndex;
                switch (operation)
                {
                    case 0:
                        if (Warehouse.CheckAddValid(good))
                        {
                            Warehouse.AddNewGood(good);
                            MessageBox.Show("Товар добавлен");
                            PostConditionIndicator.Fill = Brushes.Green;
                            OperationText.Text = "";
                        }
                        break;
                    case 1:
                        if (Warehouse.CheckShipValid(good))
                        {
                            Warehouse.ShipGood(good);
                            MessageBox.Show("Товар отгружен");
                            PostConditionIndicator.Fill = Brushes.Green;
                            OperationText.Text = "";
                        }
                        else
                        {
                            MessageBox.Show("Невозможно отгрузить товар: недостаточно товара на складе или товар не найден");
                        }
                        break;
                    case 2:
                        MessageBox.Show("Операция 'Переместить товар' пока не реализована.");
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

            OperationText_TextChanged(null, null);
        }

        // НОВЫЙ МЕТОД: Динамическая проверка Pre-условия

        private void OperationText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (OperationText is null) return;

            bool isValid = false;
            int operationIndex = OperationList.SelectedIndex;

            try
            {
                // 1. Пытаемся извлечь данные
                (string? item, int? quantity) = Warehouse.ExtractItemAndQuantity(OperationText.Text);

                if (item is not null && quantity is not null)
                {
                    // 2. Создаем Good
                    var good = new Good { name = item, quantity = quantity };

                    switch (operationIndex)
                    {
                        case 0: // Добавить новый товар
                            isValid = Warehouse.CheckAddValid(good); // 3. Проверяем Pre-условие
                            break;
                        case 1: // Отгрузить товар
                            isValid = Warehouse.CheckShipValid(good);
                            break;
                        case 2: // Переместить товар
                                // Пока не реализовано
                            isValid = false;
                            break;
                        default:
                            isValid = false;
                            break;
                    }
                }
            }
            catch
            {
                isValid = false;
            }
            // Устанавливаем цвет индикатора
            PreConditionIndicator.Fill = isValid ? Brushes.Green : Brushes.Red;
            // Сброс Post-индикатора при изменении текста (мы не знаем, выполнится ли Post)
            PostConditionIndicator.Fill = Brushes.Red;
        }

        // НОВЫЙ МЕТОД: Открытие окна контракта
        private void ShowContractButton_Click(object sender, RoutedEventArgs e)
        {
            int operationIndex = OperationList.SelectedIndex;

            // 1. Проверка, что что-то выбрано
            if (operationIndex == -1)
            {
                MessageBox.Show("Пожалуйста, выберите операцию из списка.");
                return;
            }

            // 2. ИСПРАВЛЕНИЕ ОШИБКИ: Правильный способ получить Content (название) выбранного элемента.
            // OperationList.Items[index] возвращает ListBoxItem, из которого мы берем Content.
            string title = ((ListBoxItem)OperationList.Items[operationIndex]).Content.ToString();

            // 3. Создаем экземпляр нового окна и передаем НУЖНЫЕ данные: индекс (для логики) и название (для заголовка)
            ContractWindow contractWindow = new ContractWindow(operationIndex, title);

            // Отображаем окно как модальное
            contractWindow.ShowDialog();
        }

        // НОВЫЙ МЕТОД: При смене операции
        private void OperationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Перезапускаем проверку Pre-условия для новой операции (вызывает OperationText_TextChanged)
            OperationText_TextChanged(null, null);
        }
    }
}