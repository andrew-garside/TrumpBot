﻿using System;
using System.Collections.Generic;
using TrumpBot.Configs;

namespace TrumpBot.Models
{
    public class TetherConfigModel : BaseModel
    {
        public int CheckInterval { get; set; } = 10;
        public List<string> Channels { get; set; } = new List<string>();
        public decimal LastValue { get; set; } = 0;
        public DateTime LastChange { get; set; } = DateTime.MinValue;
        public bool Enabled { get; set; } = true;
    }
}