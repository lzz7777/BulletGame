using System;

namespace XN
{
    [AttributeUsage(AttributeTargets.Method)]
    public class UpdateSystemAttribute : Attribute
    {
        public int Order { get; }

        public UpdateSystemAttribute(int order = 0)
        {
            Order = order;
        }
    }
}