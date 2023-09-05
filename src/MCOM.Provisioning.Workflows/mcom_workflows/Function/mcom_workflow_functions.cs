//------------------------------------------------------------
// Copyright (c) Microsoft Corporation. All rights reserved.
//------------------------------------------------------------

namespace MCOM.Provisioning.Functions
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.Azure.Functions.Extensions.Workflows;
    using Microsoft.Azure.WebJobs;

    /// <summary>
    /// Represents the mcom_workflow_functions flow invoked function.
    /// </summary>
    public static class mcom_workflow_functions
    {
        /// <summary>
        /// Executes the logic app workflow.
        /// </summary>
        /// <param name="zipCode">The zip code.</param>
        /// <param name="temperatureScale">The temperature scale (e.g., Celsius or Fahrenheit).</param>
        [FunctionName("mcom_workflow_functions")]
        public static Task<Weather> Run([WorkflowActionTrigger] int zipCode, string temperatureScale)
        {
            // Generate random temperature within a range based on the temperature scale
            Random rnd = new Random();
            var currentTemp = temperatureScale == "Celsius" ? rnd.Next(1, 30) : rnd.Next(40, 90);
            var lowTemp = currentTemp - 10;
            var highTemp = currentTemp + 10;

            // Create a Weather object with the temperature information
            var weather = new Weather()
            {
                ZipCode = zipCode,
                CurrentWeather = $"The current weather is {currentTemp} {temperatureScale}",
                DayLow = $"The low for the day is {lowTemp} {temperatureScale}",
                DayHigh = $"The high for the day is {highTemp} {temperatureScale}"
            };

            return Task.FromResult(weather);
        }

        /// <summary>
        /// Represents the weather information.
        /// </summary>
        public class Weather
        {
            /// <summary>
            /// Gets or sets the zip code.
            /// </summary>
            public int ZipCode { get; set; }

            /// <summary>
            /// Gets or sets the current weather.
            /// </summary>
            public string CurrentWeather { get; set; }

            /// <summary>
            /// Gets or sets the low temperature for the day.
            /// </summary>
            public string DayLow { get; set; }

            /// <summary>
            /// Gets or sets the high temperature for the day.
            /// </summary>
            public string DayHigh { get; set; }
        }
    }
}