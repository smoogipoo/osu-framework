// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Runtime.InteropServices;
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
            return true;
        }

        [DllImport("wooting_analog_wrapper")]
        private static extern int wooting_analog_initialise();
    }
}
