using Domain;

namespace TestProject1
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
    }
}