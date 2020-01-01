﻿using RichHudFramework.Game;
using RichHudFramework.UI;
using RichHudFramework.UI.FontData;
using RichHudFramework.UI.Rendering;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using VRage;
using VRage.Game.Components;
using VRage.Game.ModAPI;
using VRageMath;

namespace RichHudFramework.Server
{
    using UI.Server;
    using UI.Rendering.Server;
    using ClientData = MyTuple<string, Action<int, object>, Action>;

    [MySessionComponentDescriptor(MyUpdateOrder.BeforeSimulation, 1)]
    internal sealed partial class RichHudMaster : ModBase
    {
        private const long modID = 0112358132134, queueID = 1314086443; // replace this with the real mod ID when you're done
        private static new RichHudMaster Instance { get; set; }
        private readonly List<RichHudClient> clients;

        static RichHudMaster()
        {
            ModName = "Rich HUD Master";
            LogFileName = "RichHudMasterLog.txt";
        }

        public RichHudMaster() : base(false, true)
        {
            clients = new List<RichHudClient>();
        }

        protected override void AfterInit()
        {
            Instance = this;
            InitializeFonts();
            BindManager.Init();
            HudMain.Init();

            SendChatMessage($"Server Init");
            MyAPIUtilities.Static.RegisterMessageHandler(modID, MessageHandler);
            SendChatMessage($"Checking client queue...");
            MyAPIUtilities.Static.SendModMessage(queueID, modID);

            /*TextEditor textEditor = new TextEditor(HudMain.Root)
            {
                Size = new Vector2(500f, 300f),
                BodyColor = new Color(0, 0, 0, 125),
                Offset = new Vector2(500f, 0),
            };

            textEditor.textBox.BuilderMode = TextBuilderModes.Wrapped;
            textEditor.Title.SetText(new RichText() { { new GlyphFormat(Color.Black, alignment: TextAlignment.Center), "I'm a text editor without word wrapping!" } });
            textEditor.textBox.TextBoard.SetText(new RichString(
                "Lorem ipsum dolor sit amet, consectetur adipiscing elit. Morbi ultrices ipsum vel orci congue, sed lobortis ante lacinia. Phasellus egestas convallis lectus et aliquet. Suspendisse et lacinia purus. In ligula nisl, congue eu eros vitae, auctor vehicula nibh. Praesent eu sodales felis. Vestibulum et nisl imperdiet, facilisis urna non, euismod ligula. Etiam vel purus non mauris egestas mollis in id quam. Integer felis urna, dignissim eu ex eu, viverra finibus augue. Sed semper nibh eget mollis lobortis.", 
                GlyphFormat.White.WithFont(FontManager.GetFont("TimesNewRoman").Index))
            );

            TextEditor textEditor2 = new TextEditor(HudMain.Root)
            {
                Size = new Vector2(500f, 300f),
                BodyColor = new Color(0, 0, 0, 125),
                Offset = new Vector2(500f, 0)
            };

            textEditor2.Title.Append(new RichText() { { new GlyphFormat(Color.Black, alignment: TextAlignment.Center), "I'm a text editor without word wrapping!" } });*/
        }

        private void InitializeFonts()
        {
            FontManager.TryAddFont(SeFont.fontData);
            FontManager.TryAddFont(SeFontShadowed.fontData);
            FontManager.TryAddFont(MonoFont.fontData);
            FontManager.TryAddFont(TimesNewRoman.fontData);

            foreach (IFontMin font in FontManager.Fonts)
                SendChatMessage($"Font {font.Index}: {font.Name}, Size: {font.PtSize}");
        }

        private void MessageHandler(object message)
        {
            if (message is ClientData)
            {
                var clientData = (ClientData)message;
                Utils.Debug.AssertNotNull(clientData.Item1);
                RichHudClient client = clients.Find(x => (x.debugName == clientData.Item1));
                SendChatMessage($"Recieved registration request from {clientData.Item1}");

                if (client == null)
                {
                    clients.Add(new RichHudClient(clientData));
                }
                else
                    client.SendData(MsgTypes.RegistrationFailed, "Client already registered.");
            }
        }

        protected override void BeforeClose()
        {
            MyAPIUtilities.Static.UnregisterMessageHandler(modID, MessageHandler);

            for (int n = clients.Count - 1; n >= 0; n--)
                clients[n].Unregister();

            Instance = null;
        }
    }
}