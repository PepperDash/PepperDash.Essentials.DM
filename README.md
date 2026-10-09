![PepperDash Essentials Pluign Logo](/images/essentials-plugin-blue.png)

# Essentials Plugin Template (c) 2023

## License

Provided under MIT license

## Overview

Fork this repo when creating a new plugin for Essentials. For more information about plugins, refer to the Essentials Wiki [Plugins](https://github.com/PepperDash/Essentials/wiki/Plugins) article.

This repo contains example classes for the three main categories of devices:
* `EssentialsPluginTemplateDevice`: Used for most third party devices which require communication over a streaming mechanism such as a Com port, TCP/SSh/UDP socket, CEC, etc
* `EssentialsPluginTemplateLogicDevice`:  Used for devices that contain logic, but don't require any communication with third parties outside the program
* `EssentialsPluginTemplateCrestronDevice`:  Used for devices that represent a piece of Crestron hardware

There are matching factory classes for each of the three categories of devices.  The `EssentialsPluginTemplateConfigObject` should be used as a template and modified for any of the categories of device.  Same goes for the `EssentialsPluginTemplateBridgeJoinMap`.

This also illustrates how a plugin can contain multiple devices.

## Cloning Instructions

After forking this repository into your own GitHub space, you can create a new repository using this one as the template.  Then you must install the necessary dependencies as indicated below.

## Dependencies

The [Essentials](https://github.com/PepperDash/Essentials) libraries are required. They referenced via nuget. You must have nuget.exe installed and in the `PATH` environment variable to use the following command. Nuget.exe is available at [nuget.org](https://dist.nuget.org/win-x86-commandline/latest/nuget.exe).

### Installing Dependencies

To install dependencies once nuget.exe is installed, run the following command from the root directory of your repository:
`nuget install .\packages.config -OutputDirectory .\packages -excludeVersion`.
Alternatively, you can simply run the `GetPackages.bat` file.
To verify that the packages installed correctly, open the plugin solution in your repo and make sure that all references are found, then try and build it.

### Installing Different versions of PepperDash Core

If you need a different version of PepperDash Core, use the command `nuget install .\packages.config -OutputDirectory .\packages -excludeVersion -Version {versionToGet}`. Omitting the `-Version` option will pull the version indicated in the packages.config file.

### Instructions for Renaming Solution and Files

See the Task List in Visual Studio for a guide on how to start using the template.  There is extensive inline documentation and examples as well.

For renaming instructions in particular, see the XML `remarks` tags on class definitions

## Build Instructions (PepperDash Internal) 

## Generating Nuget Package 

In the solution folder is a file named "PDT.EssentialsPluginTemplate.nuspec" 

1. Rename the file to match your plugin solution name 
2. Edit the file to include your project specifics including
    1. <id>PepperDash.Essentials.Plugin.MakeModel</id> Convention is to use the prefix "PepperDash.Essentials.Plugin" and include the MakeModel of the device. 
    2. <projectUrl>https://github.com/PepperDash/EssentialsPluginTemplate</projectUrl> Change to your url to the project repo

There is no longer a requirement to adjust workflow files for nuget generation for private and public repositories.  This is now handled automatically in the workflow.

__If you do not make these changes to the nuspec file, the project will not generate a nuget package__
<!-- START Minimum Essentials Framework Versions -->
### Minimum Essentials Framework Versions

- 2.39.0
- 2.39.0
- 2.39.0
- 2.39.0
- 2.39.0
- 2.39.0
- 2.39.0
- 2.39.0
- 2.39.0
- 2.39.0
- 2.39.0
- 2.39.0
<!-- END Minimum Essentials Framework Versions -->
<!-- START Config Example -->
### Config Example

```json
{
    "key": "GeneratedKey",
    "uid": 1,
    "name": "GeneratedName",
    "type": "am300",
    "group": "Group",
    "properties": {
        "control": "SampleValue",
        "volumeControls": {
            "SampleValue": {
                "outLevel": 0,
                "isVolumeControlPoint": true
            }
        },
        "inputSlots": {
            "SampleValue": "SampleString"
        },
        "outputSlots": {
            "SampleValue": "SampleString"
        },
        "inputNames": {
            "SampleValue": "SampleString"
        },
        "outputNames": {
            "SampleValue": "SampleString"
        },
        "noRouteText": "SampleString",
        "inputSlotSupportsHdcp2": {
            "SampleValue": true
        }
    }
}
```
<!-- END Config Example -->
<!-- START Supported Types -->
### Supported Types

- am300
- am200
- am3200
- hdWp4k401c
- hdmd400ce
- hdmd200c1ge
- hdmd300ce
- hdmd200ce
- dmdge200c
- dge100
- hdmd4x24ke
- hdmd6x24ke
- hdmd4x14ke-bridgeable
- hdmd4x14ke
- dmmd64x64
- dmmd8x8rps
- dmmd8x8
- dmmd32x32rps
- dmmd32x32cpu3rps
- dmmd8x8cpu3rps
- dmmd16x16rps
- dmmd128x128
- dmmd8x8cpu3
- dmmd16x16cpu3
- dmmd16x16cpu3rps
- dmmd16x16
- dmmd32x32
- dmmd32x32cpu3
- hdps401
- hdps622
- hdps621
- hdps402
- hdmd8x1
- hdmd8x2
<!-- END Supported Types -->
<!-- START Join Maps -->
### Join Maps

#### Digitals

| Join | Type (RW) | Description |
| --- | --- | --- |
| 1 | R | DGE Online |
| 2 | R | DGE Sync Detected |
| 3 | R | DGE HDMI HDCP State On |
| 4 | R | DGE HDMI HDCP State Off |
| 5 | R | DGE HDMI HDCP State Toggle |

#### Serials

| Join | Type (RW) | Description |
| --- | --- | --- |
| 1 | R | DGE Current Input Resolution |
<!-- END Join Maps -->
<!-- START Interfaces Implemented -->
### Interfaces Implemented

- IRoutingNumericWithFeedback
- IIROutputPorts
- IComPorts
- IHasScreensWithLayouts
- IRouting//
- IRoutingInputsOutputs
- ICec
- IDeviceInfoProvider
- IBasicVolumeWithFeedback
- IRelayPorts
- IHasDmInHdcp
- IBasicVideoMuteWithFeedback
- IRmcRoutingWithFeedback
- IHasHdmiInHdcp
- IHasFeedback
- ITxRoutingWithFeedback
- IHasFreeRun
- IVgaBrightnessContrastControls
- IHasBasicTriListWithSmartObject
- IBridgeAdvanced
- IRouting
- ITxRouting
- IDmSwitchWithEndpointOnlineFeedback
- IMatrixRouting
- IRoutingHasVideoInputSyncFeedbacks
- IRoutingNumeric
- IRoutingInputSlot
- IRoutingOutputSlot
<!-- END Interfaces Implemented -->
<!-- START Base Classes -->
### Base Classes

- CrestronGenericBridgeableBaseDevice
- MessengerBase
- DmHdBaseTControllerBase
- DmRmcX100CController
- DmRmcControllerBase
- BasicDmTxControllerBase
- DmTxControllerBase
- JoinMapBaseAdvanced
- Dge100Controller
- CrestronGenericBaseDevice
- Device
- EssentialsBridgeableDevice
<!-- END Base Classes -->
<!-- START Public Methods -->
### Public Methods

- public void SelectVideoOut(uint source)
- public void SelectPinPointUxLandingPage()
- public void SelectAirMedia()
- public void SelectDmIn()
- public void SelectHdmiIn()
- public void SelectAirboardIn()
- public void RebootDevice()
- public void ExecuteNumericSwitch(ushort input, ushort output, eRoutingSignalType signalType)
- public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType signalType)
- public void DefaultWindowRoutes()
- public void SetWindowLayout(uint layout)
- public void SetWindowLayout(WindowLayout.eLayoutType layout)
- public void ApplyLayout(uint screenId, uint layoutIndex)
- public void AddFeedbackCollections()
- public void AddCollectionsToList(params FeedbackCollection<BoolFeedback>[] newFbs)
- public void AddCollectionsToList(params FeedbackCollection<IntFeedback>[] newFbs)
- public void AddCollectionsToList(params FeedbackCollection<StringFeedback>[] newFbs)
- public void AddCollectionToList(FeedbackCollection<BoolFeedback> newFbs)
- public void AddCollectionToList(FeedbackCollection<IntFeedback> newFbs)
- public void AddCollectionToList(FeedbackCollection<StringFeedback> newFbs)
- public void AddFeedbackToList(PepperDash.Essentials.Core.Feedback newFb)
- public void ExecuteSwitch(object inputSelector)
- public void Select()
- public void SendCurrentLayoutStatus(uint screenId, LayoutInfo layout)
- public void AutoRouteOn()
- public void AutoRouteOff()
- public void PriorityRouteOn()
- public void PriorityRouteOff()
- public void OnScreenDisplayEnable()
- public void OnScreenDisplayDisable()
- public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType signalType)
- public void UpdateDeviceInfo()
- public void MuteOff()
- public void MuteOn()
- public void SetVolume(ushort level)
- public void MuteToggle()
- public void VolumeDown(bool pressRelease)
- public void VolumeUp(bool pressRelease)
- public void SetDmInHdcpState(eHdcpCapabilityType hdcpState)
- public void VideoMuteOn()
- public void VideoMuteOff()
- public void VideoMuteToggle()
- public void MuteOff()
- public void MuteOn()
- public void SetVolume(ushort level)
- public void MuteToggle()
- public void VolumeDown(bool pressRelease)
- public void VolumeUp(bool pressRelease)
- public void SetDmInHdcpState(eHdcpCapabilityType hdcpState)
- public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType signalType)
- public void ExecuteNumericSwitch(ushort inputSelector, ushort outputSelector, eRoutingSignalType signalType)
- public void SetDmInHdcpState(eHdcpCapabilityType hdcpState)
- public void SetHdmiInHdcpState(eHdcpCapabilityType hdcpState)
- public void ExecuteNumericSwitch(ushort input, ushort output, eRoutingSignalType type)
- public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType signalType)
- public void ExecuteNumericSwitch(ushort input, ushort output, eRoutingSignalType type)
- public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType signalType)
- public void SetFreeRunEnabled(bool enable)
- public void SetVgaBrightness(ushort level)
- public void SetVgaContrast(ushort level)
- public void ExecuteNumericSwitch(ushort input, ushort output, eRoutingSignalType type)
- public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType signalType)
- public void ExecuteNumericSwitch(ushort input, ushort output, eRoutingSignalType type)
- public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType signalType)
- public void SetFreeRunEnabled(bool enable)
- public void SetVgaBrightness(ushort level)
- public void SetVgaContrast(ushort level)
- public void SetFreeRunEnabled(bool enable)
- public void SetVgaBrightness(ushort level)
- public void SetVgaContrast(ushort level)
- public void ExecuteNumericSwitch(ushort input, ushort output, eRoutingSignalType type)
- public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType signalType)
- public void SetFreeRunEnabled(bool enable)
- public void SetVgaBrightness(ushort level)
- public void SetVgaContrast(ushort level)
- public void ExecuteNumericSwitch(ushort input, ushort output, eRoutingSignalType type)
- public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType signalType)
- public void ExecuteNumericSwitch(ushort input, ushort output, eRoutingSignalType type)
- public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType signalType)
- public void SetFreeRunEnabled(bool enable)
- public void SetVgaBrightness(ushort level)
- public void SetVgaContrast(ushort level)
- public void ExecuteNumericSwitch(ushort input, ushort output, eRoutingSignalType type)
- public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType signalType)
- public void UpdateDeviceInfo()
- public void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
- public void EnableHdcp(uint port)
- public void DisableHdcp(uint port)
- public void EnableAutoRoute()
- public void DisableAutoRoute()
- public void AddFeedbackCollections()
- public void AddCollectionsToList(params FeedbackCollection<BoolFeedback>[] newFbs)
- public void AddCollectionsToList(params FeedbackCollection<IntFeedback>[] newFbs)
- public void AddCollectionsToList(params FeedbackCollection<StringFeedback>[] newFbs)
- public void AddCollectionToList(FeedbackCollection<BoolFeedback> newFbs)
- public void AddCollectionToList(FeedbackCollection<IntFeedback> newFbs)
- public void AddCollectionToList(FeedbackCollection<StringFeedback> newFbs)
- public void AddFeedbackToList(PepperDash.Essentials.Core.Feedback newFb)
- public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType signalType)
- public void ExecuteNumericSwitch(ushort inputSelector, ushort outputSelector, eRoutingSignalType signalType)
- public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType signalType)
- public void SetPortHdcpCapability(eHdcpCapabilityType hdcpMode, uint port)
- public void AddToFeedbackList(params Feedback[] newFbs)
- public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType signalType)
- public void AddInputBlade(string type, uint number)
- public void AddOutputBlade(string type, uint number)
- public void SetInputHdcpSupport(uint input, ePdtHdcpSupport hdcpSetting)
- public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType sigType)
- public void ExecuteNumericSwitch(ushort inputSelector, ushort outputSelector, eRoutingSignalType sigType)
- public void AddInputCard(string type, uint number)
- public void AddOutputCard(string type, uint number)
- public void SetInputHdcpSupport(uint input, ePdtHdcpSupport hdcpSetting)
- public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType sigType)
- public void ExecuteNumericSwitch(ushort inputSelector, ushort outputSelector, eRoutingSignalType sigType)
- public void Route(string inputSlotKey, string outputSlotKey, eRoutingSignalType type)
- public void ListRoutingPorts()
- public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType signalType)
- public void ExecuteNumericSwitch(ushort inputSelector, ushort outputSelector, eRoutingSignalType signalType)
- public void EnableHdcp(uint port)
- public void DisableHdcp(uint port)
- public void EnableAutoRoute()
- public void DisableAutoRoute()
- public void SetRoutingEnable(bool enable)
- public void AddInputCard(uint number, DMInput inputCard)
- public void AddOutputCard(uint number, DMOutput outputCard)
- public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType sigType)
- public void ExecuteNumericSwitch(ushort inputSelector, ushort outputSelector, eRoutingSignalType sigType)
- public void AddFeedbackCollections()
- public void AddCollectionsToList(params FeedbackCollection<BoolFeedback>[] newFbs)
- public void AddCollectionsToList(params FeedbackCollection<IntFeedback>[] newFbs)
- public void AddCollectionsToList(params FeedbackCollection<StringFeedback>[] newFbs)
- public void AddCollectionToList(FeedbackCollection<BoolFeedback> newFbs)
- public void AddCollectionToList(FeedbackCollection<IntFeedback> newFbs)
- public void AddCollectionToList(FeedbackCollection<StringFeedback> newFbs)
- public void AddFeedbackToList(PepperDash.Essentials.Core.Feedback newFb)
- public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType sigType)
- public void ExecuteNumericSwitch(ushort inputSelector, ushort outputSelector, eRoutingSignalType signalType)
- public void RecallEqPreset(ushort preset)
- public void GetVolumeMin()
- public void GetVolumeMax()
- public void RecallPreset(ushort preset)
- public void RecallStartupVolume()
- public void SetVolumeScaled(ushort level)
- public ushort ScaleVolumeFeedback(ushort level)
- public void SendScaledVolume(bool pressRelease)
- public void SetVolume(ushort level)
- public void MuteOn()
- public void MuteOff()
- public void VolumeUp(bool pressRelease)
- public void VolumeDown(bool pressRelease)
- public void MuteToggle()
- public void AddToFeedbackList(params Feedback[] newFbs)
- public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType signalType)
- public void Event(int id)
- public void SetVolumeScaled(ushort level)
- public ushort ScaleVolumeFeedback(ushort level)
- public void SendScaledVolume(bool pressRelease)
- public void SetVolume(ushort level)
- public void MuteOn()
- public void MuteOff()
- public void VolumeUp(bool pressRelease)
- public void VolumeDown(bool pressRelease)
- public void MuteToggle()
- public void MuteOff()
- public void MuteOn()
- public void SetVolume(ushort level)
- public void MuteToggle()
- public void VolumeDown(bool pressRelease)
- public void VolumeUp(bool pressRelease)
<!-- END Public Methods -->
<!-- START Bool Feedbacks -->
### Bool Feedbacks

- IsInSessionFeedback
- HdmiVideoSyncDetectedFeedback
- AutomaticInputRoutingEnabledFeedback
- AutoRouteOnFeedback
- PriorityRoutingOnFeedback
- InputOnScreenDisplayEnabledFeedback
- RemoteEndDetectedFeedback
- MuteFeedback
- VideoMuteIsOn
- MuteFeedback
- HdmiVideoSyncFeedback
- Hdmi1VideoSyncFeedback
- Hdmi2VideoSyncFeedback
- Hdmi1VideoSyncFeedback
- Hdmi2VideoSyncFeedback
- HdmiVideoSyncFeedback
- VgaVideoSyncFeedback
- FreeRunEnabledFeedback
- DisplayPortVideoSyncFeedback
- HdmiVideoSyncFeedback
- VgaVideoSyncFeedback
- FreeRunEnabledFeedback
- HdmiVideoSyncFeedback
- VgaVideoSyncFeedback
- FreeRunEnabledFeedback
- HdmiVideoSyncFeedback
- VgaVideoSyncFeedback
- FreeRunEnabledFeedback
- Hdmi1VideoSyncFeedback
- Hdmi2VideoSyncFeedback
- DisplayPortVideoSyncFeedback
- Hdmi1VideoSyncFeedback
- Hdmi2VideoSyncFeedback
- VgaVideoSyncFeedback
- FreeRunEnabledFeedback
- AutoRouteFeedback
- SystemIdBusyFeedback
- SystemIdBusyFeedback
- EnableAudioBreakawayFeedback
- EnableUsbBreakawayFeedback
- AutoRouteFeedback
- SystemPowerOnFeedback
- SystemPowerOffFeedback
- FrontPanelLockOnFeedback
- FrontPanelLockOffFeedback
- MuteFeedback
- MuteFeedback
- MuteFeedback
- IsOnline
- IsOnline
- IsOnline
<!-- END Bool Feedbacks -->
<!-- START Int Feedbacks -->
### Int Feedbacks

- ErrorFeedback
- NumberOfUsersConnectedFeedback
- LoginCodeFeedback
- VideoOutFeedback
- VideoSourceFeedback
- DmInHdcpStateFeedback
- VolumeLevelFeedback
- DmInHdcpStateFeedback
- VolumeLevelFeedback
- DmInHdcpStateFeedback
- HdmiInHdcpStateFeedback
- AudioVideoSourceNumericFeedback
- VideoSourceNumericFeedback
- AudioSourceNumericFeedback
- HdmiIn1HdcpCapabilityFeedback
- HdmiIn2HdcpCapabilityFeedback
- VideoSourceNumericFeedback
- AudioSourceNumericFeedback
- HdmiIn1HdcpCapabilityFeedback
- HdmiIn2HdcpCapabilityFeedback
- HdcpStateFeedback
- VideoSourceNumericFeedback
- AudioSourceNumericFeedback
- HdmiInHdcpCapabilityFeedback
- VgaBrightnessFeedback
- VgaContrastFeedback
- VideoSourceNumericFeedback
- AudioSourceNumericFeedback
- HdmiInHdcpCapabilityFeedback
- VgaBrightnessFeedback
- VgaContrastFeedback
- VideoSourceNumericFeedback
- AudioSourceNumericFeedback
- HdmiInHdcpCapabilityFeedback
- VgaBrightnessFeedback
- VgaContrastFeedback
- VideoSourceNumericFeedback
- AudioSourceNumericFeedback
- HdmiInHdcpCapabilityFeedback
- VgaBrightnessFeedback
- VgaContrastFeedback
- VideoSourceNumericFeedback
- AudioSourceNumericFeedback
- HdmiIn1HdcpCapabilityFeedback
- HdmiIn2HdcpCapabilityFeedback
- DisplayPortInHdcpCapabilityFeedback
- VideoSourceNumericFeedback
- AudioSourceNumericFeedback
- HdmiIn1HdcpCapabilityFeedback
- HdmiIn2HdcpCapabilityFeedback
- VgaBrightnessFeedback
- VgaContrastFeedback
- VideoSourceNumericFeedback
- AudioSourceNumericFeedback
- HdmiInHdcpCapabilityFeedback
- SystemIdFeebdack
- SystemIdFeebdack
- VolumeLevelFeedback
- VolumeLevelScaledFeedback
- AudioSourceNumericFeedback
- VolumeLevelFeedback
- VolumeLevelScaledFeedback
- VolumeLevelFeedback
<!-- END Int Feedbacks -->
<!-- START String Feedbacks -->
### String Feedbacks

- ConnectionAddressFeedback
- HostnameFeedback
- SerialNumberFeedback
- DeviceNameFeedback
- VideoOutputResolutionFeedback
- EdidManufacturerFeedback
- EdidNameFeedback
- EdidPreferredTimingFeedback
- EdidSerialNumberFeedback
- DeviceNameFeedback
- ActiveVideoInputFeedback
- DeviceNameFeedback
- DeviceNameFeedback
- NameFeedback
<!-- END String Feedbacks -->
