using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace Rw.Barracuda.Client.Helpers
{
    public static class FileHelper
    {
        public static async Task WriteFileAsync(string filePath, string content)
        {
            using (StreamWriter outputFile = new StreamWriter(filePath))
            {
                await outputFile.WriteAsync(content);
            }
        }

        public static void WriteFile(string filePath, string content)
        {
            File.WriteAllText(filePath, content, Encoding.UTF8);
            /*using (StreamWriter outputFile = new StreamWriter(filePath))
            {
                outputFile.Write(content, );
            }*/
        }

        public static async Task<string> ReadFileAsync(string filePath)
        {
            using (StreamReader outputFile = new StreamReader(filePath))
            {
                return await outputFile.ReadToEndAsync();
            }
        }

        public static string ReadFile(string filePath)
        {
            using (StreamReader outputFile = new StreamReader(filePath))
            {
                return outputFile.ReadToEnd();
            }
        }

        public static bool IsExists(string filePath)
        {
            return File.Exists(filePath);
        }
    }
}
