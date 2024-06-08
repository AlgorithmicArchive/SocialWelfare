using System.Reflection;
using iText.IO.Image;
using iText.Kernel.Pdf;
using iText.Layout;
using iText.Layout.Element;
using iText.Layout.Properties;


public class PdfService(IWebHostEnvironment webHostEnvironment)
{
    private readonly IWebHostEnvironment _webHostEnvironment = webHostEnvironment;

    public void CreateSanctionPdf(Dictionary<string, string> details, string Officer, string ApplicationId)
    {
        string path = Path.Combine(_webHostEnvironment.WebRootPath, "files", ApplicationId.Replace("/", "_") + "SanctionLetter.pdf");
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? string.Empty);

        string emblem = Path.Combine(_webHostEnvironment.WebRootPath, "resources", "emblem.png");
        Image image = new Image(ImageDataFactory.Create(emblem))
                        .ScaleToFit(50, 50)        // Resize the image (optional)
                        .SetHorizontalAlignment(HorizontalAlignment.CENTER);  // Center align the image

        using PdfWriter writer = new(path);
        using PdfDocument pdf = new(writer);
        using Document document = new(pdf);
        document.Add(image);
        document.Add(new Paragraph("Union Territory of Jammu and Kashmir")
            .SetBold()
            .SetTextAlignment(TextAlignment.CENTER)
            .SetFontSize(20));
        document.Add(new Paragraph("SOCIAL WELFARE DEPARTMENT\nCIVIL SECRETARIAT, JAMMU / SRINAGAR")
            .SetTextAlignment(TextAlignment.CENTER)
            .SetFontSize(20));
        document.Add(new Paragraph("Sanction Letter for Marriage Assistance Scheme")
            .SetTextAlignment(TextAlignment.CENTER)
            .SetFontSize(16));
        document.Add(new Paragraph("To\n\nTHE MANAGER\nB/O Moving Secretariat")
            .SetFontSize(14));
        document.Add(new Paragraph("\nPlease Find the Particulars of Beneficiary given below:")
            .SetFontSize(10));


        Table table = new Table(UnitValue.CreatePercentArray(2)).UseAllAvailableWidth();
        foreach (var item in details)
        {
            table.AddCell(new Cell().Add(new Paragraph(item.Key)));
            table.AddCell(new Cell().Add(new Paragraph(item.Value)));
        }
        document.Add(table);

        string accountNumber = "0110040500000050";
        document.Add(new Paragraph($"The girl referred to above has been sanctioned financial assistance under the Government of J&K Scheme \"Marriage Assistance Scheme\". You are requested to transfer an amount of Rupees 50,000/- (Fifty thousand only) to the beneficiary's bank account, whose details are mentioned above, under the \"Marriage Assistance Scheme\" after verifying the account details, by debiting the amount from my official account {accountNumber}.")
            .SetFontSize(8));

        document.Add(new Paragraph($"NO: {ApplicationId}\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t ISSUING AUTHORITY")).SetBold();
        document.Add(new Paragraph($"Date: {DateTime.Today.ToString("dd-mm-yyyy")}\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t\t{Officer}")).SetBold();
    }

    public void CreateAcknowledgement(Dictionary<string, string> details, string ApplicationId)
    {
        string path = Path.Combine(_webHostEnvironment.WebRootPath, "files", ApplicationId.Replace("/", "_") + "Acknowledgement.pdf");
        Directory.CreateDirectory(Path.GetDirectoryName(path) ?? string.Empty);

        string emblem = Path.Combine(_webHostEnvironment.WebRootPath, "resources", "emblem.png");
        Image image = new Image(ImageDataFactory.Create(emblem))
                        .ScaleToFit(50, 50)        // Resize the image (optional)
                        .SetHorizontalAlignment(HorizontalAlignment.CENTER);  // Center align the image

        using PdfWriter writer = new(path);
        using PdfDocument pdf = new(writer);
        using Document document = new(pdf);
        document.Add(image);
        document.Add(new Paragraph("Union Territory of Jammu and Kashmir")
            .SetBold()
            .SetTextAlignment(TextAlignment.CENTER)
            .SetFontSize(20));

        document.Add(new Paragraph("Acknowledgement")
            .SetBold()
            .SetTextAlignment(TextAlignment.CENTER)
            .SetFontSize(16));

        Table table = new Table(UnitValue.CreatePercentArray(2)).UseAllAvailableWidth();
        foreach (var item in details)
        {
            table.AddCell(new Cell().Add(new Paragraph(item.Key)));
            table.AddCell(new Cell().Add(new Paragraph(item.Value)));
        }
        document.Add(table);

    }
}