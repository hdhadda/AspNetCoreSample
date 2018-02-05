// --------------------------------------------------------------------------------------------------------------------
// <copyright file="GatewayIcon.cs" company="Microsoft">
//   Copyright (c) Microsoft. All rights reserved.
// </copyright>
// <summary>
//   Defines GatewayIcon type.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace Microsoft.ManagementExperience.Desktop
{
    using System;
    using System.Windows.Forms;

    /// <summary>
    /// The GatewayIcon
    /// </summary>
    /// <seealso cref="System.IDisposable" />
    public class GatewayIcon : IDisposable
    {
        /// <summary>
        /// The notify icon
        /// </summary>
        private readonly NotifyIcon notifyIcon = new NotifyIcon();

        /// <summary>
        /// The menu
        /// </summary>
        private readonly ContextMenuStrip menu = new ContextMenuStrip();

        /// <summary>
        /// The disposed state.
        /// </summary>
        private bool disposed = false;

        /// <summary>
        /// Shows this instance.
        /// </summary>
        public void Show()
        {
            this.CreateMenuItems();
            // this.notifyIcon.Icon = Resource.SMT_Systray_white;
            this.notifyIcon.ContextMenuStrip = this.menu;
            this.notifyIcon.Text = "SME";
            this.notifyIcon.Visible = true;
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (this.disposed)
            {
                return;
            }

            if (disposing)
            {
                this.notifyIcon.Dispose();
                this.menu.Dispose();
            }

            this.disposed = true;
        }

        /// <summary>
        /// Exits the specified sender.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private static void Exit(object sender, EventArgs e)
        {
            // FrontEnd.Startup.Stop();
            Application.Exit();
        }

        /// <summary>
        /// Creates the menu items.
        /// </summary>
        private void CreateMenuItems()
        {
            var open = new ToolStripMenuItem { Text = "Resource.Open" };
            open.Click += (_, __) => { System.Diagnostics.Process.Start("http://localhost:5000/api/values/6"); };
            this.menu.Items.Add(open);

            var item = new ToolStripMenuItem { Text = "Resource.Exit" };
            item.Click += Exit;
            this.menu.Items.Add(item);
        }
    }
}
