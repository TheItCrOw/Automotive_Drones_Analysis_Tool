using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using System.Text;

namespace AutomotiveDronesAnalysisTool.Model.Bases
{
    [DataContract]
    /// <summary>
    /// Base class for every model
    /// </summary>
    public class ModelBase
    {
        [DataMember]
        public Guid Id { get; set; }

    }
}
