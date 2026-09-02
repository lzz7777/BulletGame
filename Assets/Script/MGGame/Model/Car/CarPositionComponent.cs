using System;
using UnityEngine;

namespace XN
{
    public class CarPositionComponent : ComponentBase
    {
        [SerializeField]
        public float X { get; set; }
        [SerializeField]
        public float Y { get; set; }
        
        [SerializeField]
        public Action MoveXEndCb { get; set; }
        
        [SerializeField]
        public Action MoveYEndCb { get; set; }
        
        public override void OnCreate()
        {
        }

        public override void OnDestroy()
        {
            X = default;
            Y = default;
            MoveXEndCb = default;
            MoveYEndCb = default;
        }
    }
}