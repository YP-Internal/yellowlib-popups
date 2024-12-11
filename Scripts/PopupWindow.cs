using System;
using UnityEngine;

namespace YellowPanda.Popup
{
    public class PopupWindow : MonoBehaviour
    {
        protected bool isOpen;
        protected bool inScene;

        private Animator _animator;

        public EventHandler OnOpen;
        public EventHandler OnClose;

        protected virtual void Awake()
        {
            _animator = GetComponent<Animator>();
            inScene = true;
        }

        public void OpenPopup()
        {
            _animator.Play("Open", 0, 0);
            OnOpen?.Invoke(this, null);
            isOpen = true;
        }

        public void ClosePopup()
        {
            _animator.Play("Close", 0, 0);
            OnClose?.Invoke(this, null);
            isOpen = false;
        }

        public bool IsOpen()
        {
            return isOpen;
        }

        public bool InScene()
        {
            return inScene;
        }

        public virtual void OnBeginShow() { }
        public virtual void OnEndShow() { }
        public virtual void OnBeginClose() { }
        public virtual void OnEndClose()
        {
            inScene = false;
        }

        public void OpenExternalLink(string link)
        {
            Application.OpenURL(link);
        }

    }

}