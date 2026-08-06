using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Imaging;
using System.Windows.Media.Media3D;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;



namespace SchedulerUI {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    
    public partial class MainWindow : Window {

        public MainWindow() {
            InitializeComponent();
            // обновить таблицу при запуске
            AppointmentsDataGrid.ItemsSource = appointmentManager.GetAppointments();
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

                // обновить таблицу после записи
                AppointmentsDataGrid.ItemsSource = null; // сброс источника данных
                AppointmentsDataGrid.ItemsSource = appointmentManager.GetAppointments();

            }
            catch (Exception ex) {
                MessageBox.Show($"Ошибка при записи: {ex.Message}");
            } 


            
        }
    }

}


