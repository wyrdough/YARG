using System.Collections.Generic;
using Minis;
using PlasticBand.Devices;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using YARG.Core;
using YARG.Core.Game;
using YARG.Core.Logging;
using YARG.Input;
using YARG.Localization;
using YARG.Menu.Data;
using YARG.Menu.Persistent;
using YARG.Menu.ProfileList;
using YARG.Player;

namespace YARG.Menu.Dialogs
{

    public class OnboardingProfileDialog : OneTimeMessageDialog
    {
        [SerializeField]
        private TextMeshProUGUI _deviceText;
        [SerializeField]
        private ProfileListMenu _profileListMenu;

        private readonly List<InputDevice> _devices       = new();
        private readonly List<InputDevice> _manualDevices = new();
        private          ColoredButton     _getStartedButton;
        private          ColoredButton     _closeButton;
        private readonly List<InputDevice> _midiDevices = new();

        protected override void OnEnable()
        {
            InputManager.DeviceAdded += OnDeviceAdded;
            InputManager.DeviceRemoved += OnDeviceRemoved;
            GenerateDeviceText();


            _getStartedButton = AddDialogButton(
                Localize.Key("Menu.Dialog.FirstTimePlayer.Confirm"),
                MenuData.Colors.ConfirmButton,
                StepTwo
            );
            base.OnEnable();
        }

        public void StepTwo()
        {
            // Don't need device text any more
            _deviceText.text = "";

            int[] count = { 1, 1, 1 };
            foreach (var device in _devices)
            {
                GameMode gameMode;
                string profileName;

                if (device is FiveFretGuitar)
                {
                    gameMode = GameMode.FiveFretGuitar;
                    profileName = $"New Guitar Profile {count[0]}";
                    count[0]++;
                }
                else if (device is FourLaneDrumkit)
                {
                    gameMode = GameMode.FourLaneDrums;
                    profileName = $"New Drums Profile {count[1]}";
                    count[1]++;
                }
                else if (device is FiveLaneDrumkit)
                {
                    gameMode = GameMode.FiveLaneDrums;
                    profileName = $"New Drums Profile {count[1]}";
                    count[1]++;
                }
                else if (device is ProKeyboard)
                {
                    gameMode = GameMode.ProKeys;
                    profileName = $"New Keys Profile {count[2]}";
                    count[2]++;
                }
                else
                {
                    continue;
                }

                var newProfile = new YargProfile
                {
                    Name = profileName,
                    NoteSpeed = 5,
                    HighwayLength = 1,
                    GameMode = gameMode
                };

                PlayerContainer.AddProfile(newProfile);

                // Not sure if resolveDevices should be true or false here, so let's guess and see what happens
                var resolveDevices = true;

                var player = PlayerContainer.CreatePlayerFromProfile(newProfile, resolveDevices);
                if (player is null)
                {
                    YargLogger.LogFormatError("Failed to connect profile {0}!", newProfile.Name);
                    return;
                }

                player.Bindings.AddDevice(device);
            }

            StatsManager.Instance.UpdateActivePlayers();

            Message.text = Localize.Key("Menu.Dialog.FirstTimePlayer.StepTwo");
            if (_midiDevices.Count < 1)
            {
                ClearButtons();
                _closeButton = AddDialogButton(
                    Localize.Key("Menu.Dialog.FirstTimePlayer.Close"),
                    MenuData.Colors.ConfirmButton,
                    DialogManager.Instance.ClearDialog
                );
            } else {
                ClearButtons();
                _closeButton = AddDialogButton(
                    Localize.Key("Menu.Dialog.FirstTimePlayer.Close"),
                    MenuData.Colors.ConfirmButton,
                    MidiDeviceMessage
                );
            }
        }

        public void MidiDeviceMessage()
        {
            MenuManager.Instance.PushMenu(MenuManager.Menu.ProfileList);

            ClearButtons();
            _closeButton = AddDialogButton(
                Localize.Key("Menu.Dialog.FirstTimePlayer.Close"),
                MenuData.Colors.ConfirmButton,
                DialogManager.Instance.ClearDialog
            );
            Message.text = Localize.Key("Menu.Dialog.FirstTimePlayer.MIDIInstructions");
        }

        private void GenerateDeviceText()
        {
            _devices.Clear();
            _manualDevices.Clear();
            _midiDevices.Clear();

            // Enumerate the connected devices, then set the text based on connected device count
            foreach (var device in InputSystem.devices)
            {
                if (!device.enabled || PlayerContainer.IsDeviceTaken(device))
                {
                    continue;
                }

                if (device is FiveFretGuitar or FourLaneDrumkit or FiveLaneDrumkit or ProKeyboard)
                {
                    _devices.Add(device);
                }
                else if (device is MidiDevice)
                {
                    _midiDevices.Add(device);
                }
                else
                {
                    _manualDevices.Add(device);
                }
            }

            var deviceText = _devices.Count switch
            {
                0 => Localize.Key("Menu.Dialog.FirstTimePlayer.DeviceNotConnected"),
                1 => Localize.KeyFormat("Menu.Dialog.FirstTimePlayer.OneDeviceConnected", _devices[0].displayName),
                _ => Localize.KeyFormat("Menu.Dialog.FirstTimePlayer.MultipleDevicesConnected", _devices.Count),
            };

            _deviceText.text = deviceText;
        }

        private void OnDeviceAdded(InputDevice device)
        {
            GenerateDeviceText();
        }

        private void OnDeviceRemoved(InputDevice device)
        {
            GenerateDeviceText();
        }

        protected override void OnBeforeClose()
        {
            base.OnBeforeClose();
            InputManager.DeviceAdded -= OnDeviceAdded;
            InputManager.DeviceRemoved -= OnDeviceRemoved;
        }
    }
}