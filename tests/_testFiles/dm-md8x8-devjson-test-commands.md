# DM MD8x8 Chassis Command Reference

Device key: `dm-chassis-1`
Config: `dm-md8x8-test-configurationFile.json`

Verbose logging:
```
APPDEBUG:1 verbose
```

## Slot Map

Inputs (slot key `matrixInput-{n}`):

| In | Key             | Card        | Name              |
|----|-----------------|-------------|-------------------|
| 1  | matrixInput-1   | DMC-HD-DSP  | Wall Plate 1      |
| 2  | matrixInput-2   | DMC-HD-DSP  | Wall Plate 2      |
| 3  | matrixInput-3   | DMC-DVI     | TV Tuner 1        |
| 4  | matrixInput-4   | DMC-DVI     | TV Tuner 2        |
| 5  | matrixInput-5   | DMC-C       | AirMedia          |
| 6  | matrixInput-6   | DMC-C-DSP   | Codec             |
| 7  | matrixInput-7   | DMC-4KZ-C   | DM 3 (DMC-4KZ-C)  |
| 8  | matrixInput-8   | DMC-4KZ-C   | DM 4 (DMC-4KZ-C)  |

Clear input slot key: `none`

Outputs (slot key `matrixOutput-{n}`):

| Out | Key              | Card        | Name                  |
|-----|------------------|-------------|-----------------------|
| 1   | matrixOutput-1   | DMC-CO-HD   | Side Display          |
| 2   | matrixOutput-2   | DMC-CO-HD   | Projector             |
| 3   | matrixOutput-3   | DMC-CO-HD   | Output 3 (DMC-CO-HD)  |
| 4   | matrixOutput-4   | DMC-CO-HD   | Output 4 (DMC-CO-HD)  |

Signal types (`eRoutingSignalType`): `AudioVideo`, `Audio`, `Video`, `Usb`.

> Note: In Essentials v3-routing.21 the separate `UsbInput`/`UsbOutput` values were merged into a single `Usb` value. Passing `UsbInput`/`UsbOutput` returns `Requested value '...' was not found`.

## Numeric Switch (ExecuteNumericSwitch)

`ExecuteNumericSwitch(ushort input, ushort output, eRoutingSignalType type)` — input `0` clears the output.

Route AudioVideo:
```
devjson:1 {"deviceKey":"dm-chassis-1","methodName":"ExecuteNumericSwitch","params":[1,1,"AudioVideo"]}
devjson:1 {"deviceKey":"dm-chassis-1","methodName":"ExecuteNumericSwitch","params":[2,2,"AudioVideo"]}
devjson:1 {"deviceKey":"dm-chassis-1","methodName":"ExecuteNumericSwitch","params":[6,1,"AudioVideo"]}
devjson:1 {"deviceKey":"dm-chassis-1","methodName":"ExecuteNumericSwitch","params":[6,2,"AudioVideo"]}
devjson:1 {"deviceKey":"dm-chassis-1","methodName":"ExecuteNumericSwitch","params":[7,3,"AudioVideo"]}
devjson:1 {"deviceKey":"dm-chassis-1","methodName":"ExecuteNumericSwitch","params":[8,4,"AudioVideo"]}
```

Route Video only:
```
devjson:1 {"deviceKey":"dm-chassis-1","methodName":"ExecuteNumericSwitch","params":[3,1,"Video"]}
devjson:1 {"deviceKey":"dm-chassis-1","methodName":"ExecuteNumericSwitch","params":[4,2,"Video"]}
```

Route Audio only (breakaway):
```
devjson:1 {"deviceKey":"dm-chassis-1","methodName":"ExecuteNumericSwitch","params":[5,1,"Audio"]}
devjson:1 {"deviceKey":"dm-chassis-1","methodName":"ExecuteNumericSwitch","params":[6,2,"Audio"]}
```

Route USB (input-side; `Usb` value):
```
devjson:1 {"deviceKey":"dm-chassis-1","methodName":"ExecuteNumericSwitch","params":[1,1,"Usb"]}
devjson:1 {"deviceKey":"dm-chassis-1","methodName":"ExecuteNumericSwitch","params":[5,1,"Usb"]}
```

Clear by routing input 0:
```
devjson:1 {"deviceKey":"dm-chassis-1","methodName":"ExecuteNumericSwitch","params":[0,1,"AudioVideo"]}
devjson:1 {"deviceKey":"dm-chassis-1","methodName":"ExecuteNumericSwitch","params":[0,2,"AudioVideo"]}
devjson:1 {"deviceKey":"dm-chassis-1","methodName":"ExecuteNumericSwitch","params":[0,3,"AudioVideo"]}
devjson:1 {"deviceKey":"dm-chassis-1","methodName":"ExecuteNumericSwitch","params":[0,4,"AudioVideo"]}
```

## Slot-Key Switch (Route)

`Route(string inputSlotKey, string outputSlotKey, eRoutingSignalType type)` — use `none` as input to clear.

```
devjson:1 {"deviceKey":"dm-chassis-1","methodName":"Route","params":["matrixInput-1","matrixOutput-1","AudioVideo"]}
devjson:1 {"deviceKey":"dm-chassis-1","methodName":"Route","params":["matrixInput-2","matrixOutput-2","AudioVideo"]}
devjson:1 {"deviceKey":"dm-chassis-1","methodName":"Route","params":["matrixInput-3","matrixOutput-3","Video"]}
devjson:1 {"deviceKey":"dm-chassis-1","methodName":"Route","params":["matrixInput-4","matrixOutput-4","Video"]}
devjson:1 {"deviceKey":"dm-chassis-1","methodName":"Route","params":["matrixInput-6","matrixOutput-1","Audio"]}
devjson:1 {"deviceKey":"dm-chassis-1","methodName":"Route","params":["matrixInput-7","matrixOutput-3","AudioVideo"]}
devjson:1 {"deviceKey":"dm-chassis-1","methodName":"Route","params":["matrixInput-8","matrixOutput-4","AudioVideo"]}
devjson:1 {"deviceKey":"dm-chassis-1","methodName":"Route","params":["none","matrixOutput-1","AudioVideo"]}
devjson:1 {"deviceKey":"dm-chassis-1","methodName":"Route","params":["none","matrixOutput-2","AudioVideo"]}
devjson:1 {"deviceKey":"dm-chassis-1","methodName":"Route","params":["none","matrixOutput-3","AudioVideo"]}
devjson:1 {"deviceKey":"dm-chassis-1","methodName":"Route","params":["none","matrixOutput-4","AudioVideo"]}
```

## Clearing a Route

`ClearRoute(object outputSelector, ...)` and the object-based `ExecuteSwitch` expect actual
`DMOutput`/`DMInput` port objects, so a plain integer from devjson resolves to `null` and logs
`Unable to execute switch ...`. To clear an output via devjson, use one of the working forms below.

Clear via `ExecuteNumericSwitch` (input `0`):
```
devjson:1 {"deviceKey":"dm-chassis-1","methodName":"ExecuteNumericSwitch","params":[0,1,"AudioVideo"]}
devjson:1 {"deviceKey":"dm-chassis-1","methodName":"ExecuteNumericSwitch","params":[0,2,"AudioVideo"]}
```

Clear via `Route` (input slot key `none`):
```
devjson:1 {"deviceKey":"dm-chassis-1","methodName":"Route","params":["none","matrixOutput-1","AudioVideo"]}
devjson:1 {"deviceKey":"dm-chassis-1","methodName":"Route","params":["none","matrixOutput-2","AudioVideo"]}
```
