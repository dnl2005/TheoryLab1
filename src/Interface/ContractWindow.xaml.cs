using Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace UI
{
    /// <summary>
    /// Логика взаимодействия для ContractWindow.xaml
    /// </summary>
    public partial class ContractWindow : Window
    {
        public ContractWindow(int operationIndex, string operationTitle)
        {
            InitializeComponent();

            // Получаем текст контракта из Warehouse на основе переданного индекса
            string preText = Warehouse.GetPreConditionText(operationIndex);
            string postText = Warehouse.GetPostConditionText(operationIndex);

            // Устанавливаем заголовок из переданной строки (ушла сложная логика)
            OperationTitle.Text = $"Контракт для: {operationTitle}";
            PreConditionText.Text = preText;
            PostConditionText.Text = postText;
        }
    }
}
