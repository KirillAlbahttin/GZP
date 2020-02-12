using System;
using System.IO;
using System.Text;

namespace gZipA.Options
{
    /// <summary>
    /// Класс модели введеных параметров в командной строке
    /// <summary>
    public class OptionsModel
    {
        /// <summary>
        /// Команда
        /// </summary>
        public string CommandName { get; set; }

        /// <summary>
        /// Входной файл
        /// </summary>
        public string InputFile { get; set; }

        /// <summary>
        /// Входная директория файла
        /// </summary>
        public string InputDirectory { get; set; }

        /// <summary>
        /// Входной файл
        /// </summary>
        public string InputPath { get; set; }

        /// <summary>
        /// Выходной файл
        /// </summary>
        public string OutputFile { get; set; }

        /// <summary>
        /// Выходная директория файла
        /// </summary>
        public string OutputDirectory { get; set; }
        /// <summary>
        /// Выходная директория файла
        /// </summary>
        public string OutputPath { get; set; }

        /// <summary>
        /// Создает экземпляр представления модели входных параметров введенных из командной строки
        /// </summary>
        public OptionsModel(Options options)
        {
            CommandName = options.CommandName.ToLower();
            InputDirectory = (Path.GetDirectoryName(options.InputFile) == string.Empty) ? Directory.GetCurrentDirectory() : Path.GetDirectoryName(options.InputFile);
            InputFile = Path.GetFileName(options.InputFile);
            OutputDirectory = (Path.GetDirectoryName(options.OutputFile) == string.Empty || options.OutputFile == null) ? InputDirectory : Path.GetDirectoryName(options.OutputFile);
            if (CommandName == "compress")
            {
                OutputFile = (Path.GetFileName(options.OutputFile) == string.Empty || options.OutputFile == null) ? InputFile + ".gz" : Path.GetFileName(options.OutputFile);
            }
            else if (CommandName == "decompress")
            {
                OutputFile = (Path.GetFileName(options.OutputFile) == string.Empty || options.OutputFile == null) ? Path.GetFileNameWithoutExtension(InputFile) : Path.GetFileName(options.OutputFile);
            }
            InputPath = Path.Combine(InputDirectory, InputFile);
            OutputPath = Path.Combine(OutputDirectory, OutputFile);
        }

        /// <summary>
        /// Валидация введенных параметров
        /// </summary>
        public bool IsValid()
        {
            if (this.CommandName.ToLower() != "compress" && this.CommandName.ToLower() != "decompress")
            {
                Console.WriteLine("Примечание: Наименование команды должно быть compress или decompess");
                return false;
            }

            if (!File.Exists(Path.Combine(this.InputDirectory, this.InputFile)))
            {
                Console.WriteLine("Входной файл не существует!");
                return false;
            }

            if (!Directory.Exists(this.OutputDirectory))
            {
                Console.WriteLine("Выходной каталог: " + this.OutputDirectory + " не существует!");
                return false;
            }

            return true;
        }

    }
}
