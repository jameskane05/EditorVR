﻿#if UNITY_EDITOR && UNITY_2017_2_OR_NEWER
using UnityEditor.Experimental.EditorVR.Modules;

namespace UnityEditor.Experimental.EditorVR.Core
{
    partial class EditorVR
    {
        class SelectionModuleConnector : Nested, IInterfaceConnector, ILateBindInterfaceMethods<SelectionModule>
        {
            public void LateBindInterfaceMethods(SelectionModule provider)
            {
                ISelectObjectMethods.getSelectionCandidate = provider.GetSelectionCandidate;
                ISelectObjectMethods.selectObject = provider.SelectObject;
            }

            public void ConnectInterface(object target, object userData = null)
            {
                var selectionChanged = target as ISelectionChanged;
                if (selectionChanged != null)
                    evr.selectionChanged += selectionChanged.OnSelectionChanged;
            }

            public void DisconnectInterface(object target, object userData = null)
            {
                var selectionChanged = target as ISelectionChanged;
                if (selectionChanged != null)
                    evr.selectionChanged -= selectionChanged.OnSelectionChanged;
            }
        }
    }
}
#endif
