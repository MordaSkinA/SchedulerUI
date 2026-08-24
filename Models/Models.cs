using System;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Linq;
using System.IO;
using System.Diagnostics.CodeAnalysis;



namespace SchedulerUI {
    public class Client {
        public string Name { get; set; }
        public string? PhoneNumber { get; set; }
        public int Id { get; set; }
    }

    public class Service {

        public string ServiceName { get; set; }
        public int TimeDuration { get; set; }
        public double Price { get; set; }
        public int Id { get; set; }
    }

    public class Appointment {

        public Client Client { get; set; }
        public Service Service { get; set; }
        public DateTime Time { get; set; }
        public int Id { get; set; }
        public enum AppointmentStatus {
            Scheduled,
            Completed,
            Cancelled
        }
        public AppointmentStatus Status { get; set; }



    }

    class AppointmentManager {
        private AppDbContext context = new AppDbContext();

        public AppointmentManager() {
        }

        private Client GetOrCreateClient(Client client)
        {
            var existingClient = context.Clients.FirstOrDefault(c => c.Name == client.Name && c.PhoneNumber == client.PhoneNumber);
            if (existingClient != null)
            {
                return existingClient;
            }
            context.Clients.Add(client);
            context.SaveChanges();
            return client;
        }
        private Service GetOrCreateService(Service service)
        {
            var existingService = context.Services.FirstOrDefault(s => s.ServiceName == service.ServiceName && s.Price == service.Price && s.TimeDuration == service.TimeDuration);
            if (existingService != null)
            {
                return existingService;
            }
            context.Services.Add(service);
            context.SaveChanges();
            return service;
        }

        public void CheckTimeSlot(DateTime startTime, Service service, Appointment? excludeAppointment = null) {
            DateTime newStart = startTime;
            DateTime newEnd = startTime.AddMinutes(service.TimeDuration);
            
            if (startTime < DateTime.Now) {
                throw new Exception("Cannot schedule appointment in the past.");
            }

            foreach (var appointment in context.Appointments.ToList()) {
                if (appointment.Status != Appointment.AppointmentStatus.Scheduled)
                    continue;
                if (appointment == excludeAppointment)
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
            client = GetOrCreateClient(client);
            service = GetOrCreateService(service);
            Appointment appointment = new Appointment {
                Client =  client,
                Service = service,
                Time = time,
                Status = Appointment.AppointmentStatus.Scheduled
            };
            context.Appointments.Add(appointment);
            context.SaveChanges();
        }

        public void RescheduleAppointment(Appointment appointment, DateTime newTime) {
            if (appointment == null) {
                throw new ArgumentNullException(nameof(appointment));
            }
            if (appointment.Status != Appointment.AppointmentStatus.Scheduled) {
                throw new Exception("Only scheduled appointments can be rescheduled.");
            }
            CheckTimeSlot(newTime, appointment.Service, appointment );
            appointment.Time = newTime;
            context.SaveChanges();
        }

        public void CompleteAppointment(Appointment appointment) {
            if (appointment == null) {
                throw new ArgumentNullException(nameof(appointment));
            }

            if (appointment.Status == Appointment.AppointmentStatus.Scheduled) {
                    appointment.Status = Appointment.AppointmentStatus.Completed;
                context.SaveChanges();
            }
            else {
                throw new Exception("Only scheduled appointments can be completed.");
            }
            

        }

        public List<Appointment> GetAppointments() {
            return context.Appointments.ToList();
        }

        public void CancelAppointmentByClientName(string clientName, DateTime time) {
            var appointment = context.Appointments.FirstOrDefault(a => a.Client.Name == clientName && a.Time == time);
            if (appointment != null) {
                if (appointment.Status == Appointment.AppointmentStatus.Scheduled) {
                    appointment.Status = Appointment.AppointmentStatus.Cancelled;
                    context.SaveChanges();
                }
                else {
                    throw new Exception("Only scheduled appointments can be cancelled.");
                }
            }
        }

        // сохранить расписание в файл

        public async Task SaveScheduleToFileAsync(string filePath) {

            try {
                var lines = context.Appointments.Select(a => $"{a.Client.Name},{a.Client.PhoneNumber},{a.Service.ServiceName},{a.Service.Price},{a.Service.TimeDuration},{a.Time},{a.Status}").ToList();
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
                context.Appointments.RemoveRange(context.Appointments);
                
                foreach (var line in lines) {
                    var parts = line.Split(',');
                    if (parts.Length == 7) {
                        Client client = GetOrCreateClient(new Client { Name = parts[0], PhoneNumber = parts[1] });
                        Service service = GetOrCreateService(new Service { ServiceName = parts[2], Price = double.Parse(parts[3]), TimeDuration = int.Parse(parts[4]) });
                        DateTime time = DateTime.Parse(parts[5]);
                        Appointment.AppointmentStatus status = Enum.Parse<Appointment.AppointmentStatus>(parts[6]);
                        Appointment appointment = new Appointment {
                            Client = client,
                            Service = service,
                            Time = time,
                            Status = status
                        };
                        context.Appointments.Add(appointment);
                        context.SaveChanges();
                    }
                }
            }
            catch (Exception ex) {
                throw new Exception("Error occurred while loading schedule from file.", ex);
            }
        }



        // сортировка
        public List<Appointment> GetAppointmentsForDay(DateTime day) {
            return context.Appointments.Where(a => a.Time.Date == day.Date).OrderBy(a => a.Time).ToList();
        }

        public List<string> GetTodayClientNames() {
            DateTime today = DateTime.Today;
            return context.Appointments
                .Where(a => a.Time.Date == today && a.Status == Appointment.AppointmentStatus.Scheduled)
                .Select(a => a.Client.Name)
                .Distinct()
                .ToList();
        }
        public List<Appointment> GetClientHistory(string clientName) {
            return context.Appointments.Where(a => a.Client.Name == clientName).ToList();
        }


        public double GetDayRevenue(DateTime day) {
            return context.Appointments.Where(a => a.Time.Date == day.Date && a.Status == Appointment.AppointmentStatus.Completed).Sum(a => a.Service.Price);
        }

        public Appointment? GetMostExpensiveAppointment(DateTime day) {
            return context.Appointments
                .Where(a => a.Time.Date == day.Date)
                .MaxBy(a => a.Service.Price);

        }
        public string GetMostPopularService() {
            var serviceCount = context.Appointments
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





