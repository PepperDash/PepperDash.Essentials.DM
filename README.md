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

- 3.0.0
- 3.0.0
- 3.0.0
- 3.0.0
- 3.0.0
- 3.0.0
- 3.0.0
- 3.0.0
- 3.0.0
- 3.0.0
- 3.0.0
- 3.0.0
- 3.0.0
<!-- END Minimum Essentials Framework Versions -->
<!-- START Config Example -->
### Config Example

```json
{
    "key": "GeneratedKey",
    "uid": 1,
    "name": "GeneratedName",
    "type": "hdmd8x84kze",
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

- hdmd8x84kze
- hdmd4x14kze
- hdmd4x44kze
- hdmd4x24kze
- hdmd8x44kze
- dmmd8x8cpu3rps
- dmmd32x32cpu3
- dmmd32x32
- dmmd8x8
- dmmd16x16rps
- dmmd16x16cpu3rps
- dmmd32x32rps
- dmmd64x64
- dmmd32x32cpu3rps
- dmmd8x8cpu3
- dmmd16x16
- dmmd8x8rps
- dmmd128x128
- dmmd16x16cpu3
- hdmd4x14ke
- hdmd8x2
- hdmd8x1
- hdmd4x24ke
- hdmd6x24ke
- hdmd4x14ke-bridgeable
- hdps401
- hdps402
- hdps622
- hdps621
- am200
- am300
- am3200
- hdWp4k401c
- hdmd200ce
- hdmd300ce
- hdmd200c1ge
- hdmd400ce
- dmdge200c
- dge100
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

- IRoutingInputs
- IRoutingOutputs
- IRoutingMidpointWithFeedback
- IHasNamedRoutingSlots
- IHasFeedback
- IDmSwitchWithEndpointOnlineFeedback
- IBasicVolumeWithFeedback
- ITxRouting
- IRoutingHasVideoInputSyncFeedbacks
- IIROutputPorts
- IComPorts
- IHasWirelessSharing
- IHasScreensWithLayouts
- IHasNamedRoutingSlots//
- IDmInputSlot
- IRoutingSlotInfo
- IDmOutputSlot
- IRoutingMidpoint
- IHasBasicTriListWithSmartObject
- ICec
- IDeviceInfoProvider
- IBridgeAdvanced
- IRelayPorts
- IHasDmInHdcp
- IBasicVideoMuteWithFeedback
- IRmcRoutingWithFeedback
- IHasHdmiInHdcp
- ITxRoutingWithFeedback
- IHasFreeRun
- IVgaBrightnessContrastControls
<!-- END Interfaces Implemented -->
<!-- START Base Classes -->
### Base Classes

- EssentialsBridgeableDevice
- CrestronGenericBridgeableBaseDevice
- CrestronGenericBaseDevice
- Device
- MessengerBase
- JoinMapBaseAdvanced
- Dge100Controller
- DmRmcControllerBase
- DmRmcX100CController
- DmHdBaseTControllerBase
- BasicDmTxControllerBase
- DmTxControllerBase
<!-- END Base Classes -->
<!-- START Public Methods -->
### Public Methods

- public void All_Factory_Sources_Set_MinimumEssentialsFrameworkVersion_To_3()
- public void All_Factory_Sources_Set_TypeNames()
- public void Factory_Source_Contains_TypeName(string factoryClassName, string expectedTypeName)
- public void No_Duplicate_TypeNames_Across_Factory_Sources()
- public void Slots_are_built_from_ports_with_1_based_slot_numbers()
- public void Null_port_collections_produce_empty_slot_maps()
- public void Route_change_records_input_key_for_the_signal_type()
- public void AudioVideo_route_is_expanded_to_audio_and_video()
- public void Breakaway_audio_and_video_are_tracked_independently()
- public void Null_input_clears_only_that_signal_type()
- public void OutputSlotChanged_fires_on_change_but_not_on_no_op()
- public void Route_change_for_unknown_output_is_ignored()
- public void Slot_key_resolves_to_the_matching_ports_selector()
- public void Selector_object_passes_through_untouched()
- public void Null_selector_resolves_to_null()
- public void Unknown_slot_key_resolves_to_null()
- public void Port_whose_selector_is_a_different_type_resolves_to_null()
- public void Null_port_collection_resolves_to_null()
- public void Untyped_resolve_maps_a_slot_key_to_a_numeric_selector()
- public void Untyped_resolve_passes_a_non_string_selector_through()
- public void Untyped_resolve_returns_an_unmatched_key_unchanged()
- public void DMChassisConfig_Has_JsonProperty(string className, string jsonPropertyName)
- public void HdMdNxM4kEConfig_Has_JsonProperty(string className, string jsonPropertyName)
- public void HdMdNxM4kEBridgeableConfig_Has_JsonProperty(string className, string jsonPropertyName)
- public void HdMdNxM4kzEConfig_Has_JsonProperty(string className, string jsonPropertyName)
- public void DmCardAudioConfig_Has_JsonProperty(string className, string jsonPropertyName)
- public void AirMediaPropertiesConfig_Exists()
- public void DmTxPropertiesConfig_Exists()
- public void DmRmcPropertiesConfig_Exists()
- public void Config_Has_Parameterless_Constructor(string className)
- public void Assembly_Loads_Successfully()
- public void Assembly_Name_Matches_Expected()
- public void All_Factory_Types_Are_Discoverable()
- public void Factory_Count_Matches_Expected()
- public void Factory_Exists_ByName(string factoryClassName)
- public void All_Factories_Have_Parameterless_Constructor()
- public void Breakaway_audio_and_video_to_same_output_are_tracked_independently()
- public void Re_routing_one_signal_type_replaces_only_that_descriptor()
- public void Null_input_clears_only_that_signal_and_returns_a_clear_descriptor()
- public void Distinct_outputs_are_tracked_independently()
- public void Null_output_is_ignored_and_returns_null()
- public void Clear_removes_all_routes_and_reports_whether_anything_changed()
- public void CurrentRoutes_instance_is_stable_across_updates()
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
- public void ClearRoute(object outputSelector, eRoutingSignalType signalType)
- public void EnableHdcp(uint port)
- public void DisableHdcp(uint port)
- public void EnableAutoRoute()
- public void DisableAutoRoute()
- public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType signalType)
- public void ExecuteNumericSwitch(ushort inputSelector, ushort outputSelector, eRoutingSignalType signalType)
- public void AddInputCard(string type, uint number)
- public void AddOutputCard(string type, uint number)
- public void SetInputHdcpSupport(uint input, ePdtHdcpSupport hdcpSetting)
- public void ClearRoute(object outputSelector, eRoutingSignalType signalType)
- public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType sigType)
- public void ExecuteNumericSwitch(ushort inputSelector, ushort outputSelector, eRoutingSignalType sigType)
- public void Route(string inputSlotKey, string outputSlotKey, eRoutingSignalType type)
- public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType signalType)
- public void ClearRoute(object outputSelector, eRoutingSignalType signalType)
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
- public void ClearRoute(object outputSelector, eRoutingSignalType signalType)
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
- public void AddInputBlade(string type, uint number)
- public void ClearRoute(object outputSelector, eRoutingSignalType signalType)
- public void AddOutputBlade(string type, uint number)
- public void SetInputHdcpSupport(uint input, ePdtHdcpSupport hdcpSetting)
- public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType sigType)
- public void ExecuteNumericSwitch(ushort inputSelector, ushort outputSelector, eRoutingSignalType sigType)
- public void AddToFeedbackList(params Feedback[] newFbs)
- public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType signalType)
- public void ClearRoute(object outputSelector, eRoutingSignalType signalType)
- public void ClearRoute(object outputSelector, eRoutingSignalType signalType)
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
- public void MuteOff()
- public void MuteOn()
- public void SetVolume(ushort level)
- public void MuteToggle()
- public void VolumeDown(bool pressRelease)
- public void VolumeUp(bool pressRelease)
- public void SetPortHdcpCapability(eHdcpCapabilityType hdcpMode, uint port)
- public void AddToFeedbackList(params Feedback[] newFbs)
- public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType signalType)
- public void ClearRoute(object outputSelector, eRoutingSignalType signalType)
- public void ListRoutingPorts()
- public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType signalType)
- public void ExecuteNumericSwitch(ushort inputSelector, ushort outputSelector, eRoutingSignalType signalType)
- public void EnableHdcp(uint port)
- public void DisableHdcp(uint port)
- public void EnableAutoRoute()
- public void DisableAutoRoute()
- public void ClearRoute(object outputSelector, eRoutingSignalType signalType)
- public void MuteOff()
- public void MuteOn()
- public void SetVolume(ushort level)
- public void MuteToggle()
- public void VolumeDown(bool pressRelease)
- public void VolumeUp(bool pressRelease)
- public void MuteOff()
- public void MuteOn()
- public void SetVolume(ushort level)
- public void MuteToggle()
- public void VolumeDown(bool pressRelease)
- public void VolumeUp(bool pressRelease)
- public void ClearRoute(object outputSelector, eRoutingSignalType signalType)
- public void SetRoutingEnable(bool enable)
- public void AddInputCard(uint number, DMInput inputCard)
- public void AddOutputCard(uint number, DMOutput outputCard)
- public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType sigType)
- public void ExecuteNumericSwitch(ushort inputSelector, ushort outputSelector, eRoutingSignalType sigType)
- public void ClearRoute(object outputSelector, eRoutingSignalType signalType)
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
- public void ClearRoute(object outputSelector, eRoutingSignalType signalType)
- public void HandleRouteChange(
            RoutingOutputPort outputPort,
            RoutingInputPort inputPort,
            eRoutingSignalType signalType)
- public bool Clear()
- public void UpdateDeviceInfo()
- public void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
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
- public void ClearRoute(object outputSelector, eRoutingSignalType signalType)
- public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType signalType)
- public void ExecuteNumericSwitch(ushort inputSelector, ushort outputSelector, eRoutingSignalType signalType)
- public void SetDmInHdcpState(eHdcpCapabilityType hdcpState)
- public void SetHdmiInHdcpState(eHdcpCapabilityType hdcpState)
- public void UpdateDeviceInfo()
- public void ExecuteNumericSwitch(ushort input, ushort output, eRoutingSignalType type)
- public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType signalType)
- public void ExecuteNumericSwitch(ushort input, ushort output, eRoutingSignalType type)
- public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType signalType)
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
- public void SetFreeRunEnabled(bool enable)
- public void SetVgaBrightness(ushort level)
- public void SetVgaContrast(ushort level)
- public void ExecuteNumericSwitch(ushort input, ushort output, eRoutingSignalType type)
- public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType signalType)
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
<!-- END Public Methods -->
<!-- START Bool Feedbacks -->
### Bool Feedbacks

- MuteFeedback
- AutoRouteFeedback
- SystemIdBusyFeedback
- EnableAudioBreakawayFeedback
- EnableUsbBreakawayFeedback
- MuteFeedback
- SystemIdBusyFeedback
- AutoRouteFeedback
- MuteFeedback
- AutoRouteFeedback
- MuteFeedback
- MuteFeedback
- SystemPowerOnFeedback
- SystemPowerOffFeedback
- FrontPanelLockOnFeedback
- FrontPanelLockOffFeedback
- IsInSessionFeedback
- IsSharingFeedback
- HdmiVideoSyncDetectedFeedback
- AutomaticInputRoutingEnabledFeedback
- AutoRouteOnFeedback
- PriorityRoutingOnFeedback
- InputOnScreenDisplayEnabledFeedback
- RemoteEndDetectedFeedback
- IsOnline
- IsOnline
- IsOnline
- MuteFeedback
- VideoMuteIsOn
- MuteFeedback
- HdmiVideoSyncFeedback
- Hdmi1VideoSyncFeedback
- Hdmi2VideoSyncFeedback
- DisplayPortVideoSyncFeedback
- Hdmi1VideoSyncFeedback
- Hdmi2VideoSyncFeedback
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
- HdmiVideoSyncFeedback
- VgaVideoSyncFeedback
- FreeRunEnabledFeedback
- Hdmi1VideoSyncFeedback
- Hdmi2VideoSyncFeedback
- VgaVideoSyncFeedback
- FreeRunEnabledFeedback
<!-- END Bool Feedbacks -->
<!-- START Int Feedbacks -->
### Int Feedbacks

- VolumeLevelFeedback
- VolumeLevelScaledFeedback
- SystemIdFeebdack
- VolumeLevelFeedback
- VolumeLevelScaledFeedback
- SystemIdFeebdack
- AudioSourceNumericFeedback
- VolumeLevelFeedback
- VideoSourceNumericFeedback
- AudioSourceNumericFeedback
- HdmiInHdcpCapabilityFeedback
- VolumeLevelFeedback
- VolumeLevelFeedback
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
- DisplayPortInHdcpCapabilityFeedback
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
- HdmiIn1HdcpCapabilityFeedback
- HdmiIn2HdcpCapabilityFeedback
- VideoSourceNumericFeedback
- AudioSourceNumericFeedback
- HdmiInHdcpCapabilityFeedback
- VgaBrightnessFeedback
- VgaContrastFeedback
- VideoSourceNumericFeedback
- AudioSourceNumericFeedback
- HdmiIn1HdcpCapabilityFeedback
- HdmiIn2HdcpCapabilityFeedback
- VgaBrightnessFeedback
- VgaContrastFeedback
<!-- END Int Feedbacks -->
<!-- START String Feedbacks -->
### String Feedbacks

- DeviceNameFeedback
- NameFeedback
- DeviceNameFeedback
- DeviceNameFeedback
- ActiveVideoInputFeedback
- DeviceNameFeedback
- ConnectionAddressFeedback
- HostnameFeedback
- SerialNumberFeedback
- DeviceNameFeedback
- VideoOutputResolutionFeedback
- EdidManufacturerFeedback
- EdidNameFeedback
- EdidPreferredTimingFeedback
- EdidSerialNumberFeedback
<!-- END String Feedbacks -->
