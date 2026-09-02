namespace XN
{
    public static class ComponentExtend
    {
        /// <summary>
        /// 移除组件
        /// </summary>
        /// <param name="comp"></param>
        public static void Remove(this ComponentBase comp) => comp.Entity.RemoveComponent(comp);
    }
}