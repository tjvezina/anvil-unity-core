﻿using System;
using Anvil.CSharp.Core;
using Anvil.CSharp.DelayedExecution;
using Anvil.Unity.DelayedExecution;
using UnityEngine;

namespace Anvil.Unity.Content
{
    public class ContentGroup : AbstractAnvilDisposable
    {
        public event Action<AbstractContentController> OnLoadStart;
        public event Action<AbstractContentController> OnLoadComplete;
        public event Action<AbstractContentController> OnPlayInStart;
        public event Action<AbstractContentController> OnPlayInComplete;
        public event Action<AbstractContentController> OnPlayOutStart;
        public event Action<AbstractContentController> OnPlayOutComplete;


        public readonly string ID;
        public Transform ContentGroupRoot { get; private set; }
        
        public ContentManager ContentManager { get; private set; }
        
        public AbstractContentController ActiveContentController { get; private set; }
        private AbstractContentController m_PendingContentController;

        private UpdateHandle m_UpdateHandle;
        
        //TODO: Snippet about the gameObjectRoot. To be cleaned up when docs pass happens on this class.
        // /// <summary>
        // /// A custom user supplied <see cref="GameObject"/> <see cref="Transform"/> to parent this
        // /// <see cref="ContentGroup"/> to. If left null (the default), the <see cref="ContentGroup"/> will be parented
        // /// to the <see cref="ContentManager"/>'s ContentRoot.
        // /// </summary>

        public ContentGroup(ContentManager contentManager, string id, Vector3 localPosition, Transform gameObjectRoot = null)
        {
            ContentManager = contentManager;
            ID = id;

            m_UpdateHandle = UpdateHandle.Create<UnityUpdateSource>();

            InitGameObject(localPosition, gameObjectRoot);
        }

        protected override void DisposeSelf()
        {
            if (m_UpdateHandle != null)
            {
                m_UpdateHandle.Dispose();
                m_UpdateHandle = null;
            }
            base.DisposeSelf();
        }

        private void InitGameObject(Vector3 localPosition, Transform gameObjectRoot)
        {
            GameObject groupRootGO = new GameObject($"[CL - {ID}]");
            ContentGroupRoot = groupRootGO.transform;
            Transform parent = gameObjectRoot == null
                ? ContentManager.ContentRoot
                : gameObjectRoot;
            ContentGroupRoot.SetParent(parent);
            ContentGroupRoot.localPosition = localPosition;
            ContentGroupRoot.localRotation = Quaternion.identity;
            ContentGroupRoot.localScale = Vector3.one;
        }

        public void Show(AbstractContentController contentController)
        {
            //TODO: Validate the passed in controller to ensure we avoid weird cases such as:
            // - Showing the same instance that is already showing or about to be shown
            // - Might have a pending controller in the process of loading
            
            m_PendingContentController = contentController;

            //If there's an Active Controller currently being shown, we need to clear it.
            if (ActiveContentController != null)
            {
                OnPlayOutStart?.Invoke(ActiveContentController);
                ActiveContentController.InternalPlayOut();
            }
            else
            {
                //Otherwise we can just show the pending controller
                ShowPendingContentController();
            }
        }

        public void Clear()
        {
            Show(null);
        }

        private void ShowPendingContentController()
        {
            if (m_PendingContentController == null)
            {
                return;
            }
            //We can't show the pending controller right away because we may not have the necessary assets loaded. 
            //So we need to construct a Sequential Command and populate with the required commands to load the assets needed. 
            
            
            ActiveContentController = m_PendingContentController;
            m_PendingContentController = null;
            ActiveContentController.ContentGroup = this;

            OnLoadStart?.Invoke(ActiveContentController);
            ActiveContentController.OnLoadComplete += HandleOnLoadComplete;
            ActiveContentController.Load();
        }

        private void HandleOnLoadComplete()
        {
            ActiveContentController.OnLoadComplete -= HandleOnLoadComplete;
            OnLoadComplete?.Invoke(ActiveContentController);
            
            ActiveContentController.InternalInitAfterLoadComplete();

            AbstractContent content = ActiveContentController.GetContent<AbstractContent>();
            Transform transform = content.transform;
            transform.SetParent(ContentGroupRoot);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one;

            AttachLifeCycleListeners(ActiveContentController);
            OnPlayInStart?.Invoke(ActiveContentController);
            ActiveContentController.InternalPlayIn();
        }

        private void AttachLifeCycleListeners(AbstractContentController contentController)
        {
            contentController.OnPlayInComplete += HandleOnPlayInComplete;
            contentController.OnPlayOutComplete += HandleOnPlayOutComplete;
            contentController.OnClear += HandleOnClear;
        }
        
        private void RemoveLifeCycleListeners(AbstractContentController contentController)
        {
            contentController.OnPlayInComplete -= HandleOnPlayInComplete;
            contentController.OnPlayOutComplete -= HandleOnPlayOutComplete;
            contentController.OnClear -= HandleOnClear;
        }

        private void HandleOnPlayInComplete()
        {
            ActiveContentController.OnPlayInComplete -= HandleOnPlayInComplete;
            OnPlayInComplete?.Invoke(ActiveContentController);

            ActiveContentController.InternalInitAfterPlayInComplete();
        }

        private void HandleOnPlayOutComplete()
        {
            if (ActiveContentController != null)
            {
                OnPlayOutComplete?.Invoke(ActiveContentController);
                RemoveLifeCycleListeners(ActiveContentController);
                ActiveContentController.Dispose();
                ActiveContentController = null;
            }

            ShowPendingContentController();
        }

        private void HandleOnClear()
        {
            Clear();
        }

        
        
        
    }
}
