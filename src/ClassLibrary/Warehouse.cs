using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Domain
{
    public struct Good
    {
        public string? name;
        public int? quantity;
    }

    public static class Warehouse
    {
        private static List<(string warehouse, string name, int quantity)> warehouseGoods = new();
        private static List<string> warehouses = new() { "Склад 1" };

        // Операция 1: добавление нового товара
        public static Good CreateGood(string name, int quantity) => new Good { name = name, quantity = quantity };

        public static void AddNewGood(string warehouse, string name, int quantity)
        {
            // создаем склад, если его нет
            AddWarehouse(warehouse);

            // ищем товар на выбранном складе
            var existing = warehouseGoods.FirstOrDefault(x => x.warehouse == warehouse && x.name == name);

            if (existing.name != null)
            {
                // если товар есть — увеличиваем количество
                warehouseGoods.Remove(existing);
                warehouseGoods.Add((warehouse, name, existing.quantity + quantity));
            }
            else
            {
                warehouseGoods.Add((warehouse, name, quantity));
            }
        }

        public static bool CheckAddValid(string warehouse, string name, int quantity)
        {
            if (string.IsNullOrWhiteSpace(name) || quantity <= 0) return false;
            return true; // теперь разрешаем добавлять повторно — количество будет суммироваться
        }

        // Операция 2: отгузка товара со склада
        public static bool CheckShipValid(string warehouse, string name, int quantity)
        {
            var existing = warehouseGoods.FirstOrDefault(x => x.warehouse == warehouse && x.name == name);
            return existing.name != null && existing.quantity >= quantity;
        }

        // Операция 2: отгрузка товара со склада
        public static void ShipGood(string warehouse, string name, int quantity)
        {
            var existing = warehouseGoods.First(x => x.warehouse == warehouse && x.name == name);
            warehouseGoods.Remove(existing);
            int remaining = existing.quantity - quantity;
            if (remaining > 0)
                warehouseGoods.Add((warehouse, name, remaining));
        }


        // Операция 3: перемещение товара между складами

        public static void AddWarehouse(string warehouseName)
        {
            if (!warehouses.Contains(warehouseName))
                warehouses.Add(warehouseName);
        }

        public static List<string> GetWarehouses() => warehouses;

        public static bool CheckMoveValid(string fromWarehouse, string toWarehouse, string name, int quantity)
        {
            var existing = warehouseGoods.FirstOrDefault(x => x.warehouse == fromWarehouse && x.name == name);
            return existing.name != null && existing.quantity >= quantity;
        }

        public static void MoveGood(string fromWarehouse, string toWarehouse, string name, int quantity)
        {
            if (!CheckMoveValid(fromWarehouse, toWarehouse, name, quantity))
                throw new Exception($"Невозможно переместить товар {name} со склада {fromWarehouse}");

            // уменьшаем количество на исходном складе
            var source = warehouseGoods.First(x => x.warehouse == fromWarehouse && x.name == name);
            warehouseGoods.Remove(source);
            int remainingSource = source.quantity - quantity;
            if (remainingSource > 0)
                warehouseGoods.Add((fromWarehouse, name, remainingSource));

            // добавляем на склад-получатель
            var target = warehouseGoods.FirstOrDefault(x => x.warehouse == toWarehouse && x.name == name);
            if (target.name != null)
            {
                warehouseGoods.Remove(target);
                warehouseGoods.Add((toWarehouse, name, target.quantity + quantity));
            }
            else
            {
                warehouseGoods.Add((toWarehouse, name, quantity));
            }
        }


        // метод извленечения значений из строки
        public static (string? Item, int? Quantity) ExtractItemAndQuantity(string inputString)
        {
            string processedString = inputString.Trim();

            int firstQuoteIndex = processedString.IndexOf('"');
            int secondQuoteIndex = processedString.IndexOf('"', firstQuoteIndex + 1);

            string item = "";

            if (firstQuoteIndex != -1 && secondQuoteIndex != -1)
            {
                int itemLength = secondQuoteIndex - firstQuoteIndex - 1;
                item = processedString.Substring(firstQuoteIndex + 1, itemLength);
            }
            else return (null, null);

            int commaIndex = processedString.IndexOf(',');

            string ammountWithText = "";

            if (commaIndex != -1) ammountWithText = processedString.Substring(commaIndex + 1);

            else return (null, null);

            string numericAmmount = Regex.Replace(ammountWithText, @"[^\d]", "").Trim();

            int quantity;

            if (!int.TryParse(numericAmmount, out quantity)) return (null, null);

            if (string.IsNullOrWhiteSpace(item)) return (null, null);

            return (item, quantity);
        }

        // отображение товаров (чисто для дебага)
        public static string ShowGoods(string warehouse)
        {
            var list = warehouseGoods.Where(x => x.warehouse == warehouse).ToList();
            if (!list.Any()) return "Товаров на складе нет";

            StringBuilder sb = new();
            foreach (var item in list)
                sb.AppendLine($"Товар: {item.name}, количество: {item.quantity}");
            return sb.ToString();
        }


        public static string GetPreConditionText(int operationIndex)
        {
            return operationIndex switch
            {
                0 => "Требования для 'Добавить новый товар':\n" +
                     "- Название товара не пустое.\n" +
                     "- Количество > 0.\n" +
                     "- Товара с таким названием нет на складе.",
                1 => "Требования для 'Отгрузить товар':\n" +
                     "- Название товара не пустое.\n" +
                     "- Количество > 0.\n" +
                     "- Товар с таким названием существует на складе.\n" +
                     "- Достаточное количество товара для отгрузки.",
                2 => "Требования для 'Переместить товар':\n" +
                     "- Название товара не пустое.\n" +
                     "- Количество > 0.\n" +
                     "- Склад-источник существует и содержит достаточное количество товара.\n" +
                     "- Склад-получатель указан (если склада нет, он будет создан автоматически).",
                _ => "Операция не выбрана."
            };
        }

        public static string GetPostConditionText(int operationIndex)
        {
            return operationIndex switch
            {
                0 => "Утверждения после 'Добавить новый товар':\n" +
             "- В списке появился новый товар с заданным именем и количеством.\n" +
             "- Общее количество уникальных товаров увеличилось на 1.",
                1 => "Утверждения после 'Отгрузить товар':\n" +
                     "- Количество указанного товара уменьшилось на заданное значение.\n" +
                     "- Товар остался в списке даже если его количество стало равно 0.",
                2 => "Утверждения после 'Переместить товар':\n" +
                     "- Количество товара на складе-источнике уменьшилось на указанное значение.\n" +
                     "- Товар появился на складе-получателе с указанным количеством.\n" +
                     "- Если склад-получатель новый, он добавлен в список складов.",
                _ => "Операция не выбрана."
            };
        }
    }
}
