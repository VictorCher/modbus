using System;
using System.IO;
using System.IO.Ports;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Modbus
{
    class Program
    {
        

        static void Main(string[] args)
        {
            MainForm form = new MainForm();
            form.Show();
            Application.Run(form);

            

            Console.ReadKey();
        }
    }
}
