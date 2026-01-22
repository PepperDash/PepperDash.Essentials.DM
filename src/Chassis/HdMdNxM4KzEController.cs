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


namespace PepperDash.Essentials.DM.Chassis
{
	[Description("Wrapper class for all HdMdNxM4ZE switchers")]
	public class HdMdNxM4kZEController : CrestronGenericBridgeableBaseDevice, IRoutingNumericWithFeedback, IHasFeedback
	{
		private HdMdNxM4kzE _Chassis;

		//IroutingNumericEvent
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
		public string NoRouteText { get; private set; }

		#region Constructor

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
				foreach (var kvp in props.Inputs)
				{
					Debug.LogDebug(this, "props.Inputs: {0}-{1}", kvp.Key, kvp.Value);
				}
				InputNames = props.Inputs;
			}
			if (props.Outputs != null)
			{
				foreach (var kvp in props.Outputs)
				{
					Debug.LogDebug(this, "props.Outputs: {0}-{1}", kvp.Key, kvp.Value);
				}
				OutputNames = props.Outputs;
			}

			DeviceNameFeedback = new StringFeedback("DeviceName", () => Name);

			VideoInputSyncFeedbacks = new FeedbackCollection<BoolFeedback>();
			VideoOutputRouteFeedbacks = new FeedbackCollection<IntFeedback>();
			InputNameFeedbacks = new FeedbackCollection<StringFeedback>();
			OutputNameFeedbacks = new FeedbackCollection<StringFeedback>();
			OutputRouteNameFeedbacks = new FeedbackCollection<StringFeedback>();
			InputHdcpEnableFeedback = new FeedbackCollection<BoolFeedback>();

			InputPorts = new RoutingPortCollection<RoutingInputPort>();
			OutputPorts = new RoutingPortCollection<RoutingOutputPort>();

			if (_Chassis is HdMd4x14kzE _chassis)
			{
				AutoRouteFeedback = new BoolFeedback("AutoRouteFeedback", () => _chassis.AutoRouteOnFeedback?.BoolValue ?? false);
			}

			SetupInputs();
			SetupOutputs();			

			_Chassis.DMInputChange += Chassis_DMInputChange;
			_Chassis.DMOutputChange += Chassis_DMOutputChange;

			AddPostActivationAction(AddFeedbackCollections);
		}

		private void SetupInputs()
		{
			if (InputNames == null)
			{
				Debug.LogMessage(Serilog.Events.LogEventLevel.Error, "SetupInputs: InputNames is null. Ensure 'inputs' is defined in the device configuration.", this);
				return;
			}

			foreach (var kvp in InputNames)
			{
				var index = kvp.Key;
				var inputName = kvp.Value;
				var inputFbKeyPrefix = inputName.Replace(" ", "").Trim();

				if (index < 1 || index > _Chassis.NumberOfInputs)
				{
					Debug.LogMessage(Serilog.Events.LogEventLevel.Warning, "SetupInputs: Input index {index} is out of range (1-{max}). Skipping.", this, index, _Chassis.NumberOfInputs);
					continue;
				}

				var hdmiInput = _Chassis.HdmiInputs[index];
				if (hdmiInput == null)
				{
					Debug.LogMessage(Serilog.Events.LogEventLevel.Error, "SetupInputs: HdmiInput at index {index} is null. Skipping.", this, index);
					continue;
				}

				var chassisInput = _Chassis.Inputs[index];
				if (chassisInput == null)
				{
					Debug.LogMessage(Serilog.Events.LogEventLevel.Error, "SetupInputs: Chassis Input at index {index} is null. Skipping.", this, index);
					continue;
				}

				hdmiInput.Name.StringValue = inputName;

				InputPorts.Add(new RoutingInputPort(inputName, eRoutingSignalType.AudioVideo,
					eRoutingPortConnectionType.Hdmi, hdmiInput, this)
				{
					FeedbackMatchObject = hdmiInput
				});

				VideoInputSyncFeedbacks.Add(new BoolFeedback(string.Format($"{inputFbKeyPrefix}VideoInputSyncFeedback"), () => chassisInput?.VideoDetectedFeedback?.BoolValue ?? false));
				InputNameFeedbacks.Add(new StringFeedback(string.Format($"{inputFbKeyPrefix}InputNameFeedback"), () => InputNames[index]));
				
				if (hdmiInput.HdmiInputPort == null)
				{
					Debug.LogMessage(Serilog.Events.LogEventLevel.Warning, "HdmiInputPort at index {index} is null. HDCP feedback will default to false.", this, index);
				}
				
				InputHdcpEnableFeedback.Add(new BoolFeedback(string.Format($"{inputFbKeyPrefix}HdcpEnableFeedback"), () => hdmiInput?.HdmiInputPort?.HdcpSupportOnFeedback?.BoolValue ?? false));
			}
		}

		private void SetupOutputs()
		{
			if (OutputNames == null)
			{
				Debug.LogMessage(Serilog.Events.LogEventLevel.Error, "SetupOutputs: OutputNames is null. Ensure 'outputs' is defined in the device configuration.", this);
				return;
			}

			foreach (var kvp in OutputNames)
			{
				var index = kvp.Key;
				var outputName = kvp.Value;
				var outputFbKeyPrefix = outputName.Replace(" ", "").Trim();

				if (index < 1 || index > _Chassis.NumberOfOutputs)
				{
					Debug.LogMessage(Serilog.Events.LogEventLevel.Warning, "SetupOutputs: Output index {index} is out of range (1-{max}). Skipping.", this, index, _Chassis.NumberOfOutputs);
					continue;
				}

				var hdmiOutput = _Chassis.HdmiOutputs[index];
				if (hdmiOutput == null)
				{
					Debug.LogMessage(Serilog.Events.LogEventLevel.Error, "SetupOutputs: HdmiOutput at index {index} is null. Skipping.", this, index);
					continue;
				}

				var chassisOutput = _Chassis.Outputs[index];
				if (chassisOutput == null)
				{
					Debug.LogMessage(Serilog.Events.LogEventLevel.Error, "SetupOutputs: Chassis Output at index {index} is null. Skipping.", this, index);
					continue;
				}

				OutputPorts.Add(new RoutingOutputPort(outputName, eRoutingSignalType.AudioVideo,
					eRoutingPortConnectionType.Hdmi, hdmiOutput, this)
				{
					FeedbackMatchObject = hdmiOutput
				});

				VideoOutputRouteFeedbacks.Add(new IntFeedback(string.Format($"{outputFbKeyPrefix}VideoOutputRouteFeedback"), () => chassisOutput.VideoOutFeedback == null ? 0 : (int)chassisOutput.VideoOutFeedback.Number));
				OutputNameFeedbacks.Add(new StringFeedback(string.Format($"{outputFbKeyPrefix}OutputNameFeedback"), () => OutputNames[index]));
				OutputRouteNameFeedbacks.Add(new StringFeedback(string.Format($"{outputFbKeyPrefix}OutputRouteNameFeedback"), () => chassisOutput.VideoOutFeedback == null ? NoRouteText : chassisOutput.VideoOutFeedback.NameFeedback.StringValue));
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

		public void EnableHdcp(uint port)
		{
			if (port > _Chassis.NumberOfInputs) return;
			if (port <= 0) return;

			_Chassis.HdmiInputs[port].HdmiInputPort.HdcpSupportOn();
			InputHdcpEnableFeedback[InputNames[port]].FireUpdate();
		}

		public void DisableHdcp(uint port)
		{
			if (port > _Chassis.NumberOfInputs) return;
			if (port <= 0) return;

			_Chassis.HdmiInputs[port].HdmiInputPort.HdcpSupportOff();
			InputHdcpEnableFeedback[InputNames[port]].FireUpdate();
		}

		public void EnableAutoRoute()
		{
			if (_Chassis.NumberOfOutputs > 1) return;
			if (_Chassis is HdMd4x14kzE _chassis)
			{
				_chassis.AutoRouteOn();
				return;
			}

			Debug.LogVerbose(this, "EnableAutoRoute: AutoRoute is not supported on this chassis.");
		}

		public void DisableAutoRoute()
		{
			if (_Chassis.NumberOfOutputs > 1) return;
			if (_Chassis is HdMd4x14kzE _chassis)
			{
				_chassis.AutoRouteOff();
				return;
			}

			Debug.LogVerbose(this, "DisableAutoRoute: AutoRoute is not supported on this chassis.");
		}


		#region FeedbackCollection Methods
		

		public void AddFeedbackCollections()
		{
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
			Debug.LogInformation(this, $"AddFeedbackCollections: Feedbacks contains {Feedbacks.Count} items");
			foreach (var fb in Feedbacks)
			{
				// TODO - Remove after testing
				Debug.LogInformation(this, $"AddFeedbackCollections: Feedbacks = {fb.Key}");
			}
		}

		//Add Individual Feedbacks
		public void AddFeedbackToList(Essentials.Core.Feedback newFb)
		{
			if (newFb == null) return;

			if (Feedbacks.Any(f => f.Key == newFb.Key)) return;

			// TODO - Remove after testing
			Debug.LogVerbose(this, $"AddFeedbackToList: adding {newFb.Key} to Feedbacks collection");
			Feedbacks.Add(newFb);
		}

		#endregion

		#region IRouting Members

		public void ExecuteSwitch(object inputSelector, object outputSelector, eRoutingSignalType signalType)
		{
			var input = inputSelector as HdMdNxM4kzEHdmiInput;
			var output = outputSelector as HdMdNxM4kzEHdmiOutput;
			Debug.LogVerbose(this, "ExecuteSwitch: input={0} output={1}", input, output);

			if (output == null)
			{
				Debug.LogInformation(this, "Unable to make switch. output selector is not HdMdNxM4kzEHdmiOutput");
				return;
			}

			// Try to make switch only when necessary.  The unit appears to toggle when already selected.
			var current = output.VideoOut;
			if (current != input)
				output.VideoOut = input;
		}

		#endregion

		#region IRoutingNumeric Members

		public void ExecuteNumericSwitch(ushort inputSelector, ushort outputSelector, eRoutingSignalType signalType)
		{
			Debug.LogInformation(this, $"ExecuteNumericSwitch: inputSelector={inputSelector} outputSelector={outputSelector}");

			var input = inputSelector == 0 ? null : _Chassis.HdmiInputs[inputSelector];
			var output = _Chassis.HdmiOutputs[outputSelector];

			Debug.LogVerbose(this, $"ExecuteNumericSwitch: input={input} output={output}");

			ExecuteSwitch(input, output, signalType);
		}

		#endregion

		#endregion

		#region Bridge Linking

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
				Debug.LogInformation(this, "Please update config to use 'eiscapiadvanced' to get all join map features for this device.");
			}

			IsOnline.LinkInputSig(trilist.BooleanInput[joinMap.IsOnline.JoinNumber]);
			DeviceNameFeedback.LinkInputSig(trilist.StringInput[joinMap.Name.JoinNumber]);

			if (_Chassis is HdMd4x14kzE _chassis)
			{
				Debug.LogInformation(this, $"LinkToApi: _Chassis is HdMd4x14kzE, setting up AutoRoute links > joinNumber = {joinMap.EnableAutoRoute.JoinNumber}");

				trilist.SetSigTrueAction(joinMap.EnableAutoRoute.JoinNumber, () => _chassis.AutoRouteOn());
				trilist.SetSigFalseAction(joinMap.EnableAutoRoute.JoinNumber, () => _chassis.AutoRouteOff());
				AutoRouteFeedback?.LinkInputSig(trilist.BooleanInput[joinMap.EnableAutoRoute.JoinNumber]);
			}

			for (uint i = 1; i <= _Chassis.NumberOfInputs; i++)
			{
				var joinIndex = i - 1;
				var input = i;

				var joinNumberInputSync = joinMap.InputSync.JoinNumber + joinIndex;
				var joinNumberEnableInputHdcp = joinMap.EnableInputHdcp.JoinNumber + joinIndex;
				var joinNumberDisableInputHdcp = joinMap.DisableInputHdcp.JoinNumber + joinIndex;
				var joinNumberInputName = joinMap.InputName.JoinNumber + joinIndex;

				Debug.LogInformation(this, $"LinkToApi: _Chassis.NumberOfInputs > {input} | joinIndex = {joinIndex}, InputSyncJoin = {joinNumberInputSync}, EnableHdcpJoin = {joinNumberEnableInputHdcp}, DisableHdcpJoin = {joinNumberDisableInputHdcp}, InputNameJoin = {joinNumberInputName}");

				//Digital
				VideoInputSyncFeedbacks[InputNames[input]].LinkInputSig(trilist.BooleanInput[joinNumberInputSync]);
				InputHdcpEnableFeedback[InputNames[input]].LinkInputSig(trilist.BooleanInput[joinNumberEnableInputHdcp]);
				InputHdcpEnableFeedback[InputNames[input]].LinkComplementInputSig(trilist.BooleanInput[joinNumberDisableInputHdcp]);
				trilist.SetSigTrueAction(joinNumberEnableInputHdcp, () => EnableHdcp(input));
				trilist.SetSigTrueAction(joinNumberDisableInputHdcp, () => DisableHdcp(input));

				//Serial                
				InputNameFeedbacks[InputNames[input]].LinkInputSig(trilist.StringInput[joinNumberInputName]);
			}

			for (uint i = 1; i <= _Chassis.NumberOfOutputs; i++)
			{
				var joinIndex = i - 1;
				var output = i;

				var joinNumberOutputRoute = joinMap.OutputRoute.JoinNumber + joinIndex;
				var joinNumberOutputName = joinMap.OutputName.JoinNumber + joinIndex;
				var joinNumberOutputRouteName = joinMap.OutputRoutedName.JoinNumber + joinIndex;

				Debug.LogInformation(this, $"LinkToApi: _Chassis.NumberOfOutputs > {output} | joinIndex = {joinIndex}, OutputRouteJoin = {joinNumberOutputRoute}, OutputNameJoin = {joinNumberOutputName}, OutputRouteNameJoin = {joinNumberOutputRouteName}");

				//Analog
				VideoOutputRouteFeedbacks[OutputNames[output]].LinkInputSig(trilist.UShortInput[joinNumberOutputRoute]);
				trilist.SetUShortSigAction(joinNumberOutputRoute, (a) => ExecuteNumericSwitch(a, (ushort)output, eRoutingSignalType.AudioVideo));
				//Serial
				OutputNameFeedbacks[OutputNames[output]].LinkInputSig(trilist.StringInput[joinNumberOutputName]);
				OutputRouteNameFeedbacks[OutputNames[output]].LinkInputSig(trilist.StringInput[joinNumberOutputRouteName]);
			}

			_Chassis.OnlineStatusChange += Chassis_OnlineStatusChange;

			trilist.OnlineStatusChange += (d, args) =>
			{
				if (!args.DeviceOnLine) return;

				// feedback updates was moved to the Chassis_OnlineStatusChange 
				// due to the amount of time it takes for the device to come online                
			};
		}


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
				Debug.LogInformation(this, $"UpdateFeedbacks: Firing feedback for {item.Key}");
				item.FireUpdate();
			}
		}

		#endregion

		#region Events

		void Chassis_OnlineStatusChange(Crestron.SimplSharpPro.GenericBase currentDevice, Crestron.SimplSharpPro.OnlineOfflineEventArgs args)
		{
			// TODO - Remove after testing
			Debug.LogInformation(this, $"Chassis_OnlineStatusChange: DeviceOnline = {args.DeviceOnLine}");

			IsOnline.FireUpdate();
			
			if (!args.DeviceOnLine) return;

			// TODO - Remove after testing
			Debug.LogInformation(this, $"Chassis_OnlineStatusChange: Feedbacks has {Feedbacks.Count} items in the collection");

			foreach (var feedback in Feedbacks)
			{
				// TODO - Remove after testing
				Debug.LogInformation(this, $"Chassis_OnlineStatusChange: Firing update for {feedback.Key}");
				feedback.FireUpdate();
			}

			if (_Chassis is HdMd4x14kzE)
			{
				AutoRouteFeedback.FireUpdate();
			}
		}

		void Chassis_DMOutputChange(Switch device, DMOutputEventArgs args)
		{
			// TODO - Remove after testing
			var eventName = typeof(DMOutputEventIds)
                .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
    			.FirstOrDefault(f => f.IsLiteral && (int)f.GetValue(null) == args.EventId)?.Name ?? args.EventId.ToString();
			Debug.LogInformation(this, $"Chassis_DMOutputChange: received {eventName} (id-{args.EventId}); Index = {args.Index}; Number = {args.Number}; Stream = {args.Stream} ");

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

							feedback.FireUpdate();
							OnSwitchChange(new RoutingNumericEventArgs(output, inputNumber, outPort, inPort, eRoutingSignalType.AudioVideo));
						}
						else
						{
							Debug.LogInformation(this, $"Chassis_DMOutputChange: {outputName} not found in VideoOutputRouteFeedbacks");
						}
						break;
					}
				default:
					{
						Debug.LogDebug(this, $"Chassis_DMOutputChange: Unhandled DM Output Event {eventName} (id-{args.EventId}), ignoring.");
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
					case DMInputEventIds.VideoDetectedEventId:
						{
							Debug.LogDebug(this, $"Chassis_DMInputChange: received {eventName} (id-{args.EventId}) | Updating VideoInputSyncFeedbacks");
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
							Debug.LogDebug(this, $"Chassis_DMInputChange: received {eventName} (id-{args.EventId}) | Input {args.Number} Name {_Chassis.HdmiInputs[args.Number].NameFeedback.StringValue}, updating InputNameFeedbacks");
							foreach (var item in InputNameFeedbacks)
							{
								item.FireUpdate();
							}
							break;
						}
					default:
						{
							Debug.LogDebug(this, $"Chassis_DMInputChange: Unhandled DM Input Event {eventName} (id-{args.EventId}), ignoring.");
							break;
						}
				}
			}

		#endregion

		#region Factory

		public class HdMdNxM4kZEControllerFactory : EssentialsPluginDeviceFactory<HdMdNxM4kZEController>
		{
			public HdMdNxM4kZEControllerFactory()
			{
				MinimumEssentialsFrameworkVersion = "2.24.4";
				TypeNames = new List<string>() { "hdmd4x14kze", "hdmd4x24kze", "hdmd8x84kze" };
			}

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