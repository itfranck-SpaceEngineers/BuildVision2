﻿using System;
using System.Collections.Generic;
using VRage;
using BindDefinitionData = VRage.MyTuple<string, string[]>;
using BindMembers = VRage.MyTuple<
    string, // Name
    System.Func<bool>, // Analog
    System.Func<bool>, // IsPressed
    System.Func<bool>, // IsPressedAndHeld
    System.Func<bool>, // IsNewPressed
    VRage.MyTuple<
        System.Func<bool>, // IsReleased
        VRage.MyTuple<System.Action<System.Action>, System.Action<System.Action>>, // OnNewPress
        VRage.MyTuple<System.Action<System.Action>, System.Action<System.Action>>, // OnPressAndHold
        VRage.MyTuple<System.Action<System.Action>, System.Action<System.Action>>, // OnRelease
        System.Func<System.Collections.Generic.List<int>>, // GetCombo
        VRage.MyTuple<
            System.Func<System.Collections.Generic.IList<int>, bool, bool>, // SetCombo
            System.Action, // ClearCombo
            System.Action, // ClearSubscribers
            int // Index
        >
    >
>;
using ControlMembers = VRage.MyTuple<string, int, System.Func<bool>, bool>;

namespace DarkHelmet
{
    using BindGroupMembers = MyTuple<
        string, // Name                   
        BindMembers[],// Binds
        Func<IList<int>, int, bool>, // DoesComboConflict
        Func<string, int[], bool, BindMembers?>, // TryRegisterBind
        Func<IList<BindDefinitionData>, BindMembers[]>, // TryLoadBindData
        MyTuple<
            Func<string, string[], bool, BindMembers?>, // TryRegisterBind2
            Func<BindDefinitionData[]>, // GetBindData
            Action, // HandleInput
            Action // ClearSubscribers
        >
    >;

    namespace UI
    {
        public interface IBindGroup : IIndexedCollection<IBind>
        {
            string Name { get; }

            void HandleInput();
            bool DoesBindExist(string name);
            bool DoesComboConflict(IList<IControl> newCombo, IBind exception = null);
            bool TryLoadBindData(IList<BindDefinition> bindData);
            void RegisterBinds(IList<string> bindNames);
            void RegisterBinds(IList<BindDefinition> bindData);
            IBind GetBind(string name);
            bool TryRegisterBind(string bindName, out IBind bind, string[] combo = null, bool silent = false);
            bool TryRegisterBind(string bindName, IControl[] combo, out IBind newBind, bool silent = false);
            BindDefinition[] GetBindDefinitions();
            void ClearSubscribers();
            BindGroupMembers GetApiData();
        }

        public interface IBind
        {
            string Name { get; }
            int Index { get; }

            /// <summary>
            /// True if any controls in the bind are marked analog. For these types of binds, IsPressed == IsNewPressed.
            /// </summary>
            bool Analog { get; }

            /// <summary>
            /// True if just pressed.
            /// </summary>
            bool IsNewPressed { get; }

            /// <summary>
            /// True if currently pressed.
            /// </summary>
            bool IsPressed { get; }

            /// <summary>
            /// True on new press and after being held for more than 500ms.
            /// </summary>
            bool IsPressedAndHeld { get; }

            /// <summary>
            /// True if just released.
            /// </summary>
            bool IsReleased { get; }

            /// <summary>
            /// Events triggered whenever their corresponding booleans are true.
            /// </summary>
            event Action OnNewPress, OnPressAndHold, OnRelease;

            /// <summary>
            /// Returns a list of the current key combo for this bind.
            /// </summary>
            /// <returns></returns>
            IList<IControl> GetCombo();

            /// <summary>
            /// Attempts to set the binds combo to the given controls. Returns true if successful.
            /// </summary>
            bool TrySetCombo(IControl[] combo, bool silent = false);

            /// <summary>
            /// Attempts to set the binds combo to the given controls. Returns true if successful.
            /// </summary>
            bool TrySetCombo(IList<string> combo, bool silent = false);

            /// <summary>
            /// Clears the current key combination.
            /// </summary>
            void ClearCombo();

            /// <summary>
            /// Clears all event subscibers for this bind.
            /// </summary>
            void ClearSubscribers();
            BindMembers GetApiData();
        }

        /// <summary>
        /// Interface for anything used as a control
        /// </summary> 
        public interface IControl
        {
            string Name { get; }
            int Index { get; }
            bool IsPressed { get; }
            bool Analog { get; }
            ControlMembers GetApiData();
        }
    }
}