using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using Crestron.SimplSharp;
using Crestron.SimplSharpPro.DeviceSupport;
using Crestron.SimplSharpPro.DM;
using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.DM.Config;
using PepperDash.Essentials.Core.Bridges;
using PepperDash.Essentials.Core.Config;
using Crestron.SimplSharpPro;
using PepperDash.Core.Logging;


namespace PepperDash.Essentials.DM.Chassis
{
	[Description("Wrapper class for all HdMdNxM4ZE switchers")]
	public class HdMdNxM4kZEController : CrestronGenericBridgeableBaseDevice, IRoutingNumericWithFeedback, IHasFeedback
	{
		private HdMdNxM4kzE _Chassis;

		public event EventHandler<RoutingNumericEventArgs> NumericSwitchChange;

		public Dictionary<uint, string> InputNames { get; set; }
		public Dictionary<uint, string> OutputNames { get; set; }

		public RoutingPortCollection<RoutingInputPort> InputPorts { get; private set; }
		public RoutingPortCollection<RoutingOutputPort> OutputPorts { get; private set; }

		public FeedbackCollection<BoolFeedback> VideoInputSyncFeedbacks { get; private set; }
		public FeedbackCollection<IntFeedback> VideoOutputRouteFeedbacks { get; private set; }
		public FeedbackCollection<StringFeedback> InputNameFeedbacks { get; private set; }
		public FeedbackCollection<StringFeedback> OutputNameFeedbacks { get; private set; }
		public FeedbackCollection<StringFeedback> OutputRouteNameFeedbacks { get; private set; }
		public FeedbackCollection<BoolFeedback> InputHdcpEnableFeedback { get; private set; }
		public StringFeedback DeviceNameFeedback { get; private set; }
		public BoolFeedback AutoRouteFeedback { get; private set; }
		public BoolFeedback PriorityRouteFeedback { get; private set; }
		public string NoRouteText { get; private set; }

		#region Constructor

		/// <summary>
		/// Constructor for the HdMdNxM4kZEController
		/// </summary>
		/// <param name="key">The device key.</param>
		/// <param name="name">The device name.</param>
		/// <param name="chassis">The HdMdNxM4kzE chassis instance.</param>
		/// <param name="props">The HdMdNxM4kE properties config.</param>
		public HdMdNxM4kZEController(string key, string name, HdMdNxM4kzE chassis,
			HdMdNxM4kEPropertiesConfig props)
			: base(key, name, chassis)
		{
			Name = name;
			_Chassis = chassis;
			if (_Chassis == null)
			{
				Debug.LogDebug(this, "HdMdNxM4kZEController chassis is null, failed to build the device");
				return;
			}

			if (props == null)
			{
				Debug.LogDebug(this, "HdMdNxM4kZEController properties are null, failed to build the device");
				return;
			}

			NoRouteText = props.NoRouteText ?? "None";

			if (props.Inputs != null)
			{
				InputNames = props.Inputs;
				foreach (var kvp in InputNames)
				{
					Debug.LogDebug(this, "InputNames: {0}-{1}", kvp.Key, kvp.Value);
				}
			}
			if (props.Outputs != null)
			{
				OutputNames = props.Outputs;
				foreach (var kvp in OutputNames)
				{
					Debug.LogDebug(this, "OutputNamess: {0}-{1}", kvp.Key, kvp.Value);
				}
			}

			DeviceNameFeedback = new StringFeedback("DeviceName", () =>
			{
				try { return Name; }
				catch { this.LogError("Error getting DeviceNameFeedback"); return ""; }
			});

			VideoInputSyncFeedbacks = new FeedbackCollection<BoolFeedback>();
			VideoOutputRouteFeedbacks = new FeedbackCollection<IntFeedback>();
			InputNameFeedbacks = new FeedbackCollection<StringFeedback>();
			OutputNameFeedbacks = new FeedbackCollection<StringFeedback>();
			OutputRouteNameFeedbacks = new FeedbackCollection<StringFeedback>();
			InputHdcpEnableFeedback = new FeedbackCollection<BoolFeedback>();

			InputPorts = new RoutingPortCollection<RoutingInputPort>();
			OutputPorts = new RoutingPortCollection<RoutingOutputPort>();

			if (_Chassis is HdMdNxM4kzE _chassis_M4kzE)
			{
				AutoRouteFeedback = new BoolFeedback("AutoRouteFeedback", () =>
				{
					try { return _chassis_M4kzE.AutoRouteOnFeedback?.BoolValue ?? false; }
					catch { this.LogError("Error getting AutoRouteFeedback"); return false; }
				});
			}
			if (_Chassis is HdMd4xX4kzE _chassis_X4kzE)
			{
				PriorityRouteFeedback = new BoolFeedback("PriorityRouteFeedback", () =>
				{
					try { return _chassis_X4kzE.PriorityRouteOnFeedback?.BoolValue ?? false; }
					catch { this.LogError("Error getting PriorityRouteFeedback"); return false; }
				});
			}

			SetupInputs();
			SetupOutputs();

			_Chassis.DMInputChange += Chassis_DMInputChange;
			_Chassis.DMOutputChange += Chassis_DMOutputChange;
			_Chassis.BaseEvent += Chassis_BaseEvent;

			AddPostActivationAction(AddFeedbackCollections);
		}

		private void SetupInputs()
		{
			if (InputNames == null)
			{
				this.LogError("SetupInputs: InputNames is null. Ensure 'inputs' is defined in the device configuration.");
				return;
			}

			foreach (var kvp in InputNames)
			{
				var index = kvp.Key;
				var inputName = kvp.Value;
				var inputFbKeyPrefix = inputName.Replace(" ", "").Trim();

				if (index < 1 || index > _Chassis.NumberOfInputs)
				{
					this.LogWarning("SetupInputs: Input index {index} is out of range (1-{max}). Skipping.", index, _Chassis.NumberOfInputs);
					continue;
				}

				var hdmiInput = _Chassis.HdmiInputs[index];
				if (hdmiInput == null)
				{
					this.LogError("SetupInputs: HdmiInput at index {index} is null. Skipping.", index);
					continue;
				}

				var chassisInput = _Chassis.Inputs[index];
				if (chassisInput == null)
				{
					this.LogError("SetupInputs: Chassis Input at index {index} is null. Skipping.", index);
					continue;
				}

				hdmiInput.Name.StringValue = inputName;

				InputPorts.Add(new RoutingInputPort(inputName, eRoutingSignalType.AudioVideo,
					eRoutingPortConnectionType.Hdmi, hdmiInput, this)
				{
					FeedbackMatchObject = hdmiInput
				});

				VideoInputSyncFeedbacks.Add(new BoolFeedback(string.Format($"{inputFbKeyPrefix}VideoInputSyncFeedback"), () =>
				{
					try { return chassisInput?.VideoDetectedFeedback?.BoolValue ?? false; }
					catch { this.LogError($"Error getting VideoInputSyncFeedback for input {inputName}"); return false; }
				}));
				InputNameFeedbacks.Add(new StringFeedback(string.Format($"{inputFbKeyPrefix}InputNameFeedback"), () =>
				{
					try { return InputNames[index]; }
					catch { this.LogError($"Error getting InputNameFeedback for input {inputName}"); return ""; }
				}));

				if (hdmiInput.HdmiInputPort == null)
				{
					this.LogWarning("HdmiInputPort at index {index} is null. HDCP feedback will default to false.", index);
				}

				InputHdcpEnableFeedback.Add(new BoolFeedback(string.Format($"{inputFbKeyPrefix}HdcpEnableFeedback"), () =>
				{
					try { return hdmiInput?.HdmiInputPort?.HdcpSupportOnFeedback?.BoolValue ?? false; }
					catch { this.LogError($"Error getting HdcpEnableFeedback for input {inputName}"); return false; }
				}));
			}
		}

		private void SetupOutputs()
		{
			if (OutputNames == null)
			{
				this.LogWarning("SetupOutputs: OutputNames is null. Ensure 'outputs' is defined in the device configuration.");
				return;
			}

			foreach (var kvp in OutputNames)
			{
				var index = kvp.Key;
				var outputName = kvp.Value;
				var outputFbKeyPrefix = outputName.Replace(" ", "").Trim();

				if (index < 1 || index > _Chassis.NumberOfOutputs)
				{
					this.LogWarning("SetupOutputs: Output index {index} is out of range (1-{max}). Skipping.", index, _Chassis.NumberOfOutputs);
					continue;
				}

				var hdmiOutput = _Chassis.HdmiOutputs[index];
				if (hdmiOutput == null)
				{
					this.LogWarning("SetupOutputs: HdmiOutput at index {index} is null. Skipping.", index);
					continue;
				}

				var chassisOutput = _Chassis.Outputs[index];
				if (chassisOutput == null)
				{
					this.LogError("SetupOutputs: Chassis Output at index {index} is null. Skipping.", index);
					continue;
				}

				OutputPorts.Add(new RoutingOutputPort(outputName, eRoutingSignalType.AudioVideo,
					eRoutingPortConnectionType.Hdmi, hdmiOutput, this)
				{
					FeedbackMatchObject = hdmiOutput
				});

				VideoOutputRouteFeedbacks.Add(new IntFeedback(string.Format($"{outputFbKeyPrefix}VideoOutputRouteFeedback"), () =>
				{
					try { return chassisOutput.VideoOutFeedback == null ? 0 : (int)chassisOutput.VideoOutFeedback.Number; }
					catch { this.LogError($"Error getting VideoOutputRouteFeedback for output {outputName}"); return 0; }
				}));
				OutputNameFeedbacks.Add(new StringFeedback(string.Format($"{outputFbKeyPrefix}OutputNameFeedback"), () =>
				{
					try { return OutputNames[index]; }
					catch { this.LogError($"Error getting OutputNameFeedback for output {outputName}"); return ""; }
				}));
				OutputRouteNameFeedbacks.Add(new StringFeedback(string.Format($"{outputFbKeyPrefix}OutputRouteNameFeedback"), () =>
				{
					try { return chassisOutput.VideoOutFeedback == null ? NoRouteText : chassisOutput.VideoOutFeedback.NameFeedback.StringValue; }
					catch { this.LogError($"Error getting OutputRouteNameFeedback for output {outputName}"); return NoRouteText; }
				}));
			}
		}

		#endregion

		#region Methods		

		/// <summary>
		/// Raise an event when the status of a switch object changes.
		/// </summary>
		/// <param name="e">Arguments defined as IKeyName sender, output, input, and eRoutingSignalType</param>
		private void OnSwitchChange(RoutingNumericEventArgs e)
		{
			NumericSwitchChange?.Invoke(this, e);
		}

		/// <summary>
		/// Enables HDCP on the specified input port.
		/// </summary>
		/// <param name="port">The input port number to enable HDCP on.</param>
		public void EnableHdcp(uint port)
		{
			if (port > _Chassis.NumberOfInputs) return;
			if (port <= 0) return;

			var hdmiInput = _Chassis.HdmiInputs[port];
			if (hdmiInput?.HdmiInputPort == null)
			{
				this.LogWarning("EnableHdcp: HdmiInputPort at index {port} is null. Cannot enable HDCP.", port);
				return;
			}

			hdmiInput.HdmiInputPort.HdcpSupportOn();

			if (InputNames.ContainsKey(port))
			{
				var inputName = InputNames[port];
				var feedback = InputHdcpEnableFeedback.FirstOrDefault(f => f.Key == inputName);
				if (feedback == null)
				{
					return;
				}

				feedback.FireUpdate();
			}
		}

		/// <summary>
		/// Disables HDCP on the specified input port.
		/// </summary>
		/// <param name="port">The input port number to disable HDCP on.</param>
		public void DisableHdcp(uint port)
		{
			if (port > _Chassis.NumberOfInputs) return;
			if (port <= 0) return;

			var hdmiInput = _Chassis.HdmiInputs[port];
			if (hdmiInput?.HdmiInputPort == null)
			{
				this.LogWarning("DisableHdcp: HdmiInputPort at index {port} is null. Cannot disable HDCP.", port);
				return;
			}

			hdmiInput.HdmiInputPort.HdcpSupportOff();

			if (InputNames.ContainsKey(port))
			{
				var inputName = InputNames[port];
				var feedback = InputHdcpEnableFeedback.FirstOrDefault(f => f.Key == inputName);
				if (feedback == null)
				{
					return;
				}
				feedback.FireUpdate();
			}
		}

		/// <summary>
		/// Enables AutoRoute on the chassis if supported.	Auto route is supported by HdMdNxM4kzE
		/// </summary>
		public void EnableAutoRoute()
		{
			if (_Chassis.NumberOfOutputs > 1) return;
			if (_Chassis is HdMdNxM4kzE _chassis_M4kzE)
			{
				_chassis_M4kzE.AutoRouteOn();
				return;
			}

			this.LogVerbose("EnableAutoRoute: AutoRoute is not supported on this chassis.");
		}

		/// <summary>
		/// Disables AutoRoute on the chassis if supported.  Auto route is supported by HdMdNxM4kzE
		/// </summary>
		public void DisableAutoRoute()
		{
			if (_Chassis.NumberOfOutputs > 1) return;
			if (_Chassis is HdMdNxM4kzE _chassis_M4kzE)
			{
				_chassis_M4kzE.AutoRouteOff();
				return;
			}

			this.LogVerbose("DisableAutoRoute: AutoRoute is not supported on this chassis.");
		}

		/// <summary>
		/// Enables Priority Route on the chassis if supported. Priority route is support by HdMd4xX4kzE
		/// </summary>
		public void EnablePriorityRoute()
		{
			if (_Chassis is HdMd4xX4kzE _chassis_X4kzE)
			{
				_chassis_X4kzE.PriorityRouteOn();
				return;
			}

			this.LogVerbose("EnablePriorityRoute: Priority Route is not supported on this chassis.");
		}

		/// <summary>
		/// Disables Priority Route on the chassis if supported. Priority route is support by HdMd4xX4kzE
		/// </summary>
		public void DisablePriorityRoute()
		{
			if (_Chassis is HdMd4xX4kzE _chassis_X4kzE)
			{
				_chassis_X4kzE.PriorityRouteOff();
				return;
			}

			this.LogVerbose("DisablePriorityRoute: Priority Route is not supported on this chassis.");
		}


		#region FeedbackCollection Methods


		/// <summary>
		/// Adds all feedback collections to the Feedbacks collection.
		/// </summary>
		public void AddFeedbackCollections()
		{
			if (IsOnline != null)
				AddFeedbackToList(IsOnline);

			AddFeedbackToList(DeviceNameFeedback);

			if (AutoRouteFeedback != null)
			{
				AddFeedbackToList(AutoRouteFeedback);
			}

			foreach (var fb in VideoInputSyncFeedbacks)
			{
				AddFeedbackToList(fb);
			}
			foreach (var fb in InputHdcpEnableFeedback)
			{
				AddFeedbackToList(fb);
			}
			foreach (var fb in VideoOutputRouteFeedbacks)
			{
				AddFeedbackToList(fb);
			}
			foreach (var fb in InputNameFeedbacks)
			{
				AddFeedbackToList(fb);
			}
			foreach (var fb in OutputNameFeedbacks)
			{
				AddFeedbackToList(fb);
			}
			foreach (var fb in OutputRouteNameFeedbacks)
			{
				AddFeedbackToList(fb);
			}

			// TODO - Remove after testing
			this.LogInformation("AddFeedbackCollections: Feedbacks contains {feedbacksCount} items", Feedbacks.Count);
			foreach (var fb in Feedbacks)
			{
				// TODO - Remove after testing
				this.LogInformation("AddFeedbackCollections: Feedbacks = {feedbackKey}", fb.Key);
			}
		}

		/// <summary>
		/// Adds a feedback to the Feedbacks collection if it does not already exist.
		/// </summary>
		public void AddFeedbackToList(Essentials.Core.Feedback newFb)
		{
			if (newFb == null) return;

			if (Feedbacks.Any(f => f.Key == newFb.Key)) return;

			// TODO - Remove after testing
			this.LogVerbose("AddFeedbackToList: adding {feedbackKey} to Feedbacks collection", newFb.Key);
			Feedbacks.Add(newFb);
		}

		#endregion

		#region IRouting Members

		/// <summary>
		/// Executes a switch from input to output for the specified signal type.
		/// </summary>
		/// <param name="inputSelector">The input selector object.</param>
		/// <param name="outputSelector">The output selector object.</param>
		/// <param name="signalType">The type of signal to switch.</param>
		public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType signalType)
		{
			var input = inputSelector as HdMdNxM4kzEHdmiInput;
			var output = outputSelector as HdMdNxM4kzEHdmiOutput;
			this.LogVerbose("ExecuteSwitch: input={0} output={1}", input, output);

			if (output == null)
			{
				this.LogInformation("Unable to make switch. output selector is not HdMdNxM4kzEHdmiOutput");
				return;
			}

			// Try to make switch only when necessary.  The unit appears to toggle when already selected.
			var current = output.VideoOut;
			if (current != input)
				output.VideoOut = input;
		}

		#endregion

		#region IRoutingNumeric Members

		/// <summary>
		/// Executes a numeric switch from input to output for the specified signal type.
		/// </summary>
		/// <param name="inputSelector">The input selector number.</param>
		/// <param name="outputSelector">The output selector number.</param>
		/// <param name="signalType">The type of signal to switch.</param>
		public void ExecuteNumericSwitch(ushort inputSelector, ushort outputSelector, eRoutingSignalType signalType)
		{
			this.LogInformation("ExecuteNumericSwitch: inputSelector={inputSelector} outputSelector={outputSelector}", inputSelector, outputSelector);

			var input = inputSelector == 0 ? null : _Chassis.HdmiInputs[inputSelector];
			var output = _Chassis.HdmiOutputs[outputSelector];

			this.LogVerbose("ExecuteNumericSwitch: input={input} output={output}", input, output);

			ExecuteSwitch(input, output, signalType);
		}

		#endregion

		#endregion

		#region Bridge Linking

		/// <summary>
		/// Links the device to the API bridge.
		/// </summary>
		/// <param name="trilist">The trilist to link to.</param>
		/// <param name="joinStart">The join start number.</param>	
		/// <param name="joinMapKey">The join map key.</param>
		/// <param name="bridge">The EISC API bridge.</param>
		public override void LinkToApi(BasicTriList trilist, uint joinStart, string joinMapKey, EiscApiAdvanced bridge)
		{
			var joinMap = new HdMdNxM4kEControllerJoinMap(joinStart);

			var joinMapSerialized = JoinMapHelper.GetSerializedJoinMapForDevice(joinMapKey);

			if (!string.IsNullOrEmpty(joinMapSerialized))
				joinMap = JsonConvert.DeserializeObject<HdMdNxM4kEControllerJoinMap>(joinMapSerialized);

			if (bridge != null)
			{
				bridge.AddJoinMap(Key, joinMap);
			}
			else
			{
				this.LogInformation("Please update config to use 'eiscapiadvanced' to get all join map features for this device.");
			}

			if (IsOnline != null) IsOnline?.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);

			DeviceNameFeedback?.LinkInputSig(trilist.StringInput[joinMap.Name.JoinNumber]);

			if (_Chassis is HdMdNxM4kzE _chassis_M4kzE)
			{
				this.LogInformation("LinkToApi: _Chassis is HdMdNxM4kzE, setting up AutoRoute links");

				trilist.SetSigTrueAction(joinMap.EnableAutoRoute.JoinNumber, () => _chassis_M4kzE.AutoRouteOn());
				trilist.SetSigFalseAction(joinMap.EnableAutoRoute.JoinNumber, () => _chassis_M4kzE.AutoRouteOff());
				AutoRouteFeedback?.LinkInputSig(trilist.BooleanInput[joinMap.EnableAutoRoute.JoinNumber]);
			}

			if (_Chassis is HdMd4xX4kzE _chassis_X4kzE)
			{
				this.LogInformation("LinkToApi: _Chassis is HdMd4xX4kzE, setting up PriorityRoute links - not implemented");
				// trilist.SetSigTrueAction(joinMap.EnablePriorityRoute.JoinNumber, () => _chassis_X4kzE.PriorityRouteOn());
				// trilist.SetSigFalseAction(joinMap.EnablePriorityRoute.JoinNumber, () => _chassis_X4kzE.PriorityRouteOff());
				// PriorityRouteFeedback?.LinkInputSig(trilist.BooleanInput[joinMap.EnablePriorityRoute.JoinNumber]);
			}

			if (InputNames != null)
			{
				foreach (var kvp in InputNames)
				{
					var input = kvp.Key;
					var inputName = kvp.Value;

					if (input < 1 || input > _Chassis.NumberOfInputs)
					{
						this.LogWarning("LinkToApi: Input index {index} is out of range (1-{max}). Skipping.", input, _Chassis.NumberOfInputs);
						continue;
					}

					var joinIndex = input - 1;

					var joinNumberInputSync = joinMap.InputSync.JoinNumber + joinIndex;
					var joinNumberEnableInputHdcp = joinMap.EnableInputHdcp.JoinNumber + joinIndex;
					var joinNumberDisableInputHdcp = joinMap.DisableInputHdcp.JoinNumber + joinIndex;
					var joinNumberInputName = joinMap.InputName.JoinNumber + joinIndex;

					this.LogInformation("LinkToApi: Input {input} | joinIndex = {joinIndex}, InputSyncJoin = {joinNumberInputSync}, EnableHdcpJoin = {joinNumberEnableInputHdcp}, DisableHdcpJoin = {joinNumberDisableInputHdcp}, InputNameJoin = {joinNumberInputName}",
						input, joinIndex, joinNumberInputSync, joinNumberEnableInputHdcp, joinNumberDisableInputHdcp, joinNumberInputName);

					//Digital
					VideoInputSyncFeedbacks[inputName]?.LinkInputSig(trilist.BooleanInput[joinNumberInputSync]);
					InputHdcpEnableFeedback[inputName]?.LinkInputSig(trilist.BooleanInput[joinNumberEnableInputHdcp]);
					InputHdcpEnableFeedback[inputName]?.LinkComplementInputSig(trilist.BooleanInput[joinNumberDisableInputHdcp]);

					trilist.SetSigTrueAction(joinNumberEnableInputHdcp, () => EnableHdcp(input));
					trilist.SetSigTrueAction(joinNumberDisableInputHdcp, () => DisableHdcp(input));

					//Serial                
					InputNameFeedbacks[inputName]?.LinkInputSig(trilist.StringInput[joinNumberInputName]);
				}
			}
			else
			{
				this.LogWarning("LinkToApi: InputNames is null. Skipping input linking.");
			}

			if (OutputNames != null)
			{
				foreach (var kvp in OutputNames)
				{
					var output = kvp.Key;
					var outputName = kvp.Value;

					if (output < 1 || output > _Chassis.NumberOfOutputs)
					{
						this.LogWarning("LinkToApi: Output index {index} is out of range (1-{max}). Skipping.", output, _Chassis.NumberOfOutputs);
						continue;
					}

					var joinIndex = output - 1;

					var joinNumberOutputRoute = joinMap.OutputRoute.JoinNumber + joinIndex;
					var joinNumberOutputName = joinMap.OutputName.JoinNumber + joinIndex;
					var joinNumberOutputRouteName = joinMap.OutputRoutedName.JoinNumber + joinIndex;

					this.LogInformation("LinkToApi: Output {output} | joinIndex = {joinIndex}, OutputRouteJoin = {joinNumberOutputRoute}, OutputNameJoin = {joinNumberOutputName}, OutputRouteNameJoin = {joinNumberOutputRouteName}",
						output, joinIndex, joinNumberOutputRoute, joinNumberOutputName, joinNumberOutputRouteName);

					//Analog
					VideoOutputRouteFeedbacks[outputName]?.LinkInputSig(trilist.UShortInput[joinNumberOutputRoute]);

					trilist.SetUShortSigAction(joinNumberOutputRoute, (a) => ExecuteNumericSwitch(a, (ushort)output, eRoutingSignalType.AudioVideo));

					//Serial
					OutputNameFeedbacks[outputName]?.LinkInputSig(trilist.StringInput[joinNumberOutputName]);
					OutputRouteNameFeedbacks[outputName]?.LinkInputSig(trilist.StringInput[joinNumberOutputRouteName]);
				}
			}
			else
			{
				this.LogWarning("LinkToApi: OutputNames is null. Skipping output linking.");
			}

			_Chassis.OnlineStatusChange += Chassis_OnlineStatusChange;

			trilist.OnlineStatusChange += (d, args) =>
			{
				if (!args.DeviceOnLine) return;

				// feedback updates was moved to the Chassis_OnlineStatusChange 
				// due to the amount of time it takes for the device to come online                
			};
		}

		/*
		private void UpdateFeedbacks()
		{
			IsOnline?.FireUpdate();
			DeviceNameFeedback?.FireUpdate();
			AutoRouteFeedback?.FireUpdate();

			foreach (var item in VideoInputSyncFeedbacks)
			{
				item.FireUpdate();
			}

			foreach (var item in VideoOutputRouteFeedbacks)
			{
				item.FireUpdate();
			}

			foreach (var item in InputHdcpEnableFeedback)
			{
				item.FireUpdate();
			}

			foreach (var item in InputNameFeedbacks)
			{
				item.FireUpdate();
			}

			foreach (var item in OutputNameFeedbacks)
			{
				item.FireUpdate();
			}

			foreach (var item in OutputRouteNameFeedbacks)
			{
				item.FireUpdate();
			}

			foreach (var item in Feedbacks)
			{
				// TODO - Remove after testing
				this.LogInformation("UpdateFeedbacks: Firing feedback for {itemKey}", item.Key);
				item.FireUpdate();
			}				
		}
		*/
		#endregion

		#region Events

		private void Chassis_BaseEvent(GenericBase device, BaseEventArgs args)
		{
			var eventName = typeof(BaseEventArgs)
				.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
				.FirstOrDefault(f => f.IsLiteral && (int)f.GetValue(null) == args.EventId)?.Name ?? args.EventId.ToString();
			this.LogInformation("Chassis_BaseEvent: received {eventName} (id-{eventId}) received from device {deviceName}", eventName, args.EventId, device.GetType().Name);
		}


		void Chassis_OnlineStatusChange(Crestron.SimplSharpPro.GenericBase currentDevice, Crestron.SimplSharpPro.OnlineOfflineEventArgs args)
		{
			// TODO - Remove after testing
			this.LogInformation("Chassis_OnlineStatusChange: DeviceOnline = {deviceOnline}", args.DeviceOnLine);

			IsOnline?.FireUpdate();

			if (!args.DeviceOnLine) return;

			// TODO - Remove after testing
			this.LogInformation("Chassis_OnlineStatusChange: Feedbacks has {feedbackCount} items in the collection", Feedbacks.Count);

			foreach (var feedback in Feedbacks)
			{
				// TODO - Remove after testing
				this.LogInformation("Chassis_OnlineStatusChange: Firing update for {feedbackKey}", feedback?.Key);
				feedback?.FireUpdate();
			}

			if (_Chassis is HdMd4xX4kzE)
			{
				AutoRouteFeedback?.FireUpdate();
			}

			if (_Chassis is HdMd4xX4kzE)
			{
				PriorityRouteFeedback?.FireUpdate();
			}
		}

		void Chassis_DMOutputChange(Switch device, DMOutputEventArgs args)
		{
			// TODO - Remove after testing
			var eventName = typeof(DMOutputEventIds)
				.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
				.FirstOrDefault(f => f.IsLiteral && (int)f.GetValue(null) == args.EventId)?.Name ?? args.EventId.ToString();
			this.LogInformation("Chassis_DMOutputChange: received {eventName} (id-{eventId}); Index = {index}; Number = {number}; Stream = {stream} ", eventName, args.EventId, args.Index, args.Number, args.Stream);

			switch (args.EventId)
			{
				case DMOutputEventIds.VideoOutEventId:
					{
						var output = args.Number;
						var outputName = OutputNames[output];

						var inputNumber = _Chassis.HdmiOutputs[output].VideoOutFeedback?.Number ?? 0;

						var feedback = VideoOutputRouteFeedbacks.FirstOrDefault(f => f.Key == outputName);
						if (feedback != null)
						{
							var inPort = InputPorts.FirstOrDefault(p => p.FeedbackMatchObject == _Chassis.HdmiOutputs[output].VideoOutFeedback);
							var outPort = OutputPorts.FirstOrDefault(p => p.FeedbackMatchObject == _Chassis.HdmiOutputs[output]);

							try
							{
								feedback.FireUpdate();
								OnSwitchChange(new RoutingNumericEventArgs(output, inputNumber, outPort, inPort, eRoutingSignalType.AudioVideo));
							}
							catch (Exception ex)
							{
								this.LogError(ex, "Chassis_DMOutputChange: Exception occurred while updating {eventName} (id-{eventId}) {feedbackKey}", eventName, args.EventId, feedback.Key);
							}
						}
						else
						{
							this.LogInformation("Chassis_DMOutputChange: {outputName} not found in VideoOutputRouteFeedbacks", outputName);
						}
						break;
					}
				case DMOutputEventIds.AutoModeOffEventId:
				case DMOutputEventIds.AutoModeOnEventId:
					{
						this.LogInformation("Chassis_DMOutputChange: received {eventName} (id-{eventId}) | Updating AutoRouteFeedback", eventName, args.EventId);
						AutoRouteFeedback?.FireUpdate();

						break;
					}
				case DMOutputEventIds.InputPrioritiesFeedbackEventId:
					{
						this.LogInformation("Chassis_DMOutputChange: received {eventName} (id-{eventId}) | Updating PriorityRouteFeedback", eventName, args.EventId);
						PriorityRouteFeedback?.FireUpdate();

						break;
					}
				case DMOutputEventIds.OutputNameEventId:
				case DMOutputEventIds.NameFeedbackEventId:
					{
						this.LogInformation("Chassis_DMOutputChange: received {eventName} (id-{eventId}) | Output {number} Name {name}, updating OutputNameFeedbacks and OutputRouteNameFeedbacks", eventName, args.EventId, args.Number, _Chassis.HdmiOutputs[args.Number].NameFeedback.StringValue);
						foreach (var item in OutputNameFeedbacks)
						{
							item.FireUpdate();
						}
						break;
					}
				default:
					{
						this.LogInformation("Chassis_DMOutputChange: Unhandled DM Output Event {eventName} (id-{eventId}), ignoring.", eventName, args.EventId);
						break;
					}
			}
		}

		void Chassis_DMInputChange(Switch device, DMInputEventArgs args)
		{
			var eventName = typeof(DMInputEventIds)
				.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
				.FirstOrDefault(f => f.IsLiteral && (int)f.GetValue(null) == args.EventId)?.Name ?? args.EventId.ToString();

			switch (args.EventId)
			{
				case DMInputEventIds.SourceSyncEventId:
				case DMInputEventIds.VideoDetectedEventId:
					{
						this.LogInformation("Chassis_DMInputChange: received {eventName} (id-{eventId}) | Updating VideoInputSyncFeedbacks", eventName, args.EventId);
						foreach (var item in VideoInputSyncFeedbacks)
						{
							item.FireUpdate();
						}
						break;
					}
				case DMInputEventIds.InputNameFeedbackEventId:
				case DMInputEventIds.InputNameEventId:
				case DMInputEventIds.NameFeedbackEventId:
					{
						this.LogInformation("Chassis_DMInputChange: received {eventName} (id-{eventId}) | Input {number} Name {name}, updating InputNameFeedbacks", eventName, args.EventId, args.Number, _Chassis.HdmiInputs[args.Number].NameFeedback.StringValue);
						foreach (var item in InputNameFeedbacks)
						{
							item.FireUpdate();
						}
						break;
					}
				case DMInputEventIds.PriorityEventId:
					{
						this.LogInformation("Chassis_DMInputChange: received {eventName} (id-{eventId}) | Updating PriorityRouteFeedback", eventName, args.EventId);

						PriorityRouteFeedback?.FireUpdate();

						break;
					}
				default:
					{
						this.LogInformation("Chassis_DMInputChange: Unhandled DM Input Event {eventName} (id-{eventId}), ignoring.", eventName, args.EventId);
						break;
					}
			}
		}

		#endregion

		#region Factory

		/// <summary>
		/// Factory for creating HdMdNxM4kZEController devices
		/// </summary>
		public class HdMdNxM4kZEControllerFactory : EssentialsPluginDeviceFactory<HdMdNxM4kZEController>
		{
			/// <summary>
			/// Constructor
			/// </summary>
			public HdMdNxM4kZEControllerFactory()
			{
				MinimumEssentialsFrameworkVersion = "2.24.4";
				TypeNames = new List<string>() { "hdmd4x14kze", "hdmd4x24kze", "hdmd8x84kze" };
			}

			/// <summary>
			/// Builds a HdMdNxM4kZEController device
			/// </summary>
			/// <param name="dc">The device config</param>
			public override EssentialsDevice BuildDevice(DeviceConfig dc)
			{
				Debug.LogDebug("Factory Attempting to create new HD-MD-NxM-4KZ-E Device");

				var props = JsonConvert.DeserializeObject<HdMdNxM4kEPropertiesConfig>(dc.Properties.ToString());

				var type = dc.Type.ToLower();
				var control = props.Control;
				var ipid = control.IpIdInt;
				var address = control.TcpSshProperties.Address;

				switch (type)
				{
					case ("hdmd4x14kze"):
						return new HdMdNxM4kZEController(dc.Key, dc.Name, new HdMd4x14kzE(ipid, Global.ControlSystem), props);
					case ("hdmd4x24kze"):
						return new HdMdNxM4kZEController(dc.Key, dc.Name, new HdMd4x24kzE(ipid, Global.ControlSystem), props);
					case ("hdmd8x84kze"):
						return new HdMdNxM4kZEController(dc.Key, dc.Name, new HdMd8x84kzE(ipid, Global.ControlSystem), props);
					default:
						return null;
				}
			}
		}

		#endregion



	}
}