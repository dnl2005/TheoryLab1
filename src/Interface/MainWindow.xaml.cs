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
using System.Text.RegularExpressions;

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

        private static readonly Regex LettersOnly = new(@"^[\p{L}\s]+$", RegexOptions.Compiled);

        public MainWindow()
        {
            InitializeComponent();

            _calculator = new WPCalculator();
            _expressionParser = new ExpressionParser();
            _codeParser = new CodeParser();

            Loaded += (_, __) =>
            {
                if (OperationList.SelectedIndex < 0) OperationList.SelectedIndex = 0;
                OperationList_SelectionChanged(null, null);
                UpdateGoodsList();
                UpdatePreconditionIndicator();
            };
        }

        // ===================== ХЕЛПЕРЫ =====================

        // Текущий склад из левого комбобокса (для операций 1–2 и для отображения)
        private string GetSelectedWarehouse()
        {
            if (WarehouseList?.SelectedItem is ComboBoxItem selected &&
                selected.Content is string s &&
                !string.IsNullOrWhiteSpace(s))
                return s;
            return "Склад 1";
        }

        // Разобрать текст ShowGoods в пары (name, qty); домен уже не хранит нули, но на всякий случай фильтруем
        private List<(string name, int qty)> GetWarehouseItems(string warehouse)
        {
            var result = new List<(string name, int qty)>();
            var text = Warehouse.ShowGoods(warehouse);
            if (string.IsNullOrWhiteSpace(text) || text.Contains("Товаров на складе нет"))
                return result;

            var lines = text.Split('\n', StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                // Ожидаемый формат: "Товар: NAME, количество: N"
                var parts = line.Split(',', StringSplitOptions.TrimEntries);
                if (parts.Length >= 2)
                {
                    var p0 = parts[0]; // "Товар: NAME"
                    var p1 = parts[1]; // "количество: N"
                    var name = p0;
                    var qty = 0;

                    var idxName = p0.IndexOf(':');
                    if (idxName >= 0)
                        name = p0[(idxName + 1)..].Trim();

                    var idxQty = p1.IndexOf(':');
                    if (idxQty >= 0)
                    {
                        var raw = p1[(idxQty + 1)..].Trim();
                        int.TryParse(raw, out qty);
                    }

                    if (!string.IsNullOrWhiteSpace(name) && qty > 0)
                        result.Add((name, qty));
                }
            }
            return result;
        }

        // ===================== ОТОБРАЖЕНИЕ ЛЕВОЙ ПАНЕЛИ =====================

        // Обновление списка товаров на складе (левая колонка)
        private void UpdateGoodsList()
        {
            if (GoodsList == null) return;

            GoodsList.Items.Clear();

            var selectedWarehouse = GetSelectedWarehouse();
            var goodsText = Warehouse.ShowGoods(selectedWarehouse);

            if (!string.IsNullOrEmpty(goodsText) && !goodsText.Contains("Товаров на складе нет"))
            {
                string[] goodsLines = goodsText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
                foreach (string line in goodsLines)
                    GoodsList.Items.Add(line);
            }

            if (GoodsList.Items.Count == 0)
                GoodsList.Items.Add("Товаров на складе нет");
        }

        // Смена выбранного склада слева
        private void WarehouseList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            UpdateGoodsList();
            UpdatePreconditionIndicator();
            if (PostConditionIndicator != null)
                PostConditionIndicator.Fill = Brushes.Red;
        }

        // ===================== ОБРАБОТЧИКИ ПРАВОЙ ПАНЕЛИ =====================

        // Переключение операций (и панелей)
        private void OperationList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // событие может прилететь во время InitializeComponent(), когда поля ещё null
            if (!IsLoaded || EnterGoodsNameAndQuantity == null || MoveGoods == null) return;

            if (OperationList?.SelectedItem is ListBoxItem li)
            {
                var text = li.Content?.ToString() ?? string.Empty;
                bool isMove = text.Contains("Переместить");

                EnterGoodsNameAndQuantity.Visibility = isMove ? Visibility.Collapsed : Visibility.Visible;
                MoveGoods.Visibility = isMove ? Visibility.Visible : Visibility.Collapsed;

                if (isMove)
                {
                    if (WarehouseListMoveFrom?.SelectedIndex < 0) WarehouseListMoveFrom.SelectedIndex = 0;
                    if (WarehouseListMoveTo?.SelectedIndex < 0)
                        WarehouseListMoveTo.SelectedIndex = WarehouseListMoveFrom.SelectedIndex == 0 ? 1 : 0;

                    RefreshGoodsNameToMoveFromSource();
                }
            }

            UpdatePreconditionIndicator();
            if (PostConditionIndicator != null)
                PostConditionIndicator.Fill = Brushes.Red;
        }

        // Смена склада-источника
        private void WarehouseListMoveFrom_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            RefreshGoodsNameToMoveFromSource();
            UpdatePreconditionIndicator();
            if (PostConditionIndicator != null)
                PostConditionIndicator.Fill = Brushes.Red;
        }

        // Смена склада-получателя
        private void WarehouseListMoveTo_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            UpdatePreconditionIndicator();
            if (PostConditionIndicator != null)
                PostConditionIndicator.Fill = Brushes.Red;
        }

        // Смена выбранного товара для перемещения
        private void GoodsNameToMove_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded) return;

            UpdatePreconditionIndicator();
            if (PostConditionIndicator != null)
                PostConditionIndicator.Fill = Brushes.Red;
        }

        // Изменение полей ввода (название/кол-во для 1-2 операций)
        private void GoodsName_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;

            UpdatePreconditionIndicator();
            if (PostConditionIndicator != null)
                PostConditionIndicator.Fill = Brushes.Red;
        }

        private void GoodsQuantity_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;

            UpdatePreconditionIndicator();
            if (PostConditionIndicator != null)
                PostConditionIndicator.Fill = Brushes.Red;
        }

        // Количество для 3 операции (в твоём XAML опечатка в имени обработчика — оставляю так же)
        private void GoodsQuantityToMovet_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!IsLoaded) return;

            UpdatePreconditionIndicator();
            if (PostConditionIndicator != null)
                PostConditionIndicator.Fill = Brushes.Red;
        }

        // Заполнить ComboBox товаров для перемещения из склада-источника
        private void RefreshGoodsNameToMoveFromSource()
        {
            if (GoodsNameToMove == null) return;

            var fromWh = (WarehouseListMoveFrom?.SelectedItem as ComboBoxItem)?.Content?.ToString();
            if (string.IsNullOrWhiteSpace(fromWh))
            {
                GoodsNameToMove.ItemsSource = null;
                return;
            }

            // Только товары с qty > 0 на источнике
            var items = GetWarehouseItems(fromWh).Select(t => t.name).Distinct().OrderBy(s => s).ToList();
            GoodsNameToMove.ItemsSource = items;

            if (items.Count > 0)
                GoodsNameToMove.SelectedIndex = 0;
        }
        // ---- ТОЛЬКО БУКВЫ (и пробелы) для GoodsName ----
        private void GoodsName_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (sender is not TextBox tb) return;

            // Смоделируем будущий текст с учётом выделения
            string projected = tb.Text.Remove(tb.SelectionStart, tb.SelectionLength)
                                     .Insert(tb.SelectionStart, e.Text);

            e.Handled = string.IsNullOrWhiteSpace(e.Text)     // запретить управляющие, \n и т.п. (Space разрешён отдельно)
                        ? false
                        : !LettersOnly.IsMatch(projected);
        }

        // Блокируем вставку «левых» символов мышью/CTRL+V
        private void GoodsName_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (sender is not TextBox tb) return;

            if (e.DataObject.GetDataPresent(DataFormats.UnicodeText))
            {
                var paste = (string)e.DataObject.GetData(DataFormats.UnicodeText)!;
                string projected = tb.Text.Remove(tb.SelectionStart, tb.SelectionLength)
                                         .Insert(tb.SelectionStart, paste);

                if (!LettersOnly.IsMatch(projected))
                    e.CancelCommand();
            }
            else
            {
                e.CancelCommand();
            }
        }

        // ---- ТОЛЬКО ЦИФРЫ для полей количества ----
        private void Digits_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            // Разрешаем ввод только цифр 0-9
            e.Handled = !e.Text.All(char.IsDigit);
        }

        // Разрешим Backspace/Delete/стрелки, но запретим всё, что вводит нецифровые символы (например, пробел)
        private void Digits_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            // навигация/правки — ок
            if (e.Key is Key.Back or Key.Delete or Key.Left or Key.Right or Key.Tab or Key.Home or Key.End)
                return;

            // запретим пробел и все модификаторы, которые не приводят к цифрам
            if (e.Key == Key.Space)
                e.Handled = true;
        }

        // Блокируем «левую» вставку в числовые поля
        private void Digits_Pasting(object sender, DataObjectPastingEventArgs e)
        {
            if (!e.DataObject.GetDataPresent(DataFormats.UnicodeText))
            {
                e.CancelCommand();
                return;
            }

            var paste = (string)e.DataObject.GetData(DataFormats.UnicodeText)!;
            if (string.IsNullOrEmpty(paste) || !paste.All(char.IsDigit))
                e.CancelCommand();
        }

        // ===================== ВЫПОЛНЕНИЕ ОПЕРАЦИЙ =====================

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (PostConditionIndicator != null)
                PostConditionIndicator.Fill = Brushes.Red;

            try
            {
                var operation = OperationList?.SelectedIndex ?? -1;
                var selectedWarehouse = GetSelectedWarehouse();

                switch (operation)
                {
                    case 0: // Добавить новый товар
                        {
                            var name = (GoodsName?.Text ?? string.Empty).Trim();
                            if (string.IsNullOrWhiteSpace(name))
                                throw new ArgumentException("Название товара не может быть пустым.");

                            if (!int.TryParse(GoodsQuantity?.Text, out int qty) || qty <= 0)
                                throw new ArgumentException("Количество должно быть положительным целым.");

                            if (Warehouse.CheckAddValid(selectedWarehouse, name, qty))
                            {
                                Warehouse.AddNewGood(selectedWarehouse, name, qty);
                                if (PostConditionIndicator != null)
                                    PostConditionIndicator.Fill = Brushes.Green;
                                MessageBox.Show("Товар добавлен.");
                                GoodsName?.Clear();
                                GoodsQuantity?.Clear();
                                UpdateGoodsList();
                            }
                            else
                            {
                                MessageBox.Show("Невозможно добавить товар: проверьте склад, название и количество.");
                            }
                            break;
                        }

                    case 1: // Отгрузить товар
                        {
                            var name = (GoodsName?.Text ?? string.Empty).Trim();
                            if (string.IsNullOrWhiteSpace(name))
                                throw new ArgumentException("Название товара не может быть пустым.");

                            if (!int.TryParse(GoodsQuantity?.Text, out int qty) || qty <= 0)
                                throw new ArgumentException("Количество должно быть положительным целым.");

                            if (Warehouse.CheckShipValid(selectedWarehouse, name, qty))
                            {
                                Warehouse.ShipGood(selectedWarehouse, name, qty);
                                if (PostConditionIndicator != null)
                                    PostConditionIndicator.Fill = Brushes.Green;
                                MessageBox.Show("Товар отгружен.");
                                GoodsName?.Clear();
                                GoodsQuantity?.Clear();
                                UpdateGoodsList();
                            }
                            else
                            {
                                MessageBox.Show("Невозможно отгрузить: товара нет или недостаточно.");
                            }
                            break;
                        }

                    case 2: // Перемещение товара
                        {
                            var fromWh = (WarehouseListMoveFrom?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
                            var toWh = (WarehouseListMoveTo?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
                            var name = GoodsNameToMove?.SelectedItem as string; // берём из селектора

                            if (string.IsNullOrWhiteSpace(fromWh) || string.IsNullOrWhiteSpace(toWh))
                            {
                                MessageBox.Show("Выберите склады (из какого → в какой).");
                                return;
                            }
                            if (fromWh == toWh)
                            {
                                MessageBox.Show("Склады должны различаться.");
                                return;
                            }
                            if (string.IsNullOrWhiteSpace(name))
                            {
                                MessageBox.Show("Выберите товар для перемещения.");
                                return;
                            }
                            if (!int.TryParse(GoodsQuantityToMove?.Text, out int qty) || qty <= 0)
                            {
                                MessageBox.Show("Количество для перемещения должно быть положительным целым.");
                                return;
                            }

                            if (Warehouse.CheckMoveValid(fromWh, toWh, name, qty))
                            {
                                Warehouse.MoveGood(fromWh, toWh, name, qty);
                                if (PostConditionIndicator != null)
                                    PostConditionIndicator.Fill = Brushes.Green;
                                MessageBox.Show($"Перемещено: «{name}» — {qty} шт. из «{fromWh}» в «{toWh}».");
                                GoodsQuantityToMove?.Clear();

                                // Обновляем левый список (он показывает склад, выбранный слева)
                                UpdateGoodsList();

                                // И список товаров на складе-источнике (могла позиция исчезнуть при нуле)
                                RefreshGoodsNameToMoveFromSource();
                            }
                            else
                            {
                                MessageBox.Show("Невозможно переместить: проверьте наличие и достаточность товара на складе-источнике.");
                            }
                            break;
                        }

                    default:
                        MessageBox.Show("Выберите операцию.");
                        break;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            UpdatePreconditionIndicator();
        }

        // ===================== ПРЕДУСЛОВИЯ (PRE) =====================

        private void UpdatePreconditionIndicator()
        {
            bool ok = false;
            int op = OperationList?.SelectedIndex ?? -1;

            try
            {
                switch (op)
                {
                    case 0:
                        {
                            string wh = GetSelectedWarehouse();
                            string name = (GoodsName?.Text ?? "").Trim();
                            bool qtyOk = int.TryParse(GoodsQuantity?.Text, out int qty) && qty > 0;

                            if (!string.IsNullOrWhiteSpace(name) && qtyOk)
                                ok = Warehouse.CheckAddValid(wh, name, qty);
                            break;
                        }
                    case 1:
                        {
                            string wh = GetSelectedWarehouse();
                            string name = (GoodsName?.Text ?? "").Trim();
                            bool qtyOk = int.TryParse(GoodsQuantity?.Text, out int qty) && qty > 0;

                            if (!string.IsNullOrWhiteSpace(name) && qtyOk)
                                ok = Warehouse.CheckShipValid(wh, name, qty);
                            break;
                        }
                    case 2:
                        {
                            string fromWh = (WarehouseListMoveFrom?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
                            string toWh = (WarehouseListMoveTo?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "";
                            string name = (GoodsNameToMove?.SelectedItem as string) ?? (GoodsNameToMove?.Text ?? "").Trim();
                            bool qtyOk = int.TryParse(GoodsQuantityToMove?.Text, out int qty) && qty > 0;

                            if (!string.IsNullOrWhiteSpace(fromWh) &&
                                !string.IsNullOrWhiteSpace(toWh) &&
                                !string.Equals(fromWh, toWh, StringComparison.OrdinalIgnoreCase) &&
                                !string.IsNullOrWhiteSpace(name) &&
                                qtyOk)
                            {
                                ok = Warehouse.CheckMoveValid(fromWh, toWh, name, qty);
                            }
                            break;
                        }
                }
            }
            catch
            {
                ok = false;
            }

            // безопасно ставим цвет
            if (PreConditionIndicator != null)
                PreConditionIndicator.Fill = ok ? Brushes.Green : Brushes.Red;
        }

        // ===================== КОНТРАКТ =====================

        private void ShowContractButton_Click(object sender, RoutedEventArgs e)
        {
            int operationIndex = OperationList.SelectedIndex;

            if (operationIndex == -1)
            {
                MessageBox.Show("Пожалуйста, выберите операцию из списка.");
                return;
            }

            string title = (OperationList.Items[operationIndex] as ListBoxItem)?.Content?.ToString() ?? "Операция";
            ContractWindow contractWindow = new ContractWindow(operationIndex, title);
            contractWindow.ShowDialog();
        }
        private void HelpButton_Click(object sender, RoutedEventArgs e)
        {
            var w = new HelpWindow();
            w.Owner = this;
            w.ShowDialog();
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