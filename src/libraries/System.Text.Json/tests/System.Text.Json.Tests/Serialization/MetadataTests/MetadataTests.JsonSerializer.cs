﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace System.Text.Json.Tests.Serialization
{
    public abstract partial class MetadataTests
    {
        [Fact]
        public async Task RoundTripSerializerOverloads()
        {
            WeatherForecastWithPOCOs expected = CreateWeatherForecastWithPOCOs();
            string json = await Serializer.SerializeWrapper(expected, JsonContext.Default.WeatherForecastWithPOCOs);
            WeatherForecastWithPOCOs actual = await Deserializer.DeserializeWrapper(json, JsonContext.Default.WeatherForecastWithPOCOs);
            VerifyWeatherForecastWithPOCOs(expected, actual);

            json = await Serializer.SerializeWrapper(actual, typeof(WeatherForecastWithPOCOs), JsonContext.Default);
            actual = (WeatherForecastWithPOCOs)await Deserializer.DeserializeWrapper(json, typeof(WeatherForecastWithPOCOs), JsonContext.Default);
            VerifyWeatherForecastWithPOCOs(expected, actual);
        }

        [Fact]
        public void WriterIsFlushedAtRootCall()
        {
            using MemoryStream ms = new();
            using Utf8JsonWriter writer = new(ms);

            JsonSerializer.Serialize(writer, new HighLowTemps(), JsonContext.Default.HighLowTemps);
            Assert.Equal(18, writer.BytesCommitted);
            Assert.Equal(0, writer.BytesPending);
        }

        private static WeatherForecastWithPOCOs CreateWeatherForecastWithPOCOs()
        {
            return new WeatherForecastWithPOCOs
            {
                Date = DateTime.Parse("2019-08-01T00:00:00-07:00"),
                TemperatureCelsius = 25,
                Summary = "Hot",
                DatesAvailable = new List<DateTimeOffset>
                {
                    DateTimeOffset.Parse("2019-08-01T00:00:00-07:00"),
                    DateTimeOffset.Parse("2019-08-02T00:00:00-07:00"),
                },
                TemperatureRanges = new Dictionary<string, HighLowTemps> {
                    {
                        "Cold",
                        new HighLowTemps
                        {
                            High = 20,
                            Low = -10,
                        }
                    },
                    {
                        "Hot",
                        new HighLowTemps
                        {
                            High = 60,
                            Low = 20,
                        }
                    },
                },
                SummaryWords = new string[] { "Cool", "Windy", "Humid" },
            };
        }

        private static void VerifyWeatherForecastWithPOCOs(WeatherForecastWithPOCOs expected, WeatherForecastWithPOCOs obj)
        {
            Assert.Equal(expected.Date, obj.Date);
            Assert.Equal(expected.TemperatureCelsius, obj.TemperatureCelsius);
            Assert.Equal(expected.Summary, obj.Summary);
            Assert.Equal(expected.DatesAvailable.Count, obj.DatesAvailable.Count);
            for (int i = 0; i < expected.DatesAvailable.Count; i++)
            {
                Assert.Equal(expected.DatesAvailable[i], obj.DatesAvailable[i]);
            }
            List<KeyValuePair<string, HighLowTemps>> expectedTemperatureRanges = expected.TemperatureRanges.OrderBy(kv => kv.Key).ToList();
            List<KeyValuePair<string, HighLowTemps>> objTemperatureRanges = obj.TemperatureRanges.OrderBy(kv => kv.Key).ToList();
            Assert.Equal(expectedTemperatureRanges.Count, objTemperatureRanges.Count);
            for (int i = 0; i < expectedTemperatureRanges.Count; i++)
            {
                Assert.Equal(expectedTemperatureRanges[i].Key, objTemperatureRanges[i].Key);
                Assert.Equal(expectedTemperatureRanges[i].Value.Low, objTemperatureRanges[i].Value.Low);
                Assert.Equal(expectedTemperatureRanges[i].Value.High, objTemperatureRanges[i].Value.High);
            }
            Assert.Equal(expected.SummaryWords.Length, obj.SummaryWords.Length);
            for (int i = 0; i < expected.SummaryWords.Length; i++)
            {
                Assert.Equal(expected.SummaryWords[i], obj.SummaryWords[i]);
            }
        }
    }
}
