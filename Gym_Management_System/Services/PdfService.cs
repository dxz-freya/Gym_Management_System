using DinkToPdf;
using DinkToPdf.Contracts;

namespace GymManagement.Services
{
    public class PdfService
    {
        private readonly IConverter _converter;

        public PdfService(IConverter converter)
        {
            _converter = converter;
        }

        public byte[] GeneratePdf(string html)
        {
            var doc = new HtmlToPdfDocument()
            {
                GlobalSettings = new GlobalSettings
                {
                    PaperSize = PaperKind.A4,
                    Orientation = Orientation.Portrait,
                    Margins = new MarginSettings { Top = 20, Bottom = 20 }
                },
                Objects = { new ObjectSettings { HtmlContent = html } }
            };

            return _converter.Convert(doc);
        }
    }
}
