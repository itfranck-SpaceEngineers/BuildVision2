﻿using DarkHelmet.Game;
using Sandbox.ModAPI;
using System;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace DarkHelmet.IO
{
    /// <summary>
    /// Generic base for serializable config types.
    /// </summary>
    public abstract class Config<ConfigT> where ConfigT : Config<ConfigT>, new()
    {
        public static ConfigT Defaults
        {
            get
            {
                if (defaults == null)
                    defaults = new ConfigT().GetDefaults();

                return defaults;
            }
        }

        public abstract void Validate();

        private static ConfigT defaults;
        protected abstract ConfigT GetDefaults();
    }

    /// <summary>
    /// Base class for config root. Handles its own serialization/deserialization.
    /// </summary>
    public abstract class ConfigRoot<ConfigT> : Config<ConfigT> where ConfigT : ConfigRoot<ConfigT>, new()
    {
        [XmlAttribute("ConfigVersionID")]
        public virtual int VersionID { get; set; }

        public static event Action OnConfigLoad;
        public static ConfigT Current { get; private set; }
        public static string FileName { get { return ConfigIO.FileName; } set { ConfigIO.FileName = value; } }

        /// <summary>
        /// Loads config from file and applies it. Runs synchronously.
        /// </summary>
        public static void Load(bool silent = false)
        {
            Current = ConfigIO.Instance.Load(silent);
            OnConfigLoad?.Invoke();
        }

        /// <summary>
        /// Loads config from file and applies it. Runs in parallel.
        /// </summary>
        public static void LoadStart(bool silent = false) =>
            LoadStart(null, silent);

        /// <summary>
        /// Loads config from file and applies it. Runs in parallel.
        /// </summary>
        public static void LoadStart(Action Callback, bool silent = false)
        {
            ConfigIO.Instance.LoadStart((ConfigT value) =>
            {
                Current = value;
                OnConfigLoad?.Invoke();
                Callback?.Invoke();
            }, silent);
        }

        /// <summary>
        /// Writes the current configuration to the config file. Runs synchronously.
        /// </summary>
        public static void Save() =>
            ConfigIO.Instance.Save(Current);

        /// <summary>
        /// Writes the current configuration to the config file. Runs in parallel.
        /// </summary>
        public static void SaveStart(bool silent = false) =>
            ConfigIO.Instance.SaveStart(Current, silent);

        /// <summary>
        /// Resets the current configuration to the default settings and saves them.
        /// </summary>
        public static void ResetConfig(bool silent = false)
        {
            ConfigIO.Instance.SaveStart(Defaults, silent);
            Current = Defaults;
        }

        /// <summary>
        /// Handles loading/saving configuration data; singleton
        /// </summary>
        private sealed class ConfigIO : ModBase.ParallelComponent<ConfigIO>
        {
            public static string FileName { get { return fileName; } set { if (value != null && value.Length > 0) fileName = value; } }
            private static string fileName = $"config_{typeof(ConfigT).Name}.xml";

            public bool SaveInProgress { get; private set; }
            private readonly LocalFileIO cfgFile;

            public ConfigIO()
            {
                cfgFile = new LocalFileIO(FileName);
                SaveInProgress = false;
            }

            protected override void ErrorCallback(List<KnownException> known, AggregateException unknown)
            {
                if (known != null && known.Count > 0)
                {
                    SaveInProgress = false;
                    string exceptions = "";

                    foreach (Exception e in known)
                    {
                        ModBase.SendChatMessage(e.Message);
                        exceptions += e.ToString();
                    }

                    LogIO.Instance.WriteToLogStart(exceptions);
                }

                if (unknown != null)
                {
                    LogIO.Instance.WriteToLogStart("\nSave operation failed.\n" + unknown.ToString());
                    ModBase.SendChatMessage("Save operation failed.");
                    SaveInProgress = false;

                    throw unknown;
                }
            }

            /// <summary>
            /// Loads the current configuration synchronously.
            /// </summary>
            public ConfigT Load(bool silent = false)
            {
                ConfigT cfg = null;
                KnownException loadException, saveException;

                if (!SaveInProgress)
                {
                    SaveInProgress = true;

                    if (!silent) ModBase.SendChatMessage("Loading configuration...");

                    loadException = TryLoad(out cfg);
                    cfg = ValidateConfig(cfg);
                    saveException = TrySave(cfg);

                    if (loadException != null)
                    {
                        //loadException = TrySave(cfg);

                        if (saveException != null)
                        {
                            LogIO.Instance.TryWriteToLog(loadException.ToString() + "\n" + saveException.ToString());
                            ModBase.SendChatMessage("Unable to load or create configuration file.");
                        }
                    }
                    else
                        ModBase.SendChatMessage("Configuration loaded.");
                }
                else
                    ModBase.SendChatMessage("Save operation already in progress.");

                SaveInProgress = false;
                return cfg;
            }

            /// <summary>
            /// Loads the current configuration in parallel.
            /// </summary>
            public void LoadStart(Action<ConfigT> UpdateConfig, bool silent = false)
            {
                if (!SaveInProgress)
                {
                    SaveInProgress = true;
                    if (!silent) ModBase.SendChatMessage("Loading configuration...");

                    EnqueueTask(() =>
                    {
                        ConfigT cfg;
                        KnownException loadException, saveException;

                        // Load and validate
                        loadException = TryLoad(out cfg);
                        cfg = ValidateConfig(cfg);

                        // Enqueue callback when the configuration
                        EnqueueAction(() =>
                            UpdateConfig(cfg));

                        // Write validated config back to the file
                        saveException = TrySave(cfg);

                        if (loadException != null)
                        {
                            //loadException = TrySave(cfg);
                            EnqueueAction(() =>
                                LoadFinish(false, silent));

                            if (saveException != null)
                            {
                                LogIO.Instance.TryWriteToLog(loadException.ToString() + "\n" + saveException.ToString());

                                EnqueueAction(() =>
                                    ModBase.SendChatMessage("Unable to load or create configuration file."));
                            }
                        }
                        else
                            EnqueueAction(() =>
                                LoadFinish(true, silent));
                    });
                }
                else
                    ModBase.SendChatMessage("Save operation already in progress.");
            }

            private ConfigT ValidateConfig(ConfigT cfg)
            {
                if (cfg != null)
                {
                    if (cfg.VersionID != Defaults.VersionID)
                    {
                        EnqueueAction(() =>
                            ModBase.SendChatMessage("Config version mismatch. Some settings may have " +
                            "been reset. A backup of the original config file will be made."));

                        Backup();
                    }

                    cfg.Validate();

                    return cfg;
                }
                else
                {
                    EnqueueAction(() =>
                    ModBase.SendChatMessage("Unable to load configuration. Loading default settings..."));

                    return Defaults;
                }
            }

            private void LoadFinish(bool success, bool silent = false)
            {
                if (SaveInProgress)
                {
                    if (!silent)
                    {
                        if (success)
                            ModBase.SendChatMessage("Configuration loaded.");
                    }

                    SaveInProgress = false;
                }
            }

            /// <summary>
            /// Saves a given configuration to the save file in parallel.
            /// </summary>
            public void SaveStart(ConfigT cfg, bool silent = false)
            {
                if (!SaveInProgress)
                {
                    if (!silent) ModBase.SendChatMessage("Saving configuration...");
                    SaveInProgress = true;

                    EnqueueTask(() =>
                    {
                        cfg.Validate();
                        KnownException exception = TrySave(cfg);

                        if (exception != null)
                        {
                            EnqueueAction(() =>
                                SaveFinish(false, silent));

                            throw exception;
                        }
                        else
                            EnqueueAction(() =>
                                SaveFinish(true, silent));
                    });
                }
                else
                    ModBase.SendChatMessage("Save operation already in progress.");
            }

            private void SaveFinish(bool success, bool silent = false)
            {
                if (SaveInProgress)
                {
                    if (!silent)
                    {
                        if (success)
                            ModBase.SendChatMessage("Configuration saved.");
                        else
                            ModBase.SendChatMessage("Unable to save configuration.");
                    }

                    SaveInProgress = false;
                }
            }

            /// <summary>
            /// Saves the current configuration synchronously.
            /// </summary>
            public void Save(ConfigT cfg)
            {
                if (!SaveInProgress)
                {
                    cfg.Validate();
                    KnownException exception = TrySave(cfg);

                    if (exception != null)
                        throw exception;
                }
            }

            /// <summary>
            /// Creates a duplicate of the config file starting with a new file name starting with "old_"
            /// if one exists.
            /// </summary>
            private void Backup()
            {
                if (MyAPIGateway.Utilities.FileExistsInLocalStorage(cfgFile.file, typeof(ConfigT)))
                {
                    KnownException exception = cfgFile.TryDuplicate($"old_" + cfgFile.file);

                    if (exception != null)
                        throw exception;
                }
            }

            /// <summary>
            /// Attempts to load config file and creates a new one if it can't.
            /// </summary>
            private KnownException TryLoad(out ConfigT cfg)
            {
                string data;
                KnownException exception = cfgFile.TryRead(out data);
                cfg = null;

                if (exception != null || data == null)
                    return exception;
                else
                    exception = Utils.Xml.TryDeserialize(data, out cfg);

                if (exception != null)
                {
                    Backup();
                    TrySave(Defaults);
                }

                return exception;
            }

            /// <summary>
            /// Attempts to save current configuration to a file.
            /// </summary>
            private KnownException TrySave(ConfigT cfg)
            {
                string xmlOut;
                KnownException exception = Utils.Xml.TrySerialize(cfg, out xmlOut);

                if (exception == null && xmlOut != null)
                    exception = cfgFile.TryWrite(xmlOut);

                return exception;
            }
        }
    }
}