## Description
A pool implementation that lets you easily register any Monobehavior as a Unity objectPool.
The pool is customizable and has built-in functions such as defining a retrieve/release strategy, defining a prewarming budget, spawning under a specific transform

## Installation
Add to your manifest.json the following
```
"com.valeriocorso.vcontainer-pool": "https://github.com/Valerio-Corso/VContainer-ViewPool.git?path=Assets/VContainerPool#main
```

## Usage
```
using Vcontainer.ObjectPool;

...
[SerializeField] private StorageSlotView storageSlotView;

...
// Helper method that uses a default strategy, toggling the object active state and parenting to a give transform
builder.RegisterUISlotsPool(storageSlotView, 25);

// Helper method that uses a default strategy, keeping it always active and just moving it out of bounds
builder.RegisterWorldEntityPool(storageSlotView, 50);

// Or you could manually define yours
var poolName = "mypool";
var config = new ViewPoolConfig(
    initialSize: 10,
    viewPoolTransformName: poolName,
    releaseStrategy: DisableObject | ReparentToPool,
    retrieveeStrategy: PoolRetrieveStrategy.ActivateObject | PoolRetrieveStrategy.MoveToCoords,
    prewarmBudgetMs: 10f,
    outOfBoundsOffset: new Vector3(10f, 10f, 10f)
);

builder.RegisterViewPoolFactory(prefab, config, lifetime);
```
