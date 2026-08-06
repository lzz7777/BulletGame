using System.Collections.Generic;

namespace cfg.Fight
{
    public partial class BuffMutexConfigCategory
    {
        public readonly Dictionary<State, Dictionary<ChangeType, bool>> MutexDic = new();
        public readonly Dictionary<State, Dictionary<State, bool>> MutexStateDic = new();
        
        partial void PostInit()
        {
            foreach (var conf in _dataList)
            {
                MutexDic[conf.MutexId] = new();
                MutexStateDic[conf.MutexId] = new();
                
                foreach (var mutex in conf.Mutex)
                {
                    MutexDic[conf.MutexId][mutex.Type] = mutex.MutexBool;
                }

                foreach (var mutexState in conf.MutexState)
                {
                    MutexStateDic[conf.MutexId][mutexState.State] = mutexState.MutexBool;
                }
            }
        }
    }
}