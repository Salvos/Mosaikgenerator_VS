using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;

namespace Contracts
{
    [ServiceContract]
    public interface IKachelGenerator
    {
        [OperationContract]
        void genKachel(int kachelPoolID, int r, int g, int b, bool nois);
    }

    [DataContract(Name = "Color")]
    // Mögliche Typen von Farbwerten
    public enum ColorType
    {
        RED,
        GREEN,
        BLUE
    }
}
