using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Database;
using Firebase.Database.Query;

namespace AQUA_SMART_IOT.Services
{
    public class FirebaseService
    {
        private readonly FirebaseClient _liveDb;
        private readonly FirebaseClient _archiveDb;
        private readonly List<IDisposable> _subscriptions = new();

        public FirebaseService()
        {
            _liveDb = new FirebaseClient(
                "https://aquasmartiot-de9ce-default-rtdb.firebaseio.com/");

            _archiveDb = new FirebaseClient(
                "https://aquasmartdatabase-default-rtdb.firebaseio.com/");
        }

        // =========================
        // REAL-TIME EVENTS
        // =========================
        public event Action<double> TemperatureCChanged;
        public event Action<double> TemperatureFChanged;
        public event Action<double> PhChanged;
        public event Action<double> DoMgLChanged;

        public event Action<int> TurbidityChanged;
        public event Action<int> TurbiditySensorValueChanged;
        public event Action<string> TurbidityStatusChanged;

        public event Action<string> CirculationPumpStatusChanged;
        public event Action<string> HeaterStatusChanged;
        public event Action<string> RefillPumpStatusChanged;

        // =========================
        // REAL-TIME LISTENERS
        // =========================
        public void StartListeners()
        {
            ListenDouble("TEMPERATURE/CELCIUS", TemperatureCChanged);
            ListenDouble("TEMPERATURE/FAHRENHEIT", TemperatureFChanged);

            ListenDouble("PHVOLTAGE/ph_act", PhChanged);
            ListenDouble("DO/mgL", DoMgLChanged);

            ListenInt("Turbidity/turbidity", TurbidityChanged);
            ListenInt("Turbidity/sensorValue", TurbiditySensorValueChanged);
            ListenString("Turbidity/turbidityStatus", TurbidityStatusChanged);

            ListenString("STATUSMESSAGE/CIRCpumpStatus", CirculationPumpStatusChanged);
            ListenString("STATUSMESSAGE/heaterStatus", HeaterStatusChanged);
            ListenString("STATUSMESSAGE/refillPumpStatus", RefillPumpStatusChanged);
        }

        private void ListenDouble(string path, Action<double> callback)
        {
            _subscriptions.Add(
                _liveDb.Child("AQUASMART").Child(path)
                .AsObservable<double>()
                .Subscribe(d =>
                {
                    if (d.Object != null)
                        callback?.Invoke(d.Object);
                })
            );
        }

        private void ListenInt(string path, Action<int> callback)
        {
            _subscriptions.Add(
                _liveDb.Child("AQUASMART").Child(path)
                .AsObservable<int>()
                .Subscribe(d =>
                {
                    if (d.Object != null)
                        callback?.Invoke(d.Object);
                })
            );
        }

        private void ListenString(string path, Action<string> callback)
        {
            _subscriptions.Add(
                _liveDb.Child("AQUASMART").Child(path)
                .AsObservable<string>()
                .Subscribe(d =>
                {
                    if (!string.IsNullOrEmpty(d.Object))
                        callback?.Invoke(d.Object);
                })
            );
        }

        public void StopListeners()
        {
            foreach (var sub in _subscriptions)
                sub.Dispose();

            _subscriptions.Clear();
        }

        // =========================
        // ONE-TIME READS
        // =========================
        public Task<double> GetTemperatureC() =>
            _liveDb.Child("AQUASMART/TEMPERATURE/CELCIUS").OnceSingleAsync<double>();

        public Task<double> GetTemperatureF() =>
            _liveDb.Child("AQUASMART/TEMPERATURE/FAHRENHEIT").OnceSingleAsync<double>();

        public Task<double> GetPH() =>
            _liveDb.Child("AQUASMART/PHVOLTAGE/ph_act").OnceSingleAsync<double>();

        public Task<double> GetDoMgL() =>
            _liveDb.Child("AQUASMART/DO/mgL").OnceSingleAsync<double>();

        public Task<int> GetTurbidity() =>
            _liveDb.Child("AQUASMART/Turbidity/turbidity").OnceSingleAsync<int>();

        public Task<int> GetTurbiditySensorValue() =>
            _liveDb.Child("AQUASMART/Turbidity/sensorValue").OnceSingleAsync<int>();

        public Task<string> GetTurbidityStatus() =>
            _liveDb.Child("AQUASMART/Turbidity/turbidityStatus").OnceSingleAsync<string>();

        public Task<string> GetCirculationPumpStatus() =>
            _liveDb.Child("AQUASMART/STATUSMESSAGE/CIRCpumpStatus").OnceSingleAsync<string>();

        public Task<string> GetHeaterStatus() =>
            _liveDb.Child("AQUASMART/STATUSMESSAGE/heaterStatus").OnceSingleAsync<string>();

        public Task<string> GetRefillPumpStatus() =>
            _liveDb.Child("AQUASMART/STATUSMESSAGE/refillPumpStatus").OnceSingleAsync<string>();

        // =========================
        // ARCHIVE SNAPSHOT (MATCHES JSON)
        // =========================
        public async Task SaveSnapshotToArchive()
        {
            var snapshot = new
            {
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss"),

                TEMPERATURE = new
                {
                    CELCIUS = await GetTemperatureC(),
                    FAHRENHEIT = await GetTemperatureF()
                },

                PHVOLTAGE = new
                {
                    ph_act = await GetPH()
                },

                DO = new
                {
                    mgL = await GetDoMgL()
                },

                Turbidity = new
                {
                    turbidity = await GetTurbidity(),
                    sensorValue = await GetTurbiditySensorValue(),
                    turbidityStatus = await GetTurbidityStatus()
                },

                STATUSMESSAGE = new
                {
                    CIRCpumpStatus = await GetCirculationPumpStatus(),
                    heaterStatus = await GetHeaterStatus(),
                    refillPumpStatus = await GetRefillPumpStatus()
                }
            };

            await _archiveDb.Child("AQUASMART").PostAsync(snapshot);
        }

        // =========================
        // WRITE CONTROLS
        // =========================
        public Task SetCirculationPumpStatus(string status) =>
            _liveDb.Child("AQUASMART/STATUSMESSAGE/CIRCpumpStatus").PutAsync(status);

        public Task SetHeaterStatus(string status) =>
            _liveDb.Child("AQUASMART/STATUSMESSAGE/heaterStatus").PutAsync(status);

        public Task SetRefillPumpStatus(string status) =>
            _liveDb.Child("AQUASMART/STATUSMESSAGE/refillPumpStatus").PutAsync(status);
    }
}