using System.Collections.Generic;
using UnityEngine;

public class DebugHelper : MonoSingleton<DebugHelper>
{
    private Queue<DrawInfo> _drawQuery = new();
    private Queue<DrawInfo> _nextDrawQuery = new();

    private void OnDrawGizmos()
    {
        while (_drawQuery.Count > 0)
        {
            var info = _drawQuery.Dequeue();
            Gizmos.color = info.Color;

            Gizmos.DrawSphere(info.Position, info.Size);
            if (--info.Frame > 0) _nextDrawQuery.Enqueue(info);
        }

        while (_nextDrawQuery.Count > 0)
        {
            var info = _nextDrawQuery.Dequeue();
            _drawQuery.Enqueue(info);
        }
    }

    protected override void OnInit()
    {
    }

    protected override void OnRemove()
    {
    }

    public void AddSphere(Vector3 position, Color color, float size, uint frame = 1)
    {
        _drawQuery.Enqueue(new DrawInfo
        {
            Position = position,
            Color = color,
            Size = size,
            Frame = frame
        });
    }

    private struct DrawInfo
    {
        public Vector3 Position { get; set; }
        public Color Color { get; set; }
        public float Size { get; set; }

        /// <summary>
        /// 持续多少帧
        /// </summary>
        public uint Frame;
    }
}