using PepperDash.Core;
using PepperDash.Essentials.Core;
using PepperDash.Essentials.Core.Routing;
using Crestron.SimplSharpPro.DM;
using Crestron.SimplSharpPro.DM.Cards;
using System;
using System.Collections.Generic;
using System.Linq;

namespace PepperDash.Essentials.DM.Routing
{
    public class DmMatrixOutput :IRoutingOutputSlot
    {       
        private readonly CardDevice _device;
        private readonly DmChassisController _chassis;
        private readonly string _key;

        public DmMatrixOutput(CardDevice device, DmChassisController chassis, string key, string name)
        {
            try
            {
                _device = device;
                _chassis = chassis;
                _key = key;
                Name = name;

                _device.OnlineStatusChange += _device_OnlineStatusChange;

                _device.Switcher.DMOutputChange += Switcher_DMOutputChange;

            } catch (Exception ex)
            {
                Debug.LogMessage(ex, "Exception creating DmMatrixOutput {ex}", this, ex.Message);                
            }
        }

        private void Switcher_DMOutputChange(Switch device, DMOutputEventArgs args)
        {
            if (SlotNumber != args.Number) return;

            uint inputNumber = 0;
            var routeType = eRoutingSignalType.Video;



            switch (args.EventId)
            {
                case DMOutputEventIds.VideoOutEventId:
                    {
                        inputNumber = device.Outputs[(uint)SlotNumber].VideoOutFeedback == null ? 0 : device.Outputs[(uint)SlotNumber].VideoOutFeedback.Number;
                        routeType = eRoutingSignalType.Video;
                        break;
                    }
                case DMOutputEventIds.AudioOutEventId:
                    {
                        inputNumber = device.Outputs[(uint)SlotNumber].AudioOutFeedback == null ? 0 : device.Outputs[(uint)SlotNumber].AudioOutFeedback.Number;
                        routeType = eRoutingSignalType.Audio;
                        break;
                    }
                default:    return;
            }
            var inputSlot = _chassis.InputSlots.Values.FirstOrDefault(input => input.SlotNumber == inputNumber);
            SetInputRoute(routeType, inputSlot);
            
        }

        public string RxDeviceKey => "";

        private readonly Dictionary<eRoutingSignalType, IRoutingInputSlot> currentRoutes = new Dictionary<eRoutingSignalType, IRoutingInputSlot>
        {
            {eRoutingSignalType.Audio, default },
            {eRoutingSignalType.Video, default },
            {eRoutingSignalType.UsbInput, default },
            {eRoutingSignalType.UsbOutput, default },
        };

        private void SetInputRoute(eRoutingSignalType type, IRoutingInputSlot input)
        {
            if (currentRoutes.ContainsKey(type))
            {
                currentRoutes[type] = input;

                OutputSlotChanged?.Invoke(this, new EventArgs());

                return;
            }

            currentRoutes.Add(type, input);

            OutputSlotChanged?.Invoke(this, new EventArgs());
        }
        private void _device_OnlineStatusChange(Crestron.SimplSharpPro.GenericBase currentDevice, Crestron.SimplSharpPro.OnlineOfflineEventArgs args)
        {
            IsOnline.FireUpdate();
        }
        public Dictionary<eRoutingSignalType, IRoutingInputSlot> CurrentRoutes => currentRoutes;

        public int SlotNumber => (int)_device.SwitcherInputOutput.Number;
        public eRoutingSignalType SupportedSignalTypes => eRoutingSignalType.AudioVideo;
        public CardDevice Device => _device;
        public string Name { get; private set; }
        public BoolFeedback IsOnline { get; private set; }

        public string Key => $"{_key}";

        public event EventHandler OutputSlotChanged;
    }
}
