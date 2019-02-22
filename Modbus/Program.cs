using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modbus
{
    class Program
    {
        static byte[] CRC16_MODBUS(byte[] key)
        {
            ushort hash = 0xFFFF;
            const ushort polynom = 0xA001;
            foreach (ushort i in key)
            {
                hash ^= i;
                for (int j = 0; j <= 7; j++)
                {
                    bool val = Convert.ToBoolean(hash - 2 * (hash / 2));
                    hash >>= 1;
                    if (val == true) hash ^= polynom;
                }
            }
            return BitConverter.GetBytes(hash);
        }

        static void Main(string[] args)
        {
            // Ввод данных
            byte id = 1;
            byte func = 3;
            ushort adr = 1;
            ushort quantity = 1;
            // Формирование сообщения
            byte[] ADR = BitConverter.GetBytes(adr);
            byte[] QUANTITY = BitConverter.GetBytes(quantity);
            byte[] message = { id, func, ADR[1], ADR[0], QUANTITY[1], QUANTITY[0] };
            byte[] CRC16 = CRC16_MODBUS(message);
            byte[] ADU = { id, func, ADR[1], ADR[0], QUANTITY[1], QUANTITY[0], CRC16[0], CRC16[1] };
            // Вывод на экран
            foreach (byte b in ADU)
                 Console.Write($"{b:X2} ");
            
            Console.ReadKey();
        }
    }
}
