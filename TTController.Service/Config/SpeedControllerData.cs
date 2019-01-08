﻿using System;
using TTController.Service.Speed;

namespace TTController.Service.Config
{
    public class SpeedControllerData
    {
        public Type Type { get; private set; }
        public SpeedControllerConfigBase Config { get; private set; }

        public SpeedControllerData(Type type, SpeedControllerConfigBase config)
        {
            Type = type;
            Config = config;
        }
    }
}
