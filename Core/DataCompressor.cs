using System;
using System.Threading;
using System.IO;
using System.IO.Compression;

namespace gZipA.Core
{
    /// <summary>
    /// Класс компрессора данных
    /// </summary>
    public class DataCompressor
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
        private int index_block;

        /// <summary>
        /// Создает объект компрессора данных, указывая путь к входному и выходному файлам
        /// </summary>
        /// <param name="_inputFile">входной файл</param>
        /// <param name="_outputFile">выходной файл</param>
        public DataCompressor(string _inputFile, string _outputFile)
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
        /// Инициализация заголовка
        /// </summary>
        /// <param name="info">Информация о файле</param>
        /// <returns>Возвращает успешность операции инициализации заголовка архива</returns>
        private bool InitializeHead(FileInfo info)
        {
            file_length = info.Length;
            byte[] b_file_length = BitConverter.GetBytes(file_length);

            blocks = info.Length / block_size + 1;
            byte[] count = BitConverter.GetBytes(blocks);

            //место под длину файла, число означающее количество сжатых блоков, их позиции в исходном файле и их длину
            ziphead = new byte[(blocks << 1) * count.Length + (count.Length << 1)];
            //записываем в начало буфера заголовка длину и общее количество блоков
            System.Buffer.BlockCopy(b_file_length, 0, ziphead, 0, b_file_length.Length);
            System.Buffer.BlockCopy(count, 0, ziphead, b_file_length.Length, count.Length);

            return true;
        }

        /// <summary>
        /// Запись заголовка архива в файл
        /// </summary>
        /// <returns>Возвращает успешность завершения записи заголовка в архив</returns>
        private bool WriteHead()
        {
            try
            {
                using (FileStream fs = new FileStream(outputFile, FileMode.OpenOrCreate, FileAccess.Write))
                {
                    fs.Seek(0, SeekOrigin.Begin);
                    fs.Write(ziphead, 0, ziphead.Length);
                }
            }
            catch (IOException ex)
            {
                Console.WriteLine("Ошибка записи заголовка архива :" + ex.Message);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Запустить операцию архивации
        /// </summary>
        /// <returns>Возвращает успешность завершения операции сжатия</returns>
        public bool RunArchive()
        {
            bool valid = true;

            FileInfo info = new FileInfo(inputFile);
            bool is_initalized = InitializeHead(info);

            if (is_initalized == true)
            {
                //запуск потоков на выполнение цепочки - чтение данных из файла => сжатие => запись в выходной файл архива
                for (int index = 0; index < threads.Length; index++)
                {
                    int t_index = index;

                    threads[index] = new Thread(() =>
                    {
                        try
                        {
                            Compress(t_index);
                        }
                        catch (IOException ex)
                        {
                            Console.WriteLine("Ошибка ввода/вывода при архивации: " + ex.Message);
                            valid = false;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Произошла ошибка во время архивации: " + ex.Message);
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
                Console.WriteLine("Заголовок архива не был инициализирован");
                return false;
            }

            //запись сформированного заголовка(в начало файла архива)
            bool flag = WriteHead();
            if (flag == false)
            {
                Console.WriteLine("Заголовок архива не был записан");
                return false;
            }

            return valid;
        }

        /// <summary>
        /// Функция для чтения данных поступающих из входного файла и записи сжатых данных в выходной файл
        /// </summary>
        /// <param name="thread_index">Номер потока</param>
        private void Compress(int thread_index)
        {
            long seek = block_size * thread_index;
            long file_length = new FileInfo(inputFile).Length;
            // в случае если начальное смещение уже превзошло длину входного файла, то выходим из обработки данных для этого потока
            if (seek > file_length)
            {
                return;
            }

            long seek_iterate = block_size * (threads.Length - 1);
            long input_position, out_position;
            int length_bytes = BitConverter.GetBytes(long.MaxValue).Length;
            int _block;

            // буфферы куда помещаются данные, их позиция и длина сжатого блока
            byte[] t_buffer = new byte[block_size];
            byte[] inputposition = new byte[length_bytes];
            byte[] result_length = new byte[length_bytes];

            using (FileStream fsDestination = new FileStream(outputFile, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Write))
            {
                using (FileStream fsSource = new FileStream(inputFile, FileMode.Open, FileAccess.Read))
                {
                    fsSource.Seek(seek, SeekOrigin.Begin);

                    long r_bytes;
                    while (true)
                    {
                        input_position = fsSource.Position;
                        r_bytes = file_length - input_position;
                        if (r_bytes < block_size)
                        {
                            t_buffer = new byte[r_bytes];
                            fsSource.Read(t_buffer, 0, t_buffer.Length);
                        }
                        else
                        {
                            fsSource.Read(t_buffer, 0, block_size);
                        }

                        //получаем сжатые данные
                        byte[] result = CompressBlock(t_buffer);

                        //меняем конец позиции в выходном файле и инкрементируем счетчик считанных блоков
                        mutex.WaitOne();
                        out_position = length;
                        length += result.Length;
                        index_block++;
                        _block = index_block;
                        mutex.ReleaseMutex();

                        //заносим дополнительную информацию в заголовке
                        inputposition = BitConverter.GetBytes(input_position);
                        result_length = BitConverter.GetBytes(result.Length);

                        System.Buffer.BlockCopy(inputposition, 0, ziphead, _block * (inputposition.Length << 1), inputposition.Length);
                        System.Buffer.BlockCopy(result_length, 0, ziphead, _block * (inputposition.Length << 1) + inputposition.Length, result_length.Length);

                        //непосредственно запись в выходной файл
                        fsDestination.Seek(out_position + ziphead.Length, SeekOrigin.Begin);
                        fsDestination.Write(result, 0, result.Length);

                        fsSource.Seek(seek_iterate, SeekOrigin.Current);

                        if (fsSource.Position >= file_length || r_bytes < block_size)
                            break;
                    }
                }
            }
        }

        /// <summary>
        /// Функция сжатия данных
        /// </summary>
        /// <param name="data">Блок данных для сжатия</param>
        /// <returns>Вызвращает сжатый блок данных</returns>
        private byte[] CompressBlock(byte[] data)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(memory, CompressionMode.Compress, true))
                {
                    gzip.Write(data, 0, data.Length);
                }
                return memory.ToArray();
            }
        }

    }
}
