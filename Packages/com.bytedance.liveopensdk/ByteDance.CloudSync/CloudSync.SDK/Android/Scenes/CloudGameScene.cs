using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace ByteDance.CloudSync.CloudGameAndroid
{
    public abstract class CloudGameScene
    {
        protected AndroidJavaObject SceneJavaObject { get; private set; }
        internal CloudGameScene(AndroidJavaObject ajo) 
        {
            SceneJavaObject = ajo;
            
        }
        ~CloudGameScene() 
        {
            SceneJavaObject.Dispose();
        }


    }
}
