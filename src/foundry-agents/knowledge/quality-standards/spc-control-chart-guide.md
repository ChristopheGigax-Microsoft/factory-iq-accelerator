# SPC Control Chart Interpretation Guide

## Purpose
Guide for interpreting Statistical Process Control (SPC) charts to detect process drift, shifts, and out-of-control conditions in manufacturing.

## Control Chart Types

### X-bar and R Charts (Variables Data)
- **X-bar**: Tracks sample means over time
- **R chart**: Tracks sample range (variation within subgroup)
- Use for measurable characteristics (dimensions, weight, temperature)

### p-Chart (Attribute Data)
- Tracks proportion defective in samples
- Use for pass/fail inspection results

## Nelson Rules for Out-of-Control Detection

### Rule 1: Single Point Beyond 3σ
- One point falls outside the upper or lower control limit
- **Interpretation**: Special cause present, immediate investigation required

### Rule 2: Nine Points on Same Side of Center
- Nine consecutive points on one side of the center line
- **Interpretation**: Process shift, likely assignable cause (tool wear, material change)

### Rule 3: Six Points Trending
- Six consecutive points steadily increasing or decreasing
- **Interpretation**: Drift in process (e.g., tool wear, temperature drift, material degradation)

### Rule 4: Fourteen Points Alternating
- Fourteen consecutive points alternating up and down
- **Interpretation**: Two process streams mixed, or over-adjustment

### Rule 5: Two of Three Points Beyond 2σ
- Two out of three consecutive points beyond 2σ (same side)
- **Interpretation**: Early warning of shift, increased monitoring needed

## Process Capability Indices

| Index | Formula | Target |
|-------|---------|--------|
| Cp | (USL - LSL) / 6σ | ≥ 1.33 |
| Cpk | min(Cpu, Cpl) | ≥ 1.33 |
| Pp | (USL - LSL) / 6s | ≥ 1.33 |
| Ppk | min(Ppu, Ppl) | ≥ 1.33 |

## Defect Investigation Workflow

1. **Detect**: SPC chart signals out-of-control condition
2. **Contain**: Quarantine suspect production since last known good
3. **Investigate**: 
   - Check 5M1E (Man, Machine, Material, Method, Measurement, Environment)
   - Cross-reference with process parameter logs
   - Review material batch records
4. **Correct**: Implement immediate fix
5. **Prevent**: Root cause analysis → corrective action → update control plan

## Common Manufacturing Defect Categories

| Category | Examples | Typical Root Causes |
|----------|----------|-------------------|
| Dimensional | Out-of-tolerance, warping | Tool wear, thermal expansion, material shrinkage |
| Surface | Scratches, marks, discoloration | Die wear, contamination, handling damage |
| Structural | Voids, inclusions, delamination | Material issues, process parameters, moisture |
| Functional | Leak, weak joint, poor adhesion | Process parameters, material compatibility |
