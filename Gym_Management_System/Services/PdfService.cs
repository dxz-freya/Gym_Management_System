using DinkToPdf;
using DinkToPdf.Contracts;

namespace GymManagement.Services
{
    public class PdfService
    {
        private readonly IConverter _converter;
        private readonly IWebHostEnvironment _env;

        public PdfService(IConverter converter, IWebHostEnvironment env)
        {
            _converter = converter;
            _env = env;
        }

        public byte[] GenerateInvoice(string html)
        {
            var doc = new HtmlToPdfDocument()
            {
                GlobalSettings = {
                    ColorMode = ColorMode.Color,
                    Orientation = Orientation.Portrait,
                    PaperSize = PaperKind.A4,
                    Margins = new MarginSettings { Top = 20, Bottom = 20 }
                },
                Objects = {
                    new ObjectSettings() {
                        HtmlContent = html,
                        WebSettings = { DefaultEncoding = "utf-8" }
                    }
                }
            };

            return _converter.Convert(doc);
        }
    }
}
