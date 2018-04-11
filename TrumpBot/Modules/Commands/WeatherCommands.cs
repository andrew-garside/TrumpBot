﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ApixuWeatherApi.Exceptions;
using ApixuWeatherApi.Models;
using Meebey.SmartIrc4net;
using TrumpBot.Configs;
using TrumpBot.Extensions;
using TrumpBot.Models;
using TrumpBot.Services;

namespace TrumpBot.Modules.Commands
{
    internal class WeatherCommands
    {
        internal static class FormatResponse
        {

            internal static string FormatTemperatureCelsius(double temperature)
            {
                var colours = new Colours();
                string colour = IrcConstants.IrcColor.ToString();

                if (temperature < -5)
                {
                    colour += colours.Blue;
                }
                else if (temperature < 10)
                {
                    colour += colours.LightBlue;
                }
                else if (temperature < 15)
                {
                    colour += colours.Cyan;
                }
                else if (temperature < 25)
                {
                    colour += colours.Green;
                }
                else if (temperature < 30)
                {
                    colour += colours.LightGreen;
                }
                else if (temperature < 33)
                {
                    colour += colours.Yellow + "," + colours.Grey;
                }
                else if (temperature < 38)
                {
                    colour += colours.Orange;
                }
                else
                {
                    colour += colours.LightRed;
                }
                colour += IrcConstants.IrcBold.ToString() + temperature + IrcConstants.IrcNormal;
                return colour;
            }

            internal static string FormatTemperatureFahrenheit(double temperature)
            {
                var colours = new Colours();
                string colour = IrcConstants.IrcColor.ToString();

                if (temperature < 23)
                {
                    colour += colours.Blue;
                }
                else if (temperature < 50)
                {
                    colour += colours.LightBlue;
                }
                else if (temperature < 59)
                {
                    colour += colours.Cyan;
                }
                else if (temperature < 77)
                {
                    colour += colours.Green;
                }
                else if (temperature < 86)
                {
                    colour += colours.LightGreen;
                }
                else if (temperature < 91.4)
                {
                    colour += colours.Yellow + "," + colours.Grey;
                }
                else if (temperature < 100.4)
                {
                    colour += colours.Orange;
                }
                else
                {
                    colour += colours.LightRed;
                }
                colour += IrcConstants.IrcBold.ToString() + temperature + IrcConstants.IrcNormal;
                return colour;
            }

            internal static string FormatWeatherResponse(CurrentWeatherModel.CurrentCondition currentWeather, CurrentWeatherModel.Location location, bool shortWeather = false)
            {
                var b = IrcConstants.IrcBold;
                var n = IrcConstants.IrcNormal; // Gets a bit repetative

                if (shortWeather)
                {
                    return
                        $"{b}{location.Name}, {location.Country}{n}: " +
                        $"{b}Temp:{n} {FormatTemperatureCelsius(currentWeather.TemperatureCelsius)}°C " +
                        $"(feels like {FormatTemperatureCelsius(currentWeather.FeelsLikeCelsius)}°C); " +
                        $"{b}Cond:{n} {currentWeather.Condition.Text}; " +
                        $"{b}Precip:{n} {currentWeather.PrecipitationMillimetres} mm; " +
                        $"{b}Humidity:{n} {currentWeather.Humidity}%; " +
                        $"at {currentWeather.LastUpdated.ToShortTimeString()} local time";
                }
                return
                    $"Weather at {b}{location.Name}, {location.Region}, {location.Country}{n}: " +
                    $"{b}Temp:{n} {FormatTemperatureCelsius(currentWeather.TemperatureCelsius)}°C / {FormatTemperatureFahrenheit(currentWeather.TemperatureFahrenheit)}°F " +
                    $"(feels like {FormatTemperatureCelsius(currentWeather.FeelsLikeCelsius)}°C / {FormatTemperatureFahrenheit(currentWeather.FeelsLikeFahrenheit)}°F); " +
                    $"{b}Condition:{n} {currentWeather.Condition.Text}; " +
                    $"{b}Wind:{n} {currentWeather.WindKph} Kph / {currentWeather.WindMph} Mph; " +
                    $"{b}Precipitation:{n} {currentWeather.PrecipitationMillimetres} mm / {currentWeather.PrecipitationInches} in; " +
                    $"{b}Humidity:{n} {currentWeather.Humidity}%; " +
                    $"{b}Pressure:{n} {currentWeather.PressureMillibars} mbars / {currentWeather.PressureInches} in; " +
                    $"{b}Visibility:{n} {currentWeather.VisibilityKilometres} km / {currentWeather.VisibilityMiles} mi; " +
                    $"Observed at {currentWeather.LastUpdated.ToShortTimeString()} local time";
            }

            internal static List<string> FormatForecastResponse(ForecastWeatherModel.Forecast forecast, CurrentWeatherModel.Location location, bool shortWeather = false)
            {
                var b = IrcConstants.IrcBold;
                var n = IrcConstants.IrcNormal;
                var c = IrcConstants.IrcColor;
                var colours = new Colours();
                List<string> response = new List<string>();
                foreach (ForecastWeatherModel.ForecastDay day in forecast.ForecastDays)
                {
                    if (shortWeather)
                    {
                        response.Add($"Forecast for {day.Date.ToShortDateString()}: {b}Avg:{n} {FormatTemperatureCelsius(day.Day.AvgTempCelsius)}°C; " +
                                     $"{b}Min:{n} {FormatTemperatureCelsius(day.Day.MinTempCelsius)}°C; " +
                                     $"{b}Max:{n} {FormatTemperatureCelsius(day.Day.MaxTempCelsius)}°C; " +
                                     $"{b}Cond:{n} {day.Day.Condition.Text}; " +
                                     $"{b}Precip:{n} {day.Day.TotalPrecipMm} mm; " +
                                     $"{b}Avg Humidity:{n} {day.Day.AvgHumidity}%; " +
                                     $"{b}Astro:{n} Sunrise at {c}{colours.Blue}{day.Astro.Sunrise}{n}, sunset at {c}{colours.Orange}{day.Astro.Sunset}{n}");
                        continue;
                    }
                    response.Add($"Forecast for {day.Date.ToShortDateString()}: {b}Avg:{n} {FormatTemperatureCelsius(day.Day.AvgTempCelsius)}°C / {FormatTemperatureFahrenheit(day.Day.AvgTempFahrenheit)}°F; " +
                                 $"{b}Min:{n} {FormatTemperatureCelsius(day.Day.MinTempCelsius)}°C / {FormatTemperatureFahrenheit(day.Day.MinTempFahrenheit)}°F; " +
                                 $"{b}Max:{n} {FormatTemperatureCelsius(day.Day.MaxTempCelsius)}°C / {FormatTemperatureFahrenheit(day.Day.MaxTempFahrenheit)}°F; " +
                                 $"{b}Condition:{n} {day.Day.Condition.Text}; " +
                                 $"{b}Wind (Max):{n} {day.Day.MaxWindKph} Kph / {day.Day.MaxWindMph} Mph; " +
                                 $"{b}Precipitation:{n} {day.Day.TotalPrecipMm} mm / {day.Day.TotalPrecipIn} in; " +
                                 $"{b}Avg Visibility:{n} {day.Day.AvgVisibilityKm} km / {day.Day.AvgVisibilityMiles} mi; " +
                                 $"{b}Avg Humidity:{n} {day.Day.AvgHumidity}%; " +
                                 $"{b}Astro:{n} Sunrise at {c}{colours.Blue}{day.Astro.Sunrise}{n}, sunset at {c}{colours.Orange}{day.Astro.Sunset}{n}");
                }
                return response;
            }
        }

        internal class QueryWeather : ICommand
        {
            public string CommandName { get; } = "Weather-QueryWeather";
            public List<Regex> Patterns { get; set; } = new List<Regex>
            {
                new Regex(@"^w (?!set)(.*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase),
                new Regex(@"^wea (?!set)(.*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase),
                new Regex(@"^weather (?!set)(.*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase),
                new Regex(@"^ws (?!set)(.*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase)
            };
            public List<string> RunCommand(string message, string channel, string nick, GroupCollection arguments = null, bool useCache = true)
            {
                if (arguments == null || arguments.Count == 1)
                {
                    throw new ArgumentException("Not enough arguments");
                }

                bool shortQuery = arguments[0].Value.Substring(0, 2) == "ws";

                string query = arguments[1].Value;

                if (query.ToLower() == "goonyland")
                {
                    query = "Goteborg, Sweden";
                }

                WeatherApiConfigModel weatherApiConfig = (WeatherApiConfigModel) new WeatherApiConfig().LoadConfig();

                ForecastWeatherModel.ForecastWeather forecastWeather;

                try
                {
                    forecastWeather =
                        ApixuWeatherApi.Weather.ForecastWeather
                            .GetWeatherForecastAsync(query, weatherApiConfig.ApiKey,
                                days: 1)
                            .Result;
                }
                catch (Exception e)
                {
                    if (e.InnerException is QueryNotFoundException)
                    {
                        return "Could not find given location.".SplitInParts(430).ToList();
                    }
                    throw;
                }



                return FormatResponse.FormatWeatherResponse(forecastWeather.Current, forecastWeather.Location, shortWeather: shortQuery)
                    .SplitInParts(430)
                    .Concat(FormatResponse.FormatForecastResponse(forecastWeather.Forecast, forecastWeather.Location, shortWeather: shortQuery))
                    .ToList();

            }
        }

        internal class SetDefaultUserQuery : ICommand
        {
            public string CommandName { get; } = "Weather-SetDefaultUserQuery";
            public List<Regex> Patterns { get; set; } = new List<Regex>
            {
                new Regex(@"^w set (.*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase),
                new Regex(@"^wea set (.*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase),
                new Regex(@"^weather set (.*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase)
            };
            public List<string> RunCommand(string message, string channel, string nick, GroupCollection arguments = null, bool useCache = true)
            {
                if (arguments == null || arguments.Count == 1)
                {
                    throw new ArgumentException("Not enough arguments");
                }

                WeatherApiConfigModel weatherApiConfig = (WeatherApiConfigModel) new WeatherApiConfig().LoadConfig();
                weatherApiConfig.UserDefaultLocale[nick] = arguments[1].Value;
                new WeatherApiConfig().SaveConfig(weatherApiConfig);
                return $"Successfully updated default user locale for {nick} to {arguments[1].Value}".SplitInParts(430)
                    .ToList();
            }
        }

        internal class QueryDefaultWeather : ICommand
        {
            public string CommandName { get; } = "Weather-QueryDefaultWeather";
            public List<Regex> Patterns { get; set; } = new List<Regex>
            {
                new Regex(@"^w$", RegexOptions.Compiled | RegexOptions.IgnoreCase),
                new Regex(@"^wea$", RegexOptions.Compiled | RegexOptions.IgnoreCase),
                new Regex(@"^weather$", RegexOptions.Compiled | RegexOptions.IgnoreCase),
                new Regex(@"^ws$", RegexOptions.Compiled | RegexOptions.IgnoreCase)
            };
            public List<string> RunCommand(string message, string channel, string nick, GroupCollection arguments = null, bool useCache = true)
            {
                WeatherApiConfigModel weatherApiConfig = (WeatherApiConfigModel) new WeatherApiConfig().LoadConfig();
                if (!weatherApiConfig.UserDefaultLocale.ContainsKey(nick))
                {
                    return "User has no default locale set, use 'set' command to set a locale.".SplitInParts(430).ToList();
                }

                ForecastWeatherModel.ForecastWeather forecastWeather;

                bool shortQuery = arguments[0].Value == "ws";

                try
                {
                    forecastWeather =
                        ApixuWeatherApi.Weather.ForecastWeather
                            .GetWeatherForecastAsync(weatherApiConfig.UserDefaultLocale[nick], weatherApiConfig.ApiKey,
                                days: 1)
                            .Result;
                }
                catch (Exception e)
                {
                    if (e.InnerException is QueryNotFoundException)
                    {
                        return "Could not find given location.".SplitInParts(430).ToList();
                    }
                    throw;
                }

                return FormatResponse.FormatWeatherResponse(forecastWeather.Current, forecastWeather.Location, shortWeather: shortQuery)
                    .SplitInParts(430)
                    .Concat(FormatResponse.FormatForecastResponse(forecastWeather.Forecast, forecastWeather.Location, shortWeather: shortQuery))
                    .ToList();


            }
        }
    }
}
