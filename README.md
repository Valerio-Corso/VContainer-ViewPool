### WIP
A basic pool implementation that lets you easily register any Monobehavior as a Unity objectPool.

## Usage
```
[SerializeField] private StorageSlotView storageSlotView;
public void Install(IContainerBuilder builder) {
	builder.RegisterViewPoolFactory<StorageSlotView, ViewPool<StorageSlotView>>(storageSlotView, new ViewPoolConfig(5, "StorageSlotViewPool"), Lifetime.Singleton);
}

```
