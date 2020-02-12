using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using gZipA.Core;
using gZipA.Options;

namespace gZipA
{
    class Program
    {
        static void Main(string[] args)
        {
            bool result;
            Options.Options options = new Options.Options();
            Console.WriteLine(options.GetUsage());
            do
            {
                result = false;
                try
                {
                    // если запускаем .exe, а не из коммандной строки
                    if (args == null || args.Length == 0)
                    {
                        string program = Path.GetFileNameWithoutExtension(System.Diagnostics.Process.GetCurrentProcess().MainModule.FileName).Split('.')[0]; //получаем имя программы
                        Console.Write("\nВведите команду: ");
                        args = Regex.Split(Console.ReadLine(), @"\s+");
                        if (args[0].ToLower() == program.ToLower())
                        {
                            string[] arguments = new string[args.Length - 1];
                            Array.Copy(args, 1, arguments, 0, arguments.Length); // копируем аргументы командной строки
                            if (CommandLine.Parser.Default.ParseArguments(arguments, options))
                            {
                                OptionsModel model = new OptionsModel(options);
                                if (model.IsValid())
                                {
                                    if (model.CommandName == "compress")
                                    {
                                        DataCompressor compressor = new DataCompressor(model.InputPath, model.OutputPath);
                                        result = compressor.RunArchive();
                                    }
                                    else
                                    {
                                        DataDecompressor decompressor = new DataDecompressor(model.InputPath, model.OutputPath);
                                        result = decompressor.RunUnarchive();
                                    }
                                }
                                else
                                {
                                    Console.WriteLine("Входные параметры не действительны! Попробуйте ввести команду заново: ");
                                }

                            }
                        }
                    }
                    else
                    {
                        if (CommandLine.Parser.Default.ParseArguments(args, options))
                        {
                            OptionsModel model = new OptionsModel(options);
                            if (model.IsValid())
                            {
                                if (model.CommandName == "compress")
                                {
                                    DataCompressor compressor = new DataCompressor(model.InputPath, model.OutputPath);
                                    result = compressor.RunArchive();
                                }
                                else
                                {
                                    DataDecompressor decompressor = new DataDecompressor(model.InputPath, model.OutputPath);
                                    result = decompressor.RunUnarchive();
                                }
                            }
                            else
                            {
                                Console.WriteLine("Входные параметры не действительны! Попробуйте ввести команду заново: ");
                            }
                        }
                    }
                }
                catch (ArgumentException)
                {
                    Console.WriteLine("Один или несколько входных параметров не действительны: ");
                }
                finally
                {
                    if (result == true)
                    {
                        Console.WriteLine("Операция успешно выполнена!");
                    }
                    else
                    {
                        Console.WriteLine("Операция не выполнена");
                    }
                    options = new Options.Options();
                    args = null;
                    Console.WriteLine("Нажмите 'Esc' для выхода или любую клавишу для продолжения... : ");
                }
            }
            while (Console.ReadKey().KeyChar != 0x1B); // пока не нажали кнопку "esc";
        }
    }
}
