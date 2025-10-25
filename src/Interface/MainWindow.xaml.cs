using Domain;
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
    public partial class MainWindow : Window
    {
        private string _lastWp = "";
        private string _lastPost = "";
        private string _lastCode = "";

        private WPCalculator _calculator;
        private ExpressionParser _expressionParser;
        private CodeParser _codeParser;

        public MainWindow()
        {
            InitializeComponent();
            _calculator = new WPCalculator();
            _expressionParser = new ExpressionParser();
            _codeParser = new CodeParser();

            InitializeComponent();
            OperationText_TextChanged(null, null);
            UpdateGoodsList();
        }

        // Получение выбранного склада
        private string GetSelectedWarehouse()
        {
            if (WarehouseList.SelectedItem is ComboBoxItem selected)
                return selected.Content.ToString()!;
            return "Склад 1";
        }

        // Создание товара из текста
        private (string name, int quantity) CreateGood()
        {
            (string? item, int? quantity) = Warehouse.ExtractItemAndQuantity(OperationText.Text);

            if (item is null || quantity is null)
                throw new ArgumentException("Введены некорректные данные. Ожидается: \"Название\", Количество.");

            return (item, quantity.Value);
        }

        // Обновление списка товаров на складе
        private void UpdateGoodsList()
        {
            GoodsList.Items.Clear();

            var selectedWarehouseItem = WarehouseList.SelectedItem as ComboBoxItem;
            string selectedWarehouse = selectedWarehouseItem?.Content.ToString() ?? "Склад 1";

            string goodsText = Warehouse.ShowGoods(selectedWarehouse);

            if (!string.IsNullOrEmpty(goodsText))
            {
                string[] goodsLines = goodsText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in goodsLines)
                    GoodsList.Items.Add(line);
            }

            if (GoodsList.Items.Count == 0)
                GoodsList.Items.Add("Товаров на складе нет");
        }

        // Обработчик смены выбранного склада
        private void WarehouseList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateGoodsList(); // Обновляем список товаров при смене склада
        }

        // Кнопка "Выполнить"
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            PostConditionIndicator.Fill = Brushes.Red;

            try
            {
                var operation = OperationList.SelectedIndex;

                // Получаем выбранный склад для операций добавления/отгрузки
                var selectedWarehouseItem = WarehouseList.SelectedItem as ComboBoxItem;
                string selectedWarehouse = selectedWarehouseItem?.Content.ToString() ?? "Склад 1";

                switch (operation)
                {
                    case 0: // Добавить новый товар
                        {
                            (string? item, int? quantity) = Warehouse.ExtractItemAndQuantity(OperationText.Text);
                            if (item is null || quantity is null)
                                throw new ArgumentException("Введены некорректные данные. Ожидается: \"Название\", Количество.");

                            if (Warehouse.CheckAddValid(selectedWarehouse, item, quantity.Value))
                            {
                                Warehouse.AddNewGood(selectedWarehouse, item, quantity.Value);
                                PostConditionIndicator.Fill = Brushes.Green;
                                MessageBox.Show("Товар добавлен");
                                OperationText.Text = "";
                                UpdateGoodsList();
                            }
                            break;
                        }
                    case 1: // Отгрузить товар
                        {
                            (string? item, int? quantity) = Warehouse.ExtractItemAndQuantity(OperationText.Text);
                            if (item is null || quantity is null)
                                throw new ArgumentException("Введены некорректные данные. Ожидается: \"Название\", Количество.");

                            if (Warehouse.CheckShipValid(selectedWarehouse, item, quantity.Value))
                            {
                                Warehouse.ShipGood(selectedWarehouse, item, quantity.Value);
                                PostConditionIndicator.Fill = Brushes.Green;
                                MessageBox.Show("Товар отгружен");
                                OperationText.Text = "";
                                UpdateGoodsList();
                            }
                            else
                            {
                                MessageBox.Show("Невозможно отгрузить товар: недостаточно товара на складе или товар не найден");
                            }
                            break;
                        }
                    case 2: // Перемещение товара
                            // Разбираем текст операции, ожидаем формат:
                            // "название товара", количество, "склад-источник", "склад-получатель"
                        var parts = OperationText.Text.Split(',', StringSplitOptions.TrimEntries);
                        if (parts.Length == 4)
                        {
                            string name = parts[0].Trim('"');
                            int quantity = int.Parse(parts[1]);
                            string fromWarehouse = parts[2].Trim('"');
                            string toWarehouse = parts[3].Trim('"');

                            // Добавляем склад-получатель, если его нет
                            EnsureWarehouseExists(toWarehouse);

                            if (Warehouse.CheckMoveValid(fromWarehouse, toWarehouse, name, quantity))
                            {
                                Warehouse.MoveGood(fromWarehouse, toWarehouse, name, quantity);
                                PostConditionIndicator.Fill = Brushes.Green;
                                MessageBox.Show($"Товар {name} перемещен со склада {fromWarehouse} на склад {toWarehouse}");
                                OperationText.Text = "";
                                UpdateGoodsList(); // обновление списка товаров для выбранного склада
                            }
                            else
                            {
                                MessageBox.Show("Невозможно переместить товар: недостаточно товара или склад не найден");
                            }
                        }
                        else
                        {
                            MessageBox.Show("Неверный формат операции. Ожидается: \"Название товара\", количество, \"Склад-источник\", \"Склад-получатель\"");
                        }
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

        private void EnsureWarehouseExists(string warehouseName)
        {
            if (string.IsNullOrWhiteSpace(warehouseName)) return;

            if (!WarehouseList.Items.Cast<ComboBoxItem>().Any(i => i.Content.ToString() == warehouseName))
            {
                ComboBoxItem newItem = new ComboBoxItem { Content = warehouseName, FontWeight = FontWeights.Bold };
                WarehouseList.Items.Add(newItem);
                Warehouse.AddWarehouse(warehouseName);

                // Выбираем новый склад автоматически
                WarehouseList.SelectedItem = newItem;
            }
        }

        // Проверка Pre-условия
        private void OperationText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (OperationText is null) return;

            bool isValid = false;
            int operationIndex = OperationList.SelectedIndex;

            try
            {
                // Получаем выбранный склад для добавления/отгрузки
                var selectedWarehouseItem = WarehouseList.SelectedItem as ComboBoxItem;
                string selectedWarehouse = selectedWarehouseItem?.Content.ToString() ?? "Склад 1";

                switch (operationIndex)
                {
                    case 0: // Добавление
                    case 1: // Отгрузка
                        {
                            (string? item, int? quantity) = Warehouse.ExtractItemAndQuantity(OperationText.Text);
                            if (item != null && quantity != null)
                            {
                                if (operationIndex == 0)
                                    isValid = Warehouse.CheckAddValid(selectedWarehouse, item, quantity.Value);
                                else
                                    isValid = Warehouse.CheckShipValid(selectedWarehouse, item, quantity.Value);
                            }
                            break;
                        }
                    case 2: // Перемещение
                        {
                            var parts = OperationText.Text.Split(',');
                            if (parts.Length >= 4)
                            {
                                string name = parts[0].Trim().Trim('"');
                                bool quantityParsed = int.TryParse(parts[1].Trim(), out int quantity);
                                string fromWarehouse = parts[2].Trim().Trim('"');
                                string toWarehouse = parts[3].Trim().Trim('"');

                                if (quantityParsed && !string.IsNullOrWhiteSpace(name) &&
                                    !string.IsNullOrWhiteSpace(fromWarehouse) && !string.IsNullOrWhiteSpace(toWarehouse))
                                {
                                    isValid = Warehouse.CheckMoveValid(fromWarehouse, toWarehouse, name, quantity);
                                }
                            }
                            break;
                        }
                    default:
                        isValid = false;
                        break;
                }
            }
            catch
            {
                isValid = false;
            }

            // Устанавливаем цвет индикатора
            PreConditionIndicator.Fill = isValid ? Brushes.Green : Brushes.Red;
            // Сброс Post-индикатора при изменении текста
            PostConditionIndicator.Fill = Brushes.Red;
        }

        private void ShowContractButton_Click(object sender, RoutedEventArgs e)
        {
            int operationIndex = OperationList.SelectedIndex;

            if (operationIndex == -1)
            {
                MessageBox.Show("Пожалуйста, выберите операцию из списка.");
                return;
            }

            string title = ((ListBoxItem)OperationList.Items[operationIndex]).Content.ToString();
            ContractWindow contractWindow = new ContractWindow(operationIndex, title);
            contractWindow.ShowDialog();
        }

        private void OperationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OperationText_TextChanged(null, null);
        }


        // ======= КОНСТРУКТОР ФРАГМЕНТА =======
        private void CalculateBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string code = CodeEditorTextBox.Text;
                string postCondition = PostConditionTextBox.Text;

                // Парсим код и постусловие
                var program = ParseCode(code);
                var postExpression = ParseExpression(postCondition);

                if (program == null || postExpression == null)
                {
                    MessageBox.Show("Ошибка парсинга кода или выражения", "Ошибка");
                    return;
                }

                // Вычисляем WP
                var wp = _calculator.CalculateWP(program, postExpression);

                // Обновляем UI с результатами
                UpdateResultsUI(wp);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка расчета: {ex.Message}", "Ошибка");
            }
        }

        private void ShowHoareTriadBtn_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                string code = CodeEditorTextBox.Text;
                string postCondition = PostConditionTextBox.Text;

                var program = ParseCode(code);
                var postExpression = ParseExpression(postCondition);
                var preExpression = ParseExpression(WpResultTextBox.Text);

                if (program == null || postExpression == null || preExpression == null)
                    return;

                string triad = _calculator.GetHoareTriad(preExpression, program, postExpression);
                MessageBox.Show(triad, "Триада Хоара");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка");
            }
        }

        private void AddAssignmentBtn_Click(object sender, RoutedEventArgs e)
        {
            string assignment = "x := выражение;";
            CodeEditorTextBox.Text += Environment.NewLine + assignment;
        }

        private void AddIfBtn_Click(object sender, RoutedEventArgs e)
        {
            string ifStatement =
                "if (условие) {" + Environment.NewLine +
                "    // операторы" + Environment.NewLine +
                "} else {" + Environment.NewLine +
                "    // операторы" + Environment.NewLine +
                "}";
            CodeEditorTextBox.Text += Environment.NewLine + ifStatement;
        }

        private void ClearCodeBtn_Click(object sender, RoutedEventArgs e)
        {
            CodeEditorTextBox.Text = "";
        }

        private void PresetsComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PresetsComboBox.SelectedIndex <= 0) return;

            string selectedPreset = ((ComboBoxItem)PresetsComboBox.SelectedItem).Content.ToString();

            switch (selectedPreset)
            {
                case "Max из двух":
                    LoadMaxExample();
                    break;
                case "Квадратное уравнение":
                    LoadQuadraticExample();
                    break;
                case "Последовательность присваиваний":
                    LoadSequenceExample();
                    break;
            }

            // Сбрасываем выбор
            PresetsComboBox.SelectedIndex = 0;
        }

        private void LoadMaxExample()
        {
            CodeEditorTextBox.Text = "if (x1 >= x2) max := x1; else max := x2;";
            PostConditionTextBox.Text = "max > 100";
            PostConditionHumanTextBox.Text = "max больше 100";
        }

        private void LoadQuadraticExample()
        {
            CodeEditorTextBox.Text =
                "D := b*b - 4*a*c;\n" +
                "if (D >= 0)\n" +
                "    x1 := (-b + D) / (2*a);\n" +  // Упростил убрав sqrt
                "else\n" +
                "    x1 := -999;";

            PostConditionTextBox.Text = "x1 != -999";
            PostConditionHumanTextBox.Text = "корень вычислен";
        }

        private void LoadSequenceExample()
        {
            CodeEditorTextBox.Text =
                "x := x + 10;" + Environment.NewLine +
                "y := x + 1;";
            PostConditionTextBox.Text = "y == x - 9 and x > 15";
            PostConditionHumanTextBox.Text = "y равно x-9 и x больше 15";
        }

        private void UpdateResultsUI(Expression wp)
        {
            // Очищаем предыдущие результаты
            StepsListBox.Items.Clear();

            // Добавляем шаги расчета
            foreach (var step in _calculator.CalculationSteps)
            {
                StepsListBox.Items.Add(step);
            }

            // Показываем итоговый WP
            WpResultTextBox.Text = wp.ToHumanReadable();
            WpHumanResultTextBox.Text = wp.ToHumanReadable(); // Можно сделать более человеческое описание
        }

        // Заглушки для парсеров - их нужно будет реализовать
        private Statement ParseCode(string code)
        {
            try
            {
                var result = _codeParser.Parse(code);
                if (result == null)
                {
                    MessageBox.Show("Не удалось распарсить код", "Ошибка");
                }
                return result;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка парсинга кода: {ex.Message}\n\nУпростите код или используйте пресеты", "Ошибка парсинга");
                return null;
            }
        }

        private Expression ParseExpression(string expression)
        {
            try
            {
                return _expressionParser.Parse(expression);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка парсинга выражения '{expression}': {ex.Message}", "Ошибка парсинга");
                return null;
            }
        }
    }
}