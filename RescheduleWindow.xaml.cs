using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace SchedulerUI {
    /// <summary>
    /// Логика взаимодействия для RescheduleWindow.xaml
    /// </summary>
    public partial class RescheduleWindow : Window {
        public DateTime? SelectedDateTime { get; private set; }

        public RescheduleWindow() {
            InitializeComponent();
        }

        private void Save_Click(object sender, RoutedEventArgs e) {
            if (NewDatePicker.SelectedDate == null || NewTimeComboBox.SelectedItem == null) {
                MessageBox.Show("Пожалуйста, выберите дату и время.");
                return;
            }

            ComboBoxItem timeItem = (ComboBoxItem)NewTimeComboBox.SelectedItem;
            string timeString = timeItem.Content.ToString();

            DateTime selectedDate = NewDatePicker.SelectedDate.Value;
            TimeSpan selectedTime = TimeSpan.Parse(timeString);

            SelectedDateTime = selectedDate.Date + selectedTime;
            this.DialogResult = true;
            this.Close();
        }



    }
}
