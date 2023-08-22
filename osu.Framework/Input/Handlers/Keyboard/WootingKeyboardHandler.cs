// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.InteropServices;
using osu.Framework.Input.StateChanges;
using osu.Framework.Logging;
using osu.Framework.Platform;

namespace osu.Framework.Input.Handlers.Keyboard
{
    public class WootingKeyboardHandler : InputHandler
    {
        public override string Description => "Wooting";

        public override bool IsActive => true;

        public override bool Initialize(GameHost host)
        {
            if (!base.Initialize(host))
                return false;

            int initErr = wooting_analog_initialise();

            if (initErr < 0)
            {
                Logger.Log($"Wooting SDK init failed ({initErr})", LoggingTarget.Input);
                return false;
            }

            Logger.Log("Wooting SDK initialised.", LoggingTarget.Input);

            host.InputThread.Scheduler.AddDelayed(pollInput, 0, true);
            return true;
        }

        private short[] keyCodeBuffer = new short[256];
        private float[] analogValueBuffer = new float[256];

        private bool[] lastKeyStates = new bool[256];

        private unsafe void pollInput()
        {
            fixed (short* keyCodePtr = &keyCodeBuffer[0])
            {
                fixed (float* valuePtr = &analogValueBuffer[0])
                {
                    int result = wooting_analog_read_full_buffer(keyCodePtr, valuePtr, keyCodeBuffer.Length);

                    if (result < 0)
                    {
                        Logger.Log($"Failed to read Wooting analog key values ({result}).");
                        return;
                    }

                    for (int i = 0; i < result; i++)
                    {
                        if (analogValueBuffer[i] < 0.5 && lastKeyStates[keyCodeBuffer[i]])
                        {
                            Logger.Log($"Wooting key {keyCodeBuffer[i]} released");
                            lastKeyStates[keyCodeBuffer[i]] = false;
                        }
                        else if (analogValueBuffer[i] >= 0.5 && !lastKeyStates[keyCodeBuffer[i]])
                        {
                            Logger.Log($"Wooting key {keyCodeBuffer[i]} pressed");
                            lastKeyStates[keyCodeBuffer[i]] = true;
                        }
                    }
                }
            }
        }

        [DllImport("wooting_analog_wrapper")]
        private static extern int wooting_analog_initialise();

        private static extern unsafe int wooting_analog_read_full_buffer(short* keyCodes, float* analogValues, int length);
    }
}
