using Isa95DataGenerator.Models;
using Microsoft.Extensions.Logging;

namespace Isa95DataGenerator.Services;

public interface IWorkOrderOrchestrator
{
    /// <summary>
    /// Called every slow tick (60 s). Creates new work orders, tracks in-progress orders,
    /// and closes completed ones with material actuals and quality test results.
    /// </summary>
    IReadOnlyList<TelemetryMessage> ProcessTick();
}

/// <summary>
/// Orchestrates the ISA-95 production lifecycle per WorkCenter:
///   WorkRequest → (N ticks) → WorkResponse + MaterialActual + QualityTestResult
/// </summary>
public class WorkOrderOrchestrator : IWorkOrderOrchestrator
{
    private sealed record QualitySpec(
        string SpecId,
        string Parameter,
        double LowerLimit,
        double UpperLimit,
        string UnitOfMeasure);

    private sealed record ActiveOrder(
        string RequestId,
        string ResponseId,
        string WorkCenterId,
        string ProductId,
        double QuantityRequested,
        string LotId,
        DateTime ScheduledStart,
        DateTime ScheduledEnd,
        DateTime ActualStart,
        int TicksRemaining);

    private readonly IScenarioController _scenario;
    private readonly ILogger<WorkOrderOrchestrator> _logger;
    private readonly Random _rng = new();
    private readonly Dictionary<string, ActiveOrder> _active = [];
    private readonly object _lock = new();
    private int _orderSeq;
    private int _responseSeq;
    private int _testSeq;
    private int _lotSeq;

    public WorkOrderOrchestrator(IScenarioController scenario, ILogger<WorkOrderOrchestrator> logger)
    {
        _scenario = scenario;
        _logger = logger;
    }

    public IReadOnlyList<TelemetryMessage> ProcessTick()
    {
        var messages = new List<TelemetryMessage>();
        var now = DateTime.UtcNow;

        lock (_lock)
        {
            foreach (var site in DemoPlant.Instance.Sites)
            foreach (var area in site.Areas)
            foreach (var wc in area.WorkCenters)
            {
                if (wc.Products.Count == 0) continue; // quality area has no production orders

                if (_active.TryGetValue(wc.WorkCenterId, out var order))
                {
                    var updated = order with { TicksRemaining = order.TicksRemaining - 1 };
                    _active[wc.WorkCenterId] = updated;

                    if (updated.TicksRemaining <= 0)
                    {
                        messages.AddRange(CompleteOrder(updated, wc, now));
                        _active.Remove(wc.WorkCenterId);
                    }
                }
                else if (_rng.NextDouble() < 0.65) // 65% chance to start a new order each tick
                {
                    var (newOrder, msgs) = CreateOrder(wc, now);
                    _active[wc.WorkCenterId] = newOrder;
                    messages.AddRange(msgs);
                }
            }
        }

        return messages;
    }

    // ── Order Creation ────────────────────────────────────────────────────────

    (ActiveOrder order, IReadOnlyList<TelemetryMessage> messages) CreateOrder(WorkCenter wc, DateTime now)
    {
        var product   = wc.Products[_rng.Next(wc.Products.Count)];
        var qty       = Math.Round(_rng.NextDouble() * 40 + 10); // 10–50 units
        var requestId = $"WR-{now:yyyyMMdd}-{Interlocked.Increment(ref _orderSeq):D4}";
        var responseId= $"WRS-{now:yyyyMMdd}-{Interlocked.Increment(ref _responseSeq):D4}";
        var lotId     = $"LOT-{now:yyyyMMdd}-{Interlocked.Increment(ref _lotSeq):D4}";

        // Cycle ticks = StandardCycleTimeSec / 60 s per slow tick (min 1)
        var ticksNeeded   = Math.Max(1, (int)Math.Round(product.StandardCycleTimeSec / 60.0));
        var scheduledEnd  = now.AddSeconds(product.StandardCycleTimeSec);
        var primaryWu     = wc.WorkUnits[0];

        var order = new ActiveOrder(
            requestId, responseId, wc.WorkCenterId, product.ProductId,
            qty, lotId, now, scheduledEnd, now, ticksNeeded);

        var messages = new List<TelemetryMessage>
        {
            // ISA-95 Work Request
            new()
            {
                Timestamp  = now,
                WorkUnitId = primaryWu.WorkUnitId,
                Signal     = "WorkRequest",
                Value      = 0,
                Payload    = new WorkRequestPayload(
                    requestId, wc.WorkCenterId, product.ProductId,
                    qty, product.UnitOfMeasure, Priority: 1,
                    now, scheduledEnd, Status: "Active", CreatedAt: now)
            },
            // ISA-95 Material Actual — raw material consumed at start
            new()
            {
                Timestamp  = now,
                WorkUnitId = primaryWu.WorkUnitId,
                Signal     = "MaterialActual",
                Value      = 0,
                Payload    = new MaterialActualPayload(
                    lotId, product.MaterialId, wc.WorkCenterId, requestId,
                    Direction: "Consumed", Quantity: qty * 1.02, product.UnitOfMeasure) // 2% over-issue
            }
        };

        _logger.LogInformation("Created WR {requestId}: {product} qty={qty} at {wc} ({ticks} ticks)",
            requestId, product.ProductId, qty, wc.WorkCenterId, ticksNeeded);

        return (order, messages);
    }

    // ── Order Completion ──────────────────────────────────────────────────────

    IReadOnlyList<TelemetryMessage> CompleteOrder(ActiveOrder order, WorkCenter wc, DateTime now)
    {
        var scrapRate = _scenario.ShouldForceScrap(order.ProductId)
            ? _rng.NextDouble() * 0.15 + 0.10   // 10–25% scrap in QualityExcursion
            : _rng.NextDouble() * 0.04;           // 0–4% normal scrap

        var produced    = Math.Round(order.QuantityRequested * (1 - scrapRate));
        var rejected    = order.QuantityRequested - produced;
        var primaryWu   = wc.WorkUnits[0];
        var productDef  = wc.Products.FirstOrDefault(p => p.ProductId == order.ProductId);
        var uom         = productDef?.UnitOfMeasure ?? "EA";

        var messages = new List<TelemetryMessage>
        {
            // ISA-95 Work Response
            new()
            {
                Timestamp  = now,
                WorkUnitId = primaryWu.WorkUnitId,
                Signal     = "WorkResponse",
                Value      = 0,
                Payload    = new WorkResponsePayload(
                    order.ResponseId, order.RequestId, order.WorkCenterId,
                    order.ActualStart, now, produced, rejected, Status: "Completed", CompletedAt: now)
            },
            // ISA-95 Material Actual — finished goods produced at end
            new()
            {
                Timestamp  = now,
                WorkUnitId = primaryWu.WorkUnitId,
                Signal     = "MaterialActual",
                Value      = 0,
                Payload    = new MaterialActualPayload(
                    order.LotId, order.ProductId, order.WorkCenterId, order.RequestId,
                    Direction: "Produced", Quantity: produced, UnitOfMeasure: uom)
            }
        };

        // ISA-95 Quality Test Results — one per spec, measured on the CMM
        var qualityWuId = FindCmmWorkUnitId() ?? primaryWu.WorkUnitId;
        foreach (var spec in GetSpecsForProduct(order.ProductId))
        {
            var testId  = $"QT-{now:yyyyMMdd}-{Interlocked.Increment(ref _testSeq):D5}";
            var range   = spec.UpperLimit - spec.LowerLimit;
            var inSpec  = spec.LowerLimit + _rng.NextDouble() * range;

            // Force out-of-tolerance in excursion scenario (35% of specs per batch)
            var forceOot = _scenario.ShouldForceScrap(order.ProductId) && _rng.NextDouble() < 0.35;
            var measured = Math.Round(forceOot ? spec.UpperLimit + _rng.NextDouble() * range * 0.15 : inSpec, 4);
            var pass     = measured >= spec.LowerLimit && measured <= spec.UpperLimit;

            messages.Add(new TelemetryMessage
            {
                Timestamp  = now,
                WorkUnitId = qualityWuId,
                Signal     = "QualityTest",
                Value      = measured,
                Payload    = new QualityTestPayload(
                    testId, qualityWuId, order.ResponseId, order.LotId,
                    spec.SpecId, spec.Parameter,
                    measured, spec.LowerLimit, spec.UpperLimit, spec.UnitOfMeasure,
                    Result:   pass ? "Pass" : "Fail",
                    Severity: pass ? "None" : (forceOot ? "Major" : "Minor"))
            });
        }

        _logger.LogInformation(
            "Completed WRS {responseId}: produced={produced} rejected={rejected} scrap={scrap:P1}",
            order.ResponseId, produced, rejected, scrapRate);

        return messages;
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    static string? FindCmmWorkUnitId()
    {
        foreach (var site in DemoPlant.Instance.Sites)
        foreach (var area in site.Areas)
        foreach (var wc in area.WorkCenters)
        foreach (var wu in wc.WorkUnits)
            if (wu.MachineType == "CMM")
                return wu.WorkUnitId;
        return null;
    }

    static IReadOnlyList<QualitySpec> GetSpecsForProduct(string productId) => productId switch
    {
        "PROD-CRANK-7B" =>
        [
            new("SPEC-CRANK7B-DIAM",  "Diameter.Main",  49.75, 50.00, "mm"),
            new("SPEC-CRANK7B-ROUGH", "Roughness.Main",  0.00,  0.80, "µm Ra"),
            new("SPEC-CRANK7B-RUNOUT","Runout.Total",    0.00,  0.05, "mm")
        ],
        "PROD-CRANK-5A" =>
        [
            new("SPEC-CRANK5A-DIAM",  "Diameter.Main",  44.80, 45.00, "mm"),
            new("SPEC-CRANK5A-ROUGH", "Roughness.Main",  0.00,  0.60, "µm Ra")
        ],
        "PROD-PISTON-X2" =>
        [
            new("SPEC-PISTONX2-DIAM", "Diameter.Pin",   34.98, 35.00, "mm"),
            new("SPEC-PISTONX2-HGT",  "Height.Crown",   48.90, 49.10, "mm")
        ],
        _ => []
    };
}
