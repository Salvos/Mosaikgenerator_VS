using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace ImageHandler
{
    class Service
    {
        static void Main(string[] args)
        {
            ServiceHost host = null;

            try
            {

                host = new ServiceHost(typeof(Handler));
                host.AddServiceEndpoint(typeof(Handler), new BasicHttpBinding(), new Uri("http://localhost:8080/mosaikgenerator/imagehandler"));
                host.Open();

            }
            catch (Exception)
            {

                host.Close();

            }

            Console.WriteLine();
            Console.WriteLine("Enter to Exit");
            Console.ReadLine();
        }
    }
}
