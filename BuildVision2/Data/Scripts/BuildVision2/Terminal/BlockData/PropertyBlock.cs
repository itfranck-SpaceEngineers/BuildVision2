using RichHudFramework;
using Sandbox.ModAPI;
using Sandbox.ModAPI.Interfaces;
using Sandbox.ModAPI.Interfaces.Terminal;
using System.Collections.Generic;
using System.Text;
using VRage;
using VRage.ModAPI;
using VRageMath;

namespace DarkHelmet.BuildVision2
{
    /// <summary>
    /// Block property data used by the menu
    /// </summary>
    public partial class PropertyBlock
    {
        public static PropBlockConfig Cfg { get { return BvConfig.Current.block; } set { BvConfig.Current.block = value; } }

        public SuperBlock BlockData { get; }

        /// <summary>
        /// Read-only collection of block members
        /// </summary>
        public ReadOnlyCollection<IBlockMember> BlockMembers { get; }

        /// <summary>
        /// Total number of block members currently enabled and visible
        /// </summary>
        public int EnabledMembers => GetEnabledElementCount();

        public readonly Vector3D modelOffset;

        private readonly List<IBlockMember> blockMembers;
        private readonly List<BvTerminalPropertyBase> blockProperties;

        public PropertyBlock(SuperBlock data)
        {
            BlockData = data;

            blockMembers = new List<IBlockMember>();
            blockProperties = new List<BvTerminalPropertyBase>();
            BlockMembers = new ReadOnlyCollection<IBlockMember>(blockMembers);

            BoundingBoxD bb;
            data.TBlock.SlimBlock.GetWorldBoundingBox(out bb);
            modelOffset = bb.Center - data.TBlock.GetPosition();
        }

        public void GenerateProperties()
        {
            GetScrollableProps();
            GetScrollableActions();
        }

        /// <summary>
        /// Gets the block's current position.
        /// </summary>
        public Vector3D GetPosition() =>
             BlockData.TBlock != null ? BlockData.TBlock.GetPosition() : Vector3D.Zero;

        /// <summary>
        /// Applies property settings from block data and returns the number of properties successfully updated.
        /// </summary>
        public int ImportSettings(BlockData src)
        {
            int importCount = 0;

            foreach (PropertyData propData in src.terminalProperties)
            {
                BvTerminalPropertyBase prop = blockProperties.Find(x => (x.ID == propData.id) && (x.PropName == propData.name));

                if (prop != null)
                {
                    if (prop.TryImportPropertyValue(propData))
                        importCount++;
                }
            }

            return importCount;
        }

        public BlockData ExportSettings()
        {
            var propData = new List<PropertyData>(blockProperties.Count);

            for (int n = 0; n < blockProperties.Count; n++)
                propData.Add(blockProperties[n].GetPropertyData());

            return new BlockData(BlockData.TypeID, propData);
        }

        private int GetEnabledElementCount()
        {
            int count = 0;

            foreach (IBlockMember member in blockMembers)
                if (member.Enabled)
                    count++;

            return count;
        }

        /// <summary>
        /// Retrieves a Block Property's Terminal Name.
        /// </summary>
        private static string GetTooltipName(ITerminalProperty prop)
        {
            if (prop is IMyTerminalControlTitleTooltip)
            {
                var tooltip = prop as IMyTerminalControlTitleTooltip;
                StringBuilder name = MyTexts.Get(tooltip.Title), cleanedName;
                int trailingCharacters = 0;

                for (int n = name.Length - 1; n >= 0; n--)
                {
                    if ((name[n] >= '0' && name[n] <= '9') || name[n] >= 'A')
                        break;
                    else
                        trailingCharacters++;
                }

                cleanedName = new StringBuilder(name.Length - trailingCharacters);

                for (int n = 0; n < (name.Length - trailingCharacters); n++)
                {
                    if (name[n] >= ' ')
                        cleanedName.Append(name[n]);
                }

                return cleanedName.ToString();
            }
            else
                return "";
        }

        /// <summary>
        /// Filters out any any special characters from a given string.
        /// </summary>
        private static string CleanText(StringBuilder text)
        {
            if (text != null)
            {
                StringBuilder cleanedText = new StringBuilder(text.Length);

                for (int n = 0; n < text.Length; n++)
                {
                    if (text[n] >= ' ')
                        cleanedText.Append(text[n]);
                }

                return cleanedText.ToString();
            }
            else
                return "";
        }

        /// <summary>
        /// Retrieves all block ITerminalProperty values.
        /// </summary>
        private void GetScrollableProps()
        {
            List<ITerminalProperty> properties = new List<ITerminalProperty>(12);
            string name;
            BlockData.TBlock.GetProperties(properties);

            foreach (ITerminalProperty prop in properties)
            {
                var control = prop as IMyTerminalControl;

                if (control != null && control.CanUseControl(BlockData.TBlock))
                {
                    name = GetTooltipName(prop);

                    if (name.Length > 0)
                    {
                        if (prop is ITerminalProperty<StringBuilder>)
                        {
                            var textProp = prop as ITerminalProperty<StringBuilder>;

                            if (textProp.CanAccessValue(BlockData.TBlock))
                            {
                                if (prop.Id == "Name" || prop.Id == "CustomName")
                                    blockProperties.Insert(0, new TextProperty(name, textProp, control, BlockData));
                                else
                                    blockProperties.Add(new TextProperty(name, textProp, control, BlockData));
                            }
                        }
                        if (prop is IMyTerminalControlCombobox)
                        {
                            var comboBox = prop as IMyTerminalControlCombobox;

                            if (comboBox.CanAccessValue(BlockData.TBlock))
                                blockProperties.Add(new ComboBoxProperty(name, comboBox, control, BlockData));
                        }
                        else if (prop is ITerminalProperty<bool>)
                        {
                            var boolProp = prop as ITerminalProperty<bool>;

                            if (boolProp.CanAccessValue(BlockData.TBlock))
                                blockProperties.Add(new BoolProperty(name, boolProp, control, BlockData));
                        }
                        else if (prop is ITerminalProperty<float>)
                        {
                            var floatProp = prop as ITerminalProperty<float>;

                            if (floatProp.CanAccessValue(BlockData.TBlock))
                                blockProperties.Add(new FloatProperty(name, floatProp, control, BlockData));
                        }
                        else if (prop is ITerminalProperty<Color>)
                        {
                            var colorProp = prop as ITerminalProperty<Color>;

                            if (colorProp.CanAccessValue(BlockData.TBlock))
                                blockProperties.AddRange(ColorProperty.GetColorProperties(name, colorProp, control, BlockData));
                        }
                    }
                }
            }

            blockMembers.AddRange(blockProperties);
        }

        /// <summary>
        /// Retrieves a set of custom block actions.
        /// </summary>
        private void GetScrollableActions()
        {
            if (BlockData.SubtypeId.HasFlag(TBlockSubtypes.MechanicalConnection))
            {
                BlockAction.GetMechActions(BlockData, blockMembers);
            }
            else if (BlockData.SubtypeId.HasFlag(TBlockSubtypes.Door))
            {
                BlockAction.GetDoorActions(BlockData, blockMembers);
            }
            else if (BlockData.SubtypeId.HasFlag(TBlockSubtypes.Warhead))
            {
                BlockAction.GetWarheadActions(BlockData, blockMembers);
            }
            else if (BlockData.SubtypeId.HasFlag(TBlockSubtypes.LandingGear))
            {
                BlockAction.GetGearActions(BlockData, blockMembers);
            }
            else if (BlockData.SubtypeId.HasFlag(TBlockSubtypes.Connector))
            {
                BlockAction.GetConnectorActions(BlockData, blockMembers);
            }
            else if (BlockData.SubtypeId.HasFlag(TBlockSubtypes.Parachute))
            {
                BlockAction.GetChuteActions(BlockData, blockMembers);
            }
        }
    }

    public static class TerminalExtensions
    {
        /// <summary>
        /// Checks whether or not the Enabled and Visible delegates are defined and whether
        /// invoking those delegates will throw an exception.
        /// </summary>
        public static bool CanUseControl(this IMyTerminalControl control, IMyTerminalBlock tBlock)
        {
            try
            {
                if (control.Enabled != null && control.Visible != null)
                {
                    control.Enabled(tBlock);
                    control.Visible(tBlock);

                    return true;
                }
            }
            catch { }

            return false;
        }

        /// <summary>
        /// Returns true if it can retrieve the current value without throwing an exception.
        /// </summary>
        public static bool CanAccessValue<TValue>(this ITerminalProperty<TValue> terminalValue, IMyTerminalBlock tBlock)
        {
            try
            {
                terminalValue.GetValue(tBlock);
                return true;
            }
            catch { }

            return false;
        }

        /// <summary>
        /// Returns true if it can retrieve the current value without throwing an exception.
        /// </summary>
        public static bool CanAccessValue<TValue>(this IMyTerminalValueControl<TValue> terminalValue, IMyTerminalBlock tBlock)
        {
            if (terminalValue.Getter != null && terminalValue.Setter != null)
            {
                try
                {
                    terminalValue.Getter(tBlock);
                    return true;
                }
                catch { }
            }

            return false;
        }

        /// <summary>
        /// Returns true if it can retrieve the current value without throwing an exception.
        /// </summary>
        public static bool CanAccessValue(this IMyTerminalControlCombobox comboBox, IMyTerminalBlock tBlock)
        {
            if (CanAccessValue(comboBox as IMyTerminalValueControl<long>, tBlock) && comboBox.ComboBoxContent != null)
            {
                try
                {
                    comboBox.ComboBoxContent(new List<MyTerminalControlComboBoxItem>());
                    return true;
                }
                catch { }
            }

            return false;
        }
    }
}