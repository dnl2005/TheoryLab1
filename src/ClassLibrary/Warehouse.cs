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
            foreach (Good good in goods)
            {
                result.Append($"Товар: {good.name}, количество: {good.quantity}\n");
            }
            return result.ToString();
        }
    }
}
