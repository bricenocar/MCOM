﻿using Microsoft.Extensions.Logging;

namespace MCOM.Models.UnitTesting
{
    public class FakeLog
    {
        public LogLevel LogLevel { get; set; }
        public string Message { get; set; }
    }
}
