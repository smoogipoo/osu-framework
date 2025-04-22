// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Runtime.InteropServices;

namespace osu.Framework.Platform.Apple.Native
{
    internal readonly struct DCAppAttestService
    {
        internal IntPtr Handle { get; }

        // ReSharper disable once UnusedMember.Local
        private static readonly IntPtr lib_pointer = Interop.Open(Interop.LIB_DEVICE_CHECK);
        private static readonly IntPtr class_pointer = Class.Get("DCAppAttestService");
        private static readonly IntPtr sel_shared_service = Selector.Get("sharedService");
        private static readonly IntPtr sel_is_supported = Selector.Get("isSupported");
        private static readonly IntPtr sel_generate_key = Selector.Get("generateKeyWithCompletionHandler:");
        private static readonly IntPtr sel_attest_key = Selector.Get("attestKey:clientDataHash:completionHandler:");
        private static readonly IntPtr sel_generate_assertion = Selector.Get("generateAssertion:clientDataHash:completionHandler:");

        internal DCAppAttestService(IntPtr handle)
        {
            Handle = handle;
        }

        /// <summary>
        /// Whether the App Attest service is supported on this device.
        /// </summary>
        internal bool IsSupported => Interop.SendBool(Handle, sel_is_supported);

        /// <summary>
        /// Creates a new cryptographic key for use with the App Attest service.
        /// </summary>
        /// <param name="handler">
        /// A callback containing the generated key, stored in the device's secure enclave.
        /// The key should be passed into all future calls of <see cref="AttestKey"/> and <see cref="GenerateAssertion"/>,
        /// stored per-user and reused for the installed duration of the app.
        /// </param>
        internal void GenerateKey(Action<NSString, int> handler)
        {
            GCHandle pin = default;

            Action<NSString, int> pinnedHandler = (id, err) =>
            {
                // ReSharper disable once AccessToModifiedClosure
                pin.Free();
                handler(id, err);
            };

            pin = GCHandle.Alloc(pinnedHandler, GCHandleType.Pinned);

            Interop.SendIntPtr(Handle, sel_generate_key, Marshal.GetFunctionPointerForDelegate(pinnedHandler));
        }

        /// <summary>
        /// Asks Apple to attest to the validity of a generated cryptographic key.
        /// </summary>
        /// <param name="keyId">The device attestation key created via <see cref="GenerateKey"/>.</param>
        /// <param name="clientDataHash">A SHA256 hash of a unique, single-use data block that embeds a challenge from your server. Should be at least 16 bytes in length.</param>
        /// <param name="handler">A callback containing a statement from Apple about the validity of the key associated with <paramref name="keyId"/>. Send this to your server for processing.</param>
        internal void AttestKey(NSString keyId, NSData clientDataHash, Action<NSData, NSError> handler)
        {
            GCHandle pin = default;

            Action<NSData, NSError> pinnedHandler = (id, err) =>
            {
                // ReSharper disable once AccessToModifiedClosure
                pin.Free();
                handler(id, err);
            };

            pin = GCHandle.Alloc(pinnedHandler, GCHandleType.Pinned);

            Interop.SendIntPtr(Handle, sel_attest_key, keyId, clientDataHash, Marshal.GetFunctionPointerForDelegate(pinnedHandler));
        }

        /// <summary>
        /// Creates a block of data that demonstrates the legitimacy of an instance of your app running on a device.
        /// </summary>
        /// <param name="keyId">The device attestation key created via <see cref="GenerateKey"/>.</param>
        /// <param name="clientDataHash">A SHA256 hash of a unique, single-use data block that represents the client data to be signed with the attested private key. Should be at least 16 bytes in length.</param>
        /// <param name="handler">A callback containing the attestation object that should be sent to the server and validated.</param>
        internal void GenerateAssertion(NSString keyId, NSData clientDataHash, Action<NSData, NSError> handler)
        {
            GCHandle pin = default;

            Action<NSData, NSError> pinnedHandler = (id, err) =>
            {
                // ReSharper disable once AccessToModifiedClosure
                pin.Free();
                handler(id, err);
            };

            pin = GCHandle.Alloc(pinnedHandler, GCHandleType.Pinned);

            Interop.SendIntPtr(Handle, sel_generate_assertion, keyId, clientDataHash, Marshal.GetFunctionPointerForDelegate(pinnedHandler));
        }

        /// <summary>
        /// The shared App Attest service that you use to validate your app
        /// </summary>
        internal static DCAppAttestService Shared => new DCAppAttestService(Interop.SendIntPtr(class_pointer, sel_shared_service));
    }
}
