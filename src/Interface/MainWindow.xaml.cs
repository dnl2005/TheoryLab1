using ClassLibrary;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace UI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
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
    }
}