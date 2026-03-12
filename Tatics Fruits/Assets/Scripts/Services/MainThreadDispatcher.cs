using System;
using System.Collections.Generic;
using UnityEngine;

namespace Services
{
    public class MainThreadDispatcher : MonoBehaviour
    {
        private static readonly Queue<Action> Queue = new Queue<Action>();
        private static MainThreadDispatcher _instance;

        public static void Enqueue(Action action)
        {
            lock (Queue)
                Queue.Enqueue(action);
        }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            lock (Queue)
            {
                while (Queue.Count > 0)
                    Queue.Dequeue().Invoke();
            }
        }
    }
}