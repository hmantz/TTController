﻿using System;
using System.Collections.Generic;
using System.Linq;
using NLog;
using LibreHardwareMonitor.Hardware;
using TTController.Common;
using TTController.Service.Hardware;
using TTController.Service.Hardware.Sensor;
using TTController.Service.Utils;
using TTController.Service.Hardware.Sensor.Decorators;
using TTController.Service.Config.Data;

namespace TTController.Service.Managers
{
    public sealed class SensorManager : IDataProvider, IDisposable
    {
        private static readonly Logger Logger = LogManager.GetCurrentClassLogger();

        private readonly ConfigData _config;
        private readonly Dictionary<Identifier, SensorConfig> _sensorConfigs;

        private readonly LibreHardwareMonitorFacade _libreHardwareMonitorFacade;
        private readonly Dictionary<Identifier, ISensorValueProvider> _sensorValueProviders;
        private readonly HashSet<IHardware> _hardware;

        public IEnumerable<Identifier> EnabledSensors => _sensorValueProviders.Keys;

        public SensorManager(ConfigData Config)
        {
            Logger.Info("Creating Sensor Manager...");
            _config = Config;

            _libreHardwareMonitorFacade = new LibreHardwareMonitorFacade();
            _sensorValueProviders = new Dictionary<Identifier, ISensorValueProvider>();
            _hardware = new HashSet<IHardware>();

            _sensorConfigs = _config.SensorConfigs
                .SelectMany(x => x.Sensors.Select(s => (Sensor: s, Config: x.Config)))
                .ToDictionary(x => x.Sensor, x => x.Config);
        }

        public void Update()
        {
            foreach (var hardware in _hardware)
                hardware.Update();

            foreach (var provider in _sensorValueProviders.Values)
                provider.Update();
        }

        public float GetSensorValue(Identifier identifier)
        {
            if (!_sensorValueProviders.ContainsKey(identifier))
                return float.NaN;

            return _sensorValueProviders[identifier].ValueOrDefault(float.NaN);
        }

        public void EnableSensor(Identifier identifier)
        {
            if (_sensorValueProviders.ContainsKey(identifier))
                return;

            var sensor = _libreHardwareMonitorFacade.Sensors.FirstOrDefault(s => s.Identifier == identifier);
            if (sensor == null)
                return;

            Logger.Info("Enabling sensor: {0}", sensor.Identifier);

            _sensorValueProviders.Add(identifier, CreateProvider(sensor));
            _hardware.Add(sensor.Hardware);
        }

        public ISensorValueProvider CreateProvider(ISensor sensor)
        {
            ISensorValueProvider sensorValueProvider = new SensorValueProvider(sensor);

            var alpha = Math.Exp(-_config.SensorTimerInterval / (double)_config.DeviceSpeedTimerInterval);
            sensorValueProvider = new MovingAverageSensorValueDecorator(sensorValueProvider, alpha);

            if (_sensorConfigs.TryGetValue(sensor.Identifier, out var config))
            {
                if (config.Offset.HasValue)
                    sensorValueProvider = new OffsetSensorValueDecorator(sensorValueProvider, config.Offset.Value);
            }

            return sensorValueProvider;
        }

        public void EnableSensors(IEnumerable<Identifier> identifiers)
        {
            if (identifiers == null)
                return;

            foreach (var identifier in identifiers)
                EnableSensor(identifier);
        }

        public void DisableSensor(Identifier identifier)
        {
            if (!_sensorValueProviders.ContainsKey(identifier))
                return;

            var sensor = _libreHardwareMonitorFacade.Sensors.FirstOrDefault(s => s.Identifier == identifier);
            if (sensor == null)
                return;

            Logger.Info("Disabling sensor: {0}", identifier);
            _sensorValueProviders.Remove(identifier);

            var removeHardware = _libreHardwareMonitorFacade.Sensors
                .Where(s => EnabledSensors.Contains(s.Identifier) && s.Identifier != sensor.Identifier)
                .All(s => s.Hardware != sensor.Hardware);

            if (removeHardware)
                _hardware.Remove(sensor.Hardware);
        }

        public void DisableSensors(IEnumerable<Identifier> identifiers)
        {
            if (identifiers == null)
                return;

            foreach (var identifier in identifiers)
                DisableSensor(identifier);
        }

        public void Accept(ICacheCollector collector)
        {
            foreach (var (sensor, provider) in _sensorValueProviders)
                collector.StoreSensorValue(sensor, provider.Value());
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        private void Dispose(bool disposing)
        {
            Logger.Info("Disposing Sensor Manager...");

            _libreHardwareMonitorFacade.Dispose();
            _sensorValueProviders.Clear();
            _hardware.Clear();
        }
    }
}
