using System;
using System.IO.Ports;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Modbus
{
    public partial class MainForm : Form
    {
        // Ввод данных
        byte id = 1;
        byte func = 3;
        ushort adr = 0;
        ushort quantity = 4;
        SerialPort port;
        string portnames;
        int speed;
        Parity parity;
        int dataBits;
        StopBits stopBits;
 
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

        public MainForm()
        {
            InitializeComponent();
            comboBox1.Items.AddRange(SerialPort.GetPortNames());
            comboBox2.Items.AddRange(new string[] { "9600", "19200"});
            comboBox3.Items.AddRange(new string[] { "None", "Even" });
            comboBox4.Items.AddRange(new string[] { "7", "8" });
            comboBox5.Items.AddRange(new string[] { "1", "2" });
        }

        private void comboBox1_SelectedIndexChanged(object sender, EventArgs e)
        {
            portnames = comboBox1.Text;
        }
        private void comboBox2_SelectedIndexChanged(object sender, EventArgs e)
        {
            speed = int.Parse(comboBox2.Text);
        }
        private void comboBox3_SelectedIndexChanged(object sender, EventArgs e)
        {
            parity = (Parity)Enum.Parse(typeof(Parity),comboBox3.Text);
        }
        private void comboBox4_SelectedIndexChanged(object sender, EventArgs e)
        {
            dataBits = int.Parse(comboBox4.Text);
        }
        private void comboBox5_SelectedIndexChanged(object sender, EventArgs e)
        {
            stopBits = (StopBits)Enum.Parse(typeof(StopBits), comboBox5.Text);
        }

        private void name_Click(object sender, EventArgs e)
        {
            label8.Text = "";
            label9.Text = "";
            try
            {
                port = new SerialPort(portnames, speed, parity, dataBits, stopBits);
            }
            catch
            {
                MessageBox.Show("Пожалуй проверьте настройки сети и попробуйте еще раз!", "Ошибка соединения");
            }
            // Формирование сообщения
            byte[] ADR = BitConverter.GetBytes(adr);
            byte[] QUANTITY = BitConverter.GetBytes(quantity);
            byte[] PDU = { func, ADR[1], ADR[0], QUANTITY[1], QUANTITY[0] };
            byte[] message = { id, PDU[0], PDU[1], PDU[2], PDU[3], PDU[4] };
            byte[] CRC16 = CRC16_MODBUS(message);
            byte[] ADU = { id, PDU[0], PDU[1], PDU[2], PDU[3], PDU[4], CRC16[0], CRC16[1] };
            // Вывод на экран
            foreach (byte b in ADU)
                label8.Text+=($"{b:X2} ");
            // Запись сообщения в порт
            int error = 0;
            port.Open();
            port.Write(ADU, 0, ADU.Length);
            // Чтение ответа из порт
            int count;
            switch (func)
            {
                case 3: // Для функции 03: адрес устройства, функция, кол-во байт данных, данные, CRC
                    count = 1 + 1 + 1 + (quantity * 2) + 2;
                    break;
                default:
                    count = 0;
                    break;
            }
            byte[] response = new byte[count];
            port.ReadTimeout = 1000;
            try
            {
                port.Read(response, 0, count);
            }
            catch (TimeoutException)
            {
                MessageBox.Show("Время ожидания истекло","Тайм-аут");
                error = 5;
            }
            port.Close();
            
            // Вывод на экран
            foreach (byte b in response)
                label9.Text += ($"{b:X2} ");
            // Разбор ответа
            int[] data = new int[quantity];
            // Проверяем контрольную сумму
            if (error == 0)
            {
                message = new byte[count - 2];
                for (int i = 0; i < count - 2; i++)
                    message[i] = response[i];
                CRC16 = CRC16_MODBUS(message);
                if (response[count - 2] != CRC16[0] || response[count - 1] != CRC16[1])
                    MessageBox.Show("Ошибка контрольной суммы");
                // Анализируем данные
                else
                {
                    // Обработка ошибок
                    if (response[1] > 128)
                    {
                        error = response[2];
                        if (error == 1) MessageBox.Show("Функция не поддерживается");
                        else if (error == 2) MessageBox.Show("Запрошенная область памяти не доступна");
                        else if (error == 3) MessageBox.Show("Функция не поддерживает запрошенное количество данных");
                        else if (error == 4) MessageBox.Show("Функция выполнена с ошибкой");
                        else MessageBox.Show("Неизвестная ошибка");
                    }
                    else
                    {
                        switch (func)
                        {
                            case 3: // Для функции 03
                                byte[] b = new byte[2];
                                int temp = 0;
                                for (int i = 3; i < count - 2; i += 2)
                                {
                                    b[0] = response[i + 1];
                                    b[1] = response[i];
                                    data[temp] = BitConverter.ToInt16(b, 0);
                                    temp++;
                                }
                                break;
                            default:
                                count = 0;
                                break;
                        }
                    }
                }
            }
            foreach (int i in data)
                Console.WriteLine(i);
        }

        
    }
}
