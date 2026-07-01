# Alarm Correlation Guide: Extrusion Line

## Purpose
This guide documents known alarm correlation patterns for the extrusion line to support rapid root cause identification.

## Common Alarm Cascades

### Pattern 1: Thermal Runaway
**Trigger**: Barrel temperature zone alarm (high)
**Cascade sequence**:
1. Zone temperature high alarm (T > setpoint + 15°C)
2. Melt pressure high alarm (within 30 seconds)
3. Motor current spike alarm
4. Product quality deviation (dimensional out-of-spec)

**Root Cause**: Usually heater band failure (stuck ON) or thermocouple break
**Action**: Immediate shutdown, inspect heater bands and TC connections

### Pattern 2: Material Starvation
**Trigger**: Hopper low-level alarm
**Cascade sequence**:
1. Hopper level low
2. Screw torque decrease (within 60 seconds)
3. Melt pressure low
4. Output rate deviation
5. Product weight underspec

**Root Cause**: Material supply interruption, bridging in hopper, or metering feeder fault
**Action**: Check material supply, clear any bridging, verify feeder operation

### Pattern 3: Die Pressure Build-up
**Trigger**: Die pressure high alarm
**Cascade sequence**:
1. Die pressure gradual increase over 2–4 hours
2. Motor current increase
3. Melt temperature increase (viscous heating)
4. Product surface quality deterioration

**Root Cause**: Die fouling, degraded material buildup, or screen pack blockage
**Action**: Schedule die cleaning, replace screen pack, check material quality

## Alarm Priority Matrix

| Severity | Response Time | Escalation |
|----------|--------------|------------|
| Critical | Immediate stop | Shift supervisor + Maintenance lead |
| High | 15 minutes | Maintenance technician |
| Medium | End of shift | Plan for next PM window |
| Low | Next PM cycle | Log and trend |

## Correlation Time Windows
- Thermal events: 30-second correlation window
- Mechanical events: 60-second correlation window
- Quality drift: 5-minute rolling average
- Gradual degradation: 4-hour trend analysis
