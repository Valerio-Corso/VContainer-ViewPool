using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.Pool;
using VContainer.Unity;

namespace BashoKit.Pooling {
    public interface IViewPool {
        /// <summary>Called before returning the object to the pool—perfect place to reset state.</summary>
        void OnReturn();

        /// <summary>Called immediately after retrieving from the pool.</summary>
        void OnRetrieve() { }
    }

    [Flags]
    public enum PoolReleaseStrategy {
        None = 0,
        DisableObject = 1 << 0,
        MoveOutOfBounds = 1 << 1,
        ReparentToPool = 1 << 2,
        
        /// UI elements are usually disabled and reparented to the pool root
        DefaultUIElement = DisableObject | ReparentToPool,
        /// World entities are usually moved out of bounds and not disabled
        DefaultWorldEntity = MoveOutOfBounds,
    }

    [Flags]
    public enum PoolRetrieveStrategy {
        None = 0,
        ActivateObject = 1 << 0,
        Reparent = 1 << 1,
        MoveToCoord = 1 << 2,
        
        /// UI elements are usually enabled and reparented to the given transform
        DefaultUIElement = ActivateObject | Reparent,
        /// World entities are usually moved from out of bounds and not enabled as they are already active
        DefaultWorldEntity = MoveToCoord,
    }
    
    public struct ViewPoolConfig {
        public int InitialSize { get; }
        public string ViewPoolTransformName  { get; }
        public PoolReleaseStrategy ReleaseStrategy  { get; }
        public PoolRetrieveStrategy RetrieveStrategy  { get; }
        public Vector3 OutOfBoundsOffset  { get; }

        /// <summary>
        /// Millisecond budget per frame for async prewarm, use 0 to disable prewarm.
        /// </summary>
        public float PrewarmBudgetMs  { get; }

        public ViewPoolConfig(
            int initialSize,
            string viewPoolTransformName,
            PoolReleaseStrategy releaseStrategy,
            PoolRetrieveStrategy retrieveStrategy,
            float prewarmBudgetMs = 5f,
            Vector3? outOfBoundsOffset = null
        ) {
            InitialSize = initialSize;
            ViewPoolTransformName = viewPoolTransformName;
            ReleaseStrategy = releaseStrategy;
            RetrieveStrategy = retrieveStrategy;
            PrewarmBudgetMs = prewarmBudgetMs;
            OutOfBoundsOffset = outOfBoundsOffset ?? new Vector3(1000f, 1000f, 1000f);
        }
    }

    public interface IViewPool<TView> where TView : IViewPool {
        /// Retrieve a view with optional parent.
        /// Behavior is controlled by RetrieveStrategy flags.
        TView Get(Transform parent);

        TView Get(Vector3 worldPosition);

        /// <summary>Return one view to the pool.</summary>
        void Release(TView view);

        /// <summary>Return multiple views to the pool.</summary>
        void Release(IEnumerable<TView> views);

        /// <summary>Pre-warm if configured for async.</summary>
        void PostInitialize();
    }

    public class ViewPool<TView> : IViewPool<TView>, IPostInitializable
        where TView : MonoBehaviour, IViewPool {
        private readonly ViewPoolConfig _config;
        private readonly Func<Transform, TView> _viewFactory;
        private readonly ObjectPool<TView> _viewPool;
        private readonly Transform _poolRoot;

        public ViewPool(ViewPoolConfig config, Func<Transform, TView> viewFactory) {
            _config = config;
            _viewFactory = viewFactory ?? throw new ArgumentNullException(nameof(viewFactory));
            var go = new GameObject(_config.ViewPoolTransformName);
            _poolRoot = go.transform;

            _viewPool = new ObjectPool<TView>(
                createFunc: CreateView,
                actionOnRelease: OnRelease,
                actionOnDestroy: OnDestroyPooledObject,
                defaultCapacity: _config.InitialSize
            );
        }

        public void PostInitialize() {
            if (_config.PrewarmBudgetMs <= 0f) {
                for (int i = 0; i < _config.InitialSize; i++) {
                    var v = _viewPool.Get();
                    _viewPool.Release(v);
                }
            }
            else {
                CoroutineRunner.Instance.StartCoroutine(PrewarmCoroutine());
            }
        }


        private IEnumerator PrewarmCoroutine()
        {
            var buffer = new TView[_config.InitialSize];
            var spawned = 0;
            var budgetSec = _config.PrewarmBudgetMs / 1000f;

            while (spawned < _config.InitialSize)
            {
                var start = Time.realtimeSinceStartup;
                while (spawned < _config.InitialSize && Time.realtimeSinceStartup - start < budgetSec) {
                    buffer[spawned++] = Get(_config.OutOfBoundsOffset);
                }
                
                yield return null; 
            }

            // release
            yield return null;
            for (int i = 0; i < spawned; i++)
                _viewPool.Release(buffer[i]);
        }

        
        public TView Get(Transform parent = null) => InternalGet(parent, null);

        public TView Get(Vector3 worldPosition) => InternalGet(null, worldPosition);

        private TView InternalGet(Transform parent, Vector3? worldPosition) {
            var view = _viewPool.Get();
            var t = view.transform;

            // Handle reparenting
            if (_config.RetrieveStrategy.HasFlag(PoolRetrieveStrategy.Reparent)) {
                t.SetParent(parent ?? _poolRoot, worldPositionStays: false);
                t.localRotation = Quaternion.identity;
            }
            
            if (_config.RetrieveStrategy.HasFlag(PoolRetrieveStrategy.MoveToCoord)) {
                if (worldPosition.HasValue) t.position = worldPosition.Value;
                t.localRotation = Quaternion.identity;
            }
            
            if (_config.RetrieveStrategy.HasFlag(PoolRetrieveStrategy.ActivateObject)) {
                view.gameObject.SetActive(true);
            }

            view.OnRetrieve();
            return view;
        }

        public void Release(TView view) {
            if (view == null) throw new ArgumentNullException(nameof(view));
            _viewPool.Release(view);
        }

        public void Release(IEnumerable<TView> views) {
            if (views == null) throw new ArgumentNullException(nameof(views));
            foreach (var v in views)
                Release(v);
        }

        private TView CreateView()
            => _viewFactory(_poolRoot);

        private void OnRelease(TView view) {
            view.OnReturn();
            
            var transform = view.transform;
            if (_config.ReleaseStrategy.HasFlag(PoolReleaseStrategy.ReparentToPool)) {
                transform.SetParent(_poolRoot, worldPositionStays: false);
                transform.localRotation = Quaternion.identity;
            }
            
            if (_config.ReleaseStrategy.HasFlag(PoolReleaseStrategy.DisableObject)) {
                view.gameObject.SetActive(false);
            }
            
            if (_config.ReleaseStrategy.HasFlag(PoolReleaseStrategy.MoveOutOfBounds)) {
                transform.position = _config.OutOfBoundsOffset;
            }
        }

        private void OnDestroyPooledObject(TView view) {
            if (view) UnityEngine.Object.Destroy(view.gameObject);
        }
    }

    /// <summary>
    /// Helper MonoBehaviour to run coroutines for non-MonoBehaviour classes.
    /// </summary>
    public class CoroutineRunner : MonoBehaviour {
        private static CoroutineRunner _instance;

        public static CoroutineRunner Instance {
            get {
                if (_instance == null) {
                    var go = new GameObject("ViewPool.CoroutineRunner");
                    DontDestroyOnLoad(go);
                    _instance = go.AddComponent<CoroutineRunner>();
                }

                return _instance;
            }
        }
    }
}