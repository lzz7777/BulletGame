using System;
using System.Collections.Generic;
using UnityEngine;

namespace XN
{
    public class CarTintCtrl : MonoBehaviour
    {
        private List<Animation> Animations;

        private void Awake()
        {
            Animations = new List<Animation>();
            var anims = gameObject.GetComponentsInChildren<Animation>();
            if (anims is { Length: > 0 }) Animations.AddRange(anims);
        }

        public void Play()
        {
            if (Animations is not { Count: > 0 }) return;

            foreach (var ani in Animations)
            {
                ani.Stop();
                ani.Play();
            }
        }

        private void OnDestroy()
        {
            if (Animations is { Count: > 0 })
            {
                foreach (var ani in Animations) ani.Stop();
                Animations.Clear();
            }

            Animations = null;
        }
    }
}