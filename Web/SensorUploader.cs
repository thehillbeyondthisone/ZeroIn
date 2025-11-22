using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using AOSharp.Core;
using Newtonsoft.Json;
using ZeroIn.Scanner;

namespace ZeroIn.Web
{
    /// <summary>
    /// Uploads detected player data from a sensor to a controller
    /// </summary>
    public class SensorUploader
    {
        private readonly ZeroInConfig _config;
        private readonly CharacterScanner _scanner;
        private Timer _uploadTimer;
        private bool _running = false;
        private readonly HttpClient _httpClient;
        private string _sensorName;

        public SensorUploader(ZeroInConfig config, CharacterScanner scanner)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _scanner = scanner ?? throw new ArgumentNullException(nameof(scanner));
            _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };
            _sensorName = DynelManager.LocalPlayer?.Name ?? "UnknownSensor";
        }

        public void Start()
        {
            if (_running) return;

            if (!_config.EnableSensorNetwork)
            {
                ZeroIn.Log?.Info("[SensorUploader] Sensor network disabled in config");
                return;
            }

            if (_config.IsController)
            {
                ZeroIn.Log?.Info("[SensorUploader] This instance is a controller, not uploading");
                return;
            }

            _sensorName = DynelManager.LocalPlayer?.Name ?? "UnknownSensor";
            int interval = Math.Max(1, _config.SensorUploadInterval) * 1000;

            _uploadTimer = new Timer(UploadCallback, null, interval, interval);
            _running = true;

            ZeroIn.Log?.Info($"[SensorUploader] Started uploading to {_config.ControllerUrl} every {_config.SensorUploadInterval}s");
        }

        public void Stop()
        {
            if (!_running) return;

            _uploadTimer?.Dispose();
            _uploadTimer = null;
            _running = false;

            ZeroIn.Log?.Info("[SensorUploader] Stopped");
        }

        private async void UploadCallback(object state)
        {
            try
            {
                // Get all detected players
                var players = _scanner.GetDetectedCharacters();
                if (players == null || players.Count == 0)
                {
                    // No data to upload
                    return;
                }

                // Tag each player with our sensor name
                foreach (var player in players)
                {
                    if (string.IsNullOrEmpty(player.DetectedBy))
                        player.DetectedBy = _sensorName;
                }

                // Create payload
                var payload = new SensorUploadPayload
                {
                    SensorName = _sensorName,
                    Players = players,
                    PlayfieldId = Playfield.ModelIdentity.Instance,
                    Timestamp = DateTime.UtcNow
                };

                var json = JsonConvert.SerializeObject(payload);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var url = $"{_config.ControllerUrl}/api/sensor/upload";
                var response = await _httpClient.PostAsync(url, content);

                if (response.IsSuccessStatusCode)
                {
                    var responseBody = await response.Content.ReadAsStringAsync();
                    ZeroIn.Log?.Debug($"[SensorUploader] Uploaded {players.Count} players to controller");
                }
                else
                {
                    ZeroIn.Log?.Warning($"[SensorUploader] Upload failed: {response.StatusCode}");
                }
            }
            catch (HttpRequestException ex)
            {
                ZeroIn.Log?.Warning($"[SensorUploader] Network error: {ex.Message}");
            }
            catch (Exception ex)
            {
                ZeroIn.Log?.Error($"[SensorUploader] Upload error: {ex.Message}");
            }
        }

        public void Dispose()
        {
            Stop();
            _httpClient?.Dispose();
        }
    }
}
