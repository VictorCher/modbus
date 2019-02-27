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
        byte id;
        byte func;
        ushort adr;
        ushort quantity;//для некоторых функций (05,06,15,16) содержит значение для записи
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
            comboBox1.Text = "COM1";
            comboBox2.Items.AddRange(new string[] { "9600", "19200"});
            comboBox2.Text = "9600";
            comboBox3.Items.AddRange(new string[] { "None", "Even" });
            comboBox3.Text = "None";
            comboBox4.Items.AddRange(new string[] { "7", "8" });
            comboBox4.Text = "7";
            comboBox5.Items.AddRange(new string[] { "1", "2" });
            comboBox5.Text = "1";
            textBox1.Text = "1";
            textBox2.Text = "03";
            textBox3.Text = "0";
            textBox4.Text = "1";
            id = byte.Parse(textBox1.Text);
            func = byte.Parse(textBox2.Text);
            adr = ushort.Parse(textBox3.Text);
            quantity = ushort.Parse(textBox4.Text);
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
        private void textBox1_TextChanged(object sender, EventArgs e)
        {
            id = byte.Parse(textBox1.Text);
        }

        private void textBox2_TextChanged(object sender, EventArgs e)
        {
            func = byte.Parse(textBox2.Text);
        }

        private void textBox3_TextChanged(object sender, EventArgs e)
        {
            adr = ushort.Parse(textBox3.Text);
        }

        private void textBox4_TextChanged(object sender, EventArgs e)
        {
            quantity = ushort.Parse(textBox4.Text);
        }

        private void name_Click(object sender, EventArgs e)
        {
            requestTextBox.Text = "";
            responseTextBox.Text = "";
            textBox6.Text = "";
            try
            {
                port = new SerialPort(portnames, speed, parity, dataBits, stopBits);
            }
            catch
            {
                MessageBox.Show("Ошибка соединения\nПожалуй проверьте настройки сети и попробуйте еще раз!");
            }
            // Формирование сообщения
            byte[] ADR = BitConverter.GetBytes(adr);
            byte[] QUANTITY = BitConverter.GetBytes(quantity);
            // Формирование PDU для функций 01-06
            byte[] PDU;
            if(func > 6) PDU = new byte[] { func, ADR[1], ADR[0], 0, 1, 2, QUANTITY[1], QUANTITY[0] };
            else PDU = new byte[]{ func, ADR[1], ADR[0], QUANTITY[1], QUANTITY[0] };
            byte[] message = { id, PDU[0], PDU[1], PDU[2], PDU[3], PDU[4] };
            byte[] CRC16 = CRC16_MODBUS(message);
            byte[] ADU = { id, PDU[0], PDU[1], PDU[2], PDU[3], PDU[4], CRC16[0], CRC16[1] };
            // Вывод на экран
            foreach (byte b in ADU)
                requestTextBox.Text+=($"{b:X2} ");
            // Запись сообщения в порт
            int error = 0;
            try
            {
                port.Open();
            }
            catch(Exception)
            {
                MessageBox.Show("Невозможно открыть порт");
                return;
            }
            port.Write(ADU, 0, ADU.Length);
            // Чтение ответа из порт
            int count;
            switch (func)
            {
                case 1:
                case 2: // Для функций 01-02: адрес устройства, функция, кол-во байт данных, данные, CRC
                    if(quantity%8 == 0) count = 1 + 1 + 1 + (quantity/8) + 2;
                    else count = 1 + 1 + 1 + (quantity / 8 + 1) + 2;
                    break;
                case 3:
                case 4: // Для функций 03-06: адрес устройства, функция, кол-во байт данных, данные, CRC
                    count = 1 + 1 + 1 + (quantity * 2) + 2;
                    break;
                case 5: // Для функции 05: адрес устройства, функция, адрес данных, данные, CRC
                    count = 1 + 1 + 1 + 2 + 2 + 2;
                    break;
                case 6: // Для функции 06: адрес устройства, функция, адрес данных, данные, CRC
                    count = 1 + 1 + 1 + 2 +2 + (quantity * 2) + 2;
                    break;
                case 15:
                case 16: // Для функций 15,16: адрес устройства, функция, адрес данных, данные, кол-во байт данных, данные, CRC
                default:
                    return;
       
            }
            byte[] response = new byte[count];
            port.ReadTimeout = 1000;
            try
            {
                port.Read(response, 0, count);
            }
            catch (TimeoutException)
            {
                MessageBox.Show("Время ожидания истекло");
                return;
            }
            finally
            {
                port.Close();
            }
            // Вывод на экран
            foreach (byte b in response)
                responseTextBox.Text += ($"{b:X2} ");
            // Разбор ответа
            int[] data = new int[quantity];
            // Проверяем контрольную сумму
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
                    int temp = 0;
                    switch (func)
                    {
                        case 1:
                        case 2:
                            for (int i = 3; i < count - 2; i++)
                            {
                                for (int j = 0; j < 8; j++)
                                {
                                    if ((response[i] & (1 << j)) == 0) data[temp] = 0;
                                    else data[temp] = 1;
                                    temp++;
                                    // если мы проверили заданное количество бит,
                                    //то выходим из цикла досрочно
                                    if (temp == quantity) break;
                                }
                            }
                            break;
                        case 3:
                        case 4: // Для функции 03
                            byte[] b = new byte[2];
                            for (int i = 3; i < count - 2; i += 2)
                            {
                                b[0] = response[i + 1];
                                b[1] = response[i];
                                data[temp] = BitConverter.ToInt16(b, 0);
                                temp++;
                            }
                            break;
                        case 15:

                            break;
                        default:
                            count = 0;
                            break;
                    }
                }
            }
            for (int i = 0; i < data.Length; i++)
                textBox6.Text += $"Адрес: {i + adr}, Значение: {data[i],5}{Environment.NewLine}";
        }
    }
}
