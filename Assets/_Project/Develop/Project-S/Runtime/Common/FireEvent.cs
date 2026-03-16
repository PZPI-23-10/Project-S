using System;
using System.Collections.Generic;
using UnityEngine;

namespace Project_S.Runtime.Common
{
    public class ResetableFireEvent
    {
        private readonly List<Action> _actions = new();

        private bool _isInvoking;

        public void AddListener(Action action)
        {
            if (Application.isEditor && _actions.Contains(action))
            {
                Debug.LogError("Already contains");
            }

            _actions.Add(action);

            if (Invoked)
            {
                action();
            }
            else
            {
                if (_isInvoking)
                {
                    Debug.LogError("FireEvent error");
                    action();
                }
            }
        }

        public void Invoke()
        {
            if (!Invoked)
            {
                _isInvoking = true;
                if (_actions.Count > 0)
                {
                    foreach (var action in _actions)
                    {
                        action();
                    }
                }

                _isInvoking = false;
                Invoked = true;
            }
        }

        public void RemoveListener(Action action)
        {
            if (_actions.Count > 0)
            {
                _actions.Remove(action);
            }
        }

        private bool Invoked { get; set; }

        public void ResetTotal()
        {
            ResetLaunched();
            _actions.Clear();
        }

        public void ResetLaunched()
        {
            _isInvoking = false;
            Invoked = false;
        }
    }

    public class FireEvent
    {
        private readonly List<Action> _actions = new();

        private bool _isInvoking;

        public void AddListener(Action action)
        {
            if (Invoked)
            {
                action();
            }
            else
            {
                if (_isInvoking)
                {
                    Debug.LogError("FireEvent error");
                    action();
                    return;
                }

                _actions.Add(action);
            }
        }

        public void Invoke()
        {
            if (!Invoked)
            {
                _isInvoking = true;
                if (_actions.Count > 0)
                {
                    foreach (var action in _actions)
                    {
                        try
                        {
                            action();
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e.ToString());
                        }
                    }
                }

                _actions.Clear();
                Invoked = true;
            }
        }

        public void RemoveListener(Action action)
        {
            if (_actions.Count > 0)
            {
                _actions.Remove(action);
            }
        }

        public bool Invoked { get; private set; }
    }
}