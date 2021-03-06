﻿
using RichHudFramework.UI;
using RichHudFramework.UI.Client;
using VRage.Input;

namespace DarkHelmet.BuildVision2
{
    /// <summary>
    /// Wrapper used to provide easy access to Build Vision key binds.
    /// </summary>
    public sealed class BvBinds : BvComponentBase
    {
        public static BindsConfig Cfg
        {
            get { return new BindsConfig { bindData = BindGroup.GetBindDefinitions() }; }
            set { Instance.bindGroup.TryLoadBindData(value.bindData); }
        }

        public static IBind Open { get { return BindGroup[0]; } }
        public static IBind Hide { get { return BindGroup[1]; } }

        public static IBind Select { get { return BindGroup[2]; } }
        public static IBind ScrollUp { get { return BindGroup[3]; } }
        public static IBind ScrollDown { get { return BindGroup[4]; } }

        public static IBind MultX { get { return BindGroup[5]; } }
        public static IBind MultY { get { return BindGroup[6]; } }
        public static IBind MultZ { get { return BindGroup[7]; } }

        public static IBind ToggleSelectMode { get { return BindGroup[8]; } }
        public static IBind SelectAll { get { return BindGroup[9]; } }
        public static IBind CopySelection { get { return BindGroup[10]; } }
        public static IBind PasteProperties { get { return BindGroup[11]; } }
        public static IBind UndoPaste { get { return BindGroup[12]; } }

        public static IBindGroup BindGroup { get { return Instance.bindGroup; } }

        private static BvBinds Instance
        {
            get 
            {
                if (_instance == null)
                    Init();

                return _instance;
            }
            set { _instance = value; }
        }
        private static BvBinds _instance;
        private readonly IBindGroup bindGroup;

        private BvBinds() : base(false, true)
        {
            bindGroup = BindManager.GetOrCreateGroup("BvMain");
            bindGroup.RegisterBinds(BindsConfig.DefaultBinds);
        }

        private static void Init()
        {
            if (_instance == null)
            {
                _instance = new BvBinds();
                Cfg = BvConfig.Current.binds;

                BvConfig.OnConfigSave += _instance.UpdateConfig;
                BvConfig.OnConfigLoad += _instance.UpdateBinds;
            }
        }

        public override void Close()
        {
            BvConfig.OnConfigSave -= UpdateConfig;
            BvConfig.OnConfigLoad -= UpdateBinds;
            Instance = null;
        }

        private void UpdateConfig() =>
            BvConfig.Current.binds = Cfg;

        private void UpdateBinds() =>
            Cfg = BvConfig.Current.binds;
    }
}