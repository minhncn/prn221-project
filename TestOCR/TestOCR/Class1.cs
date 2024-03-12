using IronOcr;

namespace TestOCR;
public class Class1
{
    static void Main(string[] args)
    {
        var ocr = new IronTesseract();

        using (var ocrInput = new OcrInput())
        {
            ocrInput.AddImage("images/image.png");
            //ocrInput.AddPdf("document.pdf");

            // Optionally Apply Filters if needed:
            // ocrInput.Deskew();  // use only if image not straight
            // ocrInput.DeNoise(); // use only if image contains digital noise

            var ocrResult = ocr.Read(ocrInput);
            Console.WriteLine(ocrResult.Text);
        }
    }
}

