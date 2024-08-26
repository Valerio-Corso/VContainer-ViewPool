using System;
using UnityEngine;
using UnityEngine.Pool;
using VContainer.Unity;

namespace VContainer.ObjectPool {
    public interface IViewPool {
        /// Called before returning the object to the pool, perfect place to reset to default state.
        void Reset();
    }

    public struct ViewPoolConfig {
        public readonly int InitialSize;
        public readonly string ViewPoolTransformName;

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
        private readonly ViewPoolConfig _config;
        private readonly Func<Transform, TView> _viewFactory;
        private readonly ObjectPool<TView> _viewPool;
        private readonly Transform _viewPoolTransform;

        protected ViewPool(ViewPoolConfig config, Func<Transform, TView> viewFactory) {
            _config = config;
            _viewFactory = viewFactory;

            var gameObject = new GameObject(ViewPoolName);
            _viewPoolTransform = gameObject.GetComponent<Transform>();
            _viewPool = new ObjectPool<TView>(CreateView, actionOnRelease: OnRelease, actionOnDestroy: OnDestroyObject, defaultCapacity: InitialSize);
        }

        /// How many objects will be instantiated at startup.
        private int InitialSize => _config.InitialSize;
        private string ViewPoolName => _config.ViewPoolTransformName;

        public void PostInitialize() {
            // Pre-warm the pool by creating and releasing initial objects
            var tmpPool = ListPool<TView>.Get();
            for (var i = 0; i < InitialSize; i++) {
                var view = _viewPool.Get();
                tmpPool.Add(view);
            }
            
            foreach (var tmpItem in tmpPool) {
                _viewPool.Release(tmpItem);
            }
            
            ListPool<TView>.Release(tmpPool);
        }
        
        public TView Get(Transform transform = null) {
            var view = _viewPool.Get();
            var viewTransform = view.transform;
            if (transform != null) {
                viewTransform.SetParent(transform, false);
            }
            
            viewTransform.localPosition = Vector3.zero;
            viewTransform.localRotation = Quaternion.identity;
            view.gameObject.SetActive(true);
            
            return view;
        }

        public virtual void Release(TView view) {
            _viewPool.Release(view);
        }

        private TView CreateView() {
            return _viewFactory.Invoke(_viewPoolTransform);
        }

        private void OnRelease(TView view) {
            // Reset the view's state and set inactive before returning to pool
            view.gameObject.SetActive(false);
            view.Reset();
            var viewTransform = view.transform;
            viewTransform.SetParent(_viewPoolTransform, false);
            viewTransform.localPosition = Vector3.zero;
            viewTransform.localRotation = Quaternion.identity;
        }

        private void OnDestroyObject(TView view) {
            UnityEngine.Object.Destroy(view.gameObject);
        }
    }
}