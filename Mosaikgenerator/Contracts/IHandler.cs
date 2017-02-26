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
    public interface IHandler
    {
        [OperationContract]
        void cropRect(int imageId);

        [OperationContract]
        [ServiceKnownType(typeof(CropModiTypes))]
        bool crop(int imageId, int width, int height, CropModiTypes mode);

        [OperationContract]
        bool scale(int imageId, int width, int height = 0);

        [OperationContract]
        void deleteFile(int imageId);
    }

    [DataContract(Name = "CropModi")]
    // Mögliche Typen von Cropmodi
    public enum CropModiTypes
    {
        MIDDLE,
        CENTERLEFT,
        CENTERRIGHT,
        TOPLEFT,
        TOPRIGHT,
        BOTTOMLEFT,
        BOTTOMRIGHT
    }
}
