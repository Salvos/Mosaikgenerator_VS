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
    public interface IMosaikGenerator
    {
        [OperationContract]
        bool mosaikGenerator(int basisMotivID, int kachelPoolID, int mosaikPoolID, Boolean kachelnMultiUseEnabled = true, int auswahlNBesteKacheln = 1);
    }
}
