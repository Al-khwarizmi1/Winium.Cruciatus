﻿namespace Cruciatus.Core
{
    #region using

    using System;
    using System.Threading;

    using WindowsInput;
    using WindowsInput.Native;

    using NLog;

    #endregion

    public class Keyboard
    {
        private readonly Logger _logger;

        private readonly IKeyboardSimulator _keyboardSimulator;

        public Keyboard(Logger logger, IKeyboardSimulator keyboardSimulator)
        {
            _logger = logger;
            _keyboardSimulator = keyboardSimulator;
        }

        [Obsolete("SendKeys is deprecated, please use SendText instead.")]
        public Keyboard SendKeys(string text)
        {
            return SendText(text);
        }

        public Keyboard SendText(string text)
        {
            _logger.Info("Send text '{0}'", text);
            if (!string.IsNullOrEmpty(text))
            {
                _keyboardSimulator.TextEntry(text);
                Thread.Sleep(250);
            }

            return this;
        }

        public Keyboard SendEnter()
        {
            KeyPress(VirtualKeyCode.RETURN);
            return this;
        }

        public Keyboard SendBackspace()
        {
            KeyPress(VirtualKeyCode.BACK);
            return this;
        }

        public Keyboard SendEscape()
        {
            KeyPress(VirtualKeyCode.ESCAPE);
            return this;
        }

        public Keyboard SendCtrlA()
        {
            KeyPressWithModifier(VirtualKeyCode.VK_A, VirtualKeyCode.CONTROL);
            return this;
        }

        public Keyboard SendCtrlC()
        {
            KeyPressWithModifier(VirtualKeyCode.VK_C, VirtualKeyCode.CONTROL);
            return this;
        }

        public Keyboard SendCtrlV()
        {
            KeyPressWithModifier(VirtualKeyCode.VK_V, VirtualKeyCode.CONTROL);
            return this;
        }

        private IKeyboardSimulator KeyPress(VirtualKeyCode keyCode)
        {
            _logger.Info("Key press '{0}'", keyCode.ToString());
            _keyboardSimulator.KeyPress(keyCode);
            Thread.Sleep(250);
            return _keyboardSimulator;
        }

        private IKeyboardSimulator KeyPressWithModifier(VirtualKeyCode keyCode, VirtualKeyCode modifierKeyCode)
        {
            _logger.Info("Press key combo '{0} + {1}'", modifierKeyCode, keyCode);
            _keyboardSimulator.ModifiedKeyStroke(modifierKeyCode, keyCode);
            Thread.Sleep(250);
            return _keyboardSimulator;
        }
    }
}
