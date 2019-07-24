using System;
using System.Threading;
using KubernetesFunctions;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {
            var k = new KubernetestFunctionsAPI();         
            if (k.CreateFunctionsContainer("helloworld2", out string status, "gcr.io/google-samples/node-hello:1.0"))
            {
                int i = 30;
                while (true)
                { 
                    i--;
                    k.GetIPAdress("helloworld2", out string IPAddress, out status);
                    Thread.Sleep(5000);
                    Console.WriteLine("Getting IP Address");
                    if (IPAddress.Length > 0)
                    {
                        Console.WriteLine("IP Adress {0}", IPAddress);
                        break;
                    }
                    if (i <= 0)
                    {
                        Console.WriteLine("Could not get IP");
                        break;
                    }
                }

                //Thread.Sleep(50000);
                //k.DeleteContainer("helloworld", out string status4);
                //Console.WriteLine(status4);
            }
            Console.WriteLine(status);
        }
    }
}
