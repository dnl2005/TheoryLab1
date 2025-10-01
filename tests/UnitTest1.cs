using Domain;

namespace tests
{
    public class UnitTest1
    {
        [Fact]
        public void CheckAddValid_ValidInput_ReturnsTrue()
        {
            // Arrange
            // Используем товар с уникальным именем и положительным количеством
            var good = new Good { name = "Ноутбук", quantity = 5 };

            // Act
            bool result = Warehouse.CheckAddValid(good);

            // Assert
            Assert.True(result, "Номинальный валидный ввод должен вернуть True.");
        }

        [Fact]
        public void CheckAddValid_ZeroQuantity_ReturnsFalse()
        {
            // Arrange
            var good = new Good { name = "Стол", quantity = 0 };

            // Act
            bool result = Warehouse.CheckAddValid(good);

            // Assert
            Assert.False(result, "Количество, равное 0, нарушает Pre-условие и должно вернуть False.");
        }

        [Fact]
        public void CheckAddValid_NegativeQuantity_ReturnsFalse()
        {
            // Arrange
            var good = new Good { name = "Стул", quantity = -10 };

            // Act
            bool result = Warehouse.CheckAddValid(good);

            // Assert
            Assert.False(result, "Отрицательное количество нарушает Pre-условие и должно вернуть False.");
        }

        [Fact]
        public void CheckAddValid_EmptyName_ReturnsFalse()
        {
            // Arrange
            var good = new Good { name = "", quantity = 1 };

            // Act
            bool result = Warehouse.CheckAddValid(good);

            // Assert
            Assert.False(result, "Пустое название нарушает Pre-условие и должно вернуть False.");
        }

        [Fact]
        public void CheckAddValid_WhitespaceName_ReturnsFalse()
        {
            // Arrange
            // Предполагая, что в Warehouse.cs ты используешь string.IsNullOrWhiteSpace
            var good = new Good { name = "   ", quantity = 1 }; 

            // Act
            bool result = Warehouse.CheckAddValid(good);

            // Assert
            Assert.False(result, "Название из пробелов нарушает Pre-условие и должно вернуть False.");
        }
    }
}