﻿using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace MCOM.Utilities
{
    public class HttpUtilities
    {
        public static HttpResponseData HttpResponse(HttpRequestData req, HttpStatusCode statusCode, string value)
        {
            var response = req.CreateResponse(statusCode);
            response.Headers.Add("Content-Type", "application/json");
            response.WriteString(value);
            return response;
        }
    }
}
