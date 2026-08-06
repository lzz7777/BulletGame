using System;
using System.Runtime.Serialization;

namespace ByteDance.CloudSync.TeaSDK
{
    [Serializable]
    [DataContract]
    public class TeaReportEventConfig
    {
        [DataMember(Name = "name")] public string Name;

        [DataMember(Name = "params")] public string[] Params;
        public bool IsClientReport = true;
        public bool MustUseUid = true;
    }
}