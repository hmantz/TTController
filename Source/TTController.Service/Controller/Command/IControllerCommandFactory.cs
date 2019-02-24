﻿using System.Collections.Generic;
using TTController.Common;

namespace TTController.Service.Controller.Command
{
    public interface IControllerCommandFactory
    {
        IEnumerable<byte> SetRgbBytes(byte port, byte mode, IEnumerable<LedColor> colors);
        IEnumerable<byte> SetSpeedBytes(byte port, byte speed);

        IEnumerable<byte> GetPortDataBytes(byte port);
        IEnumerable<byte> SaveProfileBytes();
        IEnumerable<byte> InitBytes();
    }
}
