## Description
A basic pool implementation that lets you easily register any Monobehavior as a Unity objectPool.

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
builder.RegisterViewPoolFactory(storageSlotView, new ViewPoolConfig(5,"StorageSlotViewPool"), Lifetime.Singleton);
```
