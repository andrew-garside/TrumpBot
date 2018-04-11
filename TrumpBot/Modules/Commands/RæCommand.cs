﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TrumpBot.Extensions;

namespace TrumpBot.Modules.Commands
{
    public class RæCommand : ICommand
    {
        public string CommandName { get; } = "Ræ Command";
        public List<Regex> Patterns { get; set; } = new List<Regex>
        {
            new Regex("^ræ$", RegexOptions.Compiled | RegexOptions.IgnoreCase),
            new Regex("^rae$", RegexOptions.Compiled | RegexOptions.IgnoreCase)
        };
        public List<string> RunCommand(string message, string channel, string nick, GroupCollection arguments = null, bool useCache = true)
        {
            return ("R" + String.Concat(Enumerable.Repeat("Æ", new Random().Next(50, 450)))).SplitInParts(430).ToList();
        }
    }
}
