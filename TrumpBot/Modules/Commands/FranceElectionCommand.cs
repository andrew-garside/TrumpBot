﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TrumpBot.Extensions;
using TrumpBot.Models;

namespace TrumpBot.Modules.Commands
{
    [Command.CacheOutput(600)]
    public class FranceElectionCommand : ICommand
    {
        public string CommandName { get; } = "France election results";
        public List<Regex> Patterns { get; set; } = new List<Regex>
        {
            new Regex("^fr$", RegexOptions.Compiled | RegexOptions.IgnoreCase)
        };
        public List<string> RunCommand(string message, string channel, string nick, GroupCollection arguments = null, bool useCache = true)
        {
            List<FranceElectionApiModel.Election> electionData = new FranceElectionApiModel().GetElectionData(useCache);

            FranceElectionApiModel.Election currentElectionData = electionData.Last();

            string result = $"France {currentElectionData.Year} round {currentElectionData.Round} results:";

            result = currentElectionData.Votes.Aggregate(result,
                (current, candidate) =>
                    current + $" {candidate.Name}: {candidate.Votes:n0} votes ({candidate.Percent}%);");

            DateTime updateDate = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).AddSeconds(currentElectionData.UpdatedAt);

            result += $" Last Updated: {(int) (DateTime.UtcNow - updateDate).TotalMinutes} minutes ago";

            return result.SplitInParts(430).ToList();
        }
    }
}
