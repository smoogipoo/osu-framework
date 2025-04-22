// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Net.Http;
using System.Threading.Tasks;
using osu.Framework;
using osu.Framework.Graphics;
using osuTK;
using osuTK.Graphics;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Allocation;
using osu.Framework.IO.Network;

namespace SampleGame
{
    public partial class SampleGameGame : Game
    {
        private Box box = null!;

        [BackgroundDependencyLoader]
        private void load()
        {
            Add(box = new Box
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(150, 150),
                Colour = Color4.Tomato
            });
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            _ = checkAttestation();
        }

        private async Task checkAttestation()
        {
            const string endpoint = "";

            IAttestationService? service = Host.CreateAttestationService();

            if (service != null)
            {
                string key = await service.GenerateKey();

                WebRequest req = new WebRequest($"{endpoint}/attestation/create-challenge");
                req.AllowInsecureRequests = true;
                req.Method = HttpMethod.Post;
                req.AddParameter("key", key, RequestParameterType.Form);
                await req.PerformAsync();

                byte[] challenge = Convert.FromBase64String(req.GetResponseString()!);
                string attestation = Convert.ToBase64String(await service.AttestKey(key, challenge));

                req = new WebRequest($"{endpoint}/attestation/verify-attestation");
                req.AllowInsecureRequests = true;
                req.Method = HttpMethod.Post;
                req.AddParameter("key", key, RequestParameterType.Form);
                req.AddParameter("attestation", attestation, RequestParameterType.Form);
                await req.PerformAsync();

                byte[] clientData = [1, 3, 3, 7];
                string assertion = Convert.ToBase64String(await service.GenerateAssertion(key, clientData));

                req = new WebRequest($"{endpoint}/attestation/verify-assertion");
                req.AllowInsecureRequests = true;
                req.Method = HttpMethod.Post;
                req.AddParameter("key", key, RequestParameterType.Form);
                req.AddParameter("assertion", assertion, RequestParameterType.Form);
                req.AddParameter("clientData", Convert.ToBase64String(clientData), RequestParameterType.Form);
                await req.PerformAsync();
            }
        }

        protected override void Update()
        {
            base.Update();
            box.Rotation += (float)Time.Elapsed / 10;
        }
    }
}
