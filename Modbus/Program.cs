using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Modbus
{
    class Program
    {
        static ushort CRC16_MODBUS(string key)
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
            return hash;
        }
        static void Main(string[] args)
        {
            ushort crc16 = CRC16_MODBUS("12");
            Console.WriteLine(crc16.ToString());
            Console.WriteLine(Convert.ToString(crc16,16));


            Console.ReadKey();
        }
    }
}
