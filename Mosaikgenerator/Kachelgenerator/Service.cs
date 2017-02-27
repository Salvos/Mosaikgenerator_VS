using Contracts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Kachelgenerator
{
    class Service
    {
        static void Main(string[] args)
        {
            ServiceHost host = null;

            try
            {

                host = new ServiceHost(typeof(Generator));
                host.AddServiceEndpoint(typeof(IKachelGenerator), new BasicHttpBinding(), new Uri("http://localhost:8080/mosaikgenerator/kachelgenerator"));
                host.Open();

                Console.WriteLine();
                Console.WriteLine("Enter to Exit");
                Console.ReadKey();

            }
            catch (Exception)
            {
                host.Close();
            }
        }
    }
}
