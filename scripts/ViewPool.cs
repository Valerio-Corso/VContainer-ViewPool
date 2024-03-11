using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Pool;
using VContainer;
using VContainer.Unity;

namespace Project.Runtime.Core.Factory {
    public interface IViewPool {
        /// Called before returning the object to the pool, perfect place to reset to default state.
        void Reset();
    }

    public struct ViewPoolConfig {
        public int InitialSize;
        public string ViewPoolTransformName;

        public ViewPoolConfig(int initialSize, string viewPoolTransformName) {
            InitialSize = initialSize;
            ViewPoolTransformName = viewPoolTransformName;
        }
    }
    
    public interface IViewPool<TView> where TView : IViewPool {
        /// Optionally parent the object to the given transform
        TView Get(Transform transform = null);
        /// Release back to the pool
        void Release(TView view);
    }
    
    public class ViewPool<TView> : IViewPool<TView>, IPostInitializable where TView : MonoBehaviour, IViewPool {
        [Inject] protected Func<Transform, TView> ViewFactory;
        
        private readonly ViewPoolConfig _config;
        private readonly ObjectPool<TView> _viewPool;
        private readonly Transform _viewPoolTransform;
        
        protected ViewPool(ViewPoolConfig config) {
            _config = config;
            _viewPool = new ObjectPool<TView>(CreateView);
            var gameObject = new GameObject(ViewPoolName);
            _viewPoolTransform = gameObject.GetComponent<Transform>();
        }

        /// How many objects will be instantiated at startup.
        private int InitialSize => _config.InitialSize;

        private string ViewPoolName => _config.ViewPoolTransformName;
        
        public void PostInitialize() {
            var views = new List<TView>(InitialSize);
            for (var i = 0; i < InitialSize; i++) {
                views.Add(_viewPool.Get());
            }
            
            foreach (var view in views) {
                _viewPool.Release(view);
            }
            
            views.Clear();
        }

        public TView CreateView() { 
            return ViewFactory.Invoke(_viewPoolTransform);
        }
        
        public TView Get(Transform transform = null) {
            var view = _viewPool.Get();
            var viewTransform = view.transform;
            if (transform != null) {
                viewTransform.SetParent(transform);
            }
            
            viewTransform.localPosition = Vector3.zero;
            viewTransform.localScale = Vector3.one;
            viewTransform.localRotation = Quaternion.identity;
            view.gameObject.SetActive(true);
            return view;
        }

        public virtual void Release(TView view) {
            view.gameObject.SetActive(false);
            view.Reset();
            view.transform.SetParent(_viewPoolTransform);
            _viewPool.Release(view);
        }
    }
}