﻿//
// Author:
//   Larry Ewing <lewing@xamarin.com>
//
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;

using System.Runtime.CompilerServices;
using Xamarin.Interactive.Remote;
using Xamarin.Interactive.TreeModel;

namespace Xamarin.Interactive.Client.ViewInspector
{
    class InspectTreeNode : TreeNode
    {
        public InspectView View =>
            RepresentedObject as InspectView;

        public InspectTreeNode Parent { get; }

        string GetDisplayName (bool showBounds = false)
        {
            if (View == null)
                return String.Empty;

            var view = View;
            var boundsDisplay = String.Empty;

            if (showBounds) {
                boundsDisplay = $"({view.X}, {view.Y}, {view.Width}, {view.Height})";
            }

            if (!String.IsNullOrEmpty (view.DisplayName))
                return view.DisplayName + boundsDisplay;

            var text = view.Type;

            var ofs = text.IndexOf ('.');
            if (ofs > 0) {
                switch (text.Substring (0, ofs)) {
                case "AppKit":
                case "SceneKit":
                case "WebKit":
                case "UIKit":
                    text = text.Substring (ofs + 1);
                    break;
                }
            }

            if (!String.IsNullOrEmpty (view.Description))
                text += $" — “{view.Description}”";

            text += boundsDisplay;
            return text;
        }

        public InspectTreeNode (InspectTreeNode parent, InspectView view)
        {
            Parent = parent;
            RepresentedObject = view ?? throw new ArgumentNullException (nameof (view));
            Name = GetDisplayName ();
            Children = view.Children.Select (c => new InspectTreeNode (this, c)).ToList ();
            IsRenamable = false;
            IsEditing = false;
            IsSelectable = !view.IsFakeRoot;
            ToolTip = view.Description;
            IconName = view.Kind == ViewKind.Primary ? "view" : "layer";
            IsExpanded = true;
        }

        public IInspectTree3DNode<T> Build3D<T> (IInspectTree3DNode<T> node3D, InspectTreeState state)
        {
            node3D.BuildPrimaryPlane (state);
            state.PushGeneration ();
            foreach (var child in GetRenderedChildren (state)) {
                var child3d = node3D.BuildChild (child, state);
                node3D.Add (child3d);
            }
            state.PopGeneration ();
            return node3D;
        }

        IEnumerable<InspectTreeNode> GetRenderedChildren (InspectTreeState state)
        {
            // exploit the fact that Layers can't have Subview or Layer set
            // to walk the children in collapsed layer format
            var layer = View.Layer;
            var children = Children.OfType<InspectTreeNode>();

            foreach (var child in children) {
                if (state.CollapseLayers && layer != null && child.View == layer) {
                    foreach (var sublayer in child.Children.OfType<InspectTreeNode> ())
                        yield return sublayer;

                    yield break;
                }
                else
                    yield return child;
            }
        }
    }
}
