using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Linq;
using System.IO;



namespace SchedulerUI {
    class Client {
        public string Name { get; set; }
        public string PhoneNumber { get; set; }
    }

    class Service {

        public string ServiceName { get; set; }
        public int TimeDuration { get; set; }
        public double Price { get; set; }
    }

    class Appointment {

        public Client Client { get; set; }
        public Service Service { get; set; }
        public DateTime Time { get; set; }
        public enum AppointmentStatus {
            Scheduled,
            Completed,
            Cancelled
        }
        public AppointmentStatus Status { get; set; }



    }

    class AppointmentManager {
        private List<Appointment> appointments = new List<Appointment>();

        public AppointmentManager() {
        }

        public void CheckTimeSlot(DateTime startTime, Service service) {
            DateTime newStart = startTime;
            DateTime newEnd = startTime.AddMinutes(service.TimeDuration);

            if (startTime < DateTime.Now) {
                throw new Exception("Cannot schedule appointment in the past.");
            }

            foreach (var appointment in appointments) {
                if (appointment.Status != Appointment.AppointmentStatus.Scheduled)
                    continue;

                DateTime existingStart = appointment.Time;
                DateTime existingEnd = appointment.Time.AddMinutes(appointment.Service.TimeDuration);

                // проверка перекрытия интервалов
                if (newStart < existingEnd && existingStart < newEnd) {
                    throw new Exception("Time slot is already booked.");
                }
            }
        }

        public void ScheduleAppointment(Client client, Service service, DateTime time) {
            CheckTimeSlot(time, service);
            Appointment appointment = new Appointment {
                Client = client,
                Service = service,
                Time = time,
                Status = Appointment.AppointmentStatus.Scheduled
            };
            appointments.Add(appointment);
        }

        public void CompleteAppointment(Appointment appointment) {
            appointment.Status = Appointment.AppointmentStatus.Completed;
        }

        public List<Appointment> GetAppointments() {
            return appointments;
        }

        public void CancelAppointmentByClientName(string clientName, DateTime time) {
            var appointment = appointments.Find(a => a.Client.Name == clientName && a.Time == time);
            if (appointment != null) {
                if (appointment.Status == Appointment.AppointmentStatus.Scheduled) {
                    appointment.Status = Appointment.AppointmentStatus.Cancelled;
                }
                else {
                    throw new Exception("Only scheduled appointments can be cancelled.");
                }
            }
        }

        // сохранить расписание в файл

        public async Task SaveScheduleToFileAsync(string filePath) {

            try {
                var lines = appointments.Select(a => $"{a.Client.Name},{a.Client.PhoneNumber},{a.Service.ServiceName},{a.Service.Price},{a.Service.TimeDuration},{a.Time},{a.Status}").ToList();
                await File.WriteAllLinesAsync(filePath, lines);
            }
            catch (Exception ex) {
                throw new Exception("Error occurred while saving schedule to file.", ex);
            }
        }

        // загрузить расписание из файла

        public async Task LoadScheduleFromFileAsync(string filePath) {

            try {
                var lines = await File.ReadAllLinesAsync(filePath);
                appointments.Clear();
                foreach (var line in lines) {
                    var parts = line.Split(',');
                    if (parts.Length == 7) {
                        Client client = new Client { Name = parts[0], PhoneNumber = parts[1] };
                        Service service = new Service { ServiceName = parts[2], Price = double.Parse(parts[3]), TimeDuration = int.Parse(parts[4]) };
                        DateTime time = DateTime.Parse(parts[5]);
                        Appointment.AppointmentStatus status = Enum.Parse<Appointment.AppointmentStatus>(parts[6]);
                        Appointment appointment = new Appointment {
                            Client = client,
                            Service = service,
                            Time = time,
                            Status = status
                        };
                        appointments.Add(appointment);
                    }
                }
            }
            catch (Exception ex) {
                throw new Exception("Error occurred while loading schedule from file.", ex);
            }
        }



        // сортировка
        public List<Appointment> GetAppointmentsForDay(DateTime day) {
            return appointments.Where(a => a.Time.Date == day.Date).OrderBy(a => a.Time).ToList();
        }

        public List<string> GetTodayClientNames() {
            DateTime today = DateTime.Today;
            return appointments
                .Where(a => a.Time.Date == today && a.Status == Appointment.AppointmentStatus.Scheduled)
                .Select(a => a.Client.Name)
                .Distinct()
                .ToList();
        }
        public List<Appointment> GetClientHistory(string clientName) {
            return appointments.Where(a => a.Client.Name == clientName).ToList();
        }


        public double GetDayRevenue(DateTime day) {
            return appointments.Where(a => a.Time.Date == day.Date && a.Status == Appointment.AppointmentStatus.Completed).Sum(a => a.Service.Price);
        }

        public Appointment? GetMostExpensiveAppointment(DateTime day) {
            return appointments
                .Where(a => a.Time.Date == day.Date)
                .MaxBy(a => a.Service.Price);

        }
        public string GetMostPopularService() {
            var serviceCount = appointments
                .GroupBy(a => a.Service.ServiceName)
                .Select(g => new { ServiceName = g.Key, Count = g.Count() })
                .OrderByDescending(g => g.Count)
                .FirstOrDefault();
            return serviceCount?.ServiceName ?? "No services scheduled";
        }

        public DateTime FindNextAvailableSlot(DateTime searchFrom, Service service, DateTime workDayStart, DateTime workDayEnd) {
            DateTime currentTime = searchFrom;
            while (currentTime.AddMinutes(service.TimeDuration) < workDayEnd) {
                try {
                    CheckTimeSlot(currentTime, service);
                    return currentTime; // Found an available slot
                }
                catch (Exception) {
                    // Move to the next time slot
                    currentTime = currentTime.AddMinutes(15); // Assuming 15-minute increments
                }
            }
            throw new Exception("No available slots found for the given day.");
        }

    }

    // сортировка
}





