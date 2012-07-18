using Microsoft.Live;
using System;
using System.Collections.Generic;

namespace SkyDriveHelper
{
    public class LiveLogin
    {
        public delegate void SkyDriveGetSession(LiveConnectSession session);
        public event SkyDriveGetSession SkyDriveSessionChanged;

        private static readonly string[] scopes =
            new string[] { 
            "wl.basic", 
            "wl.signin", 
            "wl.offline_access", 
            "wl.skydrive_update",};

        private LiveAuthClient authClient;


        public LiveLogin(string _clientID)
        {
            this.authClient = new LiveAuthClient(_clientID);
            this.authClient.InitializeCompleted += authClient_InitializeCompleted;
        }

        public LiveLogin(string _clientID, string[] _scopes)
        {
            scopes = _scopes;
            this.authClient = new LiveAuthClient(_clientID);
            this.authClient.InitializeCompleted += authClient_InitializeCompleted;
        }

        public void LaunchInit()
        {
            this.authClient.InitializeAsync(scopes);
        }

        private void authClient_InitializeCompleted(object sender, LoginCompletedEventArgs e)
        {
            if (e.Status == LiveConnectSessionStatus.Connected)
            {
                if (SkyDriveSessionChanged != null) { SkyDriveSessionChanged(e.Session); }
            }
            else
            {
                this.authClient.LoginCompleted += authClient_LoginCompleted;
                this.authClient.LoginAsync(scopes);
            }
        }

        private void authClient_LoginCompleted(object sender, LoginCompletedEventArgs e)
        {
            if (e.Status == LiveConnectSessionStatus.Connected)
            {
                if (SkyDriveSessionChanged != null) { SkyDriveSessionChanged(e.Session); }
            }
            else
            {
                if (SkyDriveSessionChanged != null) { SkyDriveSessionChanged(null); }
            }
        }
    }
}
