# Stress Bench Example

```csharp
// 10K/100K için normal liste:
grid.SetObjects(FakeRows.Create(100_000));

// 1M/1B/1T için doğru yaklaşım:
// Tüm satırı RAM'e alma; ViewGrid virtual provider ile sadece görünen aralığı üret.
var provider = new HugeVirtualProvider(totalRows: 1_000_000_000_000L);
grid.SetVirtualProvider(provider);
```
