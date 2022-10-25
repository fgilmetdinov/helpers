using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Noname.Client.Helpers
{
    /// <summary>
    /// Утилиты работы с файлами
    /// </summary>
    public static class FileUtils
    {
        /// <summary>
        /// Преобразование файла в строку
        /// </summary>
        /// <param name="file"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<string> ConvertToStringAsync(Stream file, CancellationToken cancellationToken)
        {
            file.Seek(0, SeekOrigin.Begin);
            //without 'using' because will be closed source file
            var reader = new StreamReader(file);
            return await reader.ReadToEndAsync();
        }

        /// <summary>
        /// Преобразование файла в строку
        /// </summary>
        /// <param name="file"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public static async Task<string> ConvertToBase64StringAsync(Stream file, CancellationToken cancellationToken)
        {
            file.Seek(0, SeekOrigin.Begin);
            await using var memoryStream = new MemoryStream();            
            await file.CopyToAsync(memoryStream, cancellationToken);
            var bytes = memoryStream.ToArray();
            return Convert.ToBase64String(bytes);
        }
    }
}
