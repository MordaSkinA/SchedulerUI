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
using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Linq;
using System.IO;



namespace SchedulerUI {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    
    public partial class MainWindow : Window {

        public MainWindow() {
            InitializeComponent();
        }

        private AppointmentManager appointmentManager = new AppointmentManager();
        private List<string> serviceHistory = new List<string>();
        private List<string> durationHistory = new List<string>();
        private List<string> priceHistory = new List<string>();

        private  void AddToHistory(ComboBox comboBox, List<string> serviceHistory, string newValue) {
            if (!serviceHistory.Contains(newValue)) {
                serviceHistory.Add(newValue);
                comboBox.Items.Add(newValue);
            }
        }

        private void RecordClient_Click(object sender, RoutedEventArgs e) {

            if (AppointmentDatePicker.SelectedDate == null || AppointmentTimeComboBox.SelectedItem == null) {
                MessageBox.Show("Пожалуйста, выберите дату и время для записи.");
                return;
            }

            DateTime selectedDate = AppointmentDatePicker.SelectedDate.Value;

            ComboBoxItem selectedTimeItem = (ComboBoxItem)AppointmentTimeComboBox.SelectedItem;
            string timeText = selectedTimeItem.Content.ToString();
            TimeSpan timeSpan = TimeSpan.Parse(timeText);
            DateTime FinalDateTime = selectedDate.Add(timeSpan);

            string clientName = ClientNameTextBox.Text;
            string serviceName = ServiceComboBox.Text;
            string duration = DurationComboBox.Text;
            string price = PriceComboBox.Text;

            Client client = new Client { Name = clientName };
            
            if (!int.TryParse(duration, out int parsedDuration)) {
                MessageBox.Show("Некорректное значение длительности услуги.");
                return;
            }
            if (!double.TryParse(price, out double parsedPrice)) {
                MessageBox.Show("Некорректное значение цены услуги.");
                return;
            }
            Service service = new Service { 
                ServiceName = serviceName, 
                TimeDuration = parsedDuration, 
                Price = parsedPrice 
            };




            try {
                appointmentManager.ScheduleAppointment(client, service, FinalDateTime);
                MessageBox.Show($"Клиент: {clientName}, Услуга: {serviceName}, Время: {FinalDateTime}");

                AddToHistory(ServiceComboBox, serviceHistory, serviceName);
                AddToHistory(DurationComboBox, durationHistory, duration);
                AddToHistory(PriceComboBox, priceHistory, price);

            } catch (Exception ex) {
                MessageBox.Show($"Ошибка при записи: {ex.Message}");
            } 


            
        }
    }

}
