using System.Collections.Generic;

namespace XN
{
    public class CmdOperationRequest
    {
        public string PlayerId { get; set; }
        public CmdOperationData Data { get; set; }
    }

    public class CmdOperationResponse
    {
        public int Code { get; set; }
        public string Msg { get; set; }
        public string PlayerId { get; set; }
        public CmdOperationData Data { get; set; }
    }

    public class CmdOperationData
    {
        public int Cmd { get; set; }
        public int Value { get; set; }
        public int SkinId { get; set; }
        public List<long> EffectId { get; set; }
    }
}