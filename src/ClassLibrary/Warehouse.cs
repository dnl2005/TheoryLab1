using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace ClassLibrary
{
    public struct Good
    {
        public string? name;
        public int? quantity;
    }

    public static class Warehouse
    {
        private static List<Good> goods = [];

        // Операция 1: добавление нового товара
        public static Good CreateGood(string name, int quantity) => new Good { name = name, quantity = quantity };

        public static void AddNewGood(Good good) => goods.Add(good);

        public static bool CheckAddValid(Good good)
        {
            if (good.name is null || good.name == "") return false;
            if (good.quantity < 0) return false;
            if (goods.Any(x => x.name == good.name)) return false;
            return true;
        }

        // Операция 2: отгузка товара со склада
        public static bool CheckShipValid(Good good)
        {
            if (good.name is null || good.name == "") return false;
            if (good.quantity <= 0) return false;

            // Проверяем, что товар существует и его количество достаточно
            var existingGood = goods.FirstOrDefault(x => x.name == good.name);
            if (existingGood.name == null) return false; // товар не найден
            if (existingGood.quantity < good.quantity) return false; // недостаточно товара

            return true;
        }

        // Операция 2: отгрузка товара со склада
        public static void ShipGood(Good good)
        {
            // Находим товар и уменьшаем его количество
            for (int i = 0; i < goods.Count; i++)
            {
                if (goods[i].name == good.name)
                {
                    var updatedGood = goods[i];
                    updatedGood.quantity -= good.quantity;

                    // ЕСЛИ КОЛИЧЕСТВО СТАЛО 0 ИЛИ МЕНЬШЕ - УДАЛЯЕМ ТОВАР ИЗ СПИСКА
                    if (updatedGood.quantity <= 0)
                    {
                        goods.RemoveAt(i);
                    }
                    else
                    {
                        goods[i] = updatedGood;
                    }
                    break;
                }
            }
        }


        // Операция 3: перемещение товара между складами



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
        public static string ShowGoods()
        {
            StringBuilder result = new();

            // ЕСЛИ СПИСОК ПУСТОЙ - ВОЗВРАЩАЕМ СООБЩЕНИЕ
            if (goods.Count == 0)
            {
                return "Товаров на складе нет";
            }

            foreach (Good good in goods)
            {
                result.AppendLine($"Товар: {good.name}, количество: {good.quantity}");
            }
            return result.ToString();
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
                2 => "Pre-условия для 'Переместить товар' (пока не реализованы)",
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
                2 => "Post-условия для 'Переместить товар' (пока не реализованы)",
                _ => "Операция не выбрана."
            };
        }
    }
}
