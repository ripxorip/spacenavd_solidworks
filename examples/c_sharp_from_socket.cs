using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace HelloSocket
{
    internal class Program
    {
        static void Main(string[] args)
        {
            var server = "192.168.50.117";
            Int32 port = 11111;
            using (TcpClient client = new TcpClient(server, port))
            {
                NetworkStream stream = client.GetStream();
                while (true)
                {
                    var data = new Byte[256];
                    var res = stream.Read(data, 0, 32);
                    var res_array = new Int32[32/4];
                    for (int i = 0; i < 32/4; i++)
                    {
                        int num = BitConverter.ToInt32(data, i*4);
                        res_array[i] = num;
                    }

                    for (int i = 0; i < 32/4; i++)
                    {
                        Console.WriteLine(res_array[i]);
                    }
                    Console.WriteLine("*****");
                }
            }
        }
    }
}
