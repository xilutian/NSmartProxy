﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NSmartProxy.Client;
using NSmartProxy.ClientRouter.Dispatchers;
using NSmartProxy.Shared;
using static NSmartProxy.Infrastructure.I18N;

namespace NSmartProxyWinform
{
    public partial class Login : Form
    {
        private Router clientRouter;
        private ClientMngr parentForm;
        public bool Success = false;

        public const int DEFAULT_WEB_PORT = 12309; //如果没有配置，则使用此默认端口
        public Login(Router router, ClientMngr frm)
        {
            InitializeComponent();
            clientRouter = router;
            parentForm = frm;
            UpdateTexts();
        }

        private void UpdateTexts()
        {
            btnLogin.Text = L("登录");
            cbxIsAnonymous.Text = L("匿名登录");
            tbxPassword.PlaceHolderStr = L("密码");
            tbxUser.PlaceHolderStr = L("用户名");
            Text = L("登录");
        }

        private void cbxIsAnonymous_CheckedChanged(object sender, EventArgs e)
        {
            var obj = (CheckBox)sender;
            //tbxPassword.Enabled = obj.Checked;
            tbxPassword.Enabled = tbxUser.Enabled = !obj.Checked;
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            if (cbxIsAnonymous.Checked)
            {
                ClearLoginCache();
                this.Close();
                return;
            }

            if (tbxPassword.Text == "" || tbxUser.Text == "")
            {
                MessageBox.Show(L("请将用户名和密码输入完整"), L("提示"),
                    MessageBoxButtons.OK
                    , MessageBoxIcon.Warning);
                return;

            }


            string baseEndPoint = null;
            if (clientRouter == null)
            {
                var providerAddr = parentForm.tbxProviderAddr.Text;
                if (string.IsNullOrEmpty(providerAddr))
                {
                    MessageBox.Show(L("请先在主窗体设置“服务器地址”"));
                }

                baseEndPoint = $"{providerAddr}:{DEFAULT_WEB_PORT}";
            }
            else
            {
                var config = clientRouter.ConnectionManager.ClientConfig;
                baseEndPoint = $"{config.ProviderAddress}:{config.ProviderWebPort}";
            }

            btnLogin.Enabled = false;
            NSPDispatcher dispatcher = new NSPDispatcher(baseEndPoint);
            var connectAsync = dispatcher.LoginFromClient(tbxUser.Text, tbxPassword.Text);
            var delayDispose = Task.Delay(TimeSpan.FromSeconds(5000));
            var comletedTask = Task.WhenAny(delayDispose, connectAsync).Result;
            if (!connectAsync.IsCompleted) //超时
            {
                MessageBox.Show(L("连接超时"));
            }
            else if (connectAsync.IsFaulted)//出错
            {
                MessageBox.Show(connectAsync.Exception.ToString());
            }
            else if (connectAsync.Result.State == 0)
            {
                MessageBox.Show(L("登录失败"));
            }
            else
            {
                MessageBox.Show(L("登录成功"));

                CreateLoginCache(connectAsync.Result.Data.Token);
                Success = true;
                this.Close();
            }
            btnLogin.Enabled = true;
        }


        public void ClearLoginCache()
        {
            File.Delete(Router.NspClientCachePath);
        }

        public void CreateLoginCache(string token)
        {
            File.WriteAllText(Router.NspClientCachePath, token);
        }
    }
}
