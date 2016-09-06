using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace global.loader
{
    public abstract class BaseLoader : IDisposable
    {
        public delegate void LoadedHandler();

        public LoadedHandler onLoader;

        public String url { get; protected set; }

        public BaseLoader(String url)
        {
            this.url = url;
        }
        /// <summary>
        /// 同步加载
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public virtual void loadSync()
        {

        }

        public virtual void loadASync(MonoBehaviour host)
        {

        }

        public virtual IEnumerator starAsyncLoad(String url)
        {
            return null;
        }

        public virtual void Dispose()
        {
            onLoader = null;     
        }
    }
}
