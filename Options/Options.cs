using System;
using CommandLine.Extensions;
using CommandLine.Parsing;
using CommandLine.Text;

namespace gZipA.Options
{
    /// <summary>
    /// Класс представляющий аргументы командной строки
    /// <summary>
    public sealed class Options
    {
        /// <summary>
        /// Свойство-опция имя команды
        /// </summary>
        [CommandLine.Option('c', "command", Required = true, HelpText = "Выбор команды сжатия/расжатия")]
        public string CommandName { get; set; }

        /// <summary>
        /// Свойство-опция имя входного файла
        /// </summary>
        [CommandLine.Option('i', "input", Required = true, HelpText = "Имя входного файла")]
        public string InputFile { get; set; }

        /// <summary>
        /// Свойство-опция имя выходного файла
        /// </summary>
        [CommandLine.Option('o', "output", Required = false, HelpText = "Имя выходного файла")]
        public string OutputFile { get; set; }

        /// <summary>
        /// Выводит помощь в использовании аргументов командной строки
        /// </summary>
        [CommandLine.HelpOption(HelpText = "Вывести на экран.")]
        public string GetUsage()
        {
            var help = new HelpText();
            help.AdditionalNewLineAfterOption = true;
            help.Copyright = new CopyrightInfo("Kirill A", 2019);
            this.HandleParsingErrorsInHelp(help);
            help.AddPreOptionsLine("Формат использования: gZipA -с compress/decompress -i InputFileName [-o OutputFileName] ");
            help.AddPreOptionsLine("Формат использования: gZipA --command compress/decompress --input InputFileName [--output OutputFileName] ");
            help.AddPreOptionsLine("Пример: gZipA -с compress -i InputFile.rtf");
            help.AddOptions(this);
            return help;
        }

        /// <summary>
        /// Добавление строки об ошибке 
        /// </summary>
        private void HandleParsingErrorsInHelp(HelpText help)
        {
            string errors = help.RenderParsingErrorsText(this, 0);
            if (!string.IsNullOrEmpty(errors))
            {
                help.AddPreOptionsLine(string.Concat(Environment.NewLine, "Ошибка: ", errors, Environment.NewLine));
            }
        }
    }
}
