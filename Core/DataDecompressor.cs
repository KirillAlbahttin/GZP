using System;
using System.Threading;
using System.IO;
using System.IO.Compression;

namespace gZipA.Core
{
    /// <summary>
    /// Класс декомпрессора данных 
    /// </summary>
    public class DataDecompressor
    {
        /// <summary>
        /// путь до входного файла
        /// </summary>
        private string inputFile;
        /// <summary>
        /// путь до выходного файла
        /// </summary>
        private string outputFile;
        /// <summary>
        /// потоки
        /// </summary>
        private Thread[] threads;
        /// <summary>
        /// мьютекс
        /// </summary>
        private Mutex mutex;
        /// <summary>
        /// буфер заголовка архива
        /// </summary>
        private byte[] ziphead;
        /// <summary>
        /// длина входного файла
        /// </summary>
        private long file_length;
        /// <summary>
        /// общая сформированная длина выходного файла
        /// </summary>
        private long length;
        /// <summary>
        /// количество блоков
        /// </summary>
        private long blocks;
        /// <summary>
        /// размер блока
        /// </summary>
        private int block_size;
        /// <summary>
        /// счетчик обработанных блоков
        /// </summary>
        private long index_block;

        /// <summary>
        /// Создает объект декомпрессора данных, указывая путь к входному и выходному файлам
        /// </summary>
        /// <param name="_inputFile">входной файл</param>
        /// <param name="_outputFile">выходной файл</param>
        public DataDecompressor(string _inputFile, string _outputFile)
        {
            inputFile = _inputFile;
            outputFile = _outputFile;
            threads = new Thread[System.Environment.ProcessorCount];
            mutex = new Mutex();
            block_size = 4 * 1024 * 1024;
            length = 0;
            index_block = 0;
        }

        /// <summary>
        /// Чтение заголовка архива
        /// </summary>
        /// <returns>Возвращает успешность завершения считывания заголовка из архива</returns>
        private bool ReadHead()
        {
            try
            {
                using (FileStream fs = new FileStream(inputFile, FileMode.OpenOrCreate, FileAccess.Read))
                {
                    //буфер для значения длины входного файла
                    byte[] b_file_length = BitConverter.GetBytes(long.MaxValue);
                    fs.Read(b_file_length, 0, b_file_length.Length);
                    file_length = BitConverter.ToInt64(b_file_length, 0);

                    //буфер для считывания числа блоков
                    byte[] count = BitConverter.GetBytes(long.MaxValue);
                    fs.Read(count, 0, count.Length);
                    blocks = BitConverter.ToInt64(count, 0);

                    // заполняем заголовок, который будет содержать пары (позиция в выходном файле - длина блока)
                    ziphead = new byte[blocks * count.Length << 1];
                    fs.Read(ziphead, 0, ziphead.Length);
                }
            }
            catch (OverflowException ex)
            {
                Console.WriteLine("Ошибка при чтении заголовка.");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Запустить операцию разархивации
        /// </summary>
        /// <returns>Возвращает успешность завершения операции разжатия</returns>
        public bool RunUnarchive()
        {
            bool valid = true;

            //считываем заголовок
            bool flag = ReadHead();

            if (flag == true)
            {
                //запуск потоков на выполнение цепочки - чтение данных из файла-архива => рассжатие => запись в выходной файл 
                for (int index = 0; index < threads.Length; index++)
                {
                    int t_index = index;
                    threads[index] = new Thread(() =>
                    {
                        try
                        {
                            Decompress(t_index);
                        }
                        catch (IOException ex)
                        {
                            Console.WriteLine("Ошибка ввода/вывода при разархивации: " + ex.Message);
                            valid = false;
                        }
                        catch (InvalidDataException ex)
                        {
                            Console.WriteLine("Файл архива не соответствует формату: " + ex.Message);
                            valid = false;
                        }
                        catch (OverflowException ex)
                        {
                            Console.WriteLine("Входной файл не является архивом: Заголовок не был прочитан.");
                            valid = false;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Произошла ошибка во время разархивирования: " + ex.Message);
                            valid = false;
                        }

                    });
                    threads[index].Start();
                }

                for (int index = 0; index < threads.Length; index++)
                {
                    threads[index].Join();
                }
            }
            else
            {
                Console.WriteLine("Не был найден заголовок архива");
                return false;
            }

            return valid;
        }

        /// <summary>
        /// Функция для чтения заархививрованных данных и записи разархивированных данных в файл
        /// </summary>
        /// <param name="thread_index">Номер потока</param>
        private void Decompress(int thread_index)
        {
            // в случае если блоков для обработки меньше числа потоков, то выходим из новых потоков
            if (thread_index >= blocks)
            {
                return;
            }
            long output_position, data_length, n_block; // позиция в выходном файле, длина сжатого блока, номер блока
            long prev_length, seek;
            int length_buf = BitConverter.GetBytes(long.MaxValue).Length;
            // буфферы для хранения позиции в исходном файле и длины сжатого блока
            byte[] output_position_buf = new byte[length_buf];
            byte[] data_length_buf = new byte[length_buf];

            using (FileStream fsDestination = new FileStream(outputFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write))
            {
                using (FileStream fsSource = new FileStream(inputFile, FileMode.OpenOrCreate, FileAccess.Read))
                {
                    //Позицию потока ввода смещаем на длину заголовочных данных
                    fsSource.Seek(ziphead.Length + (length_buf << 1), SeekOrigin.Begin);
                    seek = fsSource.Position;

                    while (index_block < blocks)
                    {
                        //Синхронизация считывания длины текущего блока, а также для изменения счетчика считанных блоков и изменения общей длины, означающей позицию до которой исходный файл восстановлен
                        mutex.WaitOne();
                        //тонкий момент с двойной проверкой
                        if (index_block == blocks)
                        {
                            mutex.ReleaseMutex();
                            break;
                        }
                        n_block = index_block;
                        index_block++;
                        System.Buffer.BlockCopy(ziphead, length_buf * (int)(n_block << 1) + length_buf, data_length_buf, 0, data_length_buf.Length);
                        data_length = BitConverter.ToInt64(data_length_buf, 0);
                        prev_length = length;
                        length += data_length;
                        mutex.ReleaseMutex();

                        //считывание позиции в исходном файле
                        System.Buffer.BlockCopy(ziphead, length_buf * (int)(n_block << 1), output_position_buf, 0, output_position_buf.Length);
                        output_position = BitConverter.ToInt64(output_position_buf, 0);

                        fsSource.Seek(seek + prev_length, SeekOrigin.Begin);
                        byte[] t_buffer = new byte[data_length];
                        fsSource.Read(t_buffer, 0, t_buffer.Length);

                        //Декомпрессия и запись данных в файл
                        byte[] data = DecompressBlock(t_buffer, output_position);
                        fsDestination.Seek(output_position, SeekOrigin.Begin);
                        fsDestination.Write(data, 0, data.Length);
                    }
                }
            }
        }

        /// <summary>
        /// Функция расжатия данных
        /// </summary>
        /// <param name="zipdata">Блок данных для рассжатия</param>
        /// <param name="output">Позиция блока в выходном файле</param>
        /// <returns>Вызвращает разжатый блок данных</returns>
        private byte[] DecompressBlock(byte[] zipdata, long output)
        {
            using (GZipStream stream = new GZipStream(new MemoryStream(zipdata), CompressionMode.Decompress, true))
            {
                byte[] unzipdata;

                if (block_size + output > file_length)
                {
                    unzipdata = new byte[file_length - output];
                }
                else
                {
                    unzipdata = new byte[block_size];
                }
                stream.Read(unzipdata, 0, unzipdata.Length);

                return unzipdata;
            }
        }

    }
}
