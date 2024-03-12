using IronOcr;
namespace TestReadImg
{
    public class Program
    {
        static void Main(string[] args)
        {
            var testImagePath = @"../../../images/image3.jpeg";
            try
            {
                var Ocr = new IronTesseract();

                // Add a primary language (Default is English)
                Ocr.Language = OcrLanguage.English;

                // Add as many secondary languages as you like
                Ocr.AddSecondaryLanguage(OcrLanguage.VietnameseBest);

                var result = Ocr.Read(testImagePath).Text;

                Console.WriteLine(result);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }
    }
}