using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace Domain
{
    public struct Good
    {
        public string? name;
        public int? quantity;
    }

    public static class Warehouse
    {
        // (склад, товар, кол-во) — по одному кортежу на пару (склад, товар)
        private static readonly List<(string warehouse, string name, int quantity)> warehouseGoods = new();

        // ФИКСИРОВАННЫЕ СКЛАДЫ (не создаём автоматически)
        private static readonly List<string> warehouses = new()
        {
            "Склад 1", "Склад 2", "Склад 3", "Склад 4", "Склад 5"
        };

        // Если нужно — можно переинициализировать набор складов при старте
        public static void InitFixedWarehouses(IEnumerable<string> names)
        {
            warehouses.Clear();
            warehouses.AddRange(names.Distinct(StringComparer.OrdinalIgnoreCase));
            // Товары не трогаем — предполагается чистый старт перед операциями
        }

        public static List<string> GetWarehouses() => warehouses.ToList();

        public static bool WarehouseExists(string warehouseName) =>
            !string.IsNullOrWhiteSpace(warehouseName) &&
            warehouses.Any(w => string.Equals(w, warehouseName, StringComparison.OrdinalIgnoreCase));

        // ---------------- Операция 1: Добавить новый товар ----------------

        public static Good CreateGood(string name, int quantity) => new Good { name = name, quantity = quantity };

        public static bool CheckAddValid(string warehouse, string name, int quantity)
        {
            if (!WarehouseExists(warehouse)) return false;
            if (string.IsNullOrWhiteSpace(name)) return false;
            if (quantity <= 0) return false;

            // Разрешаем добавлять повторно — количество будет суммироваться
            return true;
        }

        public static void AddNewGood(string warehouse, string name, int quantity)
        {
            if (!WarehouseExists(warehouse))
                throw new InvalidOperationException($"Склад \"{warehouse}\" не существует.");
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Название товара не может быть пустым.");
            if (quantity <= 0)
                throw new ArgumentException("Количество должно быть > 0.");

            var idx = warehouseGoods.FindIndex(x =>
                string.Equals(x.warehouse, warehouse, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.name, name, StringComparison.OrdinalIgnoreCase));

            if (idx >= 0)
            {
                // Суммируем количество, если товар уже есть
                var rec = warehouseGoods[idx];
                warehouseGoods[idx] = (rec.warehouse, rec.name, rec.quantity + quantity);
            }
            else
            {
                warehouseGoods.Add((warehouse, name, quantity));
            }
        }

        // ---------------- Операция 2: Отгрузить товар ----------------

        public static bool CheckShipValid(string warehouse, string name, int quantity)
        {
            if (!WarehouseExists(warehouse)) return false;
            if (string.IsNullOrWhiteSpace(name)) return false;
            if (quantity <= 0) return false;

            var existing = warehouseGoods.FirstOrDefault(x =>
                string.Equals(x.warehouse, warehouse, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.name, name, StringComparison.OrdinalIgnoreCase));

            return existing.name != null && existing.quantity >= quantity;
        }

        public static void ShipGood(string warehouse, string name, int quantity)
        {
            if (!CheckShipValid(warehouse, name, quantity))
                throw new InvalidOperationException("Невозможно отгрузить: товара нет или недостаточно.");

            var idx = warehouseGoods.FindIndex(x =>
                string.Equals(x.warehouse, warehouse, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.name, name, StringComparison.OrdinalIgnoreCase));

            var rec = warehouseGoods[idx];
            int remaining = rec.quantity - quantity;

            if (remaining > 0)
            {
                // Обновляем остаток
                warehouseGoods[idx] = (rec.warehouse, rec.name, remaining);
            }
            else
            {
                // Политика: нулевые записи не храним — удаляем позицию
                warehouseGoods.RemoveAt(idx);
            }
        }

        // ---------------- Операция 3: Переместить товар ----------------

        public static bool CheckMoveValid(string fromWarehouse, string toWarehouse, string name, int quantity)
        {
            if (string.IsNullOrWhiteSpace(name)) return false;
            if (quantity <= 0) return false;
            if (!WarehouseExists(fromWarehouse)) return false;
            if (!WarehouseExists(toWarehouse)) return false;
            if (string.Equals(fromWarehouse, toWarehouse, StringComparison.OrdinalIgnoreCase)) return false;

            var existing = warehouseGoods.FirstOrDefault(x =>
                string.Equals(x.warehouse, fromWarehouse, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.name, name, StringComparison.OrdinalIgnoreCase));

            return existing.name != null && existing.quantity >= quantity;
        }

        public static void MoveGood(string fromWarehouse, string toWarehouse, string name, int quantity)
        {
            if (!CheckMoveValid(fromWarehouse, toWarehouse, name, quantity))
                throw new InvalidOperationException("Невозможно переместить товар: проверьте склады, название и количество.");

            // Источник
            var idxSource = warehouseGoods.FindIndex(x =>
                string.Equals(x.warehouse, fromWarehouse, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.name, name, StringComparison.OrdinalIgnoreCase));

            var src = warehouseGoods[idxSource];
            int remainingSource = src.quantity - quantity;

            if (remainingSource > 0)
            {
                // Обновляем остаток источника
                warehouseGoods[idxSource] = (src.warehouse, src.name, remainingSource);
            }
            else
            {
                // Политика: нулевые записи не храним — удаляем позицию на источнике
                warehouseGoods.RemoveAt(idxSource);
            }

            // Приёмник
            var idxTarget = warehouseGoods.FindIndex(x =>
                string.Equals(x.warehouse, toWarehouse, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(x.name, name, StringComparison.OrdinalIgnoreCase));

            if (idxTarget >= 0)
            {
                var trg = warehouseGoods[idxTarget];
                warehouseGoods[idxTarget] = (trg.warehouse, trg.name, trg.quantity + quantity);
            }
            else
            {
                warehouseGoods.Add((toWarehouse, name, quantity));
            }
        }

        // ---------------- Вспомогательное/вывод ----------------

        // Сохраняем парсер из прежней версии (если где-то используется)
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

            if (!int.TryParse(numericAmmount, out int quantity)) return (null, null);

            if (string.IsNullOrWhiteSpace(item)) return (null, null);

            return (item, quantity);
        }

        public static string ShowGoods(string warehouse)
        {
            var list = warehouseGoods
                .Where(x => string.Equals(x.warehouse, warehouse, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (!list.Any()) return "Товаров на складе нет";

            StringBuilder sb = new();
            foreach (var item in list.OrderBy(x => x.name, StringComparer.OrdinalIgnoreCase))
                sb.AppendLine($"Товар: {item.name}, количество: {item.quantity}");
            return sb.ToString();
        }

        public static string GetPreConditionText(int operationIndex)
        {
            return operationIndex switch
            {
                0 => "Требования для 'Добавить новый товар':\n" +
                     "- Название товара не пустое (только буквы).\n" +
                     "- Количество > 0.\n",
                1 => "Требования для 'Отгрузить товар':\n" +
                     "- Название товара не пустое (только буквы).\n" +
                     "- Количество > 0.\n" +
                     "- Товар существует на складе и количества достаточно.",
                2 => "Требования для 'Переместить товар':\n" +
                     "- Название товара не пустое (только буквы).\n" +
                     "- Количество > 0.\n" +
                     "- Склады не совпадают.\n" +
                     "- На складе-источнике товара достаточно.",
                _ => "Операция не выбрана."
            };
        }

        public static string GetPostConditionText(int operationIndex)
        {
            return operationIndex switch
            {
                0 => "После 'Добавить новый товар':\n" +
                     "- Если товар уже был на складе — количество увеличено.\n" +
                     "- Если товара не было — добавлен новый товар с заданным количеством.",
                1 => "После 'Отгрузить товар':\n" +
                     "- Количество товара на выбранном складе уменьшилось на указанную величину.\n" +
                     "- Если остаток стал 0 — позиция удалена (нулевые записи не храним).",
                2 => "После 'Переместить товар':\n" +
                     "- На складе-источнике количество уменьшилось на указанную величину (если стало 0 — позиция удалена).\n" +
                     "- На складе-получателе количество увеличилось на ту же величину.\n" +
                     "- Общее количество товара по системе не изменилось.",
                _ => "Операция не выбрана."
            };
        }
    }
}