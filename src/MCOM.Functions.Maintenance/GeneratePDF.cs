using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SelectPdf;

namespace MCOM.Functions.Maintenance
{
    public class GeneratePDF
    {
        private readonly ILogger _logger;

        public GeneratePDF(ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<GeneratePDF>();
        }

        [Function("GeneratePDF")]
        public async Task<IActionResult> RunAsync([HttpTrigger(AuthorizationLevel.Function, "get", "post")] HttpRequestData req)
        {
            _logger.LogInformation("Starting the conversion to pdf...");

            try
            {
                //LoaderOptimization license
                //GlobalProperties.LicenseKey = Environment.GetEnvironmentVariable("PDFLicense");

                // Variable declaration
                string url = string.Empty, html = string.Empty, base_url = string.Empty, 
                    page_size = string.Empty, page_orientation = string.Empty, 
                    web_page_width_str = string.Empty, web_page_height_str = string.Empty;

                // read parameters from query string
                if(req.Url.Query != null)
                {
                    var query = System.Web.HttpUtility.ParseQueryString(req.Url.Query);
                    if (query.Count > 0)
                    {
                        url = query.Get("url") ?? string.Empty;
                        html = query.Get("html") ?? string.Empty;
                        base_url = query.Get("base_url") ?? string.Empty;
                        page_size = query.Get("page_size") ?? string.Empty;
                        page_orientation = query.Get("page_orientation") ?? string.Empty;
                        web_page_width_str = query.Get("web_page_width") ?? string.Empty;
                        web_page_height_str = query.Get("web_page_height") ?? string.Empty;
                    }
                }                               

                // read from POST if encoding is application/x-www-form-urlencoded
                if (req.Method.Equals("POST") && req.Body != null)
                {                    
                    string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                    if(!string.IsNullOrEmpty(requestBody))
                    {
                        var postData = JsonConvert.DeserializeObject<PDFProperties>(requestBody);

                        url = postData?.Url ?? string.Empty;
                        html = postData?.Html ?? string.Empty;
                        base_url = postData?.Base_url ?? string.Empty;
                        page_size = postData?.Page_size ?? string.Empty;
                        page_orientation = postData?.Page_orientation ?? string.Empty;
                        web_page_width_str = postData?.Web_page_width ?? string.Empty;
                        web_page_height_str = postData?.Web_page_height ?? string.Empty;
                    }                    
                }

                // parse parameters
                _logger.LogInformation("Parsed url: " + url.ToString());
                _logger.LogInformation("Parsed html: " + html.ToString());
                _logger.LogInformation("Parsed base_url: " + base_url.ToString());

                int web_page_width = 1024;
                if (!string.IsNullOrEmpty(web_page_width_str))
                {
                    try
                    {
                        web_page_width = Convert.ToInt32(web_page_width_str);
                    }
                    catch { }
                }
                _logger.LogInformation("Parsed web_page_width: " + web_page_width);

                int web_page_height = 0;
                if (!string.IsNullOrEmpty(web_page_height_str))
                {
                    try
                    {
                        web_page_height = Convert.ToInt32(web_page_height_str);
                    }
                    catch { }
                }
                _logger.LogInformation("Parsed web_page_height: " + web_page_height);

                // pdf page size
                PdfPageSize pageSize = PdfPageSize.A4;
                try
                {
                    pageSize = (PdfPageSize)Enum.Parse(typeof(PdfPageSize), page_size, true);
                }
                catch { }
                _logger.LogInformation("Parsed page_size: " + pageSize.ToString());

                // pdf orientation
                PdfPageOrientation pdfOrientation = PdfPageOrientation.Portrait;
                try
                {
                    pdfOrientation = (PdfPageOrientation)Enum.Parse(
                        typeof(PdfPageOrientation), page_orientation, true);
                }
                catch { }
                _logger.LogInformation("Parsed page_orientation: " + pdfOrientation.ToString());


                // check for mandatory parameters
                if (string.IsNullOrEmpty(url) && string.IsNullOrEmpty(html))
                {
                    string responseMessage = "Url or html string not specified.";
                    return new BadRequestObjectResult(responseMessage);
                }

                // instantiate converter object
                HtmlToPdf converter = new HtmlToPdf();

                // set converter options
                converter.Options.WebPageWidth = web_page_width;
                converter.Options.WebPageHeight = web_page_height;

                converter.Options.PdfPageSize = pageSize;
                converter.Options.PdfPageOrientation = pdfOrientation;

                PdfDocument doc;

                // convert url or html string to pdf
                if (!string.IsNullOrEmpty(url))
                {
                    doc = converter.ConvertUrl(url);
                }
                else
                {
                    doc = converter.ConvertHtmlString(html, base_url);
                }

                // save pdf
                byte[] pdf = doc.Save();
                doc.Close();

                _logger.LogInformation("Conversion finished. Returning file...");

                return new FileContentResult(pdf, "application/pdf")
                {
                    FileDownloadName = "Document.pdf"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError("Conversion finished. Returning file...");
                string responseMessage = "An error occured: " +
                    ex.Message + "\r\n" + ex.StackTrace;
                return new BadRequestObjectResult(responseMessage);
            }
        }

    }

    internal class PDFProperties
    {
        public string? Url { get; set; }
        public string? Html { get; set; }
        public string? Base_url { get; set; }
        public string? Page_size { get; set; }
        public string? Page_orientation { get; set; }
        public string? Web_page_width { get; set; }
        public string? Web_page_height { get; set; }
    }
}

