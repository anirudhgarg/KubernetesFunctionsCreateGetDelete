using System;
using System.Threading;
using KubernetesFunctions;

namespace Client
{
    class Program
    {
        static void Main(string[] args)
        {            
            var k = new KubernetestFunctionsAPI(@"C:\Users\anirudhg\Desktop\config");                 

            var containerName = "helloworldcreatemultiple2";
            if (k.CreateFunctionsContainer(containerName, out string status, "gcr.io/google-samples/node-hello:1.0", 2))
            {
                Console.WriteLine(string.Format("Created Functions Container {0}", containerName));
                int i = 30;
                while (true)
                { 
                    i--;
                    k.GetIPAdress(containerName, out string IPAddress, out status);                    
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
                    Console.WriteLine("Getting IP Address. Waiting 10 seconds");
                    Thread.Sleep(10000);
                }

                //Thread.Sleep(50000);
                //k.DeleteContainer("helloworld", out string status4);
                //Console.WriteLine(status4);
            }
            Console.WriteLine(status);
            Console.ReadLine();
        }
    }
}
