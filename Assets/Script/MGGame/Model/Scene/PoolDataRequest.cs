namespace XN
{
    public class PrizePoolRequest
    {
        public string PlayerId { get; set; }
        public double GoldPool { get; set; }
        public double FortunePool { get; set; }
    }
    
    public class PrizePoolResponse
    {
        public int Code { get; set; }
        public string Msg { get; set; }
        public string PlayerId { get; set; }
        public double GoldPool { get; set; }
        public double FortunePool { get; set; }
        public long SaveTime  { get; set; }
    }
}