using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace WeifenLuo.WinFormsUI.Docking
{
    public static class DockHelper
    {
        public static bool IsDockStateAutoHide(DockState dockState)
        {
            if (dockState == DockState.DockLeftAutoHide ||
                dockState == DockState.DockRightAutoHide ||
                dockState == DockState.DockTopAutoHide ||
                dockState == DockState.DockBottomAutoHide)
                return true;
            else
                return false;
        }

        public static bool IsDockStateValid(DockState dockState, DockAreas dockableAreas)
        {
            if (((dockableAreas & DockAreas.Float) == 0) &&
                (dockState == DockState.Float))
                return false;
            else if (((dockableAreas & DockAreas.Document) == 0) &&
                (dockState == DockState.Document))
                return false;
            else if (((dockableAreas & DockAreas.DockLeft) == 0) &&
                (dockState == DockState.DockLeft || dockState == DockState.DockLeftAutoHide))
                return false;
            else if (((dockableAreas & DockAreas.DockRight) == 0) &&
                (dockState == DockState.DockRight || dockState == DockState.DockRightAutoHide))
                return false;
            else if (((dockableAreas & DockAreas.DockTop) == 0) &&
                (dockState == DockState.DockTop || dockState == DockState.DockTopAutoHide))
                return false;
            else if (((dockableAreas & DockAreas.DockBottom) == 0) &&
                (dockState == DockState.DockBottom || dockState == DockState.DockBottomAutoHide))
                return false;
            else
                return true;
        }

        public static bool IsDockWindowState(DockState state)
        {
            if (state == DockState.DockTop || state == DockState.DockBottom || state == DockState.DockLeft ||
                state == DockState.DockRight || state == DockState.Document)
                return true;
            else
                return false;
        }

        public static DockState ToggleAutoHideState(DockState state)
        {
            if (state == DockState.DockLeft)
                return DockState.DockLeftAutoHide;
            else if (state == DockState.DockRight)
                return DockState.DockRightAutoHide;
            else if (state == DockState.DockTop)
                return DockState.DockTopAutoHide;
            else if (state == DockState.DockBottom)
                return DockState.DockBottomAutoHide;
            else if (state == DockState.DockLeftAutoHide)
                return DockState.DockLeft;
            else if (state == DockState.DockRightAutoHide)
                return DockState.DockRight;
            else if (state == DockState.DockTopAutoHide)
                return DockState.DockTop;
            else if (state == DockState.DockBottomAutoHide)
                return DockState.DockBottom;
            else
                return state;
        }

		public class CursorPoint {
			public IDockDragSource DragSource;
			public Point Cursor;
			public DockPane Pane;
			public DockPanel DockPanel;
			public FloatWindow FloatWindow;

			public override string ToString() {
				string winText = "null";
				if(FloatWindow != null) {
								winText = FloatWindow.Text;
				} else if ( DockPanel != null) {
					winText = "DockPanel";
				}
				string paneText = "null";
				if(Pane != null) {
								paneText = Pane.CaptionText;
				}
				return "Cursor=[x=" + Cursor.X + "," + Cursor.Y + "],Window=" + winText + ",Pane=" + paneText;
			}
		}

		private static bool ControlContains( Control control, Point point ) {
			if ( control.Parent != null ) {
				return control.Bounds.Contains( control.Parent.PointToClient( point ) );
			}
			return control.Bounds.Contains( point );
		}

		private static bool SearchChilds( Control window, CursorPoint info, DockPanel dockPanel ) {

			// enumerate child controls at the cursor point
			List<Control> childs = new List<Control>();
			const uint CWP_SKIPDISABLED = 0x0002;
			const uint CWP_SKIPINVISIBLE = 0x0001;
			const uint CWP_SKIPTRANSPARENT = 0x0004;
			uint flags = CWP_SKIPDISABLED | CWP_SKIPINVISIBLE | CWP_SKIPTRANSPARENT;

			Control current = window;
			while ( true ) {
				childs.Add( current );
				IntPtr hwnd = NativeMethods.ChildWindowFromPointEx( 
					current.Handle, current.PointToClient( info.Cursor ), flags );
				if ( hwnd == current.Handle || hwnd == IntPtr.Zero ) {
					break;
				}
				current = Control.FromHandle( hwnd );
				if ( current == null ) {
					break;
				}
			}
			
			// make the array deepest first
			childs.Reverse();

			bool targetFound = false;
			foreach ( Control control in childs ) {
				if ( info.Pane == null ) { // not found?
					IDockContent content = control as IDockContent;
					if ( content != null && content.DockHandler.DockPanel == dockPanel ) {
						info.Pane = content.DockHandler.Pane;
						targetFound = true;
					}

					DockPane pane = control as DockPane;
					if ( pane != null && pane.DockPanel == dockPanel ) {
						info.Pane = pane;
						targetFound = true;
					}
				}

				if ( info.FloatWindow == null && info.DockPanel == null ) { // not found?
					FloatWindow floatWindow = window as FloatWindow;
					if ( floatWindow != null && floatWindow.DockPanel == dockPanel ) {
						info.FloatWindow = floatWindow;
						targetFound = true;
					}

					DockPanel panel = control as DockPanel;
					if ( panel != null && panel == dockPanel ) {
						info.DockPanel = panel;
						targetFound = true;
					}
				}
			}

			return targetFound;
		}

		public static CursorPoint CursorPointInformation( DockPanel dockPanel, IDockDragSource dragSource ) {
			FloatWindow sourceFloatWindow = dragSource as FloatWindow;
			CursorPoint info = new CursorPoint();
			info.DragSource = dragSource;
			info.Cursor = Control.MousePosition;

			// find the window beneath the cursor
			int currentProcessId = System.Diagnostics.Process.GetCurrentProcess().Id;
			NativeMethods.EnumWindows( ( hWnd, arg ) => {
				uint processId;
				NativeMethods.GetWindowThreadProcessId( hWnd, out processId );
				if ( processId == currentProcessId ) {
					if ( sourceFloatWindow != null && sourceFloatWindow.Handle == hWnd ) {
						// ignore the source floating window
						return true;
					}
					Control window = Control.FromHandle( hWnd );
					if ( window != null && window.Visible && ControlContains(window, info.Cursor)) {
						// look into child controls
						if ( SearchChilds( window, info, dockPanel ) ) {
							return false;
						}
					}
				}
				return true;
			}, IntPtr.Zero );

			return info;
		}
		/*
        public static DockPane PaneAtPoint(Point pt, DockPanel dockPanel)
        {
            if (!Win32Helper.IsRunningOnMono)
                for (Control control = Win32Helper.ControlAtPoint(pt); control != null; control = control.Parent)
                {
                    IDockContent content = control as IDockContent;
                    if (content != null && content.DockHandler.DockPanel == dockPanel)
                        return content.DockHandler.Pane;

                    DockPane pane = control as DockPane;
                    if (pane != null && pane.DockPanel == dockPanel)
                        return pane;
                }

            return null;
        }

        public static FloatWindow FloatWindowAtPoint(Point pt, DockPanel dockPanel)
        {
            if (!Win32Helper.IsRunningOnMono)
                for (Control control = Win32Helper.ControlAtPoint(pt); control != null; control = control.Parent)
                {
                    FloatWindow floatWindow = control as FloatWindow;
                    if (floatWindow != null && floatWindow.DockPanel == dockPanel)
                        return floatWindow;
                }

            return null;
        }
		*/
    }
}
