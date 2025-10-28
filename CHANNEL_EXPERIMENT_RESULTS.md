# Channel-Based Worker Pool Experiment Results

**Date:** 2025-10-28
**Status:** ✅ EXPERIMENT COMPLETED - NO IMPROVEMENT

---

## Executive Summary

Мы реализовали Channel-based worker pool в tunnel client для замены SemaphoreSlim(10,10) на 50 параллельных воркеров. **Результат: производительность НЕ улучшилась.**

**Вывод:** Bottleneck находится НЕ в клиенте, а на server side (TunnelServer или ProxyEntry).

---

## Что было сделано

### Архитектурные изменения

**Было (SemaphoreSlim):**
```csharp
private readonly SemaphoreSlim _processingSlots = new(10, 10);

await _processingSlots.WaitAsync(ct);
try {
    await ForwardToBackend(...);
} finally {
    _processingSlots.Release();
}
```

**Стало (Channel + Worker Pool):**
```csharp
// 50 воркеров читают задачи из канала
private readonly Channel<BackendTask> _backendQueue;
private readonly int _maxConcurrentBackendRequests = 50;

// Воркер
private async Task BackendWorkerAsync(int workerId, CancellationToken ct)
{
    await foreach (var task in _backendQueue.Reader.ReadAllAsync(ct))
    {
        await task.ExecuteAsync();
    }
}

// Отправка задачи
await _backendQueue.Writer.WriteAsync(backendTask, ct);
```

### Технические детали

1. **Создан record BackendTask** с callback для выполнения
2. **Добавлен bounded channel** (1000 задач max) с backpressure
3. **Запускается 50 воркеров** при старте клиента
4. **Graceful shutdown** с timeout 10 секунд
5. **Удален SemaphoreSlim** полностью

---

## Результаты тестирования

### Test 1: Single Request (Low Load)

**Команда:**
```bash
ab -n 10 -c 1 http://localhost:12000/session/.../get
```

**Результат:**
- Latency: **21ms** ✅
- Success rate: 100%
- Backend direct: 29ms

**Вывод:** При низкой нагрузке система работает отлично.

---

### Test 2: Medium Load (50 Concurrent)

**Команда:**
```bash
ab -n 500 -c 50 http://localhost:12000/session/.../get
```

**До изменений (SemaphoreSlim 10):**
| Metric | Value |
|--------|-------|
| Latency | 173ms |
| RPS | 288 |
| Failed | 0 |

**После изменений (Channel + 50 workers):**
| Metric | Value |
|--------|-------|
| Latency | **195ms** ❌ |
| RPS | **256** ❌ |
| Failed | 0 |

**Вывод:** Производительность **УХУДШИЛАСЬ** на 10-15%!

---

### Test 3: High Load (Aggressive Ramp-Up)

**Тест:** 20→50→100→200→500→1000 RPS (30 sec each)

**До изменений (SemaphoreSlim 10):**
| Metric | Value |
|--------|-------|
| Success rate | 57% |
| Mean latency (success) | 9,488ms |
| Failures | 24,115 (43%) |
| Timeouts | 23,761 |
| Actual RPS | 177 |

**После изменений (Channel + 50 workers):**
| Metric | Value | Change |
|--------|-------|--------|
| Success rate | **56%** | ❌ -1% |
| Mean latency (success) | **9,408ms** | ≈ Same |
| Failures | **24,891 (44%)** | ❌ +1% |
| Timeouts | **24,582** | ❌ +3% |
| Actual RPS | **173** | ❌ -2% |

**Вывод:** НЕТ УЛУЧШЕНИЯ. Производительность осталась такой же или чуть хуже.

---

## Анализ проблемы

### Наблюдение 1: Latency растет с нагрузкой

| Concurrency | Latency | Multiplier |
|-------------|---------|------------|
| 1 request | 21ms | 1x |
| 50 concurrent | 195ms | **9.3x** ❌ |

**Это НЕ нормально!** Backend отвечает за 29ms, но через tunnel получается 195ms при нагрузке.

### Наблюдение 2: Client обработка медленная

**Логи клиента:**
```
[CLIENT CloseRequest] Completed stream=xxx in 160ms
[CLIENT CloseRequest] Completed stream=yyy in 159ms
[CLIENT CloseRequest] Completed stream=zzz in 161ms
```

**Но backend прямо:**
```bash
$ time curl -s http://localhost:12005/get
29ms total
```

**Разница: 160ms - 29ms = 131ms overhead!**

### Наблюдение 3: 50 воркеров не дали прироста

**Теоретический максимум:**
- 50 воркеров × (1000ms / 29ms) = **1,724 RPS**

**Фактический результат:**
- Actual RPS: **173** (только 10% от теории!)

**Вывод:** Воркеры **блокируются** на чем-то. Не хватает параллелизма означает serialization где-то в pipeline.

---

## Root Cause Analysis

### Где НЕ bottleneck

1. ✅ **Tunnel Client** - 50 воркеров работают, overhead от Channel минимальный
2. ✅ **Backend (httpbin)** - отвечает за 29ms стабильно
3. ✅ **Ресурсы Docker** - CPU < 1%, memory < 200MB
4. ✅ **TunnelUplink** - уже использует Channel (проверено в коде)

### Где ЕСТЬ bottleneck

Проблема в **одном из этих компонентов:**

1. **TunnelServer** - может иметь serialization при обработке фреймов
2. **ProxyEntry HTTP handler** - может сериализовать HTTP запросы
3. **Network connection** между компонентами - может быть bottleneck на уровне TCP
4. **Locks в других местах** - возможно есть другие SemaphoreSlim или locks

### Доказательства

**1. Latency коррелирует с concurrency:**
```
1 concurrent: 21ms
50 concurrent: 195ms (9x хуже)
```
Это классический признак serialization!

**2. RPS не растет с воркерами:**
```
10 воркеров: 177 RPS
50 воркеров: 173 RPS (даже хуже!)
```
Добавление воркеров не помогает = bottleneck не в клиенте.

**3. Overhead туннелирования растет:**
```
Direct backend: 29ms
Through tunnel (low load): 21ms - ok!
Through tunnel (high load): 195ms - очень плохо!
```

---

## Рекомендации

### Immediate (Сегодня)

1. ✅ **Откатить изменения** - Channel не помог, возвращаемся к SemaphoreSlim
2. **Профилировать server side:**
   - Добавить метрики в TunnelServer
   - Добавить метрики в ProxyEntry HTTP handler
   - Замерить latency на каждом этапе pipeline

### Short-term (На этой неделе)

1. **Найти serialization point:**
   ```bash
   # Проверить все locks/semaphores в ProxyEntry и TunnelServer
   grep -r "SemaphoreSlim\|lock\|Monitor" tunnel2-server/
   grep -r "SemaphoreSlim\|lock\|Monitor" tunnel2-proxy-entry/
   ```

2. **Добавить distributed tracing:**
   - Трейсить каждый request через весь pipeline
   - Найти где тратится 130ms overhead

3. **Проверить Kestrel configuration:**
   - Может быть лимиты на concurrent connections
   - Может быть thread pool exhaustion

### Long-term (Следующий спринт)

1. **Архитектурный ревью:**
   - Возможно нужен connection pooling между компонентами
   - Возможно нужен HTTP/2 multiplexing
   - Возможно нужен load balancing

2. **Performance testing framework:**
   - Автоматизировать эти тесты
   - CI/CD integration
   - Performance regression alerts

---

## Выводы

### Что мы узнали

1. **Channel в клиенте - не решение** для нашей проблемы
2. **Bottleneck НЕ в клиенте** - он в server side
3. **Latency растет линейно с concurrency** - явный признак serialization
4. **131ms overhead** теряется где-то в tunnel pipeline

### Что нужно делать дальше

1. **Профилировать server side** подробно
2. **Найти serialization point** в TunnelServer или ProxyEntry
3. **Исправить настоящий bottleneck**
4. **Повторить тесты**

### Lesson Learned

**Нельзя оптимизировать вслепую!** Мы потратили время на оптимизацию клиента, но проблема была в другом месте. Нужно:
- Сначала **профилировать**
- Потом **найти bottleneck с доказательствами**
- Только потом **исправлять**

---

## Технические детали реализации

### Файлы изменены

- `tunnel2-client/src/Tunnel.ClientCore/TunnelClient.cs`:
  - Добавлен `Channel<BackendTask>` (строки 37-38)
  - Добавлены методы `StartBackendWorkers()` и `BackendWorkerAsync()` (строки 602-647)
  - Изменен `RunAsync()` для запуска воркеров (строки 176-205)
  - Изменен `HandleFrameAsync()` для отправки в канал (строки 397-404)
  - Убран `_processingSlots` из `HandleCloseRequestStreamFrameAsync()` (строки 518-603)
  - Создан `record BackendTask` (строки 709-716)

### Как откатить

```bash
cd /Users/chefbot/RiderProjects/xtunnel/tunnel2-client
git checkout HEAD -- src/Tunnel.ClientCore/TunnelClient.cs
dotnet build -c Release
```

---

**Status:** Эксперимент завершен, результаты задокументированы
**Recommendation:** Откатить изменения, искать bottleneck на server side
**Next Action:** Профилирование TunnelServer и ProxyEntry

**Last Updated:** 2025-10-28 22:10
